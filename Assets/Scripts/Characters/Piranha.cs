using UnityEngine;
using System.Collections;

/// <summary>
/// 食人鱼 - 快速攻击和位移
/// </summary>
public class Piranha : BaseFish
{
    [Header("Piranha Settings")]
    [SerializeField] private float dashDistance = 2f;
    [SerializeField] private int attackDamage = 1;
    
    [Header("Attack Timing (in Beats)")]
    [SerializeField] private float attackDurationBeats = 0.5f; // 攻击持续半拍
    [SerializeField] private float dashDurationBeats = 0.5f; // 冲刺持续0.4拍
    
    [Header("Hitbox")]
    [SerializeField] private Collider2D attackHitbox; // 攻击判定框
    [SerializeField] private LayerMask enemyLayer = -1;
    
    [Header("Visual")]
    [SerializeField] private Transform attackPoint; // 攻击检测点
    
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 食人鱼可以自由移动
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.SetCanFreeMove(true);
        }
        
        // 初始化时禁用hitbox
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.isTrigger = true; // 确保是触发器
        }
    }
    
    /// <summary>
    /// 食人鱼的节拍输入 - 冲刺攻击
    /// </summary>
    public override void OnRhythmInput()
    {   
        PerformDashAttack();
    }
    
    /// <summary>
    /// 执行冲刺攻击
    /// </summary>
    private void PerformDashAttack()
    {
        // 如果正在攻击，不能再次攻击
        if (isAttacking) return;
        
        // 计算冲刺目标位置
        Vector3 dashDirection = GetFacingDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 检查边界（防止冲出屏幕）
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 更新朝向
        UpdateFacing(dashDirection);
        
        // === 立即执行的动作 ===
        
        // 1. 立即切换到攻击动画
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // 2. 立即激活hitbox
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }
        
        // 3. 立即开始冲刺
        float dashTimeInSeconds = Conductor.Instance.BeatsToSeconds(dashDurationBeats);
        movementController.SetTargetPosition(targetPosition, dashTimeInSeconds);
        
        // 标记为攻击中
        isAttacking = true;
        
        // 开始攻击结束计时
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AttackEndSequence());
        
        Debug.Log($"Piranha dash attack to {targetPosition}, duration: {dashTimeInSeconds:F2}s ({dashDurationBeats} beats)");
    }
    
    /// <summary>
    /// 攻击结束序列 - 只负责在半拍后结束攻击
    /// </summary>
    private IEnumerator AttackEndSequence()
    {
        // 计算攻击持续时间（从拍数转换）
        float attackTimeInSeconds = Conductor.Instance.BeatsToSeconds(attackDurationBeats);
        
        // 等待攻击持续时间（半拍）
        yield return new WaitForSeconds(attackTimeInSeconds);
        
        // === 攻击结束 ===
        
        // 1. 禁用hitbox
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
        
        // 2. 切换回Idle动画
        if (animator != null)
        {
            animator.SetTrigger("AttackFinish");
        }
        
        // 标记攻击结束
        isAttacking = false;
        attackCoroutine = null;
        
        Debug.Log("Piranha attack finished, returning to Idle");
    }
    
    /// <summary>
    /// 当hitbox触发碰撞时调用
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 只在攻击期间处理碰撞
        if (!isAttacking) return;
        
        // 检查是否在敌人层
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        
        // 检查是否为敌人角色
        BaseFish targetFish = other.GetComponent<BaseFish>();
        if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
        {
            // 造成伤害
            targetFish.TakeDamage(attackDamage);
            
            Debug.Log($"Piranha hit {targetFish.gameObject.name}!");
            
            // 如果击杀了敌人，恢复血量
            if (targetFish.CurrentHealth <= 0)
            {
                RestoreHealth(1);
                
                // 通知连击系统
                if (ComboSystem.Instance != null)
                {
                    ComboSystem.Instance.OnEnemyKilled();
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
    /// 更新角色朝向
    /// </summary>
    private void UpdateFacing(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;
        
        // 根据方向翻转精灵
        if (spriteRenderer != null)
        {
            // 如果朝左，翻转精灵
            spriteRenderer.flipX = direction.x < 0;
        }
        
        // 或者使用旋转（如果你想要完整的360度旋转）
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0, 0, angle);
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
    /// 绘制调试信息
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 绘制攻击点
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, 0.2f);
        }
        
        // 绘制hitbox范围
        if (attackHitbox != null)
        {
            Gizmos.color = isAttacking ? Color.red : Color.yellow;
            
            if (attackHitbox is BoxCollider2D boxCollider)
            {
                Vector3 center = transform.position + (Vector3)boxCollider.offset;
                Vector3 size = boxCollider.size;
                Gizmos.DrawWireCube(center, size);
            }
            else if (attackHitbox is CircleCollider2D circleCollider)
            {
                Vector3 center = transform.position + (Vector3)circleCollider.offset;
                Gizmos.DrawWireSphere(center, circleCollider.radius);
            }
        }
        
        // 绘制冲刺方向
        Vector3 dashDirection = GetFacingDirection();
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, dashDirection * dashDistance);
        
        // 显示当前BPM信息
        if (Conductor.Instance != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"BPM: {Conductor.Instance.bpm}\n" +
                $"Attack: {attackDurationBeats} beats ({Conductor.Instance.BeatsToSeconds(attackDurationBeats):F2}s)\n" +
                $"Dash: {dashDurationBeats} beats ({Conductor.Instance.BeatsToSeconds(dashDurationBeats):F2}s)"
            );
        }
    }
}
