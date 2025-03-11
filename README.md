使用步驟:
設置追蹤管理器：
1. 在 Hierarchy 視窗中，右鍵點擊 → Create Empty。
2. 將新創建的空物件命名為 "PositionTracker"。
3. 在 Inspector 視窗中點擊 "Add Component"。
4. 搜索並添加 "PlayerPositionTracker" 組件。
5. 可以調整 "Record Interval" 參數（預設為 5 秒）。
   
設置玩家物件：
1. 找到代表玩家的遊戲物件 (NetworkPlayer PF)。
2. 確保玩家物件的 標籤（Tag） 設置為 "Player"。
3. 在 Inspector 視窗中點擊 "Add Component"。
4. 搜索並添加 "PlayerDataExporter" 組件。
