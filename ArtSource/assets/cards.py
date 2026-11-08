"""カードのフレーム（レアリティ別）・裏面・イラスト。

カードは「メモリに差し込む命令カートリッジ」のイメージ。下端に金の接点ピンと切り欠きがある。
表示 270x370 に対して 2 倍の 540x740 で書き出す。枠の中の配置（CardView.prefab と一致させる）:
  コスト   : 左上の角に重なる（別画像 ui_cost_gem）
  名前帯   : x 132..512, y 22..94
  イラスト窓: x 36..504, y 116..424  ← イラストはこの内側 460x304 に収まるサイズ
  説明欄   : x 32..508, y 444..690
  接点ピン : y 700..728
"""
from palette import *
from svgkit import *

W, H = 540, 740
WINDOW = (36, 116, 468, 308)
DESC = (32, 444, 476, 246)
NAME = (132, 22, 380, 72)
CARD_CUT = 40


def _pins():
    defs = linear("gold", [(0, "#FFF0A0"), (1, "#E0A020")])
    pins = []
    for i in range(16):
        x = 70 + i * 26
        if 7 <= i <= 8:
            continue  # 中央は切り欠き（差し込む向きのキー）
        pins.append(rrect(x, 700, 16, 28, 2, fill="url(#gold)", stroke=INK, stroke_width=2))
    notch = f'<path d="M 252 734 L 252 712 L 262 702 L 278 702 L 288 712 L 288 734 Z" fill="{INK}"/>'
    return defs, "".join(pins) + notch


def _frame(accent, border_paint, name_paint, extra_defs="", pattern_extra=""):
    pin_defs, pins = _pins()
    defs = (
        linear("base", [(0, BG_NAVY), (1, BG_DEEP)])
        + f'<clipPath id="card"><path d="{chamfer_path(6, 6, W - 12, H - 12, CARD_CUT)}"/></clipPath>'
        + pin_defs
        + extra_defs
    )
    wx, wy, ww, wh = WINDOW
    dx, dy, dw, dh = DESC
    nx, ny, nw, nh = NAME
    body = [
        f'<g clip-path="url(#card)"><rect width="{W}" height="{H}" fill="url(#base)"/>'
        + grid(0, 0, W, H, 24, accent, 1, 0.12)
        + halftone(W - 260, H - 300, 260, 300, 18, 6, accent, 0.35, origin=(1, 1))
        + pattern_extra
        + "</g>",
        chamfer(6, 6, W - 12, H - 12, CARD_CUT, fill="none", stroke=INK, stroke_width=10),
        chamfer(16, 16, W - 32, H - 32, CARD_CUT - 10, fill="none", stroke=border_paint, stroke_width=5),
        # イラスト窓
        chamfer(wx - 6, wy - 6, ww + 12, wh + 12, 20, fill=INK, stroke=border_paint, stroke_width=4),
        # 説明欄：暗い地に白文字。左上にプロンプト記号
        chamfer(dx, dy, dw, dh, 18, fill=PANEL, fill_opacity=0.95, stroke=border_paint, stroke_width=3),
        chevrons(dx + 16, dy + 20, 16, 2, accent, gap=12),
        # 名前帯（オペコード）：ネオン地に黒文字
        chamfer(nx, ny, nw, nh, 18, fill=name_paint, stroke=INK, stroke_width=5),
        f'<rect x="{nx + 24}" y="{ny + 10}" width="{nw - 48}" height="5" fill="{WHITE}" opacity="0.5"/>',
        # 側面の目盛り
        "".join(f'<rect x="8" y="{y}" width="10" height="3" fill="{accent}"/>' for y in range(140, 420, 20)),
        "".join(f'<rect x="{W - 18}" y="{y}" width="10" height="3" fill="{accent}"/>' for y in range(140, 420, 20)),
        pins,
    ]
    return document(W, H, "".join(body), defs)


def frame_normal():
    return _frame(CYAN, CYAN, CYAN)


def frame_rare():
    return _frame(MAGENTA, MAGENTA, MAGENTA)


def frame_epic():
    # 最高レアはホログラム風のグラデーションの縁
    holo = linear("holo", [(0, CYAN), (0.5, MAGENTA), (1, YELLOW)], x2=1, y2=1)
    stripes = "".join(f'<path d="M {x} 0 L {x + 10} 0 L {x + 10 - 740} 740 L {x - 740} 740 Z" fill="{YELLOW}" opacity="0.06"/>' for x in range(0, 1300, 40))
    return _frame(YELLOW, "url(#holo)", YELLOW, extra_defs=holo, pattern_extra=stripes)


def card_back():
    pin_defs, pins = _pins()
    defs = (
        linear("base", [(0, BG_NAVY), (1, BG_DEEP)])
        + f'<clipPath id="card"><path d="{chamfer_path(6, 6, W - 12, H - 12, CARD_CUT)}"/></clipPath>'
        + pin_defs
        + neon("glow", 10, CYAN)
    )
    traces = []
    for i, (ex, ey) in enumerate([(60, 120), (480, 120), (60, 640), (480, 640), (270, 90), (270, 660), (40, 370), (500, 370)]):
        traces.append(f'<path d="M 270 370 L {(270 + ex) / 2} {370 if i % 2 else ey} L {ex} {ey}" fill="none" stroke="{CYAN if i % 3 else MAGENTA}" stroke-width="4" opacity="0.7"/>')
        traces.append(circle(ex, ey, 7, fill=INK, stroke=CYAN if i % 3 else MAGENTA, stroke_width=3))
    body = [
        f'<g clip-path="url(#card)"><rect width="{W}" height="{H}" fill="url(#base)"/>{grid(0, 0, W, H, 24, CYAN, 1, 0.12)}{"".join(traces)}</g>',
        chamfer(6, 6, W - 12, H - 12, CARD_CUT, fill="none", stroke=INK, stroke_width=10),
        chamfer(16, 16, W - 32, H - 32, CARD_CUT - 10, fill="none", stroke=CYAN, stroke_width=5),
        f'<g filter="url(#glow)">{hexagon(270, 370, 120, fill=INK, stroke=CYAN, stroke_width=8)}</g>',
        hexagon(270, 370, 84, fill=MAGENTA, stroke=INK, stroke_width=6),
        f'<path d="{bolt_path(270, 370, 110)}" fill="{YELLOW}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>',
        pins,
    ]
    return document(W, H, "".join(body), defs)


def card_mask():
    # カードと同じ形の白い板。使えないカードを暗くする重ね板などに使う（色は Image.color で乗せる）
    return document(W, H, f'<path d="{chamfer_path(6, 6, W - 12, H - 12, CARD_CUT)}" fill="{WHITE}"/>')


# ---------------- イラスト（460x304、カードIDと同じファイル名） ----------------

IW, IH = 460, 304


def _illust(bg, pattern, motif, defs="", shadow=MAGENTA):
    d = (
        defs
        + f'<clipPath id="win"><rect width="{IW}" height="{IH}"/></clipPath>'
        + hard_shadow("pop", 10, 10, shadow)
        + neon("glowC", 8, CYAN)
    )
    body = f'<g clip-path="url(#win)"><rect width="{IW}" height="{IH}" fill="{bg}"/>{pattern}<g filter="url(#pop)">{motif}</g></g>'
    return document(IW, IH, body, d)


def _burst(color, opacity=0.9, points=12, r=170):
    return f'<path d="{star_path(230, 152, r, inner=0.55, points=points)}" fill="{color}" opacity="{opacity}"/>'


def illust_attack():
    # エネルギーの刃
    blade = (
        '<g transform="rotate(-38 230 160)">'
        f'<path d="M 214 250 L 214 40 L 230 0 L 246 40 L 246 250 Z" fill="{CYAN}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>'
        f'<path d="M 226 240 L 226 30 L 230 16 L 234 30 L 234 240 Z" fill="{WHITE}"/>'
        f'<rect x="178" y="246" width="104" height="22" fill="{YELLOW}" stroke="{INK}" stroke-width="6"/>'
        f'<rect x="216" y="268" width="28" height="64" fill="{INK}"/>'
        + "".join(f'<rect x="216" y="{y}" width="28" height="6" fill="{MAGENTA}"/>' for y in range(276, 330, 14))
        + "</g>"
    )
    slashes = "".join(f'<path d="M {x} {y} L {x + 120} {y - 90}" stroke="{c}" stroke-width="6" stroke-linecap="square"/>' for x, y, c in [(40, 270, YELLOW), (320, 110, MAGENTA), (70, 150, CYAN)])
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, MAGENTA, 0.35, origin=(0, 1)) + slashes, blade)


def illust_strong_attack():
    # 警告色のハンマー
    hazard = "".join(f'<path d="M {x} 60 L {x + 18} 60 L {x - 2} 150 L {x - 20} 150 Z" fill="{INK}"/>' for x in range(150, 330, 36))
    hammer = (
        '<g transform="rotate(-24 230 170)">'
        f'<rect x="218" y="140" width="24" height="170" fill="{INK}"/>'
        + "".join(f'<rect x="218" y="{y}" width="24" height="7" fill="{CYAN}"/>' for y in range(180, 300, 18))
        + f'<clipPath id="head"><rect x="130" y="60" width="200" height="90"/></clipPath>'
        + f'<rect x="130" y="60" width="200" height="90" fill="{YELLOW}"/>'
        + f'<g clip-path="url(#head)">{hazard}</g>'
        + f'<rect x="130" y="60" width="200" height="90" fill="none" stroke="{INK}" stroke-width="7"/>'
        + f'<rect x="116" y="70" width="18" height="70" fill="{MAGENTA}" stroke="{INK}" stroke-width="5"/>'
        + f'<rect x="326" y="70" width="18" height="70" fill="{MAGENTA}" stroke="{INK}" stroke-width="5"/>'
        + "</g>"
    )
    return _illust(BG_PURPLE, _burst(MAGENTA, 0.85) + _burst(YELLOW, 0.9, r=110), hammer, shadow=CYAN)


def illust_block():
    # 六角形のエネルギーシールド
    cells = "".join(hexagon(230 + dx, 150 + dy, 26, fill="none", stroke=CYAN, stroke_width=3, opacity=0.8)
                    for dx, dy in [(0, 0), (46, 26), (-46, 26), (46, -26), (-46, -26), (0, 52), (0, -52)])
    shield = (
        hexagon(230, 150, 128, fill=BG_DEEP, stroke=INK, stroke_width=14)
        + f'<g filter="url(#glowC)">{hexagon(230, 150, 120, fill="none", stroke=CYAN, stroke_width=8)}</g>'
        + cells
        + hexagon(230, 150, 34, fill=CYAN, stroke=INK, stroke_width=5)
    )
    return _illust(PANEL, grid(0, 0, IW, IH, 28, CYAN, 1.5, 0.25), shield, shadow=MAGENTA)


def illust_poison():
    # ウイルス（トゲのある球体）
    spikes = []
    import math
    for i in range(10):
        a = math.radians(i * 36)
        x1, y1 = 230 + 78 * math.cos(a), 150 + 78 * math.sin(a)
        x2, y2 = 230 + 118 * math.cos(a), 150 + 118 * math.sin(a)
        spikes.append(f'<path d="M {x1:.1f} {y1:.1f} L {x2:.1f} {y2:.1f}" stroke="{INK}" stroke-width="16" stroke-linecap="square"/>')
        spikes.append(f'<path d="M {x1:.1f} {y1:.1f} L {x2:.1f} {y2:.1f}" stroke="{LIME}" stroke-width="7"/>')
        spikes.append(circle(round(x2, 1), round(y2, 1), 13, fill=MAGENTA, stroke=INK, stroke_width=5))
    virus = (
        "".join(spikes)
        + circle(230, 150, 84, fill=LIME, stroke=INK, stroke_width=7)
        + "".join(circle(230 + dx, 150 + dy, r, fill=BG_DEEP, opacity=0.35) for dx, dy, r in [(-30, -20, 16), (26, 10, 22), (-10, 40, 12), (30, -40, 10)])
        + f'<rect x="170" y="130" width="120" height="14" fill="{MAGENTA}" opacity="0.9"/>'
    )
    glitch = "".join(f'<rect x="{x}" y="{y}" width="{w}" height="8" fill="{c}" opacity="0.8"/>' for x, y, w, c in [(20, 60, 90, LIME), (340, 220, 100, MAGENTA), (300, 40, 60, CYAN), (40, 250, 50, YELLOW)])
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(1, 0)) + glitch, virus, shadow=MAGENTA)


def illust_strength():
    # パワーアップのチップ
    pins = "".join(
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{YELLOW}" stroke="{INK}" stroke-width="3"/>'
        for i in range(5)
        for x, y, w, h in [(158 + i * 30, 44, 14, 30), (158 + i * 30, 226, 14, 30), (122, 80 + i * 30, 30, 14), (308, 80 + i * 30, 30, 14)]
    )
    arrow = f'<path d="M 230 86 L 286 150 L 252 150 L 252 214 L 208 214 L 208 150 L 174 150 Z" fill="{LIME}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>'
    chip = pins + chamfer(146, 68, 168, 172, 20, fill=BG_DEEP, stroke=INK, stroke_width=7) + chamfer(156, 78, 148, 152, 14, fill="none", stroke=CYAN, stroke_width=3) + arrow
    return _illust(BG_PURPLE, _burst(YELLOW, 0.35, points=16, r=220) + grid(0, 0, IW, IH, 32, YELLOW, 1, 0.15), chip, shadow=MAGENTA)


def illust_heal():
    # 医療の十字と心拍の線
    cross = (
        f'<path d="M 190 50 L 270 50 L 270 110 L 330 110 L 330 190 L 270 190 L 270 250 L 190 250 L 190 190 L 130 190 L 130 110 L 190 110 Z" '
        f'fill="{LIME}" stroke="{INK}" stroke-width="8" stroke-linejoin="miter"/>'
    )
    ecg = f'<path d="M 0 160 L 150 160 L 175 110 L 200 210 L 235 70 L 265 220 L 290 160 L 460 160" fill="none" stroke="{INK}" stroke-width="16" stroke-linejoin="miter"/>'
    ecg2 = f'<path d="M 0 160 L 150 160 L 175 110 L 200 210 L 235 70 L 265 220 L 290 160 L 460 160" fill="none" stroke="{CYAN}" stroke-width="7" stroke-linejoin="miter"/>'
    return _illust(PANEL, grid(0, 0, IW, IH, 24, LIME, 1, 0.2), cross + ecg + ecg2, shadow=MAGENTA)


def illust_regeneration():
    # 充電中のバッテリーと、ぐるりと回る矢印
    ring = (
        f'<path d="M 110 150 A 120 120 0 0 1 330 90" fill="none" stroke="{INK}" stroke-width="22"/>'
        f'<path d="M 110 150 A 120 120 0 0 1 330 90" fill="none" stroke="{MAGENTA}" stroke-width="12"/>'
        f'<path d="M 350 150 A 120 120 0 0 1 130 210" fill="none" stroke="{INK}" stroke-width="22"/>'
        f'<path d="M 350 150 A 120 120 0 0 1 130 210" fill="none" stroke="{MAGENTA}" stroke-width="12"/>'
        f'<path d="M 312 62 L 360 84 L 318 118 Z" fill="{MAGENTA}" stroke="{INK}" stroke-width="5"/>'
        f'<path d="M 148 238 L 100 216 L 142 182 Z" fill="{MAGENTA}" stroke="{INK}" stroke-width="5"/>'
    )
    battery = (
        f'<rect x="170" y="110" width="120" height="80" fill="{BG_DEEP}" stroke="{INK}" stroke-width="7"/>'
        f'<rect x="290" y="132" width="14" height="36" fill="{INK}"/>'
        + "".join(f'<rect x="{x}" y="120" width="22" height="60" fill="{LIME}"/>' for x in (180, 208, 236))
        + f'<path d="{bolt_path(236, 150, 90)}" fill="{YELLOW}" stroke="{INK}" stroke-width="5"/>'
    )
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, CYAN, 0.3, origin=(0, 0)), ring + battery, shadow=CYAN)


def illust_overclock():
    # 熱で赤く光るCPUチップと、上向きの矢印（HPを燃やしてメモリーを増やす）
    pins = "".join(
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{YELLOW}" stroke="{INK}" stroke-width="3"/>'
        for i in range(5)
        for x, y, w, h in [(158 + i * 30, 50, 14, 26), (158 + i * 30, 228, 14, 26), (124, 86 + i * 28, 26, 14), (310, 86 + i * 28, 26, 14)]
    )
    flames = "".join(
        f'<path d="M {x} 300 Q {x - 26} {260 - h} {x} {200 - h} Q {x + 26} {260 - h} {x} 300 Z" fill="{c}" stroke="{INK}" stroke-width="4"/>'
        for x, h, c in [(90, 30, ORANGE), (370, 40, ORANGE), (60, 0, MAGENTA), (400, 10, MAGENTA)]
    )
    chip = pins + chamfer(146, 72, 168, 160, 20, fill=BG_DEEP, stroke=INK, stroke_width=7) + chamfer(158, 84, 144, 136, 14, fill=ORANGE, fill_opacity=0.35, stroke=ORANGE, stroke_width=4)
    arrow = f'<path d="M 230 92 L 280 150 L 250 150 L 250 206 L 210 206 L 210 150 L 180 150 Z" fill="{CYAN}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>'
    return _illust(BG_PURPLE, _burst(ORANGE, 0.35, points=16, r=220) + flames, chip + arrow, shadow=MAGENTA)


def illust_exploit():
    # 六角のシールドを突き抜けるドリルの針（ブロック貫通）
    shield = hexagon(250, 150, 96, fill="none", stroke=CYAN, stroke_width=10, opacity=0.7)
    cracks = "".join(f'<path d="M 250 150 L {x} {y}" stroke="{WHITE}" stroke-width="5"/>' for x, y in [(200, 70), (320, 90), (300, 230), (190, 220)])
    needle = (
        '<g transform="rotate(-20 230 150)">'
        f'<path d="M 30 132 L 330 132 L 420 150 L 330 168 L 30 168 Z" fill="{MAGENTA}" stroke="{INK}" stroke-width="7" stroke-linejoin="miter"/>'
        + "".join(f'<path d="M {x} 132 L {x + 18} 168" stroke="{INK}" stroke-width="5"/>' for x in range(60, 320, 36))
        + f'<rect x="10" y="120" width="34" height="60" fill="{YELLOW}" stroke="{INK}" stroke-width="6"/>'
        + "</g>"
    )
    return _illust(BG_NAVY, grid(0, 0, IW, IH, 28, CYAN, 1.5, 0.2), shield + cracks + needle, shadow=CYAN)


def illust_cache_expand():
    # 扇に広がる手札と、プラスの印（手札が増える）
    cards_fan = "".join(
        f'<g transform="rotate({a} 230 300)">{chamfer(185, 70, 90, 128, 12, fill=BG_DEEP, stroke=c, stroke_width=6)}'
        f'<rect x="200" y="86" width="60" height="10" fill="{c}"/></g>'
        for a, c in [(-36, VIOLET), (-18, CYAN), (0, CYAN), (18, CYAN), (36, LIME)]
    )
    plus = hexagon(360, 70, 42, fill=LIME, stroke=INK, stroke_width=6) + f'<path d="M 340 70 L 380 70 M 360 50 L 360 90" stroke="{INK}" stroke-width="10"/>'
    return _illust(PANEL, halftone(0, 0, IW, IH, 20, 6, CYAN, 0.3, origin=(0, 1)), cards_fan + plus, shadow=MAGENTA)


def illust_overload():
    # 警告の三角と、過負荷で弾ける大きな稲妻（大ダメージの代わりに手札が減る）
    warn = f'<path d="M 230 30 L 400 280 L 60 280 Z" fill="{YELLOW}" stroke="{INK}" stroke-width="10" stroke-linejoin="miter"/>'
    bolt = f'<path d="{bolt_path(230, 180, 170)}" fill="{MAGENTA}" stroke="{INK}" stroke-width="7" stroke-linejoin="miter"/>'
    sparks = "".join(f'<path d="M {x} {y} l 16 -16 l -4 14 l 18 -6" fill="none" stroke="{CYAN}" stroke-width="6"/>' for x, y in [(40, 90), (380, 60), (400, 200), (30, 230)])
    return _illust(BG_PURPLE, _burst(MAGENTA, 0.8) + sparks, warn + bolt, shadow=CYAN)


def illust_firewall():
    # 燃えるレンガの壁（大きなブロックの代わりに手札が減る）
    bricks = []
    for row in range(5):
        y = 110 + row * 36
        off = 0 if row % 2 == 0 else 40
        for x in range(60 - off, 420, 80):
            bricks.append(f'<rect x="{max(x, 60)}" y="{y}" width="{min(76, 400 - max(x, 60))}" height="32" fill="{MAGENTA_DEEP}" stroke="{INK}" stroke-width="5"/>')
    flames = "".join(
        f'<path d="M {x} 112 Q {x - 22} {80 - h} {x} {40 - h} Q {x + 22} {80 - h} {x} 112 Z" fill="{c}" stroke="{INK}" stroke-width="4"/>'
        for x, h, c in [(100, 0, ORANGE), (170, 20, YELLOW), (240, 10, ORANGE), (310, 26, YELLOW), (370, 0, ORANGE)]
    )
    return _illust(BG_NAVY, grid(0, 0, IW, IH, 28, MAGENTA, 1.5, 0.2), flames + "".join(bricks), shadow=CYAN)


def illust_interrupt():
    # 一時停止の記号と、凍りついた六角（相手を止める）
    ice = "".join(f'<path d="M 230 150 L {230 + 150 * dx} {150 + 150 * dy}" stroke="{CYAN}" stroke-width="8" opacity="0.8"/>' for dx, dy in [(1, 0), (-1, 0), (0.5, 0.87), (-0.5, 0.87), (0.5, -0.87), (-0.5, -0.87)])
    core = hexagon(230, 150, 100, fill=BG_DEEP, stroke=INK, stroke_width=12) + f'<g filter="url(#glowC)">{hexagon(230, 150, 92, fill="none", stroke=CYAN, stroke_width=8)}</g>'
    pause = f'<rect x="192" y="100" width="28" height="100" fill="{WHITE}" stroke="{INK}" stroke-width="5"/><rect x="240" y="100" width="28" height="100" fill="{WHITE}" stroke="{INK}" stroke-width="5"/>'
    return _illust(PANEL, halftone(0, 0, IW, IH, 20, 6, CYAN, 0.35, origin=(1, 1)), ice + core + pause, shadow=MAGENTA)


def illust_power_surge():
    # 自分のHP（ライムのゲージ）が光線になって飛び出す（HPに比例したダメージ）
    gauge = chamfer(40, 110, 170, 80, 14, fill=BG_DEEP, stroke=INK, stroke_width=7) + "".join(f'<rect x="{52 + i * 30}" y="122" width="22" height="56" fill="{LIME}"/>' for i in range(5))
    beam = f'<path d="M 210 120 L 440 60 L 440 240 L 210 180 Z" fill="{LIME}" opacity="0.85" stroke="{INK}" stroke-width="6"/>' + f'<path d="M 210 140 L 440 110 L 440 190 L 210 160 Z" fill="{WHITE}"/>'
    return _illust(BG_PURPLE, _burst(LIME, 0.3, points=16, r=230), gauge + beam, shadow=MAGENTA)


def illust_backup():
    # データのディスクを重ねた山と、それを包む盾（HPに比例したブロック）
    disks = "".join(
        f'<ellipse cx="230" cy="{y}" rx="110" ry="30" fill="{c}" stroke="{INK}" stroke-width="6"/>'
        for y, c in [(220, CYAN_DEEP), (180, CYAN), (140, CYAN_DEEP), (100, CYAN)]
    )
    ring = f'<g filter="url(#glowC)">{hexagon(230, 160, 140, fill="none", stroke=CYAN, stroke_width=6)}</g>'
    return _illust(PANEL, grid(0, 0, IW, IH, 24, CYAN, 1, 0.25), ring + disks, shadow=MAGENTA)


def illust_siphon():
    # 相手（マゼンタの核）から自分（ライムの核）へデータを吸い上げる管
    tube = f'<path d="M 100 150 C 180 40, 280 260, 360 150" fill="none" stroke="{INK}" stroke-width="30"/>' + f'<path d="M 100 150 C 180 40, 280 260, 360 150" fill="none" stroke="{LIME}" stroke-width="14" stroke-dasharray="24 12"/>'
    ends = hexagon(90, 150, 56, fill=MAGENTA, stroke=INK, stroke_width=7) + hexagon(370, 150, 56, fill=LIME, stroke=INK, stroke_width=7)
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(1, 0)), tube + ends, shadow=CYAN)


def illust_broadcast():
    # 電波塔から全方向に広がる電波（相手全員に当たる）
    waves = "".join(f'<path d="M {230 - r} {150 - r * 0.2} A {r} {r} 0 0 1 {230 + r} {150 - r * 0.2}" fill="none" stroke="{c}" stroke-width="10"/>' for r, c in [(70, CYAN), (130, MAGENTA), (190, YELLOW)])
    tower = f'<path d="M 230 110 L 280 300 L 180 300 Z" fill="none" stroke="{INK}" stroke-width="16"/>' + f'<path d="M 230 110 L 280 300 L 180 300 Z" fill="none" stroke="{CYAN}" stroke-width="7"/>' + circle(230, 110, 22, fill=MAGENTA, stroke=INK, stroke_width=6)
    return _illust(BG_PURPLE, _burst(CYAN, 0.25, points=20, r=240), waves + tower, shadow=MAGENTA)


def illust_repair():
    # 歯車とレンチ（敵の自己修復）
    teeth = "".join(f'<rect x="220" y="44" width="20" height="36" fill="{VIOLET}" stroke="{INK}" stroke-width="4" transform="rotate({a} 230 150)"/>' for a in range(0, 360, 45))
    gear = teeth + circle(230, 150, 80, fill=VIOLET, stroke=INK, stroke_width=7) + circle(230, 150, 30, fill=BG_DEEP, stroke=INK, stroke_width=6)
    wrench = '<g transform="rotate(40 230 150)">' + f'<rect x="215" y="40" width="30" height="220" fill="{LIME}" stroke="{INK}" stroke-width="6"/>' + f'<path d="M 200 30 L 260 30 L 260 70 L 245 70 L 245 50 L 215 50 L 215 70 L 200 70 Z" fill="{LIME}" stroke="{INK}" stroke-width="6"/>' + '</g>'
    return _illust(PANEL, grid(0, 0, IW, IH, 28, VIOLET, 1.5, 0.25), gear + wrench, shadow=MAGENTA)


def illust_corrupt():
    # ひび割れて色が反転したプラスの印（回復がダメージに反転する）
    plus = f'<path d="M 190 50 L 270 50 L 270 110 L 330 110 L 330 190 L 270 190 L 270 250 L 190 250 L 190 190 L 130 190 L 130 110 L 190 110 Z" fill="{MAGENTA}" stroke="{INK}" stroke-width="8" stroke-linejoin="miter"/>'
    crack = f'<path d="M 200 50 L 240 120 L 210 160 L 260 250" fill="none" stroke="{INK}" stroke-width="12"/>'
    glitch = "".join(f'<rect x="{x}" y="{y}" width="{w}" height="12" fill="{c}" opacity="0.85"/>' for x, y, w, c in [(110, 90, 120, LIME), (250, 170, 110, CYAN), (150, 220, 80, LIME), (60, 130, 60, YELLOW)])
    return _illust(BG_DEEP, halftone(0, 0, IW, IH, 20, 6, MAGENTA, 0.35, origin=(0, 0)), plus + crack + glitch, shadow=LIME)


def illust_loopback():
    # 輪になった矢印（ループ）の中に、戻ってくるカートリッジ（次の手札に戻る）
    ring = f'<path d="M 330 150 A 100 100 0 1 1 290 70" fill="none" stroke="{INK}" stroke-width="30"/>' + f'<path d="M 330 150 A 100 100 0 1 1 290 70" fill="none" stroke="{CYAN}" stroke-width="16"/>'
    head = f'<path d="M 262 34 L 332 64 L 272 112 Z" fill="{CYAN}" stroke="{INK}" stroke-width="7" stroke-linejoin="miter"/>'
    cart = chamfer(196, 100, 68, 96, 12, fill=BG_DEEP, stroke=INK, stroke_width=7) + f'<rect x="208" y="112" width="44" height="40" fill="{MAGENTA}"/>' + "".join(f'<rect x="{206 + i * 12}" y="176" width="7" height="12" fill="{YELLOW}"/>' for i in range(5))
    return _illust(BG_NAVY, grid(0, 0, IW, IH, 28, CYAN, 1.5, 0.22), ring + head + cart, shadow=MAGENTA)


def illust_multithread():
    # 1本の信号が3本のスレッドに分かれて平行に走る（3回実行）
    lanes = "".join(
        f'<path d="M 70 150 L 150 150 L 200 {y} L 400 {y}" fill="none" stroke="{INK}" stroke-width="24"/>'
        f'<path d="M 70 150 L 150 150 L 200 {y} L 400 {y}" fill="none" stroke="{c}" stroke-width="11"/>'
        + chamfer(400, y - 22, 44, 44, 8, fill=c, stroke=INK, stroke_width=6)
        for y, c in [(70, CYAN), (150, MAGENTA), (230, YELLOW)]
    )
    core = hexagon(80, 150, 46, fill=LIME, stroke=INK, stroke_width=7)
    return _illust(BG_PURPLE, _burst(MAGENTA, 0.25, points=16, r=240), lanes + core, shadow=CYAN)


def illust_mining():
    # 熱で赤く光るチップと、そこから湧き出す金のトークン（HPを削って稼ぐ）
    heat = "".join(f'<path d="M {x} 250 Q {x - 16} 200 {x} 160 Q {x + 16} 120 {x} 80" fill="none" stroke="{ORANGE}" stroke-width="7" opacity="0.8"/>' for x in (120, 340))
    chip = chamfer(160, 150, 140, 110, 16, fill=BG_DEEP, stroke=INK, stroke_width=8) + f'<rect x="184" y="172" width="92" height="66" fill="{ORANGE}"/>'
    pins = "".join(f'<rect x="{172 + i * 24}" y="258" width="12" height="22" fill="{YELLOW}" stroke="{INK}" stroke-width="3"/>' for i in range(5))
    coins = "".join(hexagon(x, y, r, fill=YELLOW, stroke=INK, stroke_width=6) + hexagon(x, y, r * 0.5, fill=ORANGE) for x, y, r in [(230, 90, 40), (160, 60, 26), (310, 54, 30)])
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, YELLOW, 0.3, origin=(0, 0)), heat + chip + pins + coins, shadow=MAGENTA)


def illust_reverse_patch():
    # 敵1体に貼り付く反転パッチ（ライムの回復マークが、マゼンタに裏返る）
    patch = chamfer(120, 60, 220, 180, 22, fill=PANEL_LIGHT, stroke=INK, stroke_width=8)
    half_l = f'<path d="M 200 90 L 230 90 L 230 210 L 200 210 Z M 160 130 L 230 130 L 230 170 L 160 170 Z" fill="{LIME}"/>'
    half_r = f'<path d="M 230 90 L 260 90 L 260 210 L 230 210 Z M 230 130 L 300 130 L 300 170 L 230 170 Z" fill="{MAGENTA}"/>'
    arrows = f'<path d="M 90 250 L 370 250" stroke="{CYAN}" stroke-width="8"/><path d="M 350 236 L 374 250 L 350 264" fill="none" stroke="{CYAN}" stroke-width="8"/><path d="M 110 36 L 86 50 L 110 64" fill="none" stroke="{CYAN}" stroke-width="8"/><path d="M 88 50 L 370 50" stroke="{CYAN}" stroke-width="8"/>'
    return _illust(BG_DEEP, grid(0, 0, IW, IH, 24, MAGENTA, 1, 0.25), arrows + patch + half_l + half_r, shadow=CYAN)


def illust_kernel_panic():
    # 画面全体が警告で埋まる（敵味方すべての回復が反転する）
    stripes = "".join(f'<path d="M {x} 0 L {x + 40} 0 L {x - 60} {IH} L {x - 100} {IH} Z" fill="{YELLOW}" opacity="0.18"/>' for x in range(0, 620, 90))
    tri = f'<path d="M 230 36 L 350 250 L 110 250 Z" fill="{MAGENTA}" stroke="{INK}" stroke-width="10" stroke-linejoin="miter"/>'
    mark = f'<rect x="218" y="100" width="24" height="90" fill="{WHITE}" stroke="{INK}" stroke-width="4"/><rect x="218" y="206" width="24" height="24" fill="{WHITE}" stroke="{INK}" stroke-width="4"/>'
    pluses = "".join(f'<path d="M {x - 22} {y} L {x + 22} {y} M {x} {y - 22} L {x} {y + 22}" stroke="{c}" stroke-width="12"/>' for x, y, c in [(70, 90, LIME), (390, 80, MAGENTA), (80, 230, MAGENTA), (385, 220, LIME)])
    return _illust(BG_PURPLE, stripes + pluses, tri + mark, shadow=CYAN)


def illust_bypass():
    # 六角の盾を、細い光の線が迂回してすり抜ける（ブロック解除）
    shield = hexagon(230, 150, 100, fill=BG_DEEP, stroke=INK, stroke_width=12) + hexagon(230, 150, 92, fill="none", stroke=CYAN, stroke_width=6, opacity=0.5)
    crack = f'<path d="M 190 70 L 240 140 L 210 170 L 260 240" fill="none" stroke="{INK}" stroke-width="14"/><path d="M 190 70 L 240 140 L 210 170 L 260 240" fill="none" stroke="{WHITE}" stroke-width="5"/>'
    route = f'<path d="M 40 250 L 110 250 L 110 40 L 350 40 L 350 250 L 420 250" fill="none" stroke="{INK}" stroke-width="22"/>' + f'<path d="M 40 250 L 110 250 L 110 40 L 350 40 L 350 250 L 420 250" fill="none" stroke="{YELLOW}" stroke-width="10" stroke-dasharray="26 12"/>'
    return _illust(PANEL, grid(0, 0, IW, IH, 24, CYAN, 1, 0.2), route + shield + crack, shadow=MAGENTA)


def illust_buffer_overflow():
    # メモリの枠から溢れ出すデータの奔流（手札を捨てて大ダメージ）
    frame = chamfer(60, 60, 170, 180, 14, fill=BG_DEEP, stroke=INK, stroke_width=8) + "".join(f'<rect x="76" y="{76 + i * 26}" width="138" height="18" fill="{c}"/>' for i, c in enumerate([CYAN, CYAN, MAGENTA, MAGENTA, YELLOW, YELLOW]))
    flood = "".join(f'<rect x="{230 + i * 36}" y="{70 + (i % 3) * 14 - i * 4}" width="{30 + i * 4}" height="{150 + i * 8}" fill="{c}" stroke="{INK}" stroke-width="5" opacity="0.9"/>' for i, c in enumerate([MAGENTA, YELLOW, MAGENTA, ORANGE, YELLOW]))
    return _illust(BG_PURPLE, _burst(YELLOW, 0.3, points=14, r=240), flood + frame, shadow=CYAN)


def illust_repair_packet():
    # 相手へ送られる回復のパケット（封筒の形のデータとプラス印）
    trail = "".join(f'<rect x="{x}" y="146" width="20" height="10" fill="{LIME}" opacity="{o}"/>' for x, o in [(40, 0.3), (80, 0.5), (120, 0.7)])
    packet = chamfer(160, 80, 200, 140, 16, fill=PANEL_LIGHT, stroke=INK, stroke_width=8) + f'<path d="M 160 90 L 260 160 L 360 90" fill="none" stroke="{INK}" stroke-width="7"/>'
    plus = f'<path d="M 240 120 L 280 120 L 280 150 L 310 150 L 310 190 L 280 190 L 280 220 L 240 220 L 240 190 L 210 190 L 210 150 L 240 150 Z" fill="{LIME}" stroke="{INK}" stroke-width="6" transform="translate(0 -20) scale(1)"/>'
    target = hexagon(410, 150, 34, fill=MAGENTA, stroke=INK, stroke_width=6)
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(0, 1)), trail + packet + plus + target, shadow=MAGENTA)


def illust_worm():
    # 盾に食い込む節のある虫（守りが厚いほど効く）
    shield = hexagon(300, 150, 96, fill=BG_DEEP, stroke=INK, stroke_width=12) + hexagon(300, 150, 88, fill="none", stroke=CYAN, stroke_width=6)
    segs = "".join(circle(x, y, r, fill=LIME, stroke=INK, stroke_width=6) for x, y, r in [(70, 220, 20), (110, 196, 24), (150, 176, 28), (195, 162, 30), (240, 152, 32)])
    head = circle(240, 152, 32, fill=MAGENTA, stroke=INK, stroke_width=7) + circle(232, 142, 6, fill=WHITE) + circle(252, 142, 6, fill=WHITE)
    return _illust(PANEL, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(1, 0)), shield + segs + head, shadow=MAGENTA)


def illust_zero_day():
    # 割れたモニターから四方に広がる感染（敵味方すべてに強いウイルス）
    rays = "".join(f'<path d="M 230 150 L {x} {y}" stroke="{c}" stroke-width="10" opacity="0.7"/>' for x, y, c in [(20, 20, LIME), (440, 20, MAGENTA), (20, 290, MAGENTA), (440, 290, LIME), (230, 0, LIME), (230, 300, MAGENTA)])
    screen = chamfer(140, 70, 180, 140, 16, fill=BG_DEEP, stroke=INK, stroke_width=9) + f'<rect x="156" y="86" width="148" height="108" fill="{VIOLET}" opacity="0.8"/>'
    crack = f'<path d="M 200 86 L 236 140 L 214 160 L 262 194" fill="none" stroke="{INK}" stroke-width="12"/><path d="M 200 86 L 236 140 L 214 160 L 262 194" fill="none" stroke="{WHITE}" stroke-width="4"/>'
    blobs = "".join(circle(x, y, r, fill=LIME, stroke=INK, stroke_width=6) for x, y, r in [(110, 240, 22), (360, 60, 18), (370, 240, 26), (90, 70, 16)])
    return _illust(BG_PURPLE, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(0, 0)), rays + screen + crack + blobs, shadow=CYAN)


def illust_redo():
    # 弧を描く矢印が、一つ前の命令をなぞってもう一度書き込む（同じ効果をもう一度）
    ghost = chamfer(90, 90, 110, 130, 14, fill="none", stroke=CYAN, stroke_width=6)
    cart = chamfer(260, 90, 110, 130, 14, fill=BG_DEEP, stroke=INK, stroke_width=8) + f'<rect x="276" y="106" width="78" height="54" fill="{CYAN}"/>' + "".join(f'<rect x="{274 + i * 16}" y="196" width="9" height="14" fill="{YELLOW}"/>' for i in range(5))
    arc = f'<path d="M 150 80 Q 230 0 310 80" fill="none" stroke="{INK}" stroke-width="26"/><path d="M 150 80 Q 230 0 310 80" fill="none" stroke="{YELLOW}" stroke-width="12"/>'
    head = f'<path d="M 286 58 L 330 96 L 276 104 Z" fill="{YELLOW}" stroke="{INK}" stroke-width="6" stroke-linejoin="miter"/>'
    arc2 = f'<path d="M 150 250 Q 230 300 310 250" fill="none" stroke="{MAGENTA}" stroke-width="8" stroke-dasharray="18 10"/>'
    return _illust(BG_NAVY, grid(0, 0, IW, IH, 28, CYAN, 1.5, 0.2), ghost + arc2 + cart + arc + head, shadow=MAGENTA)


def illust_flush():
    # 空になったバッファの枠を、一筋の光が突き抜ける（空ならさらに強い）
    slots = "".join(chamfer(60 + i * 70, 190, 56, 76, 8, fill="none", stroke=CYAN, stroke_width=5) for i in range(5))
    glow = f'<path d="M 20 140 L 440 96 L 440 164 L 20 180 Z" fill="{CYAN}" opacity="0.35"/>'
    beam = f'<path d="M 20 150 L 440 110 L 440 150 L 20 170 Z" fill="{INK}"/><path d="M 30 152 L 440 120 L 440 140 L 30 164 Z" fill="{WHITE}"/>'
    return _illust(BG_DEEP, _burst(CYAN, 0.25, points=14, r=230), slots + glow + beam, shadow=MAGENTA)


def illust_swap_out():
    # コマンドを外へ追い出し、その分の板を積んで壁にする（パージしてブロック）
    out = "".join(chamfer(40 + i * 30, 40 + i * 70, 60, 76, 8, fill=BG_DEEP, stroke=MAGENTA, stroke_width=5) for i in range(3))
    arrow = f'<path d="M 150 150 L 210 150" stroke="{YELLOW}" stroke-width="12"/><path d="M 196 132 L 220 150 L 196 168" fill="none" stroke="{YELLOW}" stroke-width="10"/>'
    plates = "".join(chamfer(240, 60 + i * 50, 160, 40, 8, fill=CYAN if i % 2 == 0 else PANEL_LIGHT, stroke=INK, stroke_width=6) for i in range(4))
    return _illust(PANEL, grid(0, 0, IW, IH, 24, CYAN, 1, 0.2), out + arrow + plates, shadow=MAGENTA)


def illust_defrag():
    # ばらばらのブロックが一列に並び直り、緑に光る（パージして回復）
    scattered = "".join(f'<rect x="{x}" y="{y}" width="34" height="34" fill="{MAGENTA}" stroke="{INK}" stroke-width="5" opacity="0.6"/>' for x, y in [(40, 40), (110, 90), (60, 160), (140, 220)])
    row = "".join(f'<rect x="{210 + (i % 5) * 44}" y="{50 + (i // 5) * 44}" width="38" height="38" fill="{LIME if i < 7 else CYAN}" stroke="{INK}" stroke-width="5"/>' for i in range(10))
    plus = f'<path d="M 300 160 L 340 160 L 340 190 L 370 190 L 370 230 L 340 230 L 340 260 L 300 260 L 300 230 L 270 230 L 270 190 L 300 190 Z" fill="{LIME}" stroke="{INK}" stroke-width="6"/>'
    return _illust(BG_NAVY, halftone(0, 0, IW, IH, 20, 6, LIME, 0.3, origin=(1, 1)), scattered + row + plus, shadow=MAGENTA)


def illust_cloud_burst():
    # 金のトークンを呑んだ雲から、盾を貫く稲妻（トークンを払って貫通ダメージ）
    cloud = "".join(circle(x, y, r, fill=PANEL_LIGHT, stroke=INK, stroke_width=7) for x, y, r in [(170, 80, 50), (230, 60, 60), (300, 84, 48)]) + f'<rect x="150" y="80" width="170" height="50" fill="{PANEL_LIGHT}"/>'
    coin = hexagon(230, 80, 26, fill=YELLOW, stroke=INK, stroke_width=5) + hexagon(230, 80, 12, fill=ORANGE)
    bolt = f'<path d="M 240 130 L 200 210 L 240 210 L 210 296 L 300 190 L 256 190 L 282 130 Z" fill="{YELLOW}" stroke="{INK}" stroke-width="8" stroke-linejoin="miter"/>'
    shield = hexagon(250, 240, 54, fill="none", stroke=CYAN, stroke_width=8)
    return _illust(BG_PURPLE, _burst(YELLOW, 0.25, points=16, r=240), shield + cloud + coin + bolt, shadow=CYAN)


def illust_pipeline():
    # バッファの数だけ並んだ管を、データが連なって流れる（バッファの数でダメージ）
    pipes = "".join(f'<rect x="40" y="{30 + i * 52}" width="380" height="30" fill="{BG_DEEP}" stroke="{INK}" stroke-width="6"/>' for i in range(5))
    packets = "".join(f'<rect x="{60 + j * 70 + i * 14}" y="{36 + i * 52}" width="40" height="18" fill="{[CYAN, MAGENTA, YELLOW, LIME, CYAN][i]}"/>' for i in range(5) for j in range(5 - i))
    return _illust(PANEL, grid(0, 0, IW, IH, 24, MAGENTA, 1, 0.2), pipes + packets, shadow=MAGENTA)


def illust_brute_force():
    # 錠前に鍵を次々と叩き込む（当たれば大ダメージ）
    lock = chamfer(250, 110, 150, 150, 18, fill=BG_DEEP, stroke=INK, stroke_width=9) + f'<path d="M 280 110 L 280 70 Q 325 20 370 70 L 370 110" fill="none" stroke="{INK}" stroke-width="18"/><path d="M 280 110 L 280 70 Q 325 20 370 70 L 370 110" fill="none" stroke="{CYAN}" stroke-width="8"/>'
    hole = circle(325, 170, 16, fill=YELLOW) + f'<rect x="318" y="176" width="14" height="44" fill="{YELLOW}"/>'
    keys = "".join(f'<g opacity="{o}"><rect x="{x}" y="{y}" width="120" height="16" fill="{MAGENTA}" stroke="{INK}" stroke-width="5"/>' + circle(x, y + 8, 22, fill=MAGENTA, stroke=INK, stroke_width=5) + '</g>' for x, y, o in [(80, 70, 0.4), (90, 140, 0.7), (110, 196, 1.0)])
    return _illust(BG_NAVY, _burst(MAGENTA, 0.3, points=12, r=220), lock + hole + keys, shadow=CYAN)


def illust_reflection():
    # 盾で受けた光がそのまま跳ね返る（ブロック分のダメージ）
    incoming = f'<path d="M 440 30 L 220 130" stroke="{INK}" stroke-width="20"/><path d="M 440 30 L 220 130" stroke="{MAGENTA}" stroke-width="9"/>'
    outgoing = f'<path d="M 220 160 L 440 270" stroke="{INK}" stroke-width="26"/><path d="M 220 160 L 440 270" stroke="{YELLOW}" stroke-width="13"/>'
    shield = hexagon(150, 150, 90, fill=BG_DEEP, stroke=INK, stroke_width=12) + hexagon(150, 150, 80, fill=CYAN, opacity=0.45) + hexagon(150, 150, 50, fill="none", stroke=WHITE, stroke_width=5)
    return _illust(PANEL, grid(0, 0, IW, IH, 24, CYAN, 1, 0.2), incoming + outgoing + shield, shadow=MAGENTA)


def illust_format():
    # 走査線が上から下へ、すべてを白紙に塗り替えていく（効果をすべて消す）
    blocks = "".join(f'<rect x="{30 + (i % 6) * 70}" y="{170 + (i // 6) * 60}" width="56" height="40" fill="{[CYAN, MAGENTA, LIME, YELLOW][i % 4]}" stroke="{INK}" stroke-width="4"/>' for i in range(12))
    wiped = f'<rect x="0" y="0" width="{IW}" height="150" fill="{PANEL_LIGHT}" opacity="0.85"/>'
    bar = f'<rect x="0" y="140" width="{IW}" height="22" fill="{WHITE}" stroke="{INK}" stroke-width="5"/><rect x="0" y="162" width="{IW}" height="8" fill="{CYAN}" opacity="0.8"/>'
    return _illust(BG_DEEP, grid(0, 0, IW, IH, 24, CYAN, 1, 0.2), blocks + wiped + bar, shadow=MAGENTA)


def illust_overwrite():
    # メモリーの行を、大きな書き込みヘッドが貫いて上書きする（ブロックを貫通する大ダメージ）
    rows = ""
    for i, y in enumerate((50, 110, 170, 230)):
        hit = i in (1, 2)
        rows += chamfer(70, y, 320, 44, 10, fill=MAGENTA if hit else BG_DEEP, stroke=INK if hit else CYAN, stroke_width=6)
        rows += "".join(f'<rect x="{x}" y="{y + 16}" width="22" height="12" fill="{INK if hit else CYAN}" opacity="{1 if hit else 0.6}"/>' for x in range(92, 370, 40))
    head = (
        '<g transform="rotate(-28 250 150)">'
        f'<path d="M 120 120 L 360 120 L 440 150 L 360 180 L 120 180 Z" fill="{YELLOW}" stroke="{INK}" stroke-width="8" stroke-linejoin="miter"/>'
        f'<rect x="60" y="112" width="70" height="76" fill="{WHITE}" stroke="{INK}" stroke-width="7"/>'
        f'<path d="M 360 120 L 360 180" stroke="{INK}" stroke-width="6"/>'
        "</g>"
    )
    bars = "".join(f'<rect x="{x}" y="{y}" width="{w}" height="8" fill="{c}" opacity="0.8"/>' for x, y, w, c in [(20, 96, 60, CYAN), (380, 150, 70, MAGENTA), (30, 214, 44, MAGENTA), (400, 60, 40, CYAN)])
    return _illust(BG_NAVY, grid(0, 0, IW, IH, 28, CYAN, 1.5, 0.2) + bars, rows + head, shadow=MAGENTA)


# カードID（CardData.Id）→ 生成関数。書き出すファイル名もカードIDにそろえる
ILLUSTRATIONS = {
    "Overwrite": illust_overwrite,
    "Attack": illust_attack,
    "StrongAttack": illust_strong_attack,
    "Block": illust_block,
    "Poison": illust_poison,
    "Strength": illust_strength,
    "Heal": illust_heal,
    "Regeneration": illust_regeneration,
    "Overclock": illust_overclock,
    "Exploit": illust_exploit,
    "CacheExpand": illust_cache_expand,
    "Overload": illust_overload,
    "Firewall": illust_firewall,
    "Interrupt": illust_interrupt,
    "PowerSurge": illust_power_surge,
    "Backup": illust_backup,
    "Siphon": illust_siphon,
    "Broadcast": illust_broadcast,
    "Repair": illust_repair,
    "Corrupt": illust_corrupt,
    "Loopback": illust_loopback,
    "Multithread": illust_multithread,
    "Mining": illust_mining,
    "ReversePatch": illust_reverse_patch,
    "KernelPanic": illust_kernel_panic,
    "Bypass": illust_bypass,
    "BufferOverflow": illust_buffer_overflow,
    "RepairPacket": illust_repair_packet,
    "Worm": illust_worm,
    "ZeroDay": illust_zero_day,
    "Redo": illust_redo,
    "Flush": illust_flush,
    "SwapOut": illust_swap_out,
    "Defrag": illust_defrag,
    "CloudBurst": illust_cloud_burst,
    "Pipeline": illust_pipeline,
    "BruteForce": illust_brute_force,
    "Reflection": illust_reflection,
    "Format": illust_format,
    # 2026-09-29 追加分。専用の絵ができるまでは近い効果の絵を使い回す
    "GarbageCollection": illust_overload,
    "PortScan": illust_broadcast,
    "PrivilegeEscalation": illust_overclock,
    "Terminate": illust_exploit,
    "CodeInjection": illust_corrupt,
    # 敵専用のコマンド。絵は近い効果のものを使い回す（敵のコマンドの絵は画面に大きく出ない）
    "TrojanVirus": illust_poison,
    "RootkitShell": illust_block,
    "RootkitProbe": illust_attack,
    "RootkitVirus": illust_poison,
    "SpyKeylogger": illust_interrupt,
    "SpyVirus": illust_poison,
    "BotRepair": illust_repair,
    "BotDdos": illust_broadcast,
    "SpyMemoryLeak": illust_cache_expand,
    "SystemError": illust_overload,
    "TrojanBackdoor": illust_attack,
    "TrojanPayload": illust_strong_attack,
    "TrojanBreach": illust_bypass,
    "RootkitHijack": illust_siphon,
}


# ---------------- コマンドチップ（表示 200x280 の2倍、パックIDと同じファイル名） ----------------
# 売り物はコマンドを収めたチップ。暗い本体の左右に金のピン、上に刻印の帯、中央にダイ（発光する窓と六角のエンブレム）。
# 本体の縁取りと窓の光は、チップの種類ごとの色（accent / foil）で塗り分ける

PW, PH = 400, 560


def _chip_emblem(kind, cx, cy):
    """ダイの中央の紋章（文字は使わない）。bolt: 汎用、blade: 攻撃、plus: 回復・防御、code: 特殊"""
    if kind == "blade":
        # 斜めの刃
        return (f'<path d="M {cx - 30} {cy + 30} L {cx + 22} {cy - 22} L {cx + 32} {cy - 32} L {cx + 28} {cy - 16} L {cx - 22} {cy + 34} Z" '
                f'fill="{YELLOW}" stroke="{INK}" stroke-width="5" stroke-linejoin="miter"/>'
                f'<rect x="{cx - 36}" y="{cy + 22}" width="22" height="10" fill="{YELLOW}" stroke="{INK}" stroke-width="4" transform="rotate(-45 {cx - 25} {cy + 27})"/>')
    if kind == "plus":
        # プラス（赤十字は使わない。ゲームの配色の黄）
        return (f'<rect x="{cx - 10}" y="{cy - 30}" width="20" height="60" fill="{YELLOW}" stroke="{INK}" stroke-width="5"/>'
                f'<rect x="{cx - 30}" y="{cy - 10}" width="60" height="20" fill="{YELLOW}" stroke="{INK}" stroke-width="5"/>'
                f'<rect x="{cx - 5}" y="{cy - 25}" width="10" height="50" fill="{YELLOW}"/>')
    if kind == "code":
        # < / > の形（コードを書き換える）
        return (f'<path d="M {cx - 14} {cy - 24} L {cx - 34} {cy} L {cx - 14} {cy + 24}" fill="none" stroke="{INK}" stroke-width="14" stroke-linejoin="miter"/>'
                f'<path d="M {cx - 14} {cy - 24} L {cx - 34} {cy} L {cx - 14} {cy + 24}" fill="none" stroke="{YELLOW}" stroke-width="6" stroke-linejoin="miter"/>'
                f'<path d="M {cx + 14} {cy - 24} L {cx + 34} {cy} L {cx + 14} {cy + 24}" fill="none" stroke="{INK}" stroke-width="14" stroke-linejoin="miter"/>'
                f'<path d="M {cx + 14} {cy - 24} L {cx + 34} {cy} L {cx + 14} {cy + 24}" fill="none" stroke="{YELLOW}" stroke-width="6" stroke-linejoin="miter"/>'
                f'<path d="M {cx + 6} {cy - 28} L {cx - 6} {cy + 28}" stroke="{INK}" stroke-width="12"/>'
                f'<path d="M {cx + 6} {cy - 28} L {cx - 6} {cy + 28}" stroke="{YELLOW}" stroke-width="5"/>')
    return f'<path d="{bolt_path(cx, cy, 50)}" fill="{YELLOW}" stroke="{INK}" stroke-width="5" stroke-linejoin="miter"/>'


def _chip(accent, sub, foil_stops, emblem_fill, emblem="bolt"):
    defs = (
        linear("foil", foil_stops, x2=1, y2=1)
        + linear("gold", [(0, "#FFF0A0"), (1, "#E0A020")])
        + neon("glow", 8, accent)
    )
    bx, by, bw, bh = 70, 40, PW - 140, PH - 80
    # 左右のピン（DIP のように並べる）
    pins = "".join(
        f'<rect x="{x}" y="{y}" width="46" height="22" fill="url(#gold)" stroke="{INK}" stroke-width="4"/>'
        for y in range(by + 40, by + bh - 40, 44)
        for x in (bx - 42, bx + bw - 4)
    )
    # 本体の配線（窓へ向かって伸びる）
    traces = "".join(
        f'<path d="M {bx + 18} {y} L {bx + 60} {y} L {bx + 76} {y + 16}" fill="none" stroke="{accent}" stroke-width="4" opacity="0.55"/>'
        f'<path d="M {bx + bw - 18} {y} L {bx + bw - 60} {y} L {bx + bw - 76} {y + 16}" fill="none" stroke="{sub}" stroke-width="4" opacity="0.55"/>'
        for y in range(by + 190, by + bh - 60, 44)
    )
    body = [
        pins,
        # 本体（縁は種類ごとの箔）
        chamfer(bx, by, bw, bh, 28, fill="url(#foil)", stroke=INK, stroke_width=10),
        chamfer(bx + 14, by + 14, bw - 28, bh - 28, 20, fill=BG_DEEP, stroke=INK, stroke_width=6),
        grid(bx + 18, by + 18, bw - 36, bh - 36, 24, accent, 1.5, 0.14),
        # 上の刻印の帯（文字は入れない。刻みの模様だけ）
        chamfer(bx + 30, by + 34, bw - 60, 40, 10, fill=PANEL_LIGHT, stroke=INK, stroke_width=4),
        "".join(f'<rect x="{bx + 44 + i * 18}" y="{by + 46}" width="{8 if i % 3 else 14}" height="16" fill="{accent}" opacity="0.8"/>' for i in range(10)),
        circle(bx + 34, by + bh - 34, 9, fill=accent),
        traces,
        # ダイ（中央の窓）
        f'<g filter="url(#glow)">{chamfer(bx + 50, by + 110, bw - 100, 190, 18, fill=INK, stroke=accent, stroke_width=6)}</g>',
        grid(bx + 56, by + 116, bw - 112, 178, 18, accent, 1, 0.25),
        f'<g filter="url(#glow)">{hexagon(PW / 2, by + 205, 58, fill=INK, stroke=accent, stroke_width=8)}</g>',
        hexagon(PW / 2, by + 205, 40, fill=emblem_fill, stroke=INK, stroke_width=6),
        _chip_emblem(emblem, PW / 2, by + 205),
        # 下の目盛り
        "".join(f'<rect x="{bx + 60 + i * 26}" y="{by + bh - 56}" width="14" height="6" fill="{sub}" opacity="0.8"/>' for i in range(6)),
    ]
    return document(PW, PH, "".join(body), defs)


def pack_starter():
    return _chip(CYAN, VIOLET, [(0, CYAN), (0.5, CYAN_DEEP), (1, VIOLET)], CYAN)


def pack_premium():
    return _chip(MAGENTA, YELLOW, [(0, MAGENTA), (0.45, VIOLET), (0.75, CYAN), (1, YELLOW)], MAGENTA)


def pack_attack():
    # 攻撃系に偏ったチップ（マゼンタ × オレンジ、刃の紋章）
    return _chip(ORANGE, MAGENTA, [(0, ORANGE), (0.5, MAGENTA), (1, MAGENTA_DEEP)], ORANGE, emblem="blade")


def pack_repair():
    # 回復・防御のチップ（ライム × シアン、プラスの紋章）
    return _chip(LIME, CYAN, [(0, LIME), (0.5, CYAN), (1, CYAN_DEEP)], LIME, emblem="plus")


def pack_hack():
    # 特殊系のチップ（バイオレット × イエロー、< / > の紋章）
    return _chip(VIOLET, YELLOW, [(0, VIOLET), (0.55, BG_PURPLE), (1, YELLOW)], VIOLET, emblem="code")


# パックID（CardPackData.Id。画面上の呼び方はチップ）→ 生成関数
PACKS = {
    "StarterPack": pack_starter,
    "PremiumPack": pack_premium,
    "AttackChip": pack_attack,
    "RepairChip": pack_repair,
    "HackChip": pack_hack,
}


# ドット1粒の大きさ（書き出す画像の画素数）。1 にするとドット感なしのなめらかな絵になる
PIXEL = 6


def build():
    render(frame_normal(), "Cards/Frames/card_frame_normal.png", pixel=PIXEL)
    render(frame_rare(), "Cards/Frames/card_frame_rare.png", pixel=PIXEL)
    render(frame_epic(), "Cards/Frames/card_frame_epic.png", pixel=PIXEL)
    render(card_back(), "Cards/Frames/card_back.png", pixel=PIXEL)
    render(card_mask(), "Cards/Frames/card_mask.png", pixel=PIXEL)
    for card_id, fn in ILLUSTRATIONS.items():
        render(fn(), f"Cards/Illustrations/{card_id}.png", pixel=PIXEL)
    for pack_id, fn in PACKS.items():
        render(fn(), f"Cards/Packs/{pack_id}.png", pixel=PIXEL)
