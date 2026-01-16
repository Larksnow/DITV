using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Conductor : MonoBehaviour
{
    public static Conductor Instance;

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public float bpm = 120f;
    public float firstBeatOffset = 0f; // 调整第一拍的起始时间(秒)
    public float inputThreshold = 0.15f; // 判定区间：前后0.15秒都算准

    [Header("Debug Info")]
    public float songPosition;
    public float songPositionInBeats;
    public int completedLoops = 0;

    // 每一拍触发一次事件
    public UnityEvent<int> onBeatTriggered;

    private float secPerBeat;
    private int lastReportedBeat = 0;
    
    // 缓存所有实现了接口的物体，避免每帧Find
    private List<IBeatListener> listeners = new List<IBeatListener>();

    void Awake()
    {
        Instance = this;
        secPerBeat = 60f / bpm;
    }

    void Start()
    {
        // 游戏开始，播放音乐
        musicSource.Play(); 
    }

    void Update()
    {
        if (!musicSource.isPlaying) return;

        // 核心同步逻辑：利用 sample rate 计算精准时间
        songPosition = (float)(musicSource.timeSamples / (double)musicSource.clip.frequency);

        // 计算当前拍数
        songPositionInBeats = (songPosition - firstBeatOffset) / secPerBeat;

        // 节拍触发逻辑
        int currentBeatInt = Mathf.FloorToInt(songPositionInBeats);
        if (currentBeatInt > lastReportedBeat)
        {
            lastReportedBeat = currentBeatInt;
            
            // 1. 触发 UnityEvent (给简单的 Inspector 连线用)
            onBeatTriggered.Invoke(lastReportedBeat);

            // 2. 触发 Interface (给代码逻辑用)
            foreach(var listener in listeners)
            {
                if(listener != null) listener.OnBeat(lastReportedBeat);
            }
        }
    }

    // 注册监听者
    public void RegisterListener(IBeatListener listener)
    {
        if (!listeners.Contains(listener)) listeners.Add(listener);
    }

    public void UnregisterListener(IBeatListener listener)
    {
        if (listeners.Contains(listener)) listeners.Remove(listener);
    }
    
    /// <summary>
    /// 检查监听者是否已注册
    /// </summary>
    public bool IsListenerRegistered(IBeatListener listener)
    {
        return listeners.Contains(listener);
    }

    // --- 核心判定逻辑 ---
    
    // 返回 true 代表输入在拍子上 (Perfect/Good)
    public bool CheckInputTiming()
    {
        // 找到最近的整数拍 (比如当前是 4.9拍，最近就是第5拍；当前4.1拍，最近是第4拍)
        int closestBeat = Mathf.RoundToInt(songPositionInBeats);
        
        // 计算差距
        float timeDiff = Mathf.Abs(songPositionInBeats - closestBeat) * secPerBeat;

        return timeDiff <= inputThreshold;
    }
    
    // --- 拍数和时间转换 ---
    
    /// <summary>
    /// 将拍数转换为秒数
    /// </summary>
    public float BeatsToSeconds(float beats)
    {
        return beats * secPerBeat;
    }
    
    /// <summary>
    /// 将秒数转换为拍数
    /// </summary>
    public float SecondsToBeats(float seconds)
    {
        return seconds / secPerBeat;
    }
    
    /// <summary>
    /// 获取到下一拍的剩余时间（秒）
    /// </summary>
    public float GetTimeToNextBeat()
    {
        float currentBeat = songPositionInBeats;
        float nextBeat = Mathf.Ceil(currentBeat);
        float beatsRemaining = nextBeat - currentBeat;
        return beatsRemaining * secPerBeat;
    }
    
    /// <summary>
    /// 获取当前小节（4拍为一小节）
    /// </summary>
    public int GetCurrentMeasure()
    {
        return Mathf.FloorToInt(songPositionInBeats / 4f);
    }
    
    /// <summary>
    /// 获取当前在小节中的拍数（0-3）
    /// </summary>
    public int GetBeatInMeasure()
    {
        return Mathf.FloorToInt(songPositionInBeats) % 4;
    }
}