using UnityEngine;

public static class TempPlayerInfo
{
    public static string Name { get; set; }
    public static int CharacterSelection { get; set; }
    
    // 添加一個重置方法，需要時清除信息
    public static void Reset()
    {
        Name = null;
        CharacterSelection = 1;
    }
}