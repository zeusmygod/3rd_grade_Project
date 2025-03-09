using UnityEngine;

public class PlayerDataExporter : MonoBehaviour
{
    private string playerId;
    private static int activePlayerCount = 0;
    
    private void Start()
    {
        playerId = gameObject.name;
        activePlayerCount++;
        Debug.Log($"【位置記錄系統】初始化玩家 {playerId} 的數據導出器。目前活躍玩家數：{activePlayerCount}");
    }
    
    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(playerId))
        {
            activePlayerCount--;
            
            // 如果這是最後一個玩家，清理追蹤器
            if (activePlayerCount <= 0 && PlayerPositionTracker.Instance != null)
            {
                PlayerPositionTracker.Instance.Cleanup();
            }
            
            Debug.Log($"【位置記錄系統】玩家 {playerId} 已離開。剩餘活躍玩家數：{activePlayerCount}");
        }
    }
    
    private void OnApplicationQuit()
    {
        // 不需要特別處理，PlayerPositionTracker會自動處理數據保存
    }
} 