using UnityEngine;

/// <summary>
/// 游戏管理器 - 管理游戏状态和流程
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;
    
    [Header("Game Settings")]
    [SerializeField] private int enemiesKilled = 0;
    [SerializeField] private float gameTimer = 0f;
    
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
    /// 敌人被击杀时调用
    /// </summary>
    public void OnEnemyKilled(BaseFish enemy)
    {
        enemiesKilled++;
        Debug.Log($"Enemy killed! Total: {enemiesKilled}");
        
        // 这里可以添加生成新敌人的逻辑
    }
    
    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Game Restart!");
        // 重新加载场景或重置游戏状态
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}