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
    
    [Header("Visual")]
    [SerializeField] private LayerMask enemyLayer = -1;
    
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
        // 正在位移时不能攻击
        if (isDashing) return;
        
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
        
        Debug.Log($"Pufferfish inflate attack level {currentAttackLevel}, scale: {scaleMultiplier:F2}x");
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
        
        // 检查是否在敌人层
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
        
        // 检查是否为敌人角色
        BaseFish targetFish = other.GetComponent<BaseFish>();
        if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
        {
            targetFish.TakeDamage(attackDamage);
            
            Debug.Log($"Pufferfish hit {targetFish.gameObject.name}!");
            
            if (targetFish.CurrentHealth <= 0)
            {
                RestoreHealth(1);
                ComboSystem.Instance?.OnEnemyKilled();
            }
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
        
        // 如果在膨胀状态
        if (isInflated)
        {
            // 检查上一拍是否有输入（当前拍 - 上次输入拍 > 1 说明上一拍没输入）
            // 注意：beatCount 是当前拍，lastInputBeat 是上次输入的拍
            // 如果 lastInputBeat < beatCount - 1，说明上一拍没有输入
            if (lastInputBeat < beatCount - 1)
            {
                // 上一拍没有输入，自动缩小
                ResetToNormal();
            }
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
