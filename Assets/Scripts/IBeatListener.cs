using UnityEngine;

public interface IBeatListener
{
    // beatCount is the number of beats that have passed since the start of the song
    void OnBeat(int beatCount);
}