"""画面ごとの背景。

どの画面比率でも崩れないよう、背景は 2560x1440 で描き、画面の中央に置く（UI は CanvasScaler の Expand で 1920x1080 以上に広がる）。
中央の 1920x1080 が基準の見た目（安全領域）で、その外側は空・ビル街・床のグリッドをそのまま続けて描く。
16:9 では外側は見えず、21:9 では左右、16:10 では上下の外側が見える。
上下の警告色のバーは画面の端に貼り付く必要があるので、背景には描かず UI の部品（ui_hazard_bar）にしてある。

どの画面も同じ世界（夜のネオン街とシンセウェーブの床）で、地平線の高さと太陽の大きさだけを画面に合わせて変える。
"""
import random
from palette import *
from svgkit import *

# 書き出す大きさ
W, H = 2560, 1440
# 基準（16:9）の画面が収まる範囲。座標はこの範囲の左上を原点に指定する
SAFE_W, SAFE_H = 1920, 1080
OX, OY = (W - SAFE_W) / 2, (H - SAFE_H) / 2
VANISH_X = W / 2

# 地平線の高さ（基準の画面の上端から）。
# バトル：キャラの足元の高さ。タイトル：ロゴの真後ろに太陽が来る高さ
BATTLE_FLOOR_Y = 400
TITLE_FLOOR_Y = 430
# マップ：部屋の経路を空に浮かべるので、地平線は低め
MAP_FLOOR_Y = 760
# デッキ編集・カード購入：画面の大半をパネルが覆うので、地平線をかなり低くして太陽は小さく、空は静かに
DECK_EDIT_FLOOR_Y = 900
SHOP_FLOOR_Y = 880


def _skyline(rnd, floor_y, max_height=190):
    # 地平線沿いのビル群（画像の端から端まで）。窓はネオンの点
    out = []
    x = -20
    while x < W:
        w = rnd.randint(60, 140)
        h = rnd.randint(60, max_height)
        out.append(f'<rect x="{x}" y="{floor_y - h}" width="{w}" height="{h}" fill="{BG_NAVY}"/>')
        out.append(f'<rect x="{x}" y="{floor_y - h}" width="{w}" height="3" fill="{rnd.choice([CYAN, MAGENTA, VIOLET])}" opacity="0.8"/>')
        for wy in range(int(floor_y - h + 14), int(floor_y - 8), 16):
            for wx in range(x + 8, x + w - 8, 14):
                if rnd.random() < 0.28:
                    out.append(f'<rect x="{wx}" y="{wy}" width="6" height="6" fill="{rnd.choice([CYAN, MAGENTA, YELLOW])}" opacity="0.75"/>')
        x += w + rnd.randint(0, 20)
    return "".join(out)


def _sun(floor_y, radius):
    # 中央の太陽（シンセウェーブ風）。下側を横縞で抜く
    top = floor_y - radius * 0.8
    stripes = "".join(
        f'<rect x="{VANISH_X - radius - 20}" y="{y:.0f}" width="{radius * 2 + 40}" height="{4 + i * 2}" fill="black"/>'
        for i, y in enumerate(range(int(top), int(floor_y), 22))
    )
    return (
        f'<mask id="sunMask"><rect width="{W}" height="{H}" fill="white"/>{stripes}</mask>'
        f'<g mask="url(#sunMask)"><circle cx="{VANISH_X}" cy="{floor_y - 10}" r="{radius}" fill="url(#sun)" filter="url(#sunGlow)"/></g>'
    )


def _floor_grid(floor_y):
    # 遠近のついた床グリッド（画像の端まで）
    lines = []
    for i in range(-32, 33):
        x_far = VANISH_X + i * 40
        x_near = VANISH_X + i * 260
        lines.append(f"M {x_far} {floor_y} L {x_near} {H}")
    y = floor_y
    step = 10.0
    while y < H:
        lines.append(f"M 0 {y:.1f} L {W} {y:.1f}")
        step *= 1.28
        y += step
    d = " ".join(lines)
    return (
        f'<path d="{d}" stroke="{MAGENTA}" stroke-width="5" opacity="0.22" fill="none" filter="url(#gridGlow)"/>'
        f'<path d="{d}" stroke="{MAGENTA}" stroke-width="2" opacity="0.55" fill="none"/>'
    )


def _stickers(rnd, spots):
    # ポップな図形を空に散らす（spots は基準の画面の座標）
    out = []
    for sx, sy in spots:
        x, y = sx + OX, sy + OY
        kind = rnd.choice(["tri", "ring", "cross", "zig", "dots"])
        c = rnd.choice([CYAN, MAGENTA, YELLOW, LIME])
        if kind == "tri":
            out.append(f'<path d="M {x} {y - 18} L {x + 18} {y + 14} L {x - 18} {y + 14} Z" fill="none" stroke="{c}" stroke-width="4" stroke-linejoin="round" transform="rotate({rnd.randint(0, 90)} {x} {y})"/>')
        elif kind == "ring":
            out.append(circle(x, y, 14, fill="none", stroke=c, stroke_width=4))
        elif kind == "cross":
            out.append(f'<path d="M {x - 14} {y} L {x + 14} {y} M {x} {y - 14} L {x} {y + 14}" stroke="{c}" stroke-width="5" stroke-linecap="square"/>')
        elif kind == "zig":
            out.append(f'<path d="M {x - 30} {y} l 12 -12 l 12 12 l 12 -12 l 12 12 l 12 -12" fill="none" stroke="{c}" stroke-width="4" stroke-linejoin="miter"/>')
        else:
            out.append("".join(circle(x + i * 12, y, 3.5, fill=c) for i in range(4)))
    return "".join(out)


def _scene(seed, safe_floor_y, sun_radius, sticker_spots, skyline_max):
    rnd = random.Random(seed)
    floor_y = safe_floor_y + OY
    defs = (
        linear("sky", [(0, BG_DEEP), (0.6, BG_NAVY), (1, BG_PURPLE)])
        + linear("floor", [(0, BG_PURPLE), (0.25, BG_DEEP), (1, BG_DEEP)])
        + linear("sun", [(0, YELLOW), (0.55, ORANGE), (1, MAGENTA)])
        + neon("sunGlow", 18, MAGENTA)
        + blur("gridGlow", 4)
    )
    body = [f'<rect width="{W}" height="{floor_y}" fill="url(#sky)"/>']

    # 星（小さなネオンの点）。外側の領域にも散らす
    body.append("".join(
        circle(rnd.randint(0, W), rnd.randint(20, max(60, int(floor_y) - 120)), rnd.choice([1.5, 2, 2.5]), fill=rnd.choice([WHITE, CYAN, MAGENTA]), opacity=0.8)
        for _ in range(160)
    ))
    # 網点（左上・右上）。画像の角から広げ、横長・縦長の画面で基準範囲の外が見えても途中で切れないようにする
    body.append(halftone(0, 0, OX + 520, OY + 330, 22, 7, MAGENTA, 0.35, origin=(0, 0)))
    body.append(halftone(W - OX - 520, 0, OX + 520, OY + 330, 22, 7, CYAN, 0.3, origin=(1, 0)))

    body.append(_sun(floor_y, sun_radius))
    body.append(_stickers(rnd, sticker_spots))
    body.append(_skyline(rnd, floor_y, skyline_max))

    # 床
    body.append(f'<rect x="0" y="{floor_y}" width="{W}" height="{H - floor_y}" fill="url(#floor)"/>')
    body.append(_floor_grid(floor_y))
    # 地平線のネオンライン
    body.append(f'<rect x="0" y="{floor_y - 3}" width="{W}" height="6" fill="{CYAN}" filter="url(#gridGlow)"/>')
    body.append(f'<rect x="0" y="{floor_y - 1.5}" width="{W}" height="3" fill="{WHITE}"/>')

    return document(W, H, "".join(body), defs)


def battle_background():
    spots = [(180, 150), (560, 90), (820, 200), (1150, 110), (1330, 230), (1560, 80), (1800, 190), (380, 300), (1040, 320)]
    return _scene(11, BATTLE_FLOOR_Y, 190, spots, 190)


def title_background():
    # ロゴの後ろに大きな太陽。メニューは床の上に並ぶので、床を広く取る
    spots = [(160, 140), (430, 260), (300, 380), (1600, 120), (1770, 300), (1480, 360), (700, 90), (1240, 80)]
    return _scene(23, TITLE_FLOOR_Y, 250, spots, 150)


def map_background():
    # 地平線を低くして空を広く取り、部屋（ステージ）の経路が夜空のネットワークに浮かぶようにする
    spots = [(140, 120), (420, 90), (1500, 100), (1780, 150), (980, 70)]
    return _scene(37, MAP_FLOOR_Y, 150, spots, 120)



def deck_edit_background():
    # パネルの後ろで邪魔をしないよう、飾りは画面の端の方だけに置く
    spots = [(90, 150), (1830, 170), (120, 760), (1820, 700)]
    return _scene(41, DECK_EDIT_FLOOR_Y, 120, spots, 110)


def shop_background():
    spots = [(100, 170), (1810, 330), (90, 700), (1840, 760), (420, 60)]
    return _scene(53, SHOP_FLOOR_Y, 160, spots, 140)


# ---------------- 画面の上端・下端に貼り付くバー（UI の部品） ----------------
# 表示 420x30 の2倍。左右 200 ずつが警告色のストライプで、9-slice で真ん中だけを画面幅まで伸ばす。
# 下端に使うときは、上下を反転して置く（シアンの線が画面の内側に来るように）
BAR_W, BAR_H = 840, 60


def hazard_bar():
    rnd = random.Random(5)
    body = [f'<rect x="0" y="0" width="{BAR_W}" height="{BAR_H}" fill="{INK}"/>',
            f'<rect x="0" y="{BAR_H - 6}" width="{BAR_W}" height="6" fill="{CYAN}"/>']
    for side in (0, BAR_W - 400):
        cid = f"hz{side}"
        body.append(f'<clipPath id="{cid}"><rect x="{side}" y="0" width="400" height="{BAR_H - 6}"/></clipPath>')
        body.append(f'<g clip-path="url(#{cid})"><rect x="{side}" y="0" width="400" height="{BAR_H - 6}" fill="{YELLOW}"/>'
                    + "".join(f'<path d="M {side + i} {BAR_H} L {side + i + 28} 0 L {side + i + 56} 0 L {side + i + 28} {BAR_H} Z" fill="{INK}"/>' for i in range(-60, 460, 56))
                    + "</g>")
    # 内側の端に、グリッチでずれた帯を少しだけ
    for x0 in (410, BAR_W - 410 - 200):
        for _ in range(2):
            gx = x0 + rnd.randint(0, 120)
            body.append(f'<rect x="{gx}" y="{rnd.choice([14, 26, 38])}" width="{rnd.randint(40, 80)}" height="6" fill="{rnd.choice([MAGENTA, CYAN, YELLOW])}" opacity="0.8"/>')
    return document(BAR_W, BAR_H, "".join(body))


# ドット1粒の大きさ（書き出す画像の画素数）。1 にするとドット感なしのなめらかな絵になる
PIXEL = 4


def build():
    render(battle_background(), "Backgrounds/bg_battle.png", pixel=PIXEL)
    render(title_background(), "Backgrounds/bg_title.png", pixel=PIXEL)
    render(map_background(), "Backgrounds/bg_map.png", pixel=PIXEL)
    render(deck_edit_background(), "Backgrounds/bg_deck_edit.png", pixel=PIXEL)
    render(shop_background(), "Backgrounds/bg_shop.png", pixel=PIXEL)
    render(hazard_bar(), "UI/ui_hazard_bar.png", pixel=6)
