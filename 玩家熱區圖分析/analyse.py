import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import matplotlib.image as mpimg
import matplotlib.cm as cm
import numpy as np
import time

# ===== 參數設定 =====
image_path = "場館平面圖.png"
csv_path = "player_positions_20250420_191027.csv"
group_threshold = 0.03
min_duration = 5
min_radius = 5
max_radius = 30
time_unit = 30  # 每幾秒為一單位

# 場館邊界（世界座標）
left, right = -267.7, 169.8
bottom, top = -121.8, 19.9

# ===== 轉換世界座標為像素座標 =====
def world_to_pixel(x, z, img_width, img_height):
    px = int((x - left) / (right - left) * img_width)
    pz = int((1 - (z - bottom) / (top - bottom)) * img_height)
    return px, pz

# ===== 載入資料與圖片 =====
df = pd.read_csv(csv_path)
bg_img = mpimg.imread(image_path)
img_height, img_width = bg_img.shape[:2]

# 預處理
df["x_round"] = (df["位置X"] / group_threshold).round() * group_threshold
df["z_round"] = (df["位置Z"] / group_threshold).round() * group_threshold

# ===== 產生自訂顏色列表 =====
players = sorted(df["玩家ID"].unique())
num_players = len(players)
base_cmap = cm.get_cmap("inferno")
colors = [base_cmap(i / num_players) for i in range(num_players)]

# 玩家顏色對應字典
player_color_dict = {player: colors[i] for i, player in enumerate(players)}

# 數字ID對應到完整玩家ID
number_to_player = {}
for pid in players:
    if pid.startswith("Player_"):
        try:
            num = int(pid.split("_")[1])
            number_to_player[num] = pid
        except:
            continue

# ===== 畫圖 function =====
def plot_players(selected_players):
    fig, ax = plt.subplots(figsize=(24, 10))
    ax.imshow(bg_img)
    
    for player in selected_players:
        if player not in player_color_dict:
            continue

        color = player_color_dict[player][:3]
        player_df = df[df["玩家ID"] == player].copy()
        grouped = player_df.groupby(["x_round", "z_round"]).size().reset_index(name="total_time")
        grouped = grouped[grouped["total_time"] > min_duration].copy()

        grouped["radius"] = grouped["total_time"].apply(
            lambda t: np.interp(min(t / time_unit, 20), [1, 20], [min_radius, max_radius])
        )
        grouped["world_x"] = grouped["x_round"]
        grouped["world_z"] = grouped["z_round"]
        grouped["pixel"] = grouped.apply(
            lambda row: world_to_pixel(row["world_x"], row["world_z"], img_width, img_height), axis=1
        )
        grouped = grouped.sort_values(by="total_time")

        prev_px, prev_pz = None, None
        prev_in_bounds = False

        for idx, row in enumerate(grouped.itertuples(), start=1):
            x, z = row.world_x, row.world_z

            if not (left <= x <= right and bottom <= z <= top):
                prev_px, prev_pz = None, None
                prev_in_bounds = False
                continue

            px, pz = row.pixel
            radius = row.radius

            circ = patches.Circle(
                (px, pz),
                radius=radius,
                facecolor=color,
                edgecolor='black',
                linewidth=1.2,
                alpha=0.9
            )
            ax.add_patch(circ)

            ax.text(px, pz, str(idx), color='white', fontsize=radius * 0.8, ha='center', va='center')

            if prev_px is not None and prev_in_bounds:
                ax.plot([prev_px, px], [prev_pz, pz], color=color, linewidth=0.7)

            prev_px, prev_pz = px, pz
            prev_in_bounds = True

    ax.axis('off')
    plt.tight_layout()
    plt.show(block=False)

# ===== 全部玩家 =====
plot_players(players)

# ===== 可輸入玩家 ID 個別顯示，exit退出 =====
while True:
    if not plt.get_fignums():
        break

    user_input = input("請輸入要查詢的玩家編號（只輸入數字，例如 1）: ").strip()
    if user_input == "exit":
        break

    try:
        selected_num = int(user_input)
        selected_id = number_to_player.get(selected_num)
        if selected_id:
            print(f"顯示玩家 {selected_id} 的熱區圖...")
            plot_players([selected_id])
        else:
            print(f"找不到玩家編號 {selected_num}，請重新輸入！")
    except ValueError:
        print("請輸入有效的數字（如 1, 2, 3）")

    time.sleep(0.5)
