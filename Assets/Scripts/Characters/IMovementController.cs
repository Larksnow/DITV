using UnityEngine;

/// <summary>
/// 移动控制接口 - 处理非物理驱动的移动
/// </summary>
public interface IMovementController
{
    bool CanFreeMove { get; } // 是否允许自由移动
    bool IsMoving { get; } // 是否正在移动中
    
    /// <summary>
    /// 设置目标位置，使用插值移动
    /// </summary>
    void SetTargetPosition(Vector3 target, float duration);
    
    /// <summary>
    /// 瞬间移动到指定位置
    /// </summary>
    void MoveInstantly(Vector3 position);
    
    /// <summary>
    /// 停止当前移动
    /// </summary>
    void StopMovement();
}