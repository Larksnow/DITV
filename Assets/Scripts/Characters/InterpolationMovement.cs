using UnityEngine;
using System.Collections;

/// <summary>
/// 插值移动控制器 - 用于节拍同步的平滑移动
/// </summary>
public class InterpolationMovement : MonoBehaviour, IMovementController
{
    [Header("Movement Settings")]
    [SerializeField] private bool canFreeMove = true;
    [SerializeField] private float defaultMoveSpeed = 5f;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveDuration;
    private float moveTimer;
    private bool isMoving = false;
    private Coroutine moveCoroutine;
    
    public bool CanFreeMove => canFreeMove;
    public bool IsMoving => isMoving;
    
    void Start()
    {
        targetPosition = transform.position;
    }
    
    public void SetTargetPosition(Vector3 target, float duration)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        startPosition = transform.position;
        targetPosition = target;
        moveDuration = duration;
        moveTimer = 0f;
        isMoving = true;
        
        moveCoroutine = StartCoroutine(MoveToTarget());
    }
    
    public void MoveInstantly(Vector3 position)
    {
        StopMovement();
        transform.position = position;
        targetPosition = position;
    }
    
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        isMoving = false;
    }
    
    /// <summary>
    /// 自由移动 - 鼠标指引移动
    /// </summary>
    public void HandleFreeMovement()
    {
        if (!canFreeMove || isMoving) return;
        
        HandleMouseGuidedMovement();
    }
    
    /// <summary>
    /// 鼠标指引移动 - 鱼会自动朝向鼠标位置移动
    /// </summary>
    private void HandleMouseGuidedMovement()
    {
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // 计算鱼到鼠标的距离
        float distanceToMouse = Vector3.Distance(transform.position, mouseWorldPos);
        
        // 设置一个最小距离阈值，避免鱼在鼠标附近抖动
        float minDistance = 0.5f;
        
        if (distanceToMouse > minDistance)
        {
            // 计算移动方向
            Vector3 moveDirection = (mouseWorldPos - transform.position).normalized;
            
            // 计算移动距离
            float moveDistance = defaultMoveSpeed * Time.deltaTime;
            
            // 限制移动距离，避免超过鼠标位置
            moveDistance = Mathf.Min(moveDistance, distanceToMouse - minDistance);
            
            // 执行移动
            Vector3 newPosition = transform.position + moveDirection * moveDistance;
            
            // 限制在屏幕边界内
            newPosition = ClampToScreenBounds(newPosition);
            
            transform.position = newPosition;
            
            // 更新鱼的朝向（可选）
            UpdateFishRotation(moveDirection);
        }
    }
    
    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z * -1; // 设置Z轴距离
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
    
    /// <summary>
    /// 更新鱼的朝向
    /// </summary>
    private void UpdateFishRotation(Vector3 moveDirection)
    {
        if (moveDirection.magnitude > 0.1f)
        {
            // 计算角度
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            
            // 平滑旋转
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }
    
    /// <summary>
    /// 限制位置在屏幕边界内
    /// </summary>
    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            // 获取屏幕边界
            Vector3 screenBounds = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.transform.position.z));
            
            // 添加一些边距
            float margin = 1f;
            position.x = Mathf.Clamp(position.x, -screenBounds.x + margin, screenBounds.x - margin);
            position.y = Mathf.Clamp(position.y, -screenBounds.y + margin, screenBounds.y - margin);
        }
        
        return position;
    }
    
    private IEnumerator MoveToTarget()
    {
        while (moveTimer < moveDuration)
        {
            moveTimer += Time.deltaTime;
            float t = moveTimer / moveDuration;
            
            // 使用平滑插值
            t = Mathf.SmoothStep(0f, 1f, t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
        isMoving = false;
        moveCoroutine = null;
    }
    
    /// <summary>
    /// 设置是否允许自由移动（用于不同角色的限制）
    /// </summary>
    public void SetCanFreeMove(bool canMove)
    {
        canFreeMove = canMove;
    }
}