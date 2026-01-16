using UnityEngine;

/// <summary>
/// 敌人AI控制器 - 控制非玩家鱼的行为
/// 简单AI：每拍攻击，方向有一拍延迟（第一拍决定方向，第二拍攻击）
/// </summary>
public class EnemyAI : MonoBehaviour, IBeatListener
{
    [Header("AI Settings")]
    [SerializeField] private BaseFish controlledFish;
    [SerializeField] private Transform playerTarget;
    
    [Header("Attack Pattern")]
    [SerializeField] private bool attackEveryBeat = true;
    [SerializeField] private int attackCooldownBeats = 0; // 攻击冷却（拍数）
    private int currentCooldown = 0;
    
    [Header("Direction Delay")]
    [SerializeField] private Vector3 pendingAttackDirection; // 待执行的攻击方向
    [SerializeField] private bool hasPendingAttack = false; // 是否有待执行的攻击
    
    // 用于朝向控制
    private Vector3 targetDirection;
    
    void Awake()
    {
        if (controlledFish == null)
        {
            controlledFish = GetComponent<BaseFish>();
        }
    }
    
    void Start()
    {
        // 注册到节拍系统
        if (Conductor.Instance != null)
        {
            Conductor.Instance.RegisterListener(this);
        }
        
        // 查找玩家
        FindPlayer();
    }
    
    void OnDestroy()
    {
        if (Conductor.Instance != null)
        {
            Conductor.Instance.UnregisterListener(this);
        }
    }
    
    void Update()
    {
        // 更新朝向（朝向目标方向）
        UpdateFacing();
    }
    
    /// <summary>
    /// 查找玩家目标
    /// </summary>
    public void FindPlayer()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.GetCurrentFish() != null)
        {
            playerTarget = PlayerController.Instance.GetCurrentFish().transform;
        }
        else
        {
            // 备用：查找标记为玩家的鱼
            BaseFish[] allFish = FindObjectsOfType<BaseFish>();
            foreach (var fish in allFish)
            {
                if (fish.IsPlayer)
                {
                    playerTarget = fish.transform;
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// 设置玩家目标
    /// </summary>
    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }
    
    /// <summary>
    /// 节拍回调
    /// </summary>
    public void OnBeat(int beatCount)
    {
        if (controlledFish == null || playerTarget == null) return;
        
        // 处理冷却
        if (currentCooldown > 0)
        {
            currentCooldown--;
            return;
        }
        
        // 执行AI逻辑
        ProcessAI();
    }
    
    /// <summary>
    /// 处理AI逻辑 - 一拍延迟攻击
    /// </summary>
    private void ProcessAI()
    {
        if (hasPendingAttack)
        {
            // 第二拍：执行攻击
            ExecuteAttack();
            hasPendingAttack = false;
            
            // 同时准备下一次攻击方向
            if (attackEveryBeat)
            {
                PrepareAttack();
            }
        }
        else
        {
            // 第一拍：准备攻击方向
            PrepareAttack();
        }
    }
    
    /// <summary>
    /// 准备攻击 - 记录当前玩家方向
    /// </summary>
    private void PrepareAttack()
    {
        if (playerTarget == null) return;
        
        pendingAttackDirection = (playerTarget.position - transform.position).normalized;
        targetDirection = pendingAttackDirection;
        hasPendingAttack = true;
        
        Debug.Log($"Enemy {gameObject.name} preparing attack towards player");
    }
    
    /// <summary>
    /// 执行攻击
    /// </summary>
    private void ExecuteAttack()
    {
        if (controlledFish == null) return;
        
        // 根据角色类型执行不同的攻击
        ExecuteAttackByType();
        
        // 设置冷却
        currentCooldown = attackCooldownBeats;
        
        Debug.Log($"Enemy {gameObject.name} executed attack");
    }
    
    /// <summary>
    /// 根据角色类型执行攻击
    /// </summary>
    private void ExecuteAttackByType()
    {
        switch (controlledFish)
        {
            case Piranha piranha:
                ExecutePiranhaAttack(piranha);
                break;
            case Pufferfish pufferfish:
                ExecutePufferfishAttack(pufferfish);
                break;
            case Tuna tuna:
                ExecuteTunaAttack(tuna);
                break;
            case Swordfish swordfish:
                ExecuteSwordfishAttack(swordfish);
                break;
            default:
                // 默认攻击行为
                controlledFish.OnRhythmInput();
                break;
        }
    }
    
    /// <summary>
    /// 食人鱼攻击
    /// </summary>
    private void ExecutePiranhaAttack(Piranha piranha)
    {
        piranha.OnRhythmInput();
    }
    
    /// <summary>
    /// 刺豚攻击
    /// </summary>
    private void ExecutePufferfishAttack(Pufferfish pufferfish)
    {
        pufferfish.OnRhythmInput();
    }
    
    /// <summary>
    /// 金枪鱼攻击
    /// </summary>
    private void ExecuteTunaAttack(Tuna tuna)
    {
        tuna.OnRhythmInput();
    }
    
    /// <summary>
    /// 剑鱼攻击
    /// </summary>
    private void ExecuteSwordfishAttack(Swordfish swordfish)
    {
        swordfish.OnRhythmInput();
    }
    
    /// <summary>
    /// 更新朝向 - 朝向目标方向
    /// </summary>
    private void UpdateFacing()
    {
        if (controlledFish == null) return;
        
        // 如果有待执行的攻击，朝向攻击方向
        // 否则朝向玩家
        Vector3 faceDirection = hasPendingAttack ? pendingAttackDirection : GetDirectionToPlayer();
        
        if (faceDirection.magnitude < 0.1f) return;
        
        // 更新朝向（类似BaseFish的逻辑）
        UpdateFishFacing(faceDirection);
    }
    
    /// <summary>
    /// 获取朝向玩家的方向
    /// </summary>
    private Vector3 GetDirectionToPlayer()
    {
        if (playerTarget == null) return Vector3.right;
        return (playerTarget.position - transform.position).normalized;
    }
    
    /// <summary>
    /// 更新鱼的朝向
    /// </summary>
    private void UpdateFishFacing(Vector3 direction)
    {
        SpriteRenderer sr = controlledFish.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        
        bool shouldFaceRight = direction.x >= 0;
        sr.flipX = !shouldFaceRight;
        
        // 计算旋转角度
        float targetAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        targetAngle = Mathf.Clamp(targetAngle, -90f, 90f);
        
        if (!shouldFaceRight)
        {
            targetAngle = -targetAngle;
        }
        
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
    
    /// <summary>
    /// 获取攻击方向（供角色脚本使用）
    /// </summary>
    public Vector3 GetAttackDirection()
    {
        return pendingAttackDirection;
    }
    
    /// <summary>
    /// 重置AI状态
    /// </summary>
    public void ResetAI()
    {
        hasPendingAttack = false;
        currentCooldown = 0;
        FindPlayer();
    }
}
