using UnityEngine;
using System.Collections;

public class RhythmInput : MonoBehaviour, IBeatListener
{
    [Header("Settings")]
    public KeyCode actionKey = KeyCode.Space;
    public SpriteRenderer visualFeedback; // 用来变色测试
    
    [Header("State")]
    public bool isPunished = false; // 是否处于惩罚状态
    private int punishedBeat = -1; // 被惩罚的拍子编号

    void Start()
    {
        // 把自己注册进 Conductor
        Conductor.Instance.RegisterListener(this);
    }

    void OnDestroy()
    {
        if(Conductor.Instance != null) Conductor.Instance.UnregisterListener(this);
    }

    void Update()
    {
        // 检查是否应该解除惩罚
        CheckPunishmentRelease();

        // 如果处于惩罚状态，禁止输入
        if (isPunished)
        {
             // 视觉反馈：惩罚期间显示红色
             visualFeedback.color = Color.red; 
             return; 
        }
        else
        {
            visualFeedback.color = Color.white;
        }

        // 检测输入
        if (Input.GetKeyDown(actionKey))
        {
            // 询问 Conductor 是否踩在点上
            if (Conductor.Instance.CheckInputTiming())
            {
                HandleSuccess();
            }
            else
            {
                HandleFailure();
            }
        }
    }

    void CheckPunishmentRelease()
    {
        if (!isPunished) return;

        // 计算下一拍的开始时间（包括Early Window）
        int nextBeat = punishedBeat + 1;
        float nextBeatTime = nextBeat;
        float earlyWindowStart = nextBeatTime - (Conductor.Instance.inputThreshold / (60f / Conductor.Instance.bpm));
        
        // 如果当前时间已经进入下一拍的Early Window，解除惩罚
        if (Conductor.Instance.songPositionInBeats >= earlyWindowStart)
        {
            isPunished = false;
            punishedBeat = -1;
            Debug.Log($"Punishment Released - Entered next beat's early window at beat {Conductor.Instance.songPositionInBeats:F2}");
        }
    }

    void HandleSuccess()
    {
        Debug.Log("PERFECT! Action Triggered.");
        // TODO: 这里调用角色攻击/移动逻辑
        visualFeedback.color = Color.green; // 临时反馈
    }

    void HandleFailure()
    {
        Debug.Log("MISS! Punished until next beat's early window.");
        isPunished = true;
        
        // 记录被惩罚的拍子：当前最近的拍子
        punishedBeat = Mathf.RoundToInt(Conductor.Instance.songPositionInBeats);
        
        Debug.Log($"Punished on beat {punishedBeat}, will be released when entering beat {punishedBeat + 1}'s early window");
        
        visualFeedback.color = Color.red; // 立即显示红色
    }

    // 来自 IBeatListener 的接口实现
    public void OnBeat(int beatCount)
    {
        // 每一拍做一次 Scale Punch (果汁效果)
        transform.localScale = Vector3.one * 1.2f;
        // 简单的回弹动画可以用 DOTween 替代，这里为了无依赖先手动写
        StartCoroutine(ResetScale());

        // 注意：惩罚解除逻辑现在在Update中的CheckPunishmentRelease()处理
        // 这样可以更精确地在进入Early Window时解除惩罚
    }

    IEnumerator ResetScale()
    {
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}