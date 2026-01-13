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
        if (movementController is InterpolationMovement interpMovement)
        {
            interpMovement.SetCanFreeMove(true);
        }
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
        
        // 持续蓄力
        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            // 蓄力状态在OnBeat中处理
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
        // 这个方法在PlayerController中被调用，但实际逻辑在HandleChargeInput中
    }
    
    /// <summary>
    /// 开始蓄力
    /// </summary>
    private void StartCharge()
    {
        // 检查是否在节拍上
        if (!Conductor.Instance.CheckInputTiming())
        {
            // 错拍会由PlayerController处理，这里不需要额外处理
            Debug.Log("Charge timing missed - handled by PlayerController");
            return;
        }
        
        isCharging = true;
        chargeLevel = 0;
        chargeBeatCount = 0;
        
        Debug.Log("Tuna started charging");
        
        // 播放蓄力动画
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
        
        // 检查是否在节拍上释放
        if (!Conductor.Instance.CheckInputTiming())
        {
            // 错拍释放，取消蓄力
            CancelCharge();
            // 错拍会由PlayerController处理，这里不需要额外处理
            Debug.Log("Release timing missed - handled by PlayerController");
            return;
        }
        
        // 执行冲刺攻击
        PerformChargedDash();
        
        // 重置蓄力状态
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
        
        // 获取冲刺参数
        float damage = chargeDamages[chargeLevel - 1];
        float distance = chargeDistances[chargeLevel - 1];
        
        // 计算冲刺方向和目标位置
        Vector3 dashDirection = GetDashDirection();
        Vector3 targetPosition = transform.position + dashDirection * distance;
        targetPosition = ClampToScreenBounds(targetPosition);
        
        // 执行冲刺移动
        movementController.SetTargetPosition(targetPosition, chargeDuration);
        
        // 冲刺过程中无敌
        SetInvincible(true);
        
        // 执行攻击检测
        StartCoroutine(DashAttackCoroutine(damage));
        
        // 播放动画
        if (animator != null)
        {
            animator.SetTrigger("ChargedDash");
            animator.SetInteger("ChargeLevel", chargeLevel);
        }
        
        Debug.Log($"Tuna charged dash level {chargeLevel}, damage: {damage}, distance: {distance}");
    }
    
    /// <summary>
    /// 冲刺攻击协程
    /// </summary>
    private System.Collections.IEnumerator DashAttackCoroutine(float damage)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < chargeDuration)
        {
            // 检测冲刺路径上的敌人
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
        
        // 否则朝向最近的敌人
        BaseFish nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            return (nearestEnemy.transform.position - transform.position).normalized;
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
            Vector3 dashDirection = GetDashDirection();
            float distance = chargeDistances[chargeLevel - 1];
            Gizmos.DrawRay(transform.position, dashDirection * distance);
        }
    }
}