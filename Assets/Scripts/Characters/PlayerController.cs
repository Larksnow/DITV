using UnityEngine;
using System.Collections;

/// <summary>
/// 玩家控制器 - 管理当前选择的角色并处理节拍输入
/// </summary>
public class PlayerController : MonoBehaviour, IBeatListener
{
    [Header("Player Settings")]
    [SerializeField] private BaseFish currentFish;
    [SerializeField] private KeyCode actionKey = KeyCode.Space;
    [SerializeField] private KeyCode secondaryKey = KeyCode.LeftShift; // 用于某些角色的特殊操作
    
    [Header("Mouse Cursor")]
    [SerializeField] private UICursor uiCursor; // UI空间光标
    
    [Header("Rhythm Feedback")]
    [SerializeField] private SpriteRenderer visualFeedback; // 节拍反馈
    [SerializeField] private bool showVisualFeedback = true;
    
    [Header("Rhythm State")]
    [SerializeField] private bool isPunished = false; // 是否处于惩罚状态
    private int punishedBeat = -1; // 被惩罚的拍子编号
    
    private static PlayerController instance;
    public static PlayerController Instance => instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 注册到节拍系统
        if (Conductor.Instance != null)
        {
            Conductor.Instance.RegisterListener(this);
        }
        
        // 初始化鼠标光标
        InitializeMouseCursor();
        
        // 初始化视觉反馈
        InitializeVisualFeedback();
    }
    
    void OnDestroy()
    {
        // 取消注册
        if (Conductor.Instance != null)
        {
            Conductor.Instance.UnregisterListener(this);
        }
    }
    
    void Update()
    {
        // 检查惩罚状态
        CheckPunishmentRelease();
        
        // 处理输入
        HandleInput();
        
        // 更新视觉反馈
        UpdateVisualFeedback();
    }
    
    #region 节拍系统
    
    public void OnBeat(int beatCount)
    {
        // 节拍视觉反馈
        if (showVisualFeedback)
        {
            StartCoroutine(BeatPulse());
        }
        
        // 通知当前角色节拍事件
        if (currentFish != null)
        {
            // 这里不直接调用currentFish.OnBeat，因为BaseFish已经自己注册了
            // 如果需要额外的玩家特定的节拍逻辑，可以在这里添加
        }
    }
    
    /// <summary>
    /// 节拍脉冲效果
    /// </summary>
    private IEnumerator BeatPulse()
    {
        if (currentFish != null)
        {
            Vector3 originalScale = currentFish.transform.localScale;
            currentFish.transform.localScale = originalScale * 1.1f;
            
            yield return new WaitForSeconds(0.1f);
            
            if (currentFish != null)
            {
                currentFish.transform.localScale = originalScale;
            }
        }
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandleInput()
    {
        if (currentFish == null) return;
        
        // 主要动作输入（攻击/加速等）
        if (Input.GetKeyDown(actionKey))
        {
            HandleRhythmAction();
        }
        
        // 处理特殊输入（如刺豚的缩小、剑鱼的格挡等）
        HandleSpecialInput();
    }
    
    /// <summary>
    /// 处理节拍动作
    /// </summary>
    private void HandleRhythmAction()
    {
        // 如果处于惩罚状态，禁止输入
        if (isPunished)
        {
            Debug.Log("Input blocked - currently punished");
            return;
        }
        
        // 检查节拍时机
        if (Conductor.Instance != null && Conductor.Instance.CheckInputTiming())
        {
            // 节拍正确，执行角色动作
            HandleRhythmSuccess();
            currentFish.OnRhythmInput();
        }
        else
        {
            // 节拍错误，应用惩罚
            HandleRhythmFailure();
        }
    }
    
    /// <summary>
    /// 节拍成功处理
    /// </summary>
    private void HandleRhythmSuccess()
    {
        Debug.Log("PERFECT! Rhythm action executed.");
        
        // 视觉反馈
        if (visualFeedback != null)
        {
            StartCoroutine(FlashColor(Color.green, 0.2f));
        }
    }
    
    /// <summary>
    /// 节拍失败处理
    /// </summary>
    private void HandleRhythmFailure()
    {
        Debug.Log("MISS! Rhythm punishment applied.");
        
        // 应用惩罚
        isPunished = true;
        punishedBeat = Mathf.RoundToInt(Conductor.Instance.songPositionInBeats);
        
        // 通知连击系统
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.OnMissedBeat();
        }
        
        // 视觉反馈
        if (visualFeedback != null)
        {
            StartCoroutine(FlashColor(Color.red, 0.5f));
        }
        
        Debug.Log($"Punished on beat {punishedBeat}, will be released when entering beat {punishedBeat + 1}'s early window");
    }
    
    /// <summary>
    /// 检查是否应该解除惩罚
    /// </summary>
    private void CheckPunishmentRelease()
    {
        if (!isPunished || Conductor.Instance == null) return;
        
        // 计算下一拍的开始时间（包括Early Window）
        int nextBeat = punishedBeat + 1;
        float nextBeatTime = nextBeat;
        float earlyWindowStart = nextBeatTime - (Conductor.Instance.inputThreshold / (60f / Conductor.Instance.bpm));
        
        // 如果当前时间已经进入下一拍的Early Window，解除惩罚
        if (Conductor.Instance.songPositionInBeats >= earlyWindowStart)
        {
            isPunished = false;
            punishedBeat = -1;
            Debug.Log($"Punishment Released - Entered next beat's early window at beat {Conductor.Instance.songPositionInBeats:F2}");
        }
    }
    
    /// <summary>
    /// 处理特殊输入 - 根据角色类型
    /// </summary>
    private void HandleSpecialInput()
    {
        if (currentFish == null || isPunished) return;
        
        // 根据角色类型处理不同的特殊输入
        switch (currentFish)
        {
            case Pufferfish pufferfish:
                // 刺豚的缩小操作
                if (Input.GetKeyDown(secondaryKey))
                {
                    pufferfish.TryDeflate();
                }
                break;
                
            case Swordfish swordfish:
                // 剑鱼的格挡操作
                if (Input.GetKeyDown(secondaryKey))
                {
                    swordfish.TryParry();
                }
                break;
                
            case Tuna tuna:
                // 金枪鱼的蓄力释放
                if (Input.GetKeyUp(actionKey))
                {
                    tuna.ReleaseCharge();
                }
                break;
        }
    }
    
    #endregion
    
    #region 视觉反馈
    
    /// <summary>
    /// 初始化视觉反馈
    /// </summary>
    private void InitializeVisualFeedback()
    {
        if (visualFeedback == null && currentFish != null)
        {
            visualFeedback = currentFish.GetComponent<SpriteRenderer>();
        }
    }
    
    /// <summary>
    /// 更新视觉反馈
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (!showVisualFeedback || visualFeedback == null) return;
        
        // 根据状态更新颜色
        if (isPunished)
        {
            // 惩罚状态显示红色
            if (visualFeedback.color != Color.red)
            {
                visualFeedback.color = Color.red;
            }
        }
        else if (visualFeedback.color == Color.red)
        {
            // 恢复正常颜色
            visualFeedback.color = Color.white;
        }
    }
    
    /// <summary>
    /// 颜色闪烁效果
    /// </summary>
    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (visualFeedback == null) yield break;
        
        Color originalColor = visualFeedback.color;
        visualFeedback.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        if (visualFeedback != null && !isPunished)
        {
            visualFeedback.color = originalColor;
        }
    }
    
    #endregion
    
    #region 鼠标光标
    
    /// <summary>
    /// 初始化鼠标光标系统
    /// </summary>
    private void InitializeMouseCursor()
    {
        // 优先使用UI光标
        if (uiCursor != null)
        {
            // 设置玩家引用
            if (currentFish != null)
            {
                uiCursor.SetPlayerTransform(currentFish.transform);
            }
        }
    }
    
    #endregion
    
    #region 角色管理
    
    /// <summary>
    /// 设置当前控制的角色
    /// </summary>
    public void SetCurrentFish(BaseFish fish)
    {
        if (currentFish != null)
        {
            // 取消之前角色的玩家标记
            currentFish.SetAsPlayer(false);
        }
        
        currentFish = fish;
        
        if (currentFish != null)
        {
            // 设置新角色为玩家控制
            currentFish.SetAsPlayer(true);
            
            // 更新鼠标光标的玩家引用
            if (uiCursor != null)
            {
                uiCursor.SetPlayerTransform(currentFish.transform);
            }
            
            // 更新视觉反馈引用
            InitializeVisualFeedback();
        }
    }
    
    /// <summary>
    /// 获取当前角色
    /// </summary>
    public BaseFish GetCurrentFish()
    {
        return currentFish;
    }
    
    /// <summary>
    /// 角色选择 - 在游戏开始时调用
    /// </summary>
    public void SelectCharacter(FishType fishType)
    {
        // 销毁当前角色
        if (currentFish != null)
        {
            Destroy(currentFish.gameObject);
        }
        
        // 创建新角色
        if (CharacterManager.Instance != null)
        {
            BaseFish newFish = CharacterManager.Instance.CreateFish(fishType, transform.position, true);
            SetCurrentFish(newFish);
        }
    }
    
    #endregion
    
    #region 公共属性
    
    public bool IsPunished => isPunished;
    public bool HasCurrentFish => currentFish != null;
    
    #endregion
}

/// <summary>
/// 角色类型枚举
/// </summary>
public enum FishType
{
    Piranha,    // 食人鱼
    Pufferfish, // 刺豚
    Tuna,       // 金枪鱼
    Swordfish   // 剑鱼
}