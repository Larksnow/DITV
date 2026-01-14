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
        
        // 剑鱼默认不能自由移动（必须通过加速系统）
        canFreeMove = false;
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
            
            PerformMovement();
            
            if (animator != null)
            {
                animator.SetInteger("Speed", currentSpeed);
            }
        }
        else
        {
            PerformMovement();
        }
    }
    
    /// <summary>
    /// 尝试格挡
    /// </summary>
    public void TryParry()
    {
        if (!hasParryOpportunity || isParrying) return;
        
        if (Conductor.Instance.CheckInputTiming())
        {
            StartParry();
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
        
        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }
        
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
        
        hasParryOpportunity = true;
        beatsSinceLastParry = 0;
        
        PerformCounterAttack(attacker);
        
        Debug.Log("Parry successful! Counter attack!");
    }
    
    /// <summary>
    /// 执行反击
    /// </summary>
    private void PerformCounterAttack(BaseFish target)
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.transform.position;
        movementController.SetTargetPosition(targetPosition, 0.3f);
        
        target.TakeDamage(Mathf.RoundToInt(counterDamage));
        
        if (target.CurrentHealth <= 0)
        {
            RestoreHealth(1);
            ComboSystem.Instance?.OnEnemyKilled();
        }
        
        if (animator != null)
        {
            animator.SetTrigger("CounterAttack");
        }
    }
    
    /// <summary>
    /// 执行移动 - 使用基类的GetMouseDirection
    /// </summary>
    private void PerformMovement()
    {
        Vector3 moveDir = GetMouseDirection();
        Vector3 targetPosition = transform.position + moveDir * moveDistance * (currentSpeed / (float)maxSpeed);
        targetPosition = ClampToScreenBounds(targetPosition);
        
        float moveDuration = Conductor.Instance.BeatsToSeconds(1f); // 一拍的时间
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
        Gizmos.DrawRay(transform.position, GetMouseDirection() * moveDistance);
    }
}