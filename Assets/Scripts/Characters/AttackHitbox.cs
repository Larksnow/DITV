using UnityEngine;
using System;

/// <summary>
/// 攻击判定框 - 挂在子物体hitbox上，转发碰撞事件给父物体
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    /// <summary>
    /// 碰撞事件回调
    /// </summary>
    public Action<Collider2D> OnHitTarget;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        OnHitTarget?.Invoke(other);
    }
}
