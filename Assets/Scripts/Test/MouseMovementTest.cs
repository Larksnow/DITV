using UnityEngine;

/// <summary>
/// 鼠标移动测试 - 用于测试鼠标指引移动系统
/// </summary>
public class MouseMovementTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private FishType testFishType = FishType.Piranha;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private bool autoStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode switchFishKey = KeyCode.Tab;
    
    private BaseFish currentTestFish;
    private int currentFishIndex = 0;
    private FishType[] allFishTypes = { FishType.Piranha, FishType.Pufferfish, FishType.Tuna, FishType.Swordfish };
    
    void Start()
    {
        if (autoStart)
        {
            StartTest();
        }
    }
    
    void Update()
    {
        HandleTestInput();
        
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    /// <summary>
    /// 开始测试
    /// </summary>
    public void StartTest()
    {
        // 确保有必要的管理器
        EnsureManagers();
        
        // 创建测试角色
        CreateTestFish(testFishType);
        
        Debug.Log($"Mouse Movement Test Started with {testFishType}");
        Debug.Log("Controls:");
        Debug.Log("- Mouse: Move to guide fish movement");
        Debug.Log("- Space: Rhythm action");
        Debug.Log("- Left Shift: Secondary action (for some fish)");
        Debug.Log("- Tab: Switch fish type");
    }
    
    /// <summary>
    /// 确保必要的管理器存在
    /// </summary>
    private void EnsureManagers()
    {
        // 确保有Conductor
        if (Conductor.Instance == null)
        {
            GameObject conductorObj = new GameObject("Conductor");
            Conductor conductor = conductorObj.AddComponent<Conductor>();
            
            // 添加一个AudioSource用于测试
            AudioSource audioSource = conductorObj.AddComponent<AudioSource>();
            conductor.musicSource = audioSource;
            
            // 创建一个简单的测试音频剪辑
            CreateTestAudioClip(audioSource);
        }
        
        // 确保有PlayerController
        if (PlayerController.Instance == null)
        {
            GameObject playerControllerObj = new GameObject("PlayerController");
            playerControllerObj.AddComponent<PlayerController>();
        }
        
        // 确保有CharacterManager
        if (CharacterManager.Instance == null)
        {
            GameObject characterManagerObj = new GameObject("CharacterManager");
            characterManagerObj.AddComponent<CharacterManager>();
        }
        
        // 确保有GameManager
        if (GameManager.Instance == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
        }
    }
    
    /// <summary>
    /// 创建测试音频剪辑
    /// </summary>
    private void CreateTestAudioClip(AudioSource audioSource)
    {
        // 创建一个简单的测试音频（静音，只用于节拍计算）
        AudioClip testClip = AudioClip.Create("TestClip", 44100 * 60, 1, 44100, false); // 1分钟的静音
        audioSource.clip = testClip;
        audioSource.loop = true;
        audioSource.volume = 0f; // 静音
    }
    
    /// <summary>
    /// 创建测试角色
    /// </summary>
    private void CreateTestFish(FishType fishType)
    {
        // 销毁之前的测试角色
        if (currentTestFish != null)
        {
            Destroy(currentTestFish.gameObject);
        }
        
        // 创建新角色
        if (CharacterManager.Instance != null)
        {
            currentTestFish = CharacterManager.Instance.CreateFish(fishType, spawnPosition, true);
        }
        else
        {
            // 手动创建简单的测试角色
            GameObject fishObj = new GameObject($"Test_{fishType}");
            fishObj.transform.position = spawnPosition;
            
            // 添加视觉组件
            SpriteRenderer renderer = fishObj.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTestSprite(fishType);
            
            // 添加碰撞体
            fishObj.AddComponent<CircleCollider2D>();
            
            // 添加对应的角色脚本
            switch (fishType)
            {
                case FishType.Piranha:
                    currentTestFish = fishObj.AddComponent<Piranha>();
                    break;
                case FishType.Pufferfish:
                    currentTestFish = fishObj.AddComponent<Pufferfish>();
                    break;
                case FishType.Tuna:
                    currentTestFish = fishObj.AddComponent<Tuna>();
                    break;
                case FishType.Swordfish:
                    currentTestFish = fishObj.AddComponent<Swordfish>();
                    break;
            }
        }
        
        // 设置为玩家控制
        if (PlayerController.Instance != null && currentTestFish != null)
        {
            PlayerController.Instance.SetCurrentFish(currentTestFish);
        }
    }
    
    /// <summary>
    /// 创建测试精灵
    /// </summary>
    private Sprite CreateTestSprite(FishType fishType)
    {
        // 创建简单的彩色方块作为测试精灵
        Texture2D texture = new Texture2D(32, 32);
        Color fishColor = GetFishColor(fishType);
        
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = fishColor;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 获取角色颜色
    /// </summary>
    private Color GetFishColor(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Piranha: return Color.red;
            case FishType.Pufferfish: return Color.yellow;
            case FishType.Tuna: return Color.blue;
            case FishType.Swordfish: return Color.green;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 处理测试输入
    /// </summary>
    private void HandleTestInput()
    {
        // 切换角色类型
        if (Input.GetKeyDown(switchFishKey))
        {
            SwitchFishType();
        }
        
        // 重新开始测试
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartTest();
        }
    }
    
    /// <summary>
    /// 切换角色类型
    /// </summary>
    private void SwitchFishType()
    {
        currentFishIndex = (currentFishIndex + 1) % allFishTypes.Length;
        FishType newFishType = allFishTypes[currentFishIndex];
        
        CreateTestFish(newFishType);
        
        Debug.Log($"Switched to {newFishType}");
    }
    
    /// <summary>
    /// 显示调试信息
    /// </summary>
    private void ShowDebugInfo()
    {
        if (currentTestFish != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            float distance = Vector3.Distance(currentTestFish.transform.position, mousePos);
            
            // 在屏幕上显示信息
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Current Fish: {allFishTypes[currentFishIndex]}");
            GUILayout.Label($"Fish Position: {currentTestFish.transform.position}");
            GUILayout.Label($"Mouse Position: {mousePos}");
            GUILayout.Label($"Distance: {distance:F2}");
            GUILayout.Label($"Can Free Move: {currentTestFish.GetComponent<IMovementController>()?.CanFreeMove}");
            GUILayout.Label($"Is Punished: {currentTestFish.IsPunished}");
            GUILayout.EndArea();
        }
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            // 这个方法会被ShowDebugInfo调用
        }
    }
}