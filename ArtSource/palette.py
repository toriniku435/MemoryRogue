"""全アートで共有する色。ここを書き換えて build.py を実行すれば、全画像の配色がまとめて変わる。

方向性: サイバー × ポップ。濃紺〜黒の地に、ネオンのシアン/マゼンタ/イエロー/ライムを平塗りで置き、
太い黒フチとずらした影（ポップ）、グリッドや斜めカットの角（サイバー）で味付けする。
"""

# 地（暗い色）
BG_DEEP = "#0A0A1E"
BG_NAVY = "#141438"
BG_PURPLE = "#2A1450"
PANEL = "#10102A"
PANEL_LIGHT = "#1E1E4A"

# ネオン
CYAN = "#00F0FF"
CYAN_DEEP = "#0096C8"
MAGENTA = "#FF2EC4"
MAGENTA_DEEP = "#B8008A"
YELLOW = "#FFE600"
LIME = "#7CFF4F"
VIOLET = "#8A5CFF"
ORANGE = "#FF7A1A"

WHITE = "#FFFFFF"
# 暗い地の上の文字色（Unity 側の文字色もこれに合わせている）
TEXT = "#E8FBFF"

# 縁取り用の黒。真っ黒ではなく地の濃紺に寄せる
INK = "#07071A"
