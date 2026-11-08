"""UI パーツ。伸縮させるものは 9-slice 前提で角だけを作り込み、中央は単色にしている。

9-slice の枠幅は Unity 側のインポート設定（Assets/Scripts/Editor/ArtImportPostprocessor.cs の表）が持つ。
ここで角の大きさを変えたら、そちらの数字も合わせること。
画像はすべて表示サイズの2倍で書き出す（Image.pixelsPerUnitMultiplier = 2 で使う）
"""
from palette import *
from svgkit import *


def panel():
    # 汎用パネル：暗い地 + シアンの細線 + 角を斜めに切る
    w = h = 128
    body = chamfer(3, 3, w - 6, h - 6, 26, fill=PANEL, fill_opacity=0.92, stroke=CYAN, stroke_width=4)
    return document(w, h, body)


def memory_board():
    # 実行リストの受け皿：メモリモジュールの基板。接点ピンは別画像（ui_mem_pins）をタイルで敷く
    w = h = 128
    body = (
        chamfer(3, 3, w - 6, h - 6, 24, fill="#0E2A2E", fill_opacity=0.9, stroke=CYAN, stroke_width=4)
        + chamfer(14, 14, w - 28, h - 28, 16, fill="none", stroke=CYAN, stroke_width=1.5, opacity=0.5)
        # 角の配線とビア
        + f'<path d="M 14 44 L 14 30 L 30 14 L 44 14" fill="none" stroke="{LIME}" stroke-width="3" opacity="0.8"/>'
        + f'<path d="M 114 84 L 114 98 L 98 114 L 84 114" fill="none" stroke="{LIME}" stroke-width="3" opacity="0.8"/>'
        + circle(44, 14, 4, fill=LIME)
        + circle(84, 114, 4, fill=LIME)
    )
    return document(w, h, body)


def memory_pins():
    # メモリの金の接点1本分。Image.Type.Tiled で横に並べる
    w, h = 32, 40
    defs = linear("gold", [(0, "#FFF0A0"), (1, "#E0A020")])
    body = rrect(7, 2, 18, 36, 3, fill="url(#gold)", stroke=INK, stroke_width=2)
    return document(w, h, body, defs)


def slot():
    # 実行リストの1枠：カードを差し込むソケット。四隅のブラケットで「差し込み口」らしさを出す
    w = h = 96
    b = f'stroke="{CYAN}" stroke-width="5" fill="none" stroke-linecap="square"'
    body = (
        chamfer(4, 4, w - 8, h - 8, 14, fill=INK, fill_opacity=0.7, stroke=CYAN_DEEP, stroke_width=2)
        + f'<path d="M 6 26 L 6 6 L 26 6" {b}/><path d="M 70 6 L 90 6 L 90 26" {b}/>'
        + f'<path d="M 6 70 L 6 90 L 26 90" {b}/><path d="M 70 90 L 90 90 L 90 70" {b}/>'
    )
    return document(w, h, body)


# 主ボタンの形：右に傾いた平行四辺形。表示 180x108 の2倍で描く。
# 9-slice で縦に伸ばすと斜めの辺が途中で折れるので、高さは表示の高さぴったりにし、横だけ伸ばす前提
BUTTON_W, BUTTON_H = 360, 216
BUTTON_SLANT = 60


def _button_path(inset=0):
    w, h, s, i = BUTTON_W, BUTTON_H, BUTTON_SLANT, 6 + inset
    return f"M {i} {h - i} L {s + i} {i} L {w - i} {i} L {w - s - i} {h - i} Z"


def button_primary():
    # 主ボタン：半透明のシアンの面 + ネオンの縁 + 四隅のブラケット。背景が透けて見える
    w, h = BUTTON_W, BUTTON_H
    defs = neon("glow", 6, CYAN)
    b = f'stroke="{WHITE}" stroke-width="8" fill="none" stroke-linecap="square"'
    body = (
        f'<path d="{_button_path()}" fill="{CYAN}" fill-opacity="0.16"/>'
        + f'<path d="{_button_path()}" fill="none" stroke="{CYAN}" stroke-width="6" filter="url(#glow)"/>'
        + f'<path d="{_button_path(14)}" fill="none" stroke="{CYAN}" stroke-width="2" opacity="0.5"/>'
        # 四隅のブラケット（斜めの辺に沿わせる）
        + f'<path d="M 20 190 L 6 {h - 6} L 70 {h - 6}" {b}/>'
        + f'<path d="M {w - 20} 26 L {w - 6} 6 L {w - 70} 6" {b}/>'
        + f'<path d="M {BUTTON_SLANT + 6} 6 L {BUTTON_SLANT + 66} 6" {b}/>'
        + f'<path d="M {w - BUTTON_SLANT - 6} {h - 6} L {w - BUTTON_SLANT - 66} {h - 6}" {b}/>'
        # 右上の /// の刻み
        + "".join(f'<path d="M {x} 44 L {x + 12} 22 L {x + 22} 22 L {x + 10} 44 Z" fill="{MAGENTA}"/>' for x in (262, 282, 302))
    )
    return document(w, h, body, defs)


def button_outline():
    # カーソルを合わせたときに外へ広がる波紋の輪（白。色は Image.color で乗せる）
    return document(BUTTON_W, BUTTON_H, f'<path d="{_button_path()}" fill="none" stroke="{WHITE}" stroke-width="6"/>')


def button_mask():
    # 押したときの波紋をボタンの形で切り抜くためのマスク
    return document(BUTTON_W, BUTTON_H, f'<path d="{_button_path()}" fill="{WHITE}"/>')


def ripple_ring():
    # 押した位置から広がる円の波紋（白）
    w = h = 128
    return document(w, h, circle(64, 64, 56, fill="none", stroke=WHITE, stroke_width=8))


def button_round():
    # 小さなボタン（並べ替えの矢印など）
    w = h = 96
    body = hexagon(48, 48, 43, fill=INK, stroke=CYAN, stroke_width=5)
    return document(w, h, body)


def bar_background():
    w, h = 96, 48
    body = chamfer(2, 2, w - 4, h - 4, 10, fill=INK, stroke=CYAN, stroke_width=3)
    return document(w, h, body)


def bar_fill():
    w, h = 96, 48
    defs = linear("g", [(0, LIME), (1, CYAN)], x2=1, y2=0)
    body = chamfer(2, 2, w - 4, h - 4, 9, fill="url(#g)") + f'<rect x="10" y="9" width="{w - 20}" height="6" fill="{WHITE}" opacity="0.55"/>'
    return document(w, h, body, defs)


def stamina_gem():
    # スタミナ：六角形のエネルギーコア。数字を白で重ねるので、中心は暗くしてある
    w, h = 280, 260
    defs = neon("glow", 10, CYAN) + linear("rim", [(0, CYAN), (1, VIOLET)])
    body = (
        f'<g filter="url(#glow)">{hexagon(140, 130, 112, fill="url(#rim)", stroke=INK, stroke_width=6)}</g>'
        + hexagon(140, 130, 84, fill=INK, stroke=WHITE, stroke_width=3)
        + "".join(
            f'<path d="{bolt_path(x, y, 34)}" fill="{YELLOW}" stroke="{INK}" stroke-width="3"/>' for x, y in [(62, 70), (218, 190)]
        )
    )
    return document(w, h, body, defs)


def cost_gem():
    # カードのコスト：黒の六角形にマゼンタのフチ。白い数字を重ねる
    w = h = 144
    body = hexagon(72, 72, 64, fill=MAGENTA, stroke=INK, stroke_width=5) + hexagon(72, 72, 48, fill=INK)
    return document(w, h, body)


def badge():
    # 枚数などの数字を載せる菱形のバッジ
    w = h = 96
    body = f'<path d="M 48 4 L 92 48 L 48 92 L 4 48 Z" fill="{INK}" stroke="{CYAN}" stroke-width="6" stroke-linejoin="miter"/>'
    return document(w, h, body)


def status_pill():
    # 状態効果の札：暗い地 + 左端のマゼンタの帯
    w = h = 96
    body = (
        chamfer(3, 3, w - 6, h - 6, 12, fill=PANEL, fill_opacity=0.92, stroke=VIOLET, stroke_width=3)
        + f'<rect x="3" y="18" width="7" height="{h - 36}" fill="{MAGENTA}"/>'
    )
    return document(w, h, body)


def target_frame():
    # 対象選択できるユニットに重ねる照準。白で作り、色は Image.color で乗せる（通常時は透明）
    w = h = 192
    defs = blur("glow", 5)
    b = 'stroke="white" stroke-width="12" fill="none" stroke-linecap="square"'
    brackets = (
        f'<path d="M 12 60 L 12 12 L 60 12" {b}/><path d="M 132 12 L 180 12 L 180 60" {b}/>'
        f'<path d="M 12 132 L 12 180 L 60 180" {b}/><path d="M 180 132 L 180 180 L 132 180" {b}/>'
    )
    body = (
        f'<g filter="url(#glow)" opacity="0.9">{brackets}</g>'
        + brackets
        + chamfer(22, 22, w - 44, h - 44, 18, fill=WHITE, fill_opacity=0.1, stroke=WHITE, stroke_width=2, stroke_dasharray="8 8")
    )
    return document(w, h, body, defs)


def unit_shadow():
    # 足元の台座：ネオンの楕円リング
    w, h = 320, 96
    defs = blur("g", 5) + radial("disc", [(0, CYAN, 0.35), (1, CYAN, 0)])
    body = (
        ellipse(160, 48, 150, 40, fill="url(#disc)")
        + ellipse(160, 48, 130, 32, fill="none", stroke=CYAN, stroke_width=8, filter="url(#g)")
        + ellipse(160, 48, 130, 32, fill="none", stroke=WHITE, stroke_width=2.5)
    )
    return document(w, h, body, defs)


def deco_chevron():
    # 実行ボタンに添える >>> の飾り
    w, h = 160, 80
    return document(w, h, chevrons(12, 40, 56, 3, MAGENTA, gap=40, stroke=INK, stroke_width=4, stroke_linejoin="miter"))


def cursor_arrow():
    # 選択中を指すカーソル（右向きの三角。白で描き、色は Image.color で乗せる）
    w, h = 60, 72
    body = f'<polygon points="8,6 54,36 8,66" fill="{WHITE}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>'
    return document(w, h, body)


def circle_badge():
    # 丸いバッジ（デッキに入れられる枚数の上限「1」などを載せる。白で描き、色は Image.color で乗せる。数字は CommonText）
    w = h = 64
    return document(w, h, circle(32, 32, 26, fill=WHITE, stroke=INK, stroke_width=6))


def trash_icon():
    # ゴミ箱（デッキを空にするボタン）。ふた・取っ手・縦の切れ込みのある胴。白で描き、色は Image.color で乗せる
    w = h = 96
    body = (
        f'<rect x="38" y="12" width="20" height="12" fill="none" stroke="{WHITE}" stroke-width="6"/>'
        + f'<rect x="14" y="22" width="68" height="12" fill="{WHITE}" stroke="{INK}" stroke-width="4"/>'
        + f'<polygon points="22,38 74,38 68,86 28,86" fill="{WHITE}" stroke="{INK}" stroke-width="4" stroke-linejoin="miter"/>'
        + "".join(f'<rect x="{x}" y="46" width="6" height="32" fill="{INK}"/>' for x in (34, 45, 56))
    )
    return document(w, h, body)


def write_scan():
    # 命令を書き込むときにカードの上を走る走査線（横に引き伸ばして使う）
    w, h = 64, 64
    defs = linear("g", [(0, CYAN, 0), (0.45, CYAN, 0.55), (0.5, WHITE, 1), (0.55, CYAN, 0.55), (1, CYAN, 0)])
    return document(w, h, f'<rect width="{w}" height="{h}" fill="url(#g)"/>', defs)


def glow_frame():
    # 書き込み完了時にカードの縁を光らせる枠（白。色は Image.color で乗せる）
    w = h = 128
    defs = blur("g", 6)
    body = (
        chamfer(12, 12, w - 24, h - 24, 18, fill="none", stroke=WHITE, stroke_width=12, filter="url(#g)")
        + chamfer(12, 12, w - 24, h - 24, 18, fill="none", stroke=WHITE, stroke_width=4)
    )
    return document(w, h, body, defs)


def toast_panel():
    # 画面上部の通知：暗い地 + マゼンタの縁 + 左端の警告色の帯
    w, h = 128, 96
    defs = f'<clipPath id="stripe"><rect x="10" y="10" width="16" height="{h - 20}"/></clipPath>'
    stripes = "".join(f'<path d="M 0 {y} L 40 {y - 40} L 40 {y - 28} L 0 {y + 12} Z" fill="{INK}"/>' for y in range(0, h + 40, 24))
    body = (
        chamfer(3, 3, w - 6, h - 6, 18, fill=PANEL, fill_opacity=0.95, stroke=MAGENTA, stroke_width=4)
        + f'<g clip-path="url(#stripe)"><rect x="10" y="10" width="16" height="{h - 20}" fill="{YELLOW}"/>{stripes}</g>'
        + f'<rect x="{w - 30}" y="3" width="18" height="4" fill="{CYAN}"/>'
    )
    return document(w, h, body, defs)


def toast_icon():
    # 通知アイコン：黄色の六角形に「!」
    w = h = 96
    body = (
        hexagon(48, 48, 42, fill=YELLOW, stroke=INK, stroke_width=5)
        + f'<rect x="42" y="22" width="12" height="32" fill="{INK}"/>'
        + f'<rect x="42" y="62" width="12" height="12" fill="{INK}"/>'
    )
    return document(w, h, body)


def memory_cell():
    # メモリー残量ゲージの1コマ（メモリーチップ）。白で作り、点灯/消灯の色は Image.color で乗せる
    w, h = 80, 112
    body = (
        chamfer(4, 4, w - 8, h - 8, 14, fill=WHITE)
        + "".join(f'<rect x="16" y="{y}" width="{w - 32}" height="6" fill="{INK}" opacity="0.35"/>' for y in (30, 52, 74))
    )
    return document(w, h, body)


# ---------------- ブロックのバリア（表示 280x320 の2倍） ----------------
BARRIER_W, BARRIER_H = 560, 640


def _barrier_cells():
    import math
    cells = []
    r = 40
    dx = r * math.sqrt(3)
    row = 0
    y = 30
    while y < BARRIER_H:
        x = 30 + (dx / 2 if row % 2 else 0)
        while x < BARRIER_W:
            cells.append((round(x, 1), round(y, 1)))
            x += dx
        y += r * 1.5
        row += 1
    return cells, r


def barrier():
    # ユニットの前に張るエネルギーの壁。六角形のセルを敷き詰め、縁を光らせる
    w, h = BARRIER_W, BARRIER_H
    cells, r = _barrier_cells()
    defs = (
        f'<clipPath id="wall"><path d="{chamfer_path(8, 8, w - 16, h - 16, 60)}"/></clipPath>'
        + linear("fill", [(0, CYAN, 0.28), (0.5, CYAN, 0.12), (1, CYAN, 0.3)])
        + neon("glow", 8, CYAN)
    )
    grid_cells = "".join(hexagon(x, y, r - 4, fill="none", stroke=CYAN, stroke_width=3, opacity=0.55) for x, y in cells)
    body = (
        f'<g clip-path="url(#wall)"><rect width="{w}" height="{h}" fill="url(#fill)"/>{grid_cells}</g>'
        + f'<g filter="url(#glow)">{chamfer(8, 8, w - 16, h - 16, 60, fill="none", stroke=CYAN, stroke_width=8)}</g>'
        + chamfer(8, 8, w - 16, h - 16, 60, fill="none", stroke=WHITE, stroke_width=3)
    )
    return document(w, h, body, defs)


def _crack(seed, count):
    # 中心付近から外へ走るギザギザのひび（白。色は Image.color で乗せる）
    import random
    rnd = random.Random(seed)
    w, h = BARRIER_W, BARRIER_H
    lines = []
    for i in range(count):
        x, y = w / 2 + rnd.uniform(-60, 60), h / 2 + rnd.uniform(-80, 80)
        angle = rnd.uniform(0, 6.283)
        pts = [(x, y)]
        for _ in range(rnd.randint(4, 7)):
            import math
            angle += rnd.uniform(-0.7, 0.7)
            step = rnd.uniform(30, 70)
            x += math.cos(angle) * step
            y += math.sin(angle) * step
            pts.append((x, y))
        d = "M " + " L ".join(f"{px:.0f} {py:.0f}" for px, py in pts)
        lines.append(f'<path d="{d}" fill="none" stroke="{WHITE}" stroke-width="7" stroke-linejoin="miter"/>')
    clip = f'<clipPath id="wall"><path d="{chamfer_path(8, 8, w - 16, h - 16, 60)}"/></clipPath>'
    return document(w, h, f'<g clip-path="url(#wall)">{"".join(lines)}</g>', clip)


def barrier_crack_1():
    return _crack(1, 3)


def barrier_crack_2():
    return _crack(2, 7)


def barrier_crack_3():
    return _crack(3, 12)


def block_badge():
    # バリアの右上に出すブロック値のバッジ（盾の代わりの六角形）
    w = h = 96
    return document(w, h, hexagon(48, 48, 42, fill=INK, stroke=CYAN, stroke_width=6) + hexagon(48, 48, 30, fill="none", stroke=CYAN, stroke_width=2, opacity=0.6))


def transition_slice():
    # シーン遷移で画面を覆う帯の1本。暗い地の両端にシアン/マゼンタの縁。横にも縦にも伸ばして使う（9-slice）
    w, h = 200, 48
    body = (
        f'<rect width="{w}" height="{h}" fill="{INK}"/>'
        + f'<rect x="0" y="0" width="{w}" height="3" fill="{PANEL_LIGHT}"/>'
        + f'<rect x="0" y="0" width="10" height="{h}" fill="{CYAN}"/>'
        + f'<rect x="{w - 10}" y="0" width="10" height="{h}" fill="{MAGENTA}"/>'
    )
    return document(w, h, body)


def map_room_frame():
    # マップの部屋の縁（白。状態の色は Image.color で乗せる）。四隅にブラケット
    w = h = 240
    b = f'stroke="{WHITE}" stroke-width="10" fill="none" stroke-linecap="square"'
    body = (
        chamfer(10, 10, w - 20, h - 20, 36, fill="none", stroke=WHITE, stroke_width=8)
        + f'<path d="M 4 60 L 4 4 L 60 4" {b}/><path d="M {w - 60} 4 L {w - 4} 4 L {w - 4} 60" {b}/>'
        + f'<path d="M 4 {h - 60} L 4 {h - 4} L 60 {h - 4}" {b}/><path d="M {w - 4} {h - 60} L {w - 4} {h - 4} L {w - 60} {h - 4}" {b}/>'
    )
    return document(w, h, body)


def map_room_fill():
    # 部屋の中身（暗い地に回路の目盛り）
    w = h = 240
    defs = f'<clipPath id="room"><path d="{chamfer_path(14, 14, w - 28, h - 28, 32)}"/></clipPath>'
    body = (
        f'<g clip-path="url(#room)"><rect width="{w}" height="{h}" fill="{PANEL}" fill-opacity="0.92"/>'
        + grid(0, 0, w, h, 24, CYAN, 2, 0.15)
        + "</g>"
    )
    return document(w, h, body, defs)


def row_fill():
    # 一覧の1行の中身（暗い地）。9-slice で横長に伸ばすので、模様は入れない（伸びて太い線になる）
    w, h = 160, 120
    return document(w, h, chamfer(6, 6, w - 12, h - 12, 22, fill=PANEL, fill_opacity=0.94))


def row_frame():
    # 一覧の1行の縁（白。選択状態の色は Image.color で乗せる）。左端に太い帯、右上に刻み
    w, h = 160, 120
    body = (
        chamfer(6, 6, w - 12, h - 12, 22, fill="none", stroke=WHITE, stroke_width=6)
        + f'<rect x="16" y="30" width="10" height="{h - 60}" fill="{WHITE}"/>'
        + f'<rect x="{w - 56}" y="14" width="30" height="5" fill="{WHITE}"/>'
    )
    return document(w, h, body)


def tag():
    # 枚数や価格を載せる小さな札：黒地にシアンの縁、角を斜めに切る
    w, h = 96, 72
    body = chamfer(3, 3, w - 6, h - 6, 14, fill=INK, fill_opacity=0.95, stroke=CYAN, stroke_width=4)
    return document(w, h, body)


def section_header():
    # パネルの見出しの下に引く線：左にシアンの太線、右へ細く伸びる（横に伸ばす）
    w, h = 240, 24
    body = (
        f'<rect x="0" y="8" width="60" height="8" fill="{CYAN}"/>'
        + f'<path d="M 60 16 L 68 8 L 80 8 L 72 16 Z" fill="{MAGENTA}"/>'
        + f'<rect x="84" y="12" width="{w - 84}" height="3" fill="{CYAN}" opacity="0.55"/>'
    )
    return document(w, h, body)


def install_socket():
    # チップを差し込む基板のソケット（表示 300x300 の2倍）。中央に差し込み口、周りに配線とピン穴
    w = h = 600
    traces = "".join(
        f'<path d="M {x1} {y1} L {x2} {y2} L {x3} {y3}" fill="none" stroke="{CYAN}" stroke-width="6" opacity="0.55"/>'
        + circle(x3, y3, 9, fill=INK, stroke=CYAN, stroke_width=4)
        for x1, y1, x2, y2, x3, y3 in [
            (150, 200, 90, 140, 40, 140), (150, 300, 60, 300, 30, 300), (150, 400, 90, 460, 40, 460),
            (450, 200, 510, 140, 560, 140), (450, 300, 540, 300, 570, 300), (450, 400, 510, 460, 560, 460),
            (220, 150, 220, 70, 220, 40), (380, 150, 380, 70, 380, 40), (220, 450, 220, 530, 220, 560), (380, 450, 380, 530, 380, 560),
        ]
    )
    holes = "".join(
        rrect(x, y, 14, 14, 2, fill=INK, stroke="#E0A020", stroke_width=3)
        for i in range(8)
        for x, y in [(173 + i * 34, 160), (173 + i * 34, 426), (160, 173 + i * 34), (426, 173 + i * 34)]
    )
    body = (
        traces
        + chamfer(140, 140, 320, 320, 34, fill="#0E2A2E", fill_opacity=0.95, stroke=CYAN, stroke_width=6)
        + holes
        + chamfer(196, 196, 208, 208, 22, fill=INK, stroke=CYAN_DEEP, stroke_width=4)
        + f'<path d="M 196 236 L 196 196 L 236 196" fill="none" stroke="{WHITE}" stroke-width="6"/>'
    )
    return document(w, h, body)


def install_chip():
    # 差し込むチップ（表示 180x180 の2倍）。暗い本体に金のピン。中央の窓は白で、光の色は Image.color で別の光る画像を重ねる
    w = h = 360
    defs = linear("gold", [(0, "#FFF0A0"), (1, "#E0A020")])
    pins = "".join(
        f'<rect x="{x}" y="{y}" width="{pw}" height="{ph}" fill="url(#gold)" stroke="{INK}" stroke-width="3"/>'
        for i in range(7)
        for x, y, pw, ph in [(78 + i * 30, 20, 16, 44), (78 + i * 30, 296, 16, 44), (20, 78 + i * 30, 44, 16), (296, 78 + i * 30, 44, 16)]
    )
    body = (
        pins
        + chamfer(56, 56, 248, 248, 30, fill=BG_DEEP, stroke=INK, stroke_width=10)
        + chamfer(70, 70, 220, 220, 22, fill="none", stroke=PANEL_LIGHT, stroke_width=4)
        + chamfer(120, 120, 120, 120, 16, fill=PANEL_LIGHT, stroke=WHITE, stroke_width=5)
        + f'<path d="{bolt_path(180, 180, 80)}" fill="{WHITE}" stroke="{INK}" stroke-width="5" stroke-linejoin="miter"/>'
        + circle(92, 92, 8, fill=WHITE)
    )
    return document(w, h, body, defs)


def install_glow():
    # チップの後ろで光る四角い光輪（白。期待レアリティの色は Image.color で乗せる）
    w = h = 360
    defs = radial("g", [(0, WHITE, 1), (0.45, WHITE, 0.55), (1, WHITE, 0)])
    return document(w, h, f'<rect width="{w}" height="{h}" fill="url(#g)"/>' + chamfer(90, 90, 180, 180, 24, fill=WHITE, fill_opacity=0.35), defs)


def token():
    # トークン（キャンペーン中の通貨）：金の六角形のデータトークン。中心に菱形のコアと配線の切り欠き。
    # 文字は入れない（数字は CommonText で隣に出す）
    w = h = 144
    defs = linear("gold", [(0, "#FFF0A0"), (1, "#E0A020")])
    traces = "".join(
        f'<rect x="{x}" y="{y}" width="{tw}" height="{th}" fill="{INK}"/>'
        for x, y, tw, th in [(68, 22, 8, 22), (68, 100, 8, 22), (22, 68, 22, 8), (100, 68, 22, 8)]
    )
    body = (
        hexagon(72, 72, 64, fill="url(#gold)", stroke=INK, stroke_width=6)
        + hexagon(72, 72, 46, fill="none", stroke=INK, stroke_width=4, opacity=0.55)
        + traces
        + f'<path d="M 72 42 L 102 72 L 72 102 L 42 72 Z" fill="{INK}"/>'
        + f'<path d="M 72 54 L 90 72 L 72 90 L 54 72 Z" fill="{YELLOW}"/>'
        + f'<rect x="36" y="30" width="12" height="12" fill="{WHITE}"/>'
    )
    return document(w, h, body, defs)


def reward_rays():
    # 報酬の後ろで回る放射光（白。色は Image.color で乗せる）。ポップな集中線の帯
    import math
    w = h = 512
    defs = radial("fade", [(0, WHITE, 1), (0.55, WHITE, 0.6), (1, WHITE, 0)])
    rays = []
    count = 16
    for i in range(count):
        a0 = (i / count) * 2 * math.pi
        a1 = a0 + math.pi / count * 0.9
        x0, y0 = 256 + 256 * math.cos(a0), 256 + 256 * math.sin(a0)
        x1, y1 = 256 + 256 * math.cos(a1), 256 + 256 * math.sin(a1)
        rays.append(f"M 256 256 L {x0:.1f} {y0:.1f} L {x1:.1f} {y1:.1f} Z")
    body = (
        f'<mask id="m"><rect width="{w}" height="{h}" fill="url(#fade)"/></mask>'
        + f'<path d="{" ".join(rays)}" fill="{WHITE}" mask="url(#m)"/>'
    )
    return document(w, h, body, defs)


# ファイル名 → 生成関数
ITEMS = {
    "ui_panel": panel,
    "ui_tray": memory_board,
    "ui_mem_pins": memory_pins,
    "ui_slot": slot,
    "ui_button_primary": button_primary,
    "ui_button_outline": button_outline,
    "ui_button_mask": button_mask,
    "ui_ripple_ring": ripple_ring,
    "ui_mem_cell": memory_cell,
    "ui_barrier": barrier,
    "ui_barrier_crack_1": barrier_crack_1,
    "ui_barrier_crack_2": barrier_crack_2,
    "ui_barrier_crack_3": barrier_crack_3,
    "ui_block_badge": block_badge,
    "ui_transition_slice": transition_slice,
    "ui_map_room_frame": map_room_frame,
    "ui_map_room_fill": map_room_fill,
    "ui_button_round": button_round,
    "ui_bar_bg": bar_background,
    "ui_bar_fill": bar_fill,
    "ui_stamina_gem": stamina_gem,
    "ui_cost_gem": cost_gem,
    "ui_badge": badge,
    "ui_status_pill": status_pill,
    "ui_target_frame": target_frame,
    "ui_unit_shadow": unit_shadow,
    "ui_deco_chevron": deco_chevron,
    "ui_cursor": cursor_arrow,
    "ui_circle_badge": circle_badge,
    "ui_icon_trash": trash_icon,
    "ui_write_scan": write_scan,
    "ui_glow_frame": glow_frame,
    "ui_toast_panel": toast_panel,
    "ui_toast_icon": toast_icon,
    "ui_row_fill": row_fill,
    "ui_row_frame": row_frame,
    "ui_tag": tag,
    "ui_section_line": section_header,
    "ui_install_socket": install_socket,
    "ui_install_chip": install_chip,
    "ui_install_glow": install_glow,
    "ui_token": token,
    "ui_reward_rays": reward_rays,
}


# ドット1粒の大きさ（書き出す画像の画素数）。1 にするとドット感なしのなめらかな絵になる
PIXEL = 6


def build():
    for name, fn in ITEMS.items():
        render(fn(), f"UI/{name}.png", pixel=PIXEL)
