using UnityEngine;
using System.Collections;

/// <summary>
/// 剑鱼 - 高速移动和精准反击
/// 机制：0级速可自由移动，节拍输入加速（最高3级），不输入则每拍-1
/// 格挡：最高速时可格挡，CD为2拍，格挡成功可连续格挡
/// 反击：格挡成功后下一拍自动触发，持续一拍，带无敌帧
/// 
/// 动画状态机：
/// - ideal -> parry (Parrying = true)
/// - parry -> ideal (Parrying = false, Attacking = false)
/// - parry -> attack (Parrying = false, Attacking = true)
/// - attack -> ideal (Attacking = false)
/// 注意：多个condition是AND关系
/// </summary>
public class Swordfish : BaseFish
{
    [Header("Swordfish Settings")]
    [SerializeField] private float[] speedLevels = { 3f, 6f, 10f, 15f }; // 4个挡位的移动速度 (0-3级)
    [SerializeField] private int currentSpeed = 0; // 当前速度等级 0-3
    [SerializeField] private int noseDamage = 1;
    [SerializeField] private int counterDamage = 5;
    
    [Header("Hitboxes")]
    [SerializeField] private Collider2D noseHitbox; // 最高速时的嘴尖攻击判定（只在idle状态有效）
    [SerializeField] private Collider2D counterHitbox; // 反击冲刺时的攻击判定
    [SerializeField] private Collider2D parryHitbox; // 格挡时的碰撞体（用于检测敌人攻击）
    private AttackHitbox noseHitboxScript;
    private AttackHitbox counterHitboxScript;
    
    [Header("Parry System")]
    [SerializeField] private bool isParrying = false;
    [SerializeField] private bool canParry = false; // 是否可以格挡（最高速+CD结束）
    [SerializeField] private int parryCooldown = 0; // 格挡冷却（拍数）
    [SerializeField] private int parryCooldownMax = 2; // 格挡CD（2拍）
    private Coroutine parryCoroutine;
    
    [Header("Counter Attack")]
    [SerializeField] private bool isCounterAttacking = false; // 是否正在反击
    [SerializeField] private bool pendingCounterAttack = false; // 是否有待执行的反击
    [SerializeField] private float counterDashDistance = 5f; // 反击冲刺距离
    
    [Header("Movement")]
    private int lastInputBeat = -1;
    
    // 公开属性
    public bool IsParrying => isParrying;
    public bool CanParry => canParry;
    public int CurrentSpeed => currentSpeed;
    public int MaxSpeedLevel => speedLevels.Length - 1; // 最高等级 = 3
    public bool IsMaxSpeed => currentSpeed >= MaxSpeedLevel;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 剑鱼始终可以自由移动
        canFreeMove = true;
        
        // 初始化速度
        UpdateSpeed(0);
        
        // 初始化嘴尖hitbox
        if (noseHitbox != null)
        {
            noseHitbox.enabled = false;
            noseHitbox.isTrigger = true;
            
            noseHitboxScript = noseHitbox.GetComponent<AttackHitbox>();
            if (noseHitboxScript == null)
            {
                noseHitboxScript = noseHitbox.gameObject.AddComponent<AttackHitbox>();
            }
            noseHitboxScript.OnHitTarget = HandleNoseHit;
        }
        
        // 初始化反击hitbox
        if (counterHitbox != null)
        {
            counterHitbox.enabled = false;
            counterHitbox.isTrigger = true;
            
            counterHitboxScript = counterHitbox.GetComponent<AttackHitbox>();
            if (counterHitboxScript == null)
            {
                counterHitboxScript = counterHitbox.gameObject.AddComponent<AttackHitbox>();
            }
            counterHitboxScript.OnHitTarget = HandleCounterHit;
        }
        
        // 初始化格挡hitbox（默认禁用）
        if (parryHitbox != null)
        {
            parryHitbox.enabled = false;
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 更新嘴尖hitbox状态（只在最高速且idle状态时启用）
        UpdateNoseHitbox();
    }
    
    /// <summary>
    /// 更新嘴尖hitbox状态
    /// </summary>
    private void UpdateNoseHitbox()
    {
        if (noseHitbox == null) return;
        
        // 只在最高速、非格挡、非反击时启用嘴尖hitbox（idle状态）
        bool shouldEnable = currentSpeed >= MaxSpeedLevel && !isParrying && !isCounterAttacking;
        noseHitbox.enabled = shouldEnable;
    }
    
    /// <summary>
    /// 处理嘴尖碰撞
    /// </summary>
    private void HandleNoseHit(Collider2D other)
    {
        // 只在idle状态（最高速、非格挡、非反击）处理
        if (currentSpeed < MaxSpeedLevel || isParrying || isCounterAttacking) return;
        
        // 使用基类的统一伤害检测方法
        if (TryDealDamage(other, noseDamage))
        {
            Debug.Log($"Swordfish nose hit {other.gameObject.name}!");
        }
    }
    
    /// <summary>
    /// 处理反击碰撞
    /// </summary>
    private void HandleCounterHit(Collider2D other)
    {
        // 只在反击状态处理
        if (!isCounterAttacking) return;
        
        // 使用基类的统一伤害检测方法
        if (TryDealDamage(other, counterDamage))
        {
            Debug.Log($"Swordfish counter hit {other.gameObject.name}!");
        }
    }
    
    protected override void HandleFreeMovement()
    {
        // 反击时不能自由移动
        if (!isCounterAttacking)
        {
            base.HandleFreeMovement();
        }
    }
    
    public override void OnRhythmInput()
    {
        lastInputBeat = GetCurrentBeat();
        AccelerateOnBeat();
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
    /// 在节拍上加速
    /// </summary>
    private void AccelerateOnBeat()
    {
        if (currentSpeed < MaxSpeedLevel)
        {
            UpdateSpeed(currentSpeed + 1);
            Debug.Log($"Swordfish speed: {currentSpeed}/{MaxSpeedLevel}");
        }
    }
    
    /// <summary>
    /// 更新速度等级，同时更新freeMovementSpeed
    /// </summary>
    private void UpdateSpeed(int newSpeed)
    {
        currentSpeed = Mathf.Clamp(newSpeed, 0, MaxSpeedLevel);
        freeMovementSpeed = speedLevels[currentSpeed];
        Debug.Log($"Swordfish speed level: {currentSpeed}, movement speed: {freeMovementSpeed}");
    }
    
    /// <summary>
    /// 尝试格挡（由PlayerController调用）
    /// </summary>
    public void TryParry()
    {
        // 只有最高速度且CD结束时才能格挡
        if (!canParry || isParrying)
        {
            Debug.Log($"Cannot parry: canParry={canParry}, isParrying={isParrying}, speed={currentSpeed}/{MaxSpeedLevel}");
            return;
        }
        
        StartParry();
    }
    
    /// <summary>
    /// 开始格挡 - 格挡窗口和节拍判定窗口一致
    /// </summary>
    private void StartParry()
    {
        isParrying = true;
        canParry = false; // 格挡后进入CD
        parryCooldown = parryCooldownMax;
        
        // 禁用嘴尖hitbox
        if (noseHitbox != null)
        {
            noseHitbox.enabled = false;
        }
        
        // 启用格挡hitbox
        if (parryHitbox != null)
        {
            parryHitbox.enabled = true;
        }
        
        Debug.Log("Swordfish parry activated");
        
        // 动画：ideal -> parry (Parrying = true)
        if (animator != null)
        {
            animator.SetBool("Parrying", true);
        }
        
        // 停止之前的格挡协程
        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
        }
        
        // 格挡持续时间 = 节拍判定窗口 * 2（前后各一个窗口）
        float parryDuration = Conductor.Instance != null ? Conductor.Instance.inputThreshold * 2f : 0.3f;
        parryCoroutine = StartCoroutine(ParryCoroutine(parryDuration));
    }
    
    /// <summary>
    /// 格挡协程
    /// </summary>
    private IEnumerator ParryCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        EndParry();
    }
    
    /// <summary>
    /// 结束格挡（回到idle）
    /// </summary>
    private void EndParry()
    {
        isParrying = false;
        
        // 禁用格挡hitbox
        if (parryHitbox != null)
        {
            parryHitbox.enabled = false;
        }
        
        // 动画：parry -> ideal (Parrying = false, Attacking = false)
        if (animator != null)
        {
            animator.SetBool("Parrying", false);
            animator.SetBool("Attacking", false);
        }
        
        parryCoroutine = null;
        Debug.Log("Swordfish parry ended, back to idle");
    }
    
    /// <summary>
    /// 格挡成功，标记下一拍反击
    /// </summary>
    public void OnParrySuccess(BaseFish attacker)
    {
        if (!isParrying) return;
        
        // 停止格挡协程
        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
        }
        
        isParrying = false;
        
        // 格挡成功，重置CD，可以连续格挡
        parryCooldown = 0;
        canParry = true;
        
        // 禁用格挡hitbox
        if (parryHitbox != null)
        {
            parryHitbox.enabled = false;
        }
        
        // 标记下一拍执行反击
        pendingCounterAttack = true;
        
        // 动画：parry -> attack (Parrying = false, Attacking = true)
        if (animator != null)
        {
            animator.SetBool("Parrying", false);
            animator.SetBool("Attacking", true);
        }
        
        Debug.Log("Parry successful! Counter attack will trigger next beat!");
    }
    
    /// <summary>
    /// 执行反击（在下一拍自动触发）- 朝鼠标方向冲刺
    /// </summary>
    private void PerformCounterAttack()
    {
        isCounterAttacking = true;
        pendingCounterAttack = false;
        
        // 启用反击hitbox
        if (counterHitbox != null)
        {
            counterHitbox.enabled = true;
        }
        
        // 设置无敌帧
        SetInvincible(true);
        
        // 朝鼠标方向冲刺
        Vector3 dashDirection = GetMouseDirection();
        Vector3 targetPosition = transform.position + dashDirection * counterDashDistance;
        targetPosition = ClampToScreenBounds(targetPosition);
        
        float dashDuration = Conductor.Instance != null ? Conductor.Instance.BeatsToSeconds(1f) : 0.5f;
        movementController.SetTargetPosition(targetPosition, dashDuration);
        
        Debug.Log($"Counter attack executed towards mouse direction!");
        
        // 反击持续一拍后结束
        StartCoroutine(EndCounterAttackAfterBeat());
    }
    
    /// <summary>
    /// 反击结束协程
    /// </summary>
    private IEnumerator EndCounterAttackAfterBeat()
    {
        float beatDuration = Conductor.Instance != null ? Conductor.Instance.BeatsToSeconds(1f) : 0.5f;
        yield return new WaitForSeconds(beatDuration);
        
        isCounterAttacking = false;
        
        // 禁用反击hitbox
        if (counterHitbox != null)
        {
            counterHitbox.enabled = false;
        }
        
        // 动画：attack -> ideal (Attacking = false)
        if (animator != null)
        {
            animator.SetBool("Attacking", false);
        }
        
        Debug.Log("Counter attack ended, back to idle");
    }
    
    protected override void OnBeatCustom(int beatCount)
    {
        // 如果有待执行的反击，在这一拍执行
        if (pendingCounterAttack)
        {
            PerformCounterAttack();
            return; // 反击拍不处理其他逻辑
        }
        
        // 处理格挡CD（只在最高速时）
        if (currentSpeed >= MaxSpeedLevel)
        {
            // CD倒计时
            if (parryCooldown > 0)
            {
                parryCooldown--;
                Debug.Log($"Swordfish parry cooldown: {parryCooldown}");
            }
            
            // CD结束且最高速，可以格挡
            if (parryCooldown <= 0 && !canParry && !isCounterAttacking)
            {
                canParry = true;
                Debug.Log("Swordfish can parry now!");
            }
        }
        else
        {
            // 不是最高速，不能格挡
            canParry = false;
        }
        
        // 速度衰减：每拍-1（不管有没有输入，只要上一拍没输入就减）
        if (lastInputBeat < beatCount && currentSpeed > 0)
        {
            UpdateSpeed(currentSpeed - 1);
            Debug.Log($"Swordfish speed decay: {currentSpeed}/{MaxSpeedLevel}");
            
            // 速度下降，不能格挡
            if (currentSpeed < MaxSpeedLevel)
            {
                canParry = false;
            }
        }
    }
    
    /// <summary>
    /// 受到攻击时检查是否格挡成功
    /// </summary>
    public override void TakeDamage(int damage = 1)
    {
        // 反击中无敌
        if (isCounterAttacking) return;
        
        if (isParrying)
        {
            // 格挡成功，不受伤害，触发反击
            // 需要找到攻击者来反击
            BaseFish attacker = FindNearestEnemy();
            if (attacker != null)
            {
                OnParrySuccess(attacker);
            }
            else
            {
                EndParry();
            }
            return;
        }
        
        // 正常受伤
        base.TakeDamage(damage);
        
        // 受伤时速度重置
        UpdateSpeed(0);
        canParry = false;
        pendingCounterAttack = false;
        
        // 禁用所有hitbox
        if (noseHitbox != null) noseHitbox.enabled = false;
        if (counterHitbox != null) counterHitbox.enabled = false;
        if (parryHitbox != null) parryHitbox.enabled = false;
        
        if (animator != null)
        {
            animator.SetBool("Attacking", false);
            animator.SetBool("Parrying", false);
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
    
    void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();
    }
    
    void OnDrawGizmos()
    {
        if (isParrying)
        {
            DrawDebugGizmos();
        }
    }
    
    private void DrawDebugGizmos()
    {
        // 绘制嘴尖hitbox状态
        if (noseHitbox != null)
        {
            Gizmos.color = (noseHitbox.enabled) ? Color.red : Color.gray;
            Gizmos.DrawWireSphere(noseHitbox.transform.position, 0.3f);
        }
        
        // 绘制反击hitbox状态
        if (counterHitbox != null)
        {
            Gizmos.color = (counterHitbox.enabled) ? Color.magenta : Color.gray;
            Gizmos.DrawWireCube(counterHitbox.transform.position, Vector3.one * 0.5f);
        }
        
        // 绘制移动方向
        if (Application.isPlaying && currentSpeed > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, GetMouseDirection() * freeMovementSpeed * 0.5f);
        }
        
        // 格挡状态指示
        if (isParrying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
        
        // 反击状态指示
        if (isCounterAttacking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
        
        // 待反击指示（显示冲刺方向）
        if (pendingCounterAttack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, GetMouseDirection() * counterDashDistance);
        }
    }
}