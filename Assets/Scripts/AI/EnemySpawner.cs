using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人生成器 - 管理敌人的生成和对象池
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject piranhaPrefab;
    [SerializeField] private GameObject pufferfishPrefab;
    [SerializeField] private GameObject tunaPrefab;
    [SerializeField] private GameObject swordfishPrefab;
    
    [Header("Pool Tools")]
    [SerializeField] private PoolTool piranhaPool;
    [SerializeField] private PoolTool pufferfishPool;
    [SerializeField] private PoolTool tunaPool;
    [SerializeField] private PoolTool swordfishPool;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 8f; // 生成半径
    [SerializeField] private float minDistanceFromPlayer = 3f; // 与玩家最小距离
    [SerializeField] private int maxEnemies = 10; // 最大敌人数量
    
    [Header("Auto Spawn")]
    [SerializeField] private bool autoSpawn = false;
    [SerializeField] private float spawnInterval = 5f; // 自动生成间隔（秒）
    private float spawnTimer = 0f;
    
    // 当前活跃的敌人列表
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    // 单例
    private static EnemySpawner instance;
    public static EnemySpawner Instance => instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // 清理已销毁的敌人引用
        CleanupDestroyedEnemies();
        
        // 自动生成
        if (autoSpawn && activeEnemies.Count < maxEnemies)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnRandomEnemy();
                spawnTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// 生成随机类型的敌人
    /// </summary>
    public GameObject SpawnRandomEnemy()
    {
        FishType randomType = (FishType)Random.Range(0, 4);
        return SpawnEnemy(randomType);
    }
    
    /// <summary>
    /// 生成指定类型的敌人
    /// </summary>
    public GameObject SpawnEnemy(FishType fishType)
    {
        return SpawnEnemy(fishType, GetRandomSpawnPosition());
    }
    
    /// <summary>
    /// 在指定位置生成敌人
    /// </summary>
    public GameObject SpawnEnemy(FishType fishType, Vector3 position)
    {
        if (activeEnemies.Count >= maxEnemies)
        {
            Debug.LogWarning("Max enemies reached!");
            return null;
        }
        
        GameObject enemy = GetEnemyFromPool(fishType);
        if (enemy == null)
        {
            Debug.LogError($"Failed to spawn enemy of type {fishType}");
            return null;
        }
        
        // 设置位置
        enemy.transform.position = position;
        
        // 确保是敌人（非玩家）并设置targetLayer
        BaseFish fish = enemy.GetComponent<BaseFish>();
        if (fish != null)
        {
            fish.SetAsPlayer(false);
            fish.SetTargetLayer(LayerMask.GetMask("Player")); // 敌人攻击玩家
        }
        
        // 添加或获取EnemyAI组件
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai == null)
        {
            ai = enemy.AddComponent<EnemyAI>();
        }
        ai.ResetAI();
        
        // 添加到活跃列表
        activeEnemies.Add(enemy);
        
        Debug.Log($"Spawned {fishType} enemy at {position}");
        return enemy;
    }
    
    /// <summary>
    /// 从对象池获取敌人
    /// </summary>
    private GameObject GetEnemyFromPool(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Piranha:
                return piranhaPool != null ? piranhaPool.GetObjectFromPool() : InstantiateFallback(piranhaPrefab);
            case FishType.Pufferfish:
                return pufferfishPool != null ? pufferfishPool.GetObjectFromPool() : InstantiateFallback(pufferfishPrefab);
            case FishType.Tuna:
                return tunaPool != null ? tunaPool.GetObjectFromPool() : InstantiateFallback(tunaPrefab);
            case FishType.Swordfish:
                return swordfishPool != null ? swordfishPool.GetObjectFromPool() : InstantiateFallback(swordfishPrefab);
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 备用实例化（没有对象池时）
    /// </summary>
    private GameObject InstantiateFallback(GameObject prefab)
    {
        if (prefab == null) return null;
        return Instantiate(prefab);
    }
    
    /// <summary>
    /// 回收敌人到对象池
    /// </summary>
    public void DespawnEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        activeEnemies.Remove(enemy);
        
        // 尝试回收到对象池
        BaseFish fish = enemy.GetComponent<BaseFish>();
        if (fish != null)
        {
            switch (fish)
            {
                case Piranha _ when piranhaPool != null:
                    piranhaPool.ReleaseObjectToPool(enemy);
                    return;
                case Pufferfish _ when pufferfishPool != null:
                    pufferfishPool.ReleaseObjectToPool(enemy);
                    return;
                case Tuna _ when tunaPool != null:
                    tunaPool.ReleaseObjectToPool(enemy);
                    return;
                case Swordfish _ when swordfishPool != null:
                    swordfishPool.ReleaseObjectToPool(enemy);
                    return;
            }
        }
        
        // 没有对象池则销毁
        Destroy(enemy);
    }
    
    /// <summary>
    /// 获取随机生成位置
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 playerPos = Vector3.zero;
        if (PlayerController.Instance != null && PlayerController.Instance.GetCurrentFish() != null)
        {
            playerPos = PlayerController.Instance.GetCurrentFish().transform.position;
        }
        
        // 在圆环内随机位置（距离玩家minDistanceFromPlayer到spawnRadius之间）
        for (int i = 0; i < 10; i++) // 最多尝试10次
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistanceFromPlayer, spawnRadius);
            
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );
            
            // 检查是否在屏幕内
            if (IsPositionOnScreen(spawnPos))
            {
                return spawnPos;
            }
        }
        
        // 备用：屏幕边缘
        return GetScreenEdgePosition();
    }
    
    /// <summary>
    /// 检查位置是否在屏幕内
    /// </summary>
    private bool IsPositionOnScreen(Vector3 worldPos)
    {
        if (Camera.main == null) return true;
        
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);
        return viewportPos.x >= 0.1f && viewportPos.x <= 0.9f &&
               viewportPos.y >= 0.1f && viewportPos.y <= 0.9f;
    }
    
    /// <summary>
    /// 获取屏幕边缘位置
    /// </summary>
    private Vector3 GetScreenEdgePosition()
    {
        if (Camera.main == null) return Vector3.zero;
        
        float edge = Random.Range(0, 4);
        Vector3 viewportPos;
        
        switch ((int)edge)
        {
            case 0: // 上
                viewportPos = new Vector3(Random.Range(0.1f, 0.9f), 0.95f, 10f);
                break;
            case 1: // 下
                viewportPos = new Vector3(Random.Range(0.1f, 0.9f), 0.05f, 10f);
                break;
            case 2: // 左
                viewportPos = new Vector3(0.05f, Random.Range(0.1f, 0.9f), 10f);
                break;
            default: // 右
                viewportPos = new Vector3(0.95f, Random.Range(0.1f, 0.9f), 10f);
                break;
        }
        
        return Camera.main.ViewportToWorldPoint(viewportPos);
    }
    
    /// <summary>
    /// 清理已销毁的敌人引用
    /// </summary>
    private void CleanupDestroyedEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }
    
    /// <summary>
    /// 清除所有敌人
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                DespawnEnemy(enemy);
            }
        }
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// 获取当前敌人数量
    /// </summary>
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// 获取所有活跃敌人
    /// </summary>
    public List<GameObject> GetActiveEnemies()
    {
        return new List<GameObject>(activeEnemies);
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制生成范围
        Vector3 center = Vector3.zero;
        if (Application.isPlaying && PlayerController.Instance != null && PlayerController.Instance.GetCurrentFish() != null)
        {
            center = PlayerController.Instance.GetCurrentFish().transform.position;
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, spawnRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, minDistanceFromPlayer);
    }
}
