"""パーティクル用のテクスチャ。すべて白で作り、色はパーティクル側で乗せる"""
from palette import *
from svgkit import *


def fx_shard():
    # 砕けたデータ片のような菱形
    defs = blur("g", 6)
    shape = "M 64 14 L 100 64 L 64 114 L 28 64 Z"
    return document(128, 128, f'<path d="{shape}" fill="{WHITE}" opacity="0.6" filter="url(#g)"/><path d="{shape}" fill="{WHITE}"/>', defs)


def fx_sparkle():
    defs = radial("c", [(0, WHITE, 0.9), (1, WHITE, 0)])
    return document(128, 128, circle(64, 64, 40, fill="url(#c)") + sparkle(64, 64, 60, thin=0.1, fill=WHITE), defs)


def fx_glow():
    defs = radial("c", [(0, WHITE, 1), (0.4, WHITE, 0.5), (1, WHITE, 0)])
    return document(128, 128, circle(64, 64, 64, fill="url(#c)"), defs)


# ドット1粒の大きさ（書き出す画像の画素数）。1 にするとドット感なしのなめらかな絵になる
PIXEL = 8


def build():
    render(fx_shard(), "Effects/fx_shard.png", pixel=PIXEL)
    render(fx_sparkle(), "Effects/fx_sparkle.png", pixel=PIXEL)
    render(fx_glow(), "Effects/fx_glow.png", pixel=PIXEL)
