using UnityEngine;

/// <summary>
/// 鱼类角色基类 - 所有可操作角色的基础
/// </summary>
public abstract class BaseFish : MonoBehaviour, IBeatListener
{
    [Header("Base Fish Settings")]
    [SerializeField] protected int maxHealth = 3;
    [SerializeField] protected int currentHealth;
    [SerializeField] protected bool isInvincible = false;
    [SerializeField] protected bool isPlayer = true; // 是否为玩家控制
    
    [Header("Components")]
    protected IMovementController movementController;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
    
    // 无敌帧相关
    private int invincibleBeatCount = 0;
    private int invincibleDuration = 1; // 一拍的无敌时间
    
    protected virtual void Awake()
    {
        // 获取组件
        movementController = GetComponent<IMovementController>();
        if (movementController == null)
        {
            movementController = gameObject.AddComponent<InterpolationMovement>();
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // 初始化血量
        currentHealth = maxHealth;
    }
    
    protected virtual void Start()
    {
        // 注册到节拍系统
        if (Conductor.Instance != null)
        {
            Conductor.Instance.RegisterListener(this);
        }
    }
    
    protected virtual void OnDestroy()
    {
        // 取消注册
        if (Conductor.Instance != null)
        {
            Conductor.Instance.UnregisterListener(this);
        }
    }
    
    protected virtual void Update()
    {
        // 处理自由移动（只有玩家且允许自由移动的角色）
        if (isPlayer && movementController.CanFreeMove)
        {
            HandleFreeMovement();
        }
    }
    
    #region 节拍系统
    
    public virtual void OnBeat(int beatCount)
    {
        // 处理无敌帧倒计时
        if (isInvincible)
        {
            invincibleBeatCount--;
            if (invincibleBeatCount <= 0)
            {
                SetInvincible(false);
            }
        }
        
        // 子类可以重写此方法添加额外的节拍逻辑
        OnBeatCustom(beatCount);
    }
    
    /// <summary>
    /// 子类重写的节拍逻辑
    /// </summary>
    protected virtual void OnBeatCustom(int beatCount) { }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理节拍输入 - 每个角色实现不同的逻辑
    /// </summary>
    public abstract void OnRhythmInput();
    
    #endregion
    
    #region 移动处理
    
    /// <summary>
    /// 处理自由移动
    /// </summary>
    protected virtual void HandleFreeMovement()
    {
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.HandleFreeMovement();
        }
    }
    
    #endregion
    
    #region 战斗系统
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public virtual void TakeDamage(int damage = 1)
    {
        if (isInvincible) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // 触发无敌帧
        SetInvincible(true);
        
        // 视觉反馈
        OnTakeDamage();
        
        // 检查死亡
        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }
    
    /// <summary>
    /// 恢复血量（击杀敌人时）
    /// </summary>
    public virtual void RestoreHealth(int amount = 1)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }
    
    /// <summary>
    /// 设置无敌状态
    /// </summary>
    protected void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        if (invincible)
        {
            invincibleBeatCount = invincibleDuration;
        }
        
        // 视觉反馈
        UpdateInvincibleVisual();
    }
    
    /// <summary>
    /// 受伤时的视觉反馈
    /// </summary>
    protected virtual void OnTakeDamage()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }
    
    /// <summary>
    /// 更新无敌状态的视觉效果
    /// </summary>
    protected virtual void UpdateInvincibleVisual()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = isInvincible ? 0.5f : 1f;
            spriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// 死亡处理
    /// </summary>
    protected virtual void OnDeath()
    {
        if (isPlayer)
        {
            // 玩家死亡 - 重新开始游戏
            GameManager.Instance?.RestartGame();
        }
        else
        {
            // 敌人死亡 - 通知游戏管理器
            GameManager.Instance?.OnEnemyKilled(this);
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region 公共属性和方法
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsPlayer => isPlayer;
    
    /// <summary>
    /// 设置为玩家控制
    /// </summary>
    public void SetAsPlayer(bool isPlayerControlled)
    {
        isPlayer = isPlayerControlled;
    }
    
    /// <summary>
    /// 检查是否被惩罚（通过PlayerController）
    /// </summary>
    protected bool IsPlayerPunished()
    {
        return isPlayer && PlayerController.Instance != null && PlayerController.Instance.IsPunished;
    }
    
    #endregion
}