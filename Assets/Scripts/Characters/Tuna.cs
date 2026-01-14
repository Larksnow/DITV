using UnityEngine;

/// <summary>
/// 金枪鱼 - 蓄力冲刺攻击
/// </summary>
public class Tuna : BaseFish
{
    [Header("Tuna Settings")]
    [SerializeField] private float[] chargeDamages = { 2f, 3f, 4f }; // 三个蓄力等级的伤害
    [SerializeField] private float[] chargeDistances = { 3f, 5f, 7f }; // 对应的冲刺距离
    [SerializeField] private float chargeDuration = 1f; // 冲刺持续时间（一拍）
    [SerializeField] private float freeMovementSpeed = 2f; // 自由移动速度（较慢）
    
    [Header("Charge State")]
    [SerializeField] private int chargeLevel = 0; // 0-3，蓄力等级
    [SerializeField] private bool isCharging = false;
    [SerializeField] private bool isDashing = false;
    [SerializeField] private int chargeBeatCount = 0;
    [SerializeField] private int maxChargeBeats = 3;
    
    [Header("Visual")]
    [SerializeField] private LayerMask enemyLayer = -1;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 金枪鱼可以自由移动，但速度较慢
        freeMovementSpeed = 2f;
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 处理蓄力输入
        HandleChargeInput();
    }
    
    /// <summary>
    /// 处理蓄力输入
    /// </summary>
    private void HandleChargeInput()
    {
        if (isDashing) return;
        
        // 开始蓄力
        if (Input.GetKeyDown(KeyCode.Space) && !isCharging)
        {
            StartCharge();
        }
        
        // 释放蓄力
        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            ReleaseCharge();
        }
    }
    
    public override void OnRhythmInput()
    {
        // 金枪鱼的主要输入通过蓄力系统处理
    }
    
    /// <summary>
    /// 开始蓄力
    /// </summary>
    private void StartCharge()
    {
        if (!Conductor.Instance.CheckInputTiming()) return;
        
        isCharging = true;
        chargeLevel = 0;
        chargeBeatCount = 0;
        
        Debug.Log("Tuna started charging");
        
        if (animator != null)
        {
            animator.SetBool("IsCharging", true);
        }
    }
    
    /// <summary>
    /// 释放蓄力
    /// </summary>
    public void ReleaseCharge()
    {
        if (!isCharging) return;
        
        if (!Conductor.Instance.CheckInputTiming())
        {
            CancelCharge();
            return;
        }
        
        PerformChargedDash();
        
        isCharging = false;
        chargeLevel = 0;
        chargeBeatCount = 0;
        
        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
        }
    }
    
    /// <summary>
    /// 取消蓄力
    /// </summary>
    private void CancelCharge()
    {
        isCharging = false;
        chargeLevel = 0;
        chargeBeatCount = 0;
        
        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
        }
        
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
        
        movementController.SetTargetPosition(targetPosition, chargeDuration);
        SetInvincible(true);
        
        StartCoroutine(DashAttackCoroutine(damage));
        
        if (animator != null)
        {
            animator.SetTrigger("ChargedDash");
            animator.SetInteger("ChargeLevel", chargeLevel);
        }
        
        Debug.Log($"Tuna charged dash level {chargeLevel}");
    }
    
    /// <summary>
    /// 冲刺攻击协程
    /// </summary>
    private System.Collections.IEnumerator DashAttackCoroutine(float damage)
    {
        float elapsed = 0f;
        
        while (elapsed < chargeDuration)
        {
            CheckDashCollision(damage);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isDashing = false;
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
        // 处理蓄力逻辑
        if (isCharging)
        {
            chargeBeatCount++;
            
            if (chargeBeatCount <= maxChargeBeats)
            {
                chargeLevel = chargeBeatCount;
                Debug.Log($"Tuna charge level: {chargeLevel}");
                
                // 更新蓄力视觉效果
                if (animator != null)
                {
                    animator.SetInteger("ChargeLevel", chargeLevel);
                }
            }
        }
    }
    
    protected override void HandleFreeMovement()
    {
        if (!isCharging && !isDashing)
        {
            base.HandleFreeMovement();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (chargeLevel > 0)
        {
            Gizmos.color = Color.blue;
            Vector3 dashDirection = GetMouseDirection();
            float distance = chargeDistances[chargeLevel - 1];
            Gizmos.DrawRay(transform.position, dashDirection * distance);
        }
    }
}