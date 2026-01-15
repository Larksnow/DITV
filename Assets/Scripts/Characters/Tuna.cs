using UnityEngine;
using System.Collections;

/// <summary>
/// 金枪鱼 - 蓄力冲刺攻击
/// </summary>
public class Tuna : BaseFish
{
    [Header("Tuna Settings")]
    [SerializeField] private float[] chargeDamages = { 2f, 3f, 4f }; // 三个蓄力等级的伤害
    [SerializeField] private float[] chargeDistances = { 3f, 5f, 7f }; // 对应的冲刺距离
    [SerializeField] private float dashDurationBeats = 1f; // 冲刺持续时间（拍数）
    
    [Header("Charge State")]
    [SerializeField] private int chargeLevel = 0; // 0-3，蓄力等级
    [SerializeField] private bool isCharging = false;
    [SerializeField] private bool isDashing = false;
    [SerializeField] private int chargeBeatCount = 0;
    [SerializeField] private int maxChargeBeats = 3;
    
    [Header("Scale Effect")]
    [SerializeField] private float maxScaleMultiplier = 1.3f; // 最大蓄力时的缩放倍数
    [SerializeField] private float scaleSpeed = 5f; // 缩放速度
    private Vector3 originalScale;
    private float targetScaleMultiplier = 1f;
    
    [Header("Visual")]
    [SerializeField] private LayerMask enemyLayer = -1;
    
    // 公开属性供PlayerController使用
    public bool IsCharging => isCharging;
    public bool IsDashing => isDashing;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 金枪鱼可以自由移动，但速度较慢
        freeMovementSpeed = 2f;
        
        // 保存原始缩放
        originalScale = transform.localScale;
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 平滑缩放效果
        UpdateScaleEffect();
    }
    
    /// <summary>
    /// 节拍输入 - 开始蓄力（由PlayerController在节拍正确时调用）
    /// </summary>
    public override void OnRhythmInput()
    {
        // 如果正在冲刺，忽略输入
        if (isDashing) return;
        
        // 如果没在蓄力，开始蓄力
        if (!isCharging)
        {
            StartCharge();
        }
        // 如果已经在蓄力，这个输入会被忽略（松开按键时才释放）
    }
    
    /// <summary>
    /// 开始蓄力
    /// </summary>
    private void StartCharge()
    {
        isCharging = true;
        chargeLevel = 0;
        chargeBeatCount = 0;
        targetScaleMultiplier = 1f;
        
        Debug.Log("Tuna started charging");
    }
    
    /// <summary>
    /// 释放蓄力（由PlayerController调用）
    /// </summary>
    /// <param name="onBeat">是否在节拍上释放</param>
    public void ReleaseCharge(bool onBeat)
    {
        if (!isCharging) return;
        
        if (onBeat && chargeLevel > 0)
        {
            // 节拍正确且有蓄力，执行冲刺
            PerformChargedDash();
        }
        else
        {
            // 节拍错误或没蓄力，取消
            CancelCharge();
        }
        
        isCharging = false;
        chargeBeatCount = 0;
    }
    
    /// <summary>
    /// 取消蓄力
    /// </summary>
    private void CancelCharge()
    {
        chargeLevel = 0;
        targetScaleMultiplier = 1f;
        
        Debug.Log("Tuna charge cancelled");
    }
    
    /// <summary>
    /// 执行蓄力冲刺
    /// </summary>
    private void PerformChargedDash()
    {
        if (chargeLevel <= 0) return;
        
        isDashing = true;
        
        float damage = chargeDamages[chargeLevel - 1];
        float distance = chargeDistances[chargeLevel - 1];
        
        // 使用基类的GetMouseDirection
        Vector3 dashDirection = GetMouseDirection();
        Vector3 targetPosition = transform.position + dashDirection * distance;
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 计算冲刺时间（拍数转秒数）
        float dashTimeInSeconds = Conductor.Instance.BeatsToSeconds(dashDurationBeats);
        
        // 开始移动
        movementController.SetTargetPosition(targetPosition, dashTimeInSeconds);
        SetInvincible(true);
        
        // 同时开始攻击动画
        if (animator != null)
        {
            animator.SetBool("Attacking", true);
        }
        
        // 开始冲刺攻击协程
        StartCoroutine(DashAttackCoroutine(damage, dashTimeInSeconds));
        
        Debug.Log($"Tuna charged dash level {chargeLevel}, duration: {dashTimeInSeconds:F2}s");
    }
    
    /// <summary>
    /// 冲刺攻击协程
    /// </summary>
    private IEnumerator DashAttackCoroutine(float damage, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            CheckDashCollision(damage);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 冲刺结束
        isDashing = false;
        chargeLevel = 0;
        targetScaleMultiplier = 1f;
        
        // 结束攻击动画
        if (animator != null)
        {
            animator.SetBool("Attacking", false);
        }
    }
    
    /// <summary>
    /// 检测冲刺碰撞
    /// </summary>
    private void CheckDashCollision(float damage)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 1f, enemyLayer);
        
        foreach (Collider2D collider in hitColliders)
        {
            BaseFish targetFish = collider.GetComponent<BaseFish>();
            if (targetFish != null && !targetFish.IsPlayer && targetFish != this)
            {
                targetFish.TakeDamage(Mathf.RoundToInt(damage));
                
                if (targetFish.CurrentHealth <= 0)
                {
                    RestoreHealth(1);
                    ComboSystem.Instance?.OnEnemyKilled();
                }
            }
        }
    }
    
    /// <summary>
    /// 更新缩放效果
    /// </summary>
    private void UpdateScaleEffect()
    {
        // 计算当前目标缩放
        float currentMultiplier = transform.localScale.x / originalScale.x;
        
        // 平滑插值到目标缩放
        float newMultiplier = Mathf.Lerp(currentMultiplier, targetScaleMultiplier, Time.deltaTime * scaleSpeed);
        transform.localScale = originalScale * newMultiplier;
    }
    
    protected override void OnBeatCustom(int beatCount)
    {
        // 处理蓄力逻辑 - 每拍增加蓄力等级
        if (isCharging)
        {
            chargeBeatCount++;
            
            if (chargeBeatCount <= maxChargeBeats)
            {
                chargeLevel = chargeBeatCount;
                
                // 计算缩放目标（1到maxScaleMultiplier之间）
                float scaleProgress = (float)chargeLevel / maxChargeBeats;
                targetScaleMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, scaleProgress);
                
                Debug.Log($"Tuna charge level: {chargeLevel}, scale: {targetScaleMultiplier:F2}x");
            }
        }
    }
    
    protected override void HandleFreeMovement()
    {
        // 蓄力和冲刺时不能自由移动
        if (!isCharging && !isDashing)
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
        if (isCharging || isDashing)
        {
            DrawDebugGizmos();
        }
    }
    
    private void DrawDebugGizmos()
    {
        if (!Application.isPlaying) return;
        
        // 绘制冲刺方向和距离
        if (chargeLevel > 0 && chargeLevel <= chargeDistances.Length)
        {
            Vector3 dashDirection = GetMouseDirection();
            float distance = chargeDistances[chargeLevel - 1];
            
            // 蓄力时蓝色，冲刺时红色
            Gizmos.color = isDashing ? Color.red : Color.blue;
            Gizmos.DrawRay(transform.position, dashDirection * distance);
            
            // 绘制冲刺终点
            Vector3 endPoint = transform.position + dashDirection * distance;
            Gizmos.DrawWireSphere(endPoint, 0.3f);
        }
        
        // 绘制碰撞检测范围
        if (isDashing)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, 1f);
        }
    }
}
