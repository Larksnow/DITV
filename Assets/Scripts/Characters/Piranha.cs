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
    [SerializeField] private float dashDurationBeats = 0.5f; // 冲刺持续半拍
    
    [Header("Hitbox")]
    [SerializeField] private Collider2D attackHitbox; // 攻击判定框
    
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private AttackHitbox hitboxScript;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 初始化时禁用hitbox
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.isTrigger = true;
            
            // 获取或添加AttackHitbox脚本来接收碰撞事件
            hitboxScript = attackHitbox.GetComponent<AttackHitbox>();
            if (hitboxScript == null)
            {
                hitboxScript = attackHitbox.gameObject.AddComponent<AttackHitbox>();
            }
            hitboxScript.OnHitTarget = HandleHitboxCollision;
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
        
        // 计算冲刺目标位置 - 使用基类的GetMouseDirection
        Vector3 dashDirection = GetMouseDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 检查边界（使用基类方法）
        targetPosition = ClampToScreenBounds(targetPosition);
        
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
        
        Debug.Log($"Piranha dash attack, duration: {dashTimeInSeconds:F2}s ({dashDurationBeats} beats)");
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
    }
    
    /// <summary>
    /// 处理hitbox碰撞 - 由AttackHitbox脚本转发
    /// </summary>
    private void HandleHitboxCollision(Collider2D other)
    {
        // 使用基类的统一伤害检测方法
        if (TryDealDamage(other, attackDamage))
        {
            Debug.Log($"[Piranha] Hit {other.gameObject.name}!");
        }
    }
    
    /// <summary>
    /// 绘制调试信息 - 选中时显示
    /// </summary>
    void OnDrawGizmosSelected()
    {
        DrawHitboxGizmos();
    }
    
    /// <summary>
    /// 始终绘制调试信息（攻击时）
    /// </summary>
    void OnDrawGizmos()
    {
        // 攻击时始终显示红色hitbox
        if (isAttacking)
        {
            DrawHitboxGizmos();
        }
    }
    
    private void DrawHitboxGizmos()
    {
        // 绘制hitbox范围
        if (attackHitbox != null)
        {
            Gizmos.color = isAttacking ? Color.red : Color.yellow;
            
            // 获取hitbox的实际世界位置
            Transform hitboxTransform = attackHitbox.transform;
            
            if (attackHitbox is BoxCollider2D boxCollider)
            {
                // 使用hitbox自身的transform
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                    hitboxTransform.position, 
                    hitboxTransform.rotation, 
                    hitboxTransform.lossyScale
                );
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size * 0.9f); // 半透明填充
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (attackHitbox is CircleCollider2D circleCollider)
            {
                Vector3 center = hitboxTransform.TransformPoint(circleCollider.offset);
                Gizmos.DrawWireSphere(center, circleCollider.radius * hitboxTransform.lossyScale.x);
            }
        }
        
        // 绘制冲刺方向
        if (Application.isPlaying)
        {
            Vector3 dashDirection = GetMouseDirection();
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, dashDirection * dashDistance);
        }
    }
}