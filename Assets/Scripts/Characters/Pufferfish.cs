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
        
        // 刺豚不能自由移动
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.SetCanFreeMove(false);
        }
    }
    
    public override void OnRhythmInput()
    {
        if (isPunished) return;
        
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
        if (!canDeflate || isPunished || movementController.IsMoving) return;
        
        // 检查节拍时机
        if (Conductor.Instance.CheckInputTiming())
        {
            PerformDeflateAndDash();
        }
        else
        {
            ApplyMissPenalty();
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
        
        // 计算位移方向（朝向最近的敌人相反方向，或输入方向）
        Vector3 dashDirection = GetDashDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 限制在屏幕边界内
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 执行位移（带无敌帧）
        movementController.SetTargetPosition(targetPosition, dashDuration);
        SetInvincible(true); // 位移过程中无敌
        
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
        
        Debug.Log($"Pufferfish deflate and dash to {targetPosition}");
    }
    
    /// <summary>
    /// 获取冲刺方向 - 优先使用鼠标方向
    /// </summary>
    private Vector3 GetDashDirection()
    {
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 directionToMouse = (mouseWorldPos - transform.position).normalized;
        
        // 如果鼠标距离足够远，使用鼠标方向
        float distanceToMouse = Vector3.Distance(transform.position, mouseWorldPos);
        if (distanceToMouse > 1f)
        {
            return directionToMouse;
        }
        
        // 否则远离最近的敌人
        BaseFish nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            return (transform.position - nearestEnemy.transform.position).normalized;
        }
        
        // 默认向右
        return Vector3.right;
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