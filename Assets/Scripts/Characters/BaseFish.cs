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
    
    [Header("Movement Settings")]
    [SerializeField] protected bool canFreeMove = true; // 是否允许自由移动（debug开关）
    [SerializeField] protected float freeMovementSpeed = 5f; // 自由移动速度
    [SerializeField] protected float rotationSpeed = 15f; // 朝向旋转速度
    [SerializeField] protected float screenMargin = 1f; // 屏幕边距
    
    [Header("Components")]
    protected IMovementController movementController;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
    
    // 无敌帧相关
    private int invincibleBeatCount = 0;
    private int invincibleDuration = 1; // 一拍的无敌时间
    
    // 朝向相关
    protected bool isFacingRight = true;
    
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
        if (!isPlayer) return;
        
        // 始终更新朝向（跟手）
        UpdateFacingToMouse();
        
        // 处理自由移动
        if (canFreeMove && !movementController.IsMoving)
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
    
    #region 移动和朝向系统
    
    /// <summary>
    /// 处理自由移动 - 鼠标指引移动
    /// </summary>
    protected virtual void HandleFreeMovement()
    {
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // 计算鱼到鼠标的距离
        float distanceToMouse = Vector3.Distance(transform.position, mouseWorldPos);
        
        // 设置一个最小距离阈值，避免鱼在鼠标附近抖动
        float minDistance = 0.5f;
        
        if (distanceToMouse > minDistance)
        {
            // 计算移动方向 - 始终朝向鼠标方向移动
            Vector3 moveDirection = GetMouseDirection();
            
            // 计算移动距离
            float moveDistance = freeMovementSpeed * Time.deltaTime;
            
            // 限制移动距离，避免超过鼠标位置
            moveDistance = Mathf.Min(moveDistance, distanceToMouse - minDistance);
            
            // 执行移动
            Vector3 newPosition = transform.position + moveDirection * moveDistance;
            
            // 限制在屏幕边界内
            newPosition = ClampToScreenBounds(newPosition);
            
            transform.position = newPosition;
        }
    }
    
    /// <summary>
    /// 更新朝向 - 头部始终指向鼠标方向
    /// </summary>
    protected virtual void UpdateFacingToMouse()
    {
        Vector3 mouseDirection = GetMouseDirection();
        
        if (mouseDirection.magnitude < 0.1f) return;
        
        // 检查是否需要翻转（越过Y轴）
        bool shouldFaceRight = mouseDirection.x >= 0;
        
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            
            // 翻转精灵
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !isFacingRight;
            }
        }
        
        // 计算目标角度（-90到90度范围）
        // 使用绝对值的x来计算角度，保证上下方向正确
        float targetAngle = Mathf.Atan2(mouseDirection.y, Mathf.Abs(mouseDirection.x)) * Mathf.Rad2Deg;
        
        // 限制角度在-90到90度之间
        targetAngle = Mathf.Clamp(targetAngle, -90f, 90f);
        
        // 当朝左时，flipX翻转了精灵，但旋转中心不变
        // 所以需要反转角度，让视觉上的头部指向正确方向
        if (!isFacingRight)
        {
            targetAngle = -targetAngle;
        }
        
        // 平滑旋转到目标角度
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
    
    /// <summary>
    /// 获取鼠标方向（从角色指向鼠标的单位向量）
    /// </summary>
    protected Vector3 GetMouseDirection()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 direction = (mouseWorldPos - transform.position).normalized;
        return direction;
    }
    
    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    protected Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;
        
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z * -1;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
    
    /// <summary>
    /// 限制位置在屏幕边界内
    /// </summary>
    protected Vector3 ClampToScreenBounds(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 screenBounds = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.transform.position.z));
            
            position.x = Mathf.Clamp(position.x, -Mathf.Abs(screenBounds.x) + screenMargin, Mathf.Abs(screenBounds.x) - screenMargin);
            position.y = Mathf.Clamp(position.y, -Mathf.Abs(screenBounds.y) + screenMargin, Mathf.Abs(screenBounds.y) - screenMargin);
        }
        
        return position;
    }
    
    /// <summary>
    /// 设置是否允许自由移动
    /// </summary>
    public void SetCanFreeMove(bool canMove)
    {
        canFreeMove = canMove;
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
    public bool CanFreeMove => canFreeMove;
    public bool IsFacingRight => isFacingRight;
    
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