using UnityEngine;

/// <summary>
/// 食人鱼 - 快速攻击和位移
/// </summary>
public class Piranha : BaseFish
{
    [Header("Piranha Settings")]
    [SerializeField] private float dashDistance = 2f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackAngle = 60f; // 扇形攻击角度
    [SerializeField] private int attackDamage = 1;
    
    [Header("Visual")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer = -1;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 食人鱼可以自由移动
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.SetCanFreeMove(true);
        }
    }
    
    /// <summary>
    /// 食人鱼的节拍输入 - 冲刺攻击
    /// </summary>
    public override void OnRhythmInput()
    {
        if (isPunished || movementController.IsMoving) return;
        
        PerformDashAttack();
    }
    
    /// <summary>
    /// 执行冲刺攻击
    /// </summary>
    private void PerformDashAttack()
    {
        // 计算冲刺目标位置
        Vector3 dashDirection = GetFacingDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 检查边界（防止冲出屏幕）
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 执行冲刺移动
        movementController.SetTargetPosition(targetPosition, dashDuration);
        
        // 执行攻击检测
        PerformAttack();
        
        // 播放动画
        if (animator != null)
        {
            animator.SetTrigger("DashAttack");
        }
        
        Debug.Log($"Piranha dash attack to {targetPosition}");
    }
    
    /// <summary>
    /// 执行攻击检测
    /// </summary>
    private void PerformAttack()
    {
        Vector3 attackDirection = GetFacingDirection();
        Vector3 attackCenter = attackPoint != null ? attackPoint.position : transform.position;
        
        // 获取攻击范围内的所有碰撞体
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackCenter, attackRange, enemyLayer);
        
        foreach (Collider2D collider in hitColliders)
        {
            // 检查是否在扇形攻击范围内
            Vector3 directionToTarget = (collider.transform.position - attackCenter).normalized;
            float angleToTarget = Vector3.Angle(attackDirection, directionToTarget);
            
            if (angleToTarget <= attackAngle / 2f)
            {
                // 检查是否为敌人
                BaseFish targetFish = collider.GetComponent<BaseFish>();
                if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
                {
                    // 造成伤害
                    targetFish.TakeDamage(attackDamage);
                    
                    // 如果击杀了敌人，恢复血量
                    if (targetFish.CurrentHealth <= 0)
                    {
                        RestoreHealth(1);
                        
                        // 通知连击系统
                        ComboSystem.Instance?.OnEnemyKilled();
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 获取面向方向 - 优先使用鼠标方向
    /// </summary>
    private Vector3 GetFacingDirection()
    {
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 directionToMouse = (mouseWorldPos - transform.position).normalized;
        
        // 如果鼠标距离足够远，使用鼠标方向
        float distanceToMouse = Vector3.Distance(transform.position, mouseWorldPos);
        if (distanceToMouse > 0.5f)
        {
            return directionToMouse;
        }
        
        // 否则使用当前面向方向（默认向右）
        return transform.right;
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
    /// 限制位置在屏幕边界内
    /// </summary>
    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        // 获取屏幕边界
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 screenBounds = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.transform.position.z));
            
            position.x = Mathf.Clamp(position.x, -screenBounds.x + 1f, screenBounds.x - 1f);
            position.y = Mathf.Clamp(position.y, -screenBounds.y + 1f, screenBounds.y - 1f);
        }
        
        return position;
    }
    
    /// <summary>
    /// 绘制攻击范围（用于调试）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        
        // 绘制扇形攻击范围
        Vector3 attackDirection = GetFacingDirection();
        Vector3 leftBoundary = Quaternion.Euler(0, 0, attackAngle / 2f) * attackDirection;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -attackAngle / 2f) * attackDirection;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(attackPoint.position, leftBoundary * attackRange);
        Gizmos.DrawRay(attackPoint.position, rightBoundary * attackRange);
    }
}