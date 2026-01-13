using UnityEngine;

/// <summary>
/// 角色管理器 - 管理角色预制体和选择
/// </summary>
public class CharacterManager : MonoBehaviour
{
    private static CharacterManager instance;
    public static CharacterManager Instance => instance;
    
    [Header("Character Prefabs")]
    [SerializeField] private GameObject piranhaPrefab;
    [SerializeField] private GameObject pufferfishPrefab;
    [SerializeField] private GameObject tunaPrefab;
    [SerializeField] private GameObject swordfishPrefab;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 根据角色类型获取预制体
    /// </summary>
    public GameObject GetFishPrefab(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Piranha:
                return piranhaPrefab;
            case FishType.Pufferfish:
                return pufferfishPrefab;
            case FishType.Tuna:
                return tunaPrefab;
            case FishType.Swordfish:
                return swordfishPrefab;
            default:
                Debug.LogError($"Unknown fish type: {fishType}");
                return null;
        }
    }
    
    /// <summary>
    /// 创建角色实例
    /// </summary>
    public BaseFish CreateFish(FishType fishType, Vector3 position, bool isPlayer = false)
    {
        GameObject prefab = GetFishPrefab(fishType);
        if (prefab == null) return null;
        
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        BaseFish fish = instance.GetComponent<BaseFish>();
        
        if (fish != null)
        {
            fish.SetAsPlayer(isPlayer);
        }
        
        return fish;
    }
    
    /// <summary>
    /// 获取角色名称
    /// </summary>
    public string GetFishName(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Piranha:
                return "食人鱼";
            case FishType.Pufferfish:
                return "刺豚";
            case FishType.Tuna:
                return "金枪鱼";
            case FishType.Swordfish:
                return "剑鱼";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 获取角色描述
    /// </summary>
    public string GetFishDescription(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Piranha:
                return "快速攻击，攻击带位移，可自由移动";
            case FishType.Pufferfish:
                return "范围攻击，可连续充气，攻击后可位移";
            case FishType.Tuna:
                return "蓄力冲刺，高伤害，移动较慢";
            case FishType.Swordfish:
                return "高速移动，格挡反击，必须持续移动";
            default:
                return "";
        }
    }
}