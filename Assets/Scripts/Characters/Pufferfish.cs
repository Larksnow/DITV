using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 刺豚 - 范围攻击和战术位移
/// 机制：每拍可攻击（膨胀），最多连续3次，每次范围更大
/// 攻击后下一拍可选择位移（缩小逃脱），位移距离随攻击层级增加
/// </summary>
public class Pufferfish : BaseFish
{
    [Header("Pufferfish Settings")]
    [SerializeField] private float[] attackRanges = { 1f, 1.5f, 2f }; // 三个攻击等级的范围
    [SerializeField] private float[] dashDistances = { 4f, 6f, 8f }; // 对应的位移距离
    [SerializeField] private float[] scaleMultipliers = { 1.2f, 1.4f, 1.6f }; // 对应的缩放倍数
    [SerializeField] private float dashDurationBeats = 1f; // 位移持续时间（拍数）
    [SerializeField] private float scaleDuration = 0.1f; // 缩放动画时间
    [SerializeField] private int attackDamage = 1;
    
    [Header("Hitbox")]
    [SerializeField] private Collider2D attackHitbox; // 攻击判定框
    private AttackHitbox hitboxScript;
    private Vector3 originalHitboxScale;
    
    [Header("State")]
    [SerializeField] private int currentAttackLevel = 0; // 0-3，当前攻击等级
    [SerializeField] private bool isInflated = false;
    [SerializeField] private bool canDeflate = false; // 是否可以位移逃脱
    [SerializeField] private bool isDashing = false;
    private int lastInputBeat = -1; // 上次输入的拍子编号
    
    [Header("Scale Effect")]
    private Vector3 originalScale;
    private Tweener currentScaleTween;
    
    // 公开属性供PlayerController使用
    public bool IsInflated => isInflated;
    public bool CanDeflate => canDeflate;
    public bool IsDashing => isDashing;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 保存原始缩放
        originalScale = transform.localScale;
        
        // 初始化hitbox
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.isTrigger = true;
            originalHitboxScale = attackHitbox.transform.localScale;
            
            // 获取或添加AttackHitbox脚本
            hitboxScript = attackHitbox.GetComponent<AttackHitbox>();
            if (hitboxScript == null)
            {
                hitboxScript = attackHitbox.gameObject.AddComponent<AttackHitbox>();
            }
            hitboxScript.OnHitTarget = HandleHitboxCollision;
        }
    }
    
    protected override void Update()
    {
        base.Update();
    }
    
    /// <summary>
    /// 节拍输入 - 膨胀攻击
    /// </summary>
    public override void OnRhythmInput()
    {
        // 如果正在冲刺，打断冲刺
        if (isDashing)
        {
            InterruptDash();
        }
        
        // 已达到最大攻击层级，不接受更多攻击输入
        if (currentAttackLevel >= attackRanges.Length)
        {
            Debug.Log("Pufferfish at max level, attack input ignored");
            return;
        }
        
        // 记录这一拍的输入
        lastInputBeat = GetCurrentBeat();
        
        // 执行膨胀攻击
        PerformInflateAttack();
    }
    
    /// <summary>
    /// 打断冲刺
    /// </summary>
    private void InterruptDash()
    {
        isDashing = false;
        currentAttackLevel = 0;
        
        // 停止移动
        movementController.StopMovement();
        
        // 结束无敌
        SetInvincible(false);
        
        Debug.Log("Pufferfish dash interrupted by attack");
    }
    
    /// <summary>
    /// 获取当前拍子编号
    /// </summary>
    private int GetCurrentBeat()
    {
        if (Conductor.Instance == null) return 0;
        return Mathf.RoundToInt(Conductor.Instance.songPositionInBeats);
    }
    
    /// <summary>
    /// 执行充气攻击
    /// </summary>
    private void PerformInflateAttack()
    {
        // 增加攻击等级
        currentAttackLevel++;
        
        // 设置状态
        isInflated = true;
        canDeflate = true;
        
        // 膨胀时：先切换动画
        if (animator != null)
        {
            animator.SetBool("Attacking", true);
        }
        
        // 计算目标缩放
        float scaleMultiplier = scaleMultipliers[currentAttackLevel - 1];
        Vector3 targetScale = originalScale * scaleMultiplier;
        
        // 再放大（纯视觉效果，不影响逻辑）
        ScaleTo(targetScale);
        
        // 激活并缩放hitbox（立即生效）
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
            attackHitbox.transform.localScale = originalHitboxScale * scaleMultiplier;
        }
        
        // 每次攻击时主动检测范围内的敌人
        DealDamageInRange();
        
        Debug.Log($"Pufferfish inflate attack level {currentAttackLevel}, scale: {scaleMultiplier:F2}x");
    }
    
    /// <summary>
    /// 主动检测范围内的敌人并造成伤害
    /// </summary>
    private void DealDamageInRange()
    {
        // 使用当前攻击等级的范围
        float attackRange = attackRanges[currentAttackLevel - 1];
        
        // 使用基类的范围伤害检测方法
        DealDamageInRadius(transform.position, attackRange, attackDamage);
    }
    
    /// <summary>
    /// 使用DOTween缩放
    /// </summary>
    private void ScaleTo(Vector3 targetScale, System.Action onComplete = null)
    {
        // 停止之前的缩放
        if (currentScaleTween != null && currentScaleTween.IsActive())
        {
            currentScaleTween.Kill();
        }
        
        currentScaleTween = transform.DOScale(targetScale, scaleDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    /// <summary>
    /// 处理hitbox碰撞
    /// </summary>
    private void HandleHitboxCollision(Collider2D other)
    {
        // 只在膨胀状态处理碰撞
        if (!isInflated) return;
        
        // 使用基类的统一伤害检测方法
        if (TryDealDamage(other, attackDamage))
        {
            Debug.Log($"Pufferfish hit {other.gameObject.name}!");
        }
    }
    
    /// <summary>
    /// 尝试缩小并位移（由PlayerController调用）
    /// </summary>
    public void TryDeflate()
    {
        if (!canDeflate || isDashing) return;
        
        // 记录这一拍的输入
        lastInputBeat = GetCurrentBeat();
        
        PerformDeflateAndDash();
    }
    
    /// <summary>
    /// 执行缩小和冲刺
    /// </summary>
    private void PerformDeflateAndDash()
    {
        if (currentAttackLevel <= 0) return;
        
        isDashing = true;
        isInflated = false;
        canDeflate = false;
        
        // 计算位移距离（根据当前攻击层级）
        float dashDistance = dashDistances[currentAttackLevel - 1];
        
        // 计算位移方向 - 使用基类的GetMouseDirection
        Vector3 dashDirection = GetMouseDirection();
        Vector3 targetPosition = transform.position + dashDirection * dashDistance;
        
        // 限制在屏幕边界内
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 计算位移时间（拍数转秒数）
        float dashTimeInSeconds = Conductor.Instance.BeatsToSeconds(dashDurationBeats);
        
        // 执行位移（带无敌帧）
        movementController.SetTargetPosition(targetPosition, dashTimeInSeconds);
        SetInvincible(true);
        
        // 禁用hitbox（立即生效）
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.transform.localScale = originalHitboxScale;
        }
        
        // 缩小时：先缩小（纯视觉效果）
        ScaleTo(originalScale, () =>
        {
            // 缩小完成后再切换动画
            if (animator != null)
            {
                animator.SetBool("Attacking", false);
            }
        });
        
        // 开始位移协程
        StartCoroutine(DashCoroutine(dashTimeInSeconds));
        
        Debug.Log($"Pufferfish deflate and dash, level {currentAttackLevel}, distance: {dashDistance}");
    }
    
    /// <summary>
    /// 位移协程
    /// </summary>
    private IEnumerator DashCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // 位移结束
        isDashing = false;
        currentAttackLevel = 0;
        
        Debug.Log("Pufferfish dash complete");
    }
    
    /// <summary>
    /// 重置到初始状态（缩小）
    /// </summary>
    private void ResetToNormal()
    {
        isInflated = false;
        canDeflate = false;
        currentAttackLevel = 0;
        
        // 禁用hitbox（立即生效）
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.transform.localScale = originalHitboxScale;
        }
        
        // 缩小时：先缩小（纯视觉效果）
        ScaleTo(originalScale, () =>
        {
            // 缩小完成后再切换动画
            if (animator != null)
            {
                animator.SetBool("Attacking", false);
            }
        });
        
        Debug.Log("Pufferfish reset to normal");
    }
    
    protected override void OnBeatCustom(int beatCount)
    {
        // 如果正在位移，不处理
        if (isDashing) return;
        
        // 如果在膨胀状态，延迟检查是否需要复原
        // 延迟到 late window 结束后（inputThreshold 秒后）
        // 这样玩家有完整的输入窗口，复原时机仍然接近节拍
        if (isInflated && lastInputBeat < beatCount)
        {
            StartCoroutine(DelayedResetCheck(beatCount));
        }
    }
    
    /// <summary>
    /// 延迟检查是否需要复原（等待 late window 结束）
    /// </summary>
    private IEnumerator DelayedResetCheck(int beatCount)
    {
        // 等待 late window 结束
        // 这个延迟是必要的，给玩家完整的输入窗口
        // 延迟时间 = inputThreshold，复原时机在节拍后 0.15 秒左右
        float waitTime = Conductor.Instance != null ? Conductor.Instance.inputThreshold : 0.15f;
        yield return new WaitForSeconds(waitTime);
        
        // 再次检查：如果玩家在 late window 内输入了，lastInputBeat 会更新
        if (isInflated && !isDashing && lastInputBeat < beatCount)
        {
            ResetToNormal();
        }
    }
    
    protected override void HandleFreeMovement()
    {
        // 位移时不能自由移动
        if (!isDashing)
        {
            base.HandleFreeMovement();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();
    }
    
    void OnDrawGizmos()
    {
        if (isInflated || isDashing)
        {
            DrawDebugGizmos();
        }
    }
    
    private void DrawDebugGizmos()
    {
        // 绘制攻击范围
        if (currentAttackLevel > 0 && currentAttackLevel <= attackRanges.Length)
        {
            // 攻击范围 - 红色
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, attackRanges[currentAttackLevel - 1]);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRanges[currentAttackLevel - 1]);
        }
        
        // 绘制位移方向
        if (Application.isPlaying && canDeflate && currentAttackLevel > 0)
        {
            Vector3 dashDirection = GetMouseDirection();
            float dashDistance = dashDistances[currentAttackLevel - 1];
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, dashDirection * dashDistance);
            
            Vector3 endPoint = transform.position + dashDirection * dashDistance;
            Gizmos.DrawWireSphere(endPoint, 0.3f);
        }
    }
}
