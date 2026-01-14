using UnityEngine;
using System.Collections;

/// <summary>
/// 插值移动控制器 - 用于节拍同步的平滑移动（如冲刺、位移等）
/// 注意：自由移动和朝向由BaseFish处理
/// </summary>
public class InterpolationMovement : MonoBehaviour, IMovementController
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveDuration;
    private float moveTimer;
    private bool isMoving = false;
    private Coroutine moveCoroutine;
    
    // IMovementController接口实现
    public bool CanFreeMove => true; // 由BaseFish控制
    public bool IsMoving => isMoving;
    
    void Start()
    {
        targetPosition = transform.position;
    }
    
    /// <summary>
    /// 设置目标位置并开始移动
    /// </summary>
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
    
    /// <summary>
    /// 瞬间移动到指定位置
    /// </summary>
    public void MoveInstantly(Vector3 position)
    {
        StopMovement();
        transform.position = position;
        targetPosition = position;
    }
    
    /// <summary>
    /// 停止当前移动
    /// </summary>
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
    /// 移动协程 - 平滑插值移动
    /// </summary>
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
    
    // 以下方法保留接口兼容性，但实际由BaseFish处理
    public void HandleFreeMovement() { }
    public void SetCanFreeMove(bool canMove) { }
}