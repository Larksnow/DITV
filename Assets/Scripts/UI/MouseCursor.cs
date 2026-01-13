using UnityEngine;

/// <summary>
/// 鼠标光标视觉反馈 - 显示鼠标在游戏世界中的位置
/// </summary>
public class MouseCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private GameObject cursorPrefab; // 光标预制体
    [SerializeField] private float cursorScale = 0.5f;
    [SerializeField] private Color cursorColor = Color.white;
    [SerializeField] private bool showCursor = true;
    
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // 玩家角色
    
    private GameObject cursorInstance;
    private SpriteRenderer cursorRenderer;
    
    void Start()
    {
        CreateCursor();
        
        // 隐藏系统鼠标光标
        Cursor.visible = false;
    }
    
    void Update()
    {
        if (showCursor)
        {
            UpdateCursorPosition();
        }
    }
    
    void OnDestroy()
    {
        // 恢复系统鼠标光标
        Cursor.visible = true;
    }
    
    /// <summary>
    /// 创建光标对象
    /// </summary>
    private void CreateCursor()
    {
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
        }
        else
        {
            // 创建简单的圆形光标
            cursorInstance = new GameObject("MouseCursor");
            cursorInstance.AddComponent<SpriteRenderer>();
            
            // 创建一个简单的圆形精灵
            CreateCircleSprite();
        }
        
        cursorRenderer = cursorInstance.GetComponent<SpriteRenderer>();
        if (cursorRenderer != null)
        {
            cursorRenderer.color = cursorColor;
            cursorRenderer.sortingOrder = 100; // 确保在最上层
        }
        
        cursorInstance.transform.localScale = Vector3.one * cursorScale;
    }
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    private void CreateCircleSprite()
    {
        // 创建一个简单的圆形纹理
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // 内部透明，边缘有颜色
                    if (distance >= radius - 4f)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        cursorRenderer.sprite = sprite;
    }
    
    /// <summary>
    /// 更新光标位置
    /// </summary>
    private void UpdateCursorPosition()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        if (cursorInstance != null)
        {
            cursorInstance.transform.position = mouseWorldPos;
            
            // 根据距离玩家的远近调整光标大小
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(mouseWorldPos, playerTransform.position);
                float scaleMultiplier = Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(distance / 5f));
                cursorInstance.transform.localScale = Vector3.one * cursorScale * scaleMultiplier;
            }
        }
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
    /// 设置玩家角色引用
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
    
    /// <summary>
    /// 设置光标可见性
    /// </summary>
    public void SetCursorVisible(bool visible)
    {
        showCursor = visible;
        if (cursorInstance != null)
        {
            cursorInstance.SetActive(visible);
        }
    }
}