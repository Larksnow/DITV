using UnityEngine;

/// <summary>
/// 剑鱼 - 高速移动和精准反击
/// </summary>
public class Swordfish : BaseFish
{
    [Header("Swordfish Settings")]
    [SerializeField] private int maxSpeed = 4;
    [SerializeField] private int currentSpeed = 0;
    [SerializeField] private float speedIncrement = 1f;
    [SerializeField] private float noseDamage = 2f;
    [SerializeField] private float counterDamage = 4f;
    [SerializeField] private float moveDistance = 2f;
    
    [Header("Parry System")]
    [SerializeField] private bool hasParryOpportunity = false;
    [SerializeField] private int beatsSinceLastParry = 0;
    [SerializeField] private int parryInterval = 2; // 每两拍一次格挡机会
    [SerializeField] private bool isParrying = false;
    [SerializeField] private float parryWindow = 0.2f;
    
    [Header("Movement")]
    [SerializeField] private Vector3 currentDirection = Vector3.right;
    [SerializeField] private bool mustKeepMoving = true;
    
    [Header("Visual")]
    [SerializeField] private Transform nosePoint;
    [SerializeField] private LayerMask enemyLayer = -1;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 剑鱼不能自由移动，必须通过加速系统移动
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.SetCanFreeMove(false);
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 剑鱼必须持续移动
        if (mustKeepMoving && currentSpeed > 0 && !movementController.IsMoving)
        {
            ContinueMovement();
        }
        
        // 检测鼻部碰撞（最高速时）
        if (currentSpeed >= maxSpeed)
        {
            CheckNoseCollision();
        }
    }
    
    public override void OnRhythmInput()
    {
        // 剑鱼的节拍输入由PlayerController处理时机，这里直接执行动作
        // if (isDashing) return;
        
        // 加速
        AccelerateOnBeat();
    }
    
    /// <summary>
    /// 在节拍上加速
    /// </summary>
    private void AccelerateOnBeat()
    {
        if (currentSpeed < maxSpeed)
        {
            currentSpeed++;
            Debug.Log($"Swordfish speed: {currentSpeed}/{maxSpeed}");
            
            // 更新移动方向
            UpdateMovementDirection();
            
            // 执行移动
            PerformMovement();
            
            // 播放动画
            if (animator != null)
            {
                animator.SetInteger("Speed", currentSpeed);
            }
        }
        else
        {
            // 已达最高速，继续移动
            PerformMovement();
        }
    }
    
    /// <summary>
    /// 尝试格挡
    /// </summary>
    public void TryParry()
    {
        if (!hasParryOpportunity || isParrying) return;
        
        // 检查节拍时机
        if (Conductor.Instance.CheckInputTiming())
        {
            StartParry();
        }
        else
        {
            // 错拍会由PlayerController处理，这里不需要额外处理
            Debug.Log("Parry timing missed - handled by PlayerController");
        }
    }
    
    /// <summary>
    /// 开始格挡
    /// </summary>
    private void StartParry()
    {
        isParrying = true;
        hasParryOpportunity = false;
        beatsSinceLastParry = 0;
        
        Debug.Log("Swordfish parry activated");
        
        // 播放格挡动画
        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }
        
        // 启动格挡窗口
        Invoke(nameof(EndParry), parryWindow);
    }
    
    /// <summary>
    /// 结束格挡
    /// </summary>
    private void EndParry()
    {
        isParrying = false;
    }
    
    /// <summary>
    /// 格挡成功，执行反击
    /// </summary>
    public void OnParrySuccess(BaseFish attacker)
    {
        if (!isParrying) return;
        
        CancelInvoke(nameof(EndParry));
        isParrying = false;
        
        // 刷新格挡机会
        hasParryOpportunity = true;
        beatsSinceLastParry = 0;
        
        // 执行反击
        PerformCounterAttack(attacker);
        
        Debug.Log("Parry successful! Counter attack!");
    }
    
    /// <summary>
    /// 执行反击
    /// </summary>
    private void PerformCounterAttack(BaseFish target)
    {
        if (target == null) return;
        
        // 朝向目标
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        currentDirection = directionToTarget;
        
        // 强力冲刺反击
        Vector3 targetPosition = target.transform.position;
        movementController.SetTargetPosition(targetPosition, 0.3f);
        
        // 造成反击伤害
        target.TakeDamage(Mathf.RoundToInt(counterDamage));
        
        if (target.CurrentHealth <= 0)
        {
            RestoreHealth(1);
            ComboSystem.Instance?.OnEnemyKilled();
        }
        
        // 播放反击动画
        if (animator != null)
        {
            animator.SetTrigger("CounterAttack");
        }
    }
    
    /// <summary>
    /// 更新移动方向 - 优先使用鼠标方向
    /// </summary>
    private void UpdateMovementDirection()
    {
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 directionToMouse = (mouseWorldPos - transform.position).normalized;
        
        // 如果鼠标距离足够远，使用鼠标方向
        float distanceToMouse = Vector3.Distance(transform.position, mouseWorldPos);
        if (distanceToMouse > 1f)
        {
            currentDirection = directionToMouse;
        }
        else
        {
            // 如果鼠标太近，朝向最近的敌人
            BaseFish nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                currentDirection = (nearestEnemy.transform.position - transform.position).normalized;
            }
        }
    }
    
    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z * -1;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
    
    /// <summary>
    /// 执行移动
    /// </summary>
    private void PerformMovement()
    {
        Vector3 targetPosition = transform.position + currentDirection * moveDistance * (currentSpeed / (float)maxSpeed);
        targetPosition = ClampToScreenBounds(targetPosition);
        
        float moveDuration = 60f / Conductor.Instance.bpm; // 一拍的时间
        movementController.SetTargetPosition(targetPosition, moveDuration);
    }
    
    /// <summary>
    /// 持续移动
    /// </summary>
    private void ContinueMovement()
    {
        PerformMovement();
    }
    
    /// <summary>
    /// 检测鼻部碰撞
    /// </summary>
    private void CheckNoseCollision()
    {
        if (nosePoint == null) return;
        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(nosePoint.position, 0.5f, enemyLayer);
        
        foreach (Collider2D collider in hitColliders)
        {
            BaseFish targetFish = collider.GetComponent<BaseFish>();
            if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
            {
                targetFish.TakeDamage(Mathf.RoundToInt(noseDamage));
                
                if (targetFish.CurrentHealth <= 0)
                {
                    RestoreHealth(1);
                    ComboSystem.Instance?.OnEnemyKilled();
                }
            }
        }
    }
    
    /// <summary>
    /// 找到最近的敌人
    /// </summary>
    private BaseFish FindNearestEnemy()
    {
        BaseFish[] allFish = FindObjectsOfType<BaseFish>();
        BaseFish nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (BaseFish fish in allFish)
        {
            if (!fish.IsPlayer && fish != this)
            {
                float distance = Vector3.Distance(transform.position, fish.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = fish;
                }
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// 限制位置在屏幕边界内
    /// </summary>
    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 screenBounds = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.transform.position.z));
            position.x = Mathf.Clamp(position.x, -screenBounds.x + 1f, screenBounds.x - 1f);
            position.y = Mathf.Clamp(position.y, -screenBounds.y + 1f, screenBounds.y - 1f);
        }
        return position;
    }
    
    protected override void OnBeatCustom(int beatCount)
    {
        // 处理格挡机会
        if (currentSpeed >= maxSpeed)
        {
            beatsSinceLastParry++;
            
            if (beatsSinceLastParry >= parryInterval)
            {
                hasParryOpportunity = true;
                Debug.Log("Swordfish parry opportunity available");
            }
        }
        
        // 如果停止输入太久，速度会下降
        // 这里可以添加速度衰减逻辑
    }
    
    /// <summary>
    /// 受到攻击时检查是否格挡成功
    /// </summary>
    public override void TakeDamage(int damage = 1)
    {
        if (isParrying)
        {
            // 格挡成功，不受伤害
            Debug.Log("Attack parried!");
            return;
        }
        
        // 正常受伤
        base.TakeDamage(damage);
        
        // 受伤时速度重置
        currentSpeed = 0;
        if (animator != null)
        {
            animator.SetInteger("Speed", currentSpeed);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (nosePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(nosePoint.position, 0.5f);
        }
        
        // 绘制移动方向
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, currentDirection * moveDistance);
    }
}