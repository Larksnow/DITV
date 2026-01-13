using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI鼠标光标 - 适用于Canvas下的UI组件
/// </summary>
public class UICursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Image cursorImage; // UI Image组件
    [SerializeField] private float cursorScale = 1f;
    [SerializeField] private Color cursorColor = Color.white;
    [SerializeField] private bool showCursor = true;
    
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // 玩家角色
    
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera uiCamera;
    
    void Start()
    {
        InitializeCursor();
        
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
    /// 初始化光标
    /// </summary>
    private void InitializeCursor()
    {
        // 获取RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("UICursor需要RectTransform组件！");
            return;
        }
        
        // 获取Image组件
        if (cursorImage == null)
        {
            cursorImage = GetComponent<Image>();
        }
        
        // 如果没有Image组件，添加一个
        if (cursorImage == null)
        {
            cursorImage = gameObject.AddComponent<Image>();
            CreateDefaultCursorSprite();
        }
        
        // 设置颜色
        cursorImage.color = cursorColor;
        
        // 设置大小
        rectTransform.localScale = Vector3.one * cursorScale;
        
        // 获取Canvas信息
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            uiCamera = parentCanvas.worldCamera;
            if (uiCamera == null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null; // Screen Space Overlay不需要摄像机
            }
            else if (uiCamera == null)
            {
                uiCamera = Camera.main; // 使用主摄像机作为默认
            }
        }
    }
    
    /// <summary>
    /// 创建默认光标精灵
    /// </summary>
    private void CreateDefaultCursorSprite()
    {
        // 创建一个简单的圆形纹理
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 4f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // 创建空心圆
                    if (distance >= radius - 6f)
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
        
        // 创建精灵
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        cursorImage.sprite = sprite;
        
        // 设置默认大小
        rectTransform.sizeDelta = new Vector2(32, 32);
    }
    
    /// <summary>
    /// 更新光标位置
    /// </summary>
    private void UpdateCursorPosition()
    {
        if (rectTransform == null || parentCanvas == null) return;
        
        Vector2 screenPosition = Input.mousePosition;
        
        // 根据Canvas的渲染模式转换坐标
        Vector2 localPosition;
        
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Screen Space Overlay模式
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                null,
                out localPosition
            );
        }
        else
        {
            // Screen Space Camera 或 World Space模式
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                uiCamera,
                out localPosition
            );
        }
        
        // 设置位置
        rectTransform.localPosition = localPosition;
        
        // 根据距离玩家的远近调整光标大小
        UpdateCursorScale();
    }
    
    /// <summary>
    /// 更新光标缩放
    /// </summary>
    private void UpdateCursorScale()
    {
        if (playerTransform == null) return;
        
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // 计算距离
        float distance = Vector3.Distance(mouseWorldPos, playerTransform.position);
        
        // 根据距离调整缩放
        float scaleMultiplier = Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(distance / 8f));
        rectTransform.localScale = Vector3.one * cursorScale * scaleMultiplier;
    }
    
    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        
        // 使用主摄像机转换到世界坐标
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mouseScreenPos.z = mainCam.transform.position.z * -1;
            return mainCam.ScreenToWorldPoint(mouseScreenPos);
        }
        
        return Vector3.zero;
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
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 设置光标颜色
    /// </summary>
    public void SetCursorColor(Color color)
    {
        cursorColor = color;
        if (cursorImage != null)
        {
            cursorImage.color = color;
        }
    }
    
    /// <summary>
    /// 设置光标缩放
    /// </summary>
    public void SetCursorScale(float scale)
    {
        cursorScale = scale;
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * scale;
        }
    }
}