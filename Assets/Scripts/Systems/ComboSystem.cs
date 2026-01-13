using UnityEngine;

/// <summary>
/// 连击系统 - 管理连续击杀计数
/// </summary>
public class ComboSystem : MonoBehaviour, IBeatListener
{
    private static ComboSystem instance;
    public static ComboSystem Instance => instance;
    
    [Header("Combo Settings")]
    [SerializeField] private int currentCombo = 0;
    [SerializeField] private int beatsInCurrentMeasure = 0;
    [SerializeField] private int killsInCurrentMeasure = 0;
    [SerializeField] private int beatsPerMeasure = 4;
    
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
    
    void Start()
    {
        if (Conductor.Instance != null)
        {
            Conductor.Instance.RegisterListener(this);
        }
    }
    
    void OnDestroy()
    {
        if (Conductor.Instance != null)
        {
            Conductor.Instance.UnregisterListener(this);
        }
    }
    
    public void OnBeat(int beatCount)
    {
        beatsInCurrentMeasure++;
        
        // 检查是否完成一个小节
        if (beatsInCurrentMeasure >= beatsPerMeasure)
        {
            CheckMeasureCombo();
            ResetMeasure();
        }
    }
    
    /// <summary>
    /// 敌人被击杀时调用
    /// </summary>
    public void OnEnemyKilled()
    {
        killsInCurrentMeasure++;
        Debug.Log($"Kill in measure: {killsInCurrentMeasure}");
    }
    
    /// <summary>
    /// 检查小节连击
    /// </summary>
    private void CheckMeasureCombo()
    {
        if (killsInCurrentMeasure > 0)
        {
            currentCombo++;
            Debug.Log($"Combo: {currentCombo}");
        }
        else
        {
            // 没有击杀，重置连击
            if (currentCombo > 0)
            {
                Debug.Log($"Combo broken! Final combo: {currentCombo}");
                currentCombo = 0;
            }
        }
    }
    
    /// <summary>
    /// 重置小节计数
    /// </summary>
    private void ResetMeasure()
    {
        beatsInCurrentMeasure = 0;
        killsInCurrentMeasure = 0;
    }
    
    /// <summary>
    /// 错拍时中断连击
    /// </summary>
    public void OnMissedBeat()
    {
        if (currentCombo > 0)
        {
            Debug.Log($"Combo broken by missed beat! Final combo: {currentCombo}");
            currentCombo = 0;
        }
    }
    
    public int CurrentCombo => currentCombo;
}