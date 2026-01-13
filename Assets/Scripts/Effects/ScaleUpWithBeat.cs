using UnityEngine;
using DG.Tweening;

public class ScaleUpWithBeat : MonoBehaviour, IBeatListener
{
    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.2f; // 缩放倍数
    [SerializeField] private float scaleDuration = 0.1f; // 缩放持续时间
    [SerializeField] private Ease scaleEase = Ease.OutBack; // 缓动类型
    
    [Header("Options")]
    [SerializeField] private bool scaleOnEveryBeat = true; // 是否每拍都缩放
    [SerializeField] private int beatInterval = 1; // 间隔多少拍缩放一次（当scaleOnEveryBeat为false时）
    
    private Vector3 originalScale;
    private Tween currentTween;
    private int beatCounter = 0;

    void Start()
    {
        // 记录原始缩放
        originalScale = transform.localScale;
        
        // 注册到Conductor
        if (Conductor.Instance != null)
        {
            Conductor.Instance.RegisterListener(this);
        }
    }

    void OnDestroy()
    {
        // 清理DOTween
        if (currentTween != null)
        {
            currentTween.Kill();
        }
        
        // 取消注册
        if (Conductor.Instance != null)
        {
            Conductor.Instance.UnregisterListener(this);
        }
    }

    public void OnBeat(int beatCount)
    {
        beatCounter++;
        
        // 检查是否应该在这一拍缩放
        bool shouldScale = scaleOnEveryBeat || (beatCounter % beatInterval == 0);
        
        if (shouldScale)
        {
            DoScaleEffect();
        }
    }

    private void DoScaleEffect()
    {
        // 如果有正在进行的缩放动画，先停止
        if (currentTween != null)
        {
            currentTween.Kill();
        }
        
        // 重置到原始大小
        transform.localScale = originalScale;
        
        // 创建缩放动画：放大 -> 回到原始大小
        Vector3 targetScale = originalScale * scaleMultiplier;
        
        currentTween = transform.DOScale(targetScale, scaleDuration * 0.5f)
            .SetEase(scaleEase)
            .OnComplete(() =>
            {
                // 放大完成后，缩回原始大小
                currentTween = transform.DOScale(originalScale, scaleDuration * 0.5f)
                    .SetEase(Ease.OutQuad);
            });
    }

    // 手动触发缩放效果（用于测试或特殊情况）
    [ContextMenu("Test Scale Effect")]
    public void TestScaleEffect()
    {
        DoScaleEffect();
    }
}