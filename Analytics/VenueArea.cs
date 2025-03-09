using UnityEngine;

public class VenueArea : MonoBehaviour
{
    [Tooltip("場館的唯一識別名稱")]
    public string venueName;
    
    private BoxCollider areaCollider;
    
    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider>();
        if (areaCollider == null)
        {
            areaCollider = gameObject.AddComponent<BoxCollider>();
            areaCollider.isTrigger = true;
        }
        
        if (string.IsNullOrEmpty(venueName))
        {
            venueName = gameObject.name;
            Debug.LogWarning($"【場館系統】場館 {gameObject.name} 未設置名稱，使用物件名稱作為場館名稱");
        }
    }

    // 檢查指定位置是否在場館範圍內
    public bool IsPositionInside(Vector3 position)
    {
        // 將世界座標轉換為本地座標
        Vector3 localPoint = transform.InverseTransformPoint(position);
        
        // 獲取碰撞器的範圍
        Vector3 halfExtents = areaCollider.size * 0.5f;
        
        // 檢查點是否在碰撞器範圍內
        return Mathf.Abs(localPoint.x) <= halfExtents.x &&
               Mathf.Abs(localPoint.y) <= halfExtents.y &&
               Mathf.Abs(localPoint.z) <= halfExtents.z;
    }
} 