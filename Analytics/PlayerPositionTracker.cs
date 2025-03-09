using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class PlayerPositionTracker : MonoBehaviour
{
    [SerializeField] private float recordInterval = 5f; // 記錄間隔，預設5秒
    [SerializeField] private string customSavePath = "TrackData"; // 預設為專案目錄下的TrackData文件夾

    // 玩家在場館的停留時間記錄
    private class VenueTimeRecord
    {
        public float totalTime;
        public float lastRecordTime;
        public string currentVenue;
    }

    private static PlayerPositionTracker instance;
    public static PlayerPositionTracker Instance
    {
        get
        {
            if (instance == null)
            {
                var existing = FindObjectOfType<PlayerPositionTracker>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("PlayerPositionTracker");
                    instance = go.AddComponent<PlayerPositionTracker>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    // 使用字典儲存每個時間點的所有玩家位置
    private List<(float timestamp, Dictionary<string, Vector3> positions)> positionRecords = 
        new List<(float timestamp, Dictionary<string, Vector3> positions)>();
    
    // 用於追蹤玩家的唯一ID
    private Dictionary<int, string> playerUniqueIds = new Dictionary<int, string>();
    private int nextPlayerId = 1;

    // 用於追蹤玩家在各場館的停留時間
    private Dictionary<string, Dictionary<string, float>> playerVenueTimes = new Dictionary<string, Dictionary<string, float>>();
    private Dictionary<string, VenueTimeRecord> playerCurrentVenues = new Dictionary<string, VenueTimeRecord>();

    private Coroutine recordRoutine;
    private bool isQuitting = false;
    private string actualSavePath;
    private string csvFilePath;
    private string venueTimeCsvFilePath;

    // 獲取實際儲存路徑
    private string GetActualSavePath()
    {
        if (string.IsNullOrEmpty(customSavePath))
        {
            return Application.persistentDataPath;
        }

        // 如果是相對路徑，則相對於遊戲目錄
        if (!Path.IsPathRooted(customSavePath))
        {
            return Path.Combine(Application.dataPath, "..", customSavePath);
        }

        return customSavePath;
    }

    // 確保目錄存在
    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
                Debug.Log($"【位置記錄系統】成功創建目錄：{path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"【位置記錄系統】創建目錄失敗：{e.Message}");
                // 如果創建失敗，使用預設路徑
                actualSavePath = Application.persistentDataPath;
            }
        }
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 設置並驗證儲存路徑
        actualSavePath = GetActualSavePath();
        EnsureDirectoryExists(actualSavePath);
        
        // 設定CSV檔案路徑
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        csvFilePath = Path.Combine(actualSavePath, $"player_positions_{timestamp}.csv");
        venueTimeCsvFilePath = Path.Combine(actualSavePath, $"venue_times_{timestamp}.csv");
        
        StartRecording();
        
        Debug.Log($"【位置記錄系統】數據檔案將儲存在：\n{csvFilePath}");
        
        // 在遊戲開始時創建CSV檔案並寫入表頭
        try
        {
            File.WriteAllText(csvFilePath, "記錄時間,日期時間,玩家ID,位置X,位置Y,位置Z,當前場館\n");
            Debug.Log($"【位置記錄系統】成功創建CSV檔案");
        }
        catch (Exception e)
        {
            Debug.LogError($"【位置記錄系統】創建CSV檔案失敗：{e.Message}");
        }
    }

    // 為玩家生成唯一ID
    private string GenerateUniquePlayerId(GameObject player)
    {
        // 忽略非玩家物件
        if (player.name.Contains("EventSystem") || player.name.Contains("PlayerPositionTracker"))
        {
            return null;
        }

        int instanceId = player.GetInstanceID();
        if (!playerUniqueIds.ContainsKey(instanceId))
        {
            string uniqueId = $"Player_{nextPlayerId++}";
            playerUniqueIds[instanceId] = uniqueId;
            Debug.Log($"【位置記錄系統】為玩家 {player.name} (InstanceID: {instanceId}) 分配ID: {uniqueId}");
        }
        return playerUniqueIds[instanceId];
    }

    private void StartRecording()
    {
        if (recordRoutine == null)
        {
            recordRoutine = StartCoroutine(RecordPositionsRoutine());
        }
    }

    private void StopRecording()
    {
        if (recordRoutine != null)
        {
            StopCoroutine(recordRoutine);
            recordRoutine = null;
        }
    }
    
    // 每隔指定時間記錄所有玩家位置的協程
    private IEnumerator RecordPositionsRoutine()
    {
        while (!isQuitting)
        {
            RecordAllPlayerPositions();
            yield return new WaitForSeconds(recordInterval);
        }
    }
    
    // 記錄所有玩家的位置
    private void RecordAllPlayerPositions()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Dictionary<string, Vector3> currentPositions = new Dictionary<string, Vector3>();
        float currentTime = Time.time;
        string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // 獲取所有場館區域
        VenueArea[] venues = FindObjectsOfType<VenueArea>();
        
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                string playerId = GenerateUniquePlayerId(player);
                
                if (playerId != null) // 只記錄有效的玩家ID
                {
                    Vector3 position = player.transform.position;
                    currentPositions[playerId] = position;

                    // 檢查玩家所在的場館
                    string currentVenue = "None";
                    foreach (var venue in venues)
                    {
                        if (venue.IsPositionInside(position))
                        {
                            currentVenue = venue.venueName;
                            break;
                        }
                    }

                    // 更新場館停留時間
                    UpdateVenueTime(playerId, currentVenue, currentTime);

                    // 直接寫入CSV檔案
                    try
                    {
                        string line = $"{currentTime:F2},{currentDateTime},{playerId},{position.x:F2},{position.y:F2},{position.z:F2},{currentVenue}\n";
                        File.AppendAllText(csvFilePath, line);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"【位置記錄系統】寫入數據失敗：{e.Message}");
                    }
                }
            }
        }
        
        // 保存這個時間點的所有位置記錄
        positionRecords.Add((currentTime, currentPositions));
        Debug.Log($"【位置記錄系統】記錄完成 時間：{currentDateTime} 玩家數：{currentPositions.Count}，玩家ID列表：{string.Join(", ", currentPositions.Keys)}");
    }

    // 更新場館停留時間
    private void UpdateVenueTime(string playerId, string venueName, float currentTime)
    {
        // 確保玩家有時間記錄
        if (!playerCurrentVenues.ContainsKey(playerId))
        {
            playerCurrentVenues[playerId] = new VenueTimeRecord
            {
                lastRecordTime = currentTime,
                currentVenue = venueName
            };
        }

        var record = playerCurrentVenues[playerId];
        
        // 如果玩家換了場館或仍在同一場館，更新時間
        if (record.currentVenue != venueName || record.currentVenue == venueName)
        {
            // 計算在上一個場館的停留時間
            float timeSpent = currentTime - record.lastRecordTime;
            
            // 將時間加入到總計中
            if (!string.IsNullOrEmpty(record.currentVenue) && record.currentVenue != "None")
            {
                if (!playerVenueTimes.ContainsKey(playerId))
                {
                    playerVenueTimes[playerId] = new Dictionary<string, float>();
                }
                
                if (!playerVenueTimes[playerId].ContainsKey(record.currentVenue))
                {
                    playerVenueTimes[playerId][record.currentVenue] = 0f;
                }
                
                playerVenueTimes[playerId][record.currentVenue] += timeSpent;
            }
        }

        // 更新當前記錄
        record.currentVenue = venueName;
        record.lastRecordTime = currentTime;
    }

    // 導出場館停留時間統計
    private void ExportVenueTimes()
    {
        if (playerVenueTimes.Count == 0)
        {
            Debug.LogWarning("【位置記錄系統】沒有場館停留時間記錄");
            return;
        }

        StringBuilder csv = new StringBuilder();
        
        // 獲取所有唯一的場館名稱
        HashSet<string> allVenues = new HashSet<string>();
        foreach (var playerData in playerVenueTimes.Values)
        {
            foreach (var venueName in playerData.Keys)
            {
                allVenues.Add(venueName);
            }
        }

        // 寫入表頭
        csv.Append("玩家ID");
        foreach (var venue in allVenues.OrderBy(v => v))
        {
            csv.Append($",{venue}停留時間(秒)");
        }
        csv.AppendLine();

        // 寫入每個玩家的數據
        foreach (var playerEntry in playerVenueTimes)
        {
            csv.Append(playerEntry.Key);
            foreach (var venue in allVenues.OrderBy(v => v))
            {
                float time = playerEntry.Value.ContainsKey(venue) ? playerEntry.Value[venue] : 0f;
                csv.Append($",{time:F2}");
            }
            csv.AppendLine();
        }

        try
        {
            File.WriteAllText(venueTimeCsvFilePath, csv.ToString());
            Debug.Log($"【位置記錄系統】場館停留時間統計已保存到：\n{venueTimeCsvFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"【位置記錄系統】保存場館停留時間統計失敗：{e.Message}");
        }
    }

    // 清理方法
    public void Cleanup()
    {
        StopRecording();
        playerUniqueIds.Clear();
        positionRecords.Clear();
        
        if (instance == this)
        {
            instance = null;
        }
        
        Destroy(gameObject);
    }

    // 設置自定義儲存路徑的公開方法
    public void SetCustomSavePath(string newPath)
    {
        customSavePath = newPath;
        actualSavePath = GetActualSavePath();
        EnsureDirectoryExists(actualSavePath);
        Debug.Log($"【位置記錄系統】儲存路徑已更新為：\n{actualSavePath}");
    }

    // 獲取當前儲存路徑的方法
    public static string GetSavePath()
    {
        return Instance.actualSavePath;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        StopRecording();
        ExportVenueTimes(); // 在退出時導出場館停留時間統計
        Debug.Log($"【位置記錄系統】系統關閉，數據已保存到：\n{csvFilePath}");
        
        if (instance == this)
        {
            instance = null;
        }
    }
} 