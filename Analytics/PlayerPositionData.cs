using System;
using UnityEngine;

[Serializable]
public class PlayerPositionRecord
{
    public string playerId;
    public Vector3 position;
    public float timestamp;
    public string dateTimeString;

    public PlayerPositionRecord(string id, Vector3 pos, float time)
    {
        playerId = id;
        position = pos;
        timestamp = time;
        dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public string ToCsvString()
    {
        return $"{playerId},{timestamp:F2},{dateTimeString},{position.x:F2},{position.y:F2},{position.z:F2}";
    }
} 