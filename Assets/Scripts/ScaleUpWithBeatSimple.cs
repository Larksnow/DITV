using UnityEngine;
using System.Collections;

public class ScaleUpWithBeatSimple : MonoBehaviour, IBeatListener
{
    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.2f; // 缩放倍数
    [SerializeField] private float scaleDuration = 0.1f; // 缩放持续时间
    
    [Header("Options")]
    [SerializeField] private bool scaleOnEveryBeat = true; // 是否每拍都缩放
    [SerializeField] private int beatInterval = 1; // 间隔多少拍缩放一次
    
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
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
        // 停止协程
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
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
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        scaleCoroutine = StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        Vector3 targetScale = originalScale * scaleMultiplier;
        float halfDuration = scaleDuration * 0.5f;
        
        // 放大阶段
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            // 使用EaseOut效果
            t = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
        
        // 缩小阶段
        elapsed = 0f;
        startScale = targetScale;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            // 使用EaseOut效果
            t = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(startScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
        scaleCoroutine = null;
    }

    // 手动触发缩放效果（用于测试）
    [ContextMenu("Test Scale Effect")]
    public void TestScaleEffect()
    {
        DoScaleEffect();
    }
}