using UnityEngine;

/// <summary>
/// 玩家控制器 - 管理当前选择的角色并处理输入
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private BaseFish currentFish;
    [SerializeField] private KeyCode actionKey = KeyCode.Space;
    [SerializeField] private KeyCode secondaryKey = KeyCode.LeftShift; // 用于某些角色的特殊操作
    
    [Header("Mouse Cursor")]
    [SerializeField] private MouseCursor mouseCursor; // 世界空间光标
    [SerializeField] private UICursor uiCursor; // UI空间光标
    
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
        // 初始化鼠标光标
        InitializeMouseCursor();
    }
    
    void Update()
    {
        HandleInput();
    }
    
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
        else if (mouseCursor == null)
        {
            // 如果没有UI光标，创建世界空间光标
            GameObject cursorObj = new GameObject("MouseCursor");
            mouseCursor = cursorObj.AddComponent<MouseCursor>();
            
            // 设置玩家引用
            if (currentFish != null)
            {
                mouseCursor.SetPlayerTransform(currentFish.transform);
            }
        }
    }
    
    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandleInput()
    {
        if (currentFish == null) return;
        
        // 主要动作输入（攻击/加速等）
        if (Input.GetKeyDown(actionKey))
        {
            currentFish.OnRhythmInput();
        }
        
        // 处理特殊输入（如刺豚的缩小、剑鱼的格挡等）
        HandleSpecialInput();
    }
    
    /// <summary>
    /// 处理特殊输入 - 根据角色类型
    /// </summary>
    private void HandleSpecialInput()
    {
        if (currentFish == null) return;
        
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
            else if (mouseCursor != null)
            {
                mouseCursor.SetPlayerTransform(currentFish.transform);
            }
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
        GameObject fishPrefab = CharacterManager.Instance.GetFishPrefab(fishType);
        if (fishPrefab != null)
        {
            GameObject fishInstance = Instantiate(fishPrefab, transform.position, Quaternion.identity);
            SetCurrentFish(fishInstance.GetComponent<BaseFish>());
        }
    }
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