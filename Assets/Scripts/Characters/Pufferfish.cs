using UnityEngine;

/// <summary>
/// 刺豚 - 范围攻击和战术位移
/// </summary>
public class Pufferfish : BaseFish
{
    [Header("Pufferfish Settings")]
    [SerializeField] private float[] attackRanges = { 1f, 1.5f, 2f }; // 三个攻击等级的范围
    [SerializeField] private float[] dashDistances = { 2f, 3f, 4f }; // 对应的位移距离
    [SerializeField] private float inflateDuration = 1f; // 充气持续时间（一拍）
    [SerializeField] private float dashDuration = 2f; // 位移持续时间（两拍）
    [SerializeField] private int attackDamage = 1;
    
    [Header("State")]
    [SerializeField] private int currentAttackLevel = 0; // 0-2，当前攻击等级
    [SerializeField] private bool isInflated = false;
    [SerializeField] private bool canDeflate = false;
    
    [Header("Visual")]
    [SerializeField] private LayerMask enemyLayer = -1;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 刺豚默认不能自由移动（可以通过Inspector调整）
        canFreeMove = false;
    }
    
    public override void OnRhythmInput()
    {
        PerformInflateAttack();
    }
    
    /// <summary>
    /// 执行充气攻击
    /// </summary>
    private void PerformInflateAttack()
    {
        // 增加攻击等级（最多3级）
        currentAttackLevel = Mathf.Min(currentAttackLevel + 1, attackRanges.Length);
        
        // 设置充气状态
        isInflated = true;
        canDeflate = true;
        
        // 执行攻击
        PerformRangeAttack();
        
        // 播放动画
        if (animator != null)
        {
            animator.SetTrigger("Inflate");
            animator.SetInteger("AttackLevel", currentAttackLevel);
        }
        
        Debug.Log($"Pufferfish inflate attack level {currentAttackLevel}");
    }
    
    /// <summary>
    /// 执行范围攻击
    /// </summary>
    private void PerformRangeAttack()
    {
        float attackRange = attackRanges[currentAttackLevel - 1];
        
        // 获取攻击范围内的所有敌人
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        
        foreach (Collider2D collider in hitColliders)
        {
            BaseFish targetFish = collider.GetComponent<BaseFish>();
            if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
            {
                targetFish.TakeDamage(attackDamage);
                
                if (targetFish.CurrentHealth <= 0)
                {
                    RestoreHealth(1);
                    ComboSystem.Instance?.OnEnemyKilled();
                }
            }
        }
    }
    
    /// <summary>
    /// 尝试缩小并位移
    /// </summary>
    public void TryDeflate()
    {
        if (!canDeflate || movementController.IsMoving) return;
        
        // 检查节拍时机
        if (Conductor.Instance.CheckInputTiming())
        {
            PerformDeflateAndDash();
        }
    }
    
    /// <summary>
    /// 执行缩小和冲刺
    /// </summary>
    private void PerformDeflateAndDash()
    {
        if (currentAttackLevel <= 0) return;
        
        // 计算位移距离
        float dashDistance = dashDistances[currentAttackLevel - 1];
        
        // 计算位移方向 - 使用基类的GetMouseDirection
        Vector3 dashDirection = GetMouseDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 限制在屏幕边界内（使用基类方法）
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 执行位移（带无敌帧）
        movementController.SetTargetPosition(targetPosition, dashDuration);
        SetInvincible(true);
        
        // 重置状态
        isInflated = false;
        canDeflate = false;
        currentAttackLevel = 0;
        
        // 播放动画
        if (animator != null)
        {
            animator.SetTrigger("Deflate");
            animator.SetInteger("AttackLevel", 0);
        }
        
        Debug.Log($"Pufferfish deflate and dash");
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
        // 充气状态持续一拍后自动结束
        if (isInflated)
        {
            // 这里可以添加充气状态的视觉效果更新
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (currentAttackLevel > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRanges[currentAttackLevel - 1]);
        }
    }
}