"""書き出した PNG を1枚に並べて確認する。 python ArtSource/preview.py <Assets/Art 以下のフォルダ> <出力.png>"""
import os
import sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from svgkit import ART_OUT

folder = os.path.join(ART_OUT, sys.argv[1])
out = sys.argv[2]
cell = int(sys.argv[3]) if len(sys.argv) > 3 else 260
files = sorted(f for f in os.listdir(folder) if f.endswith(".png"))
cols = min(5, len(files))
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGBA", (cols * cell, rows * (cell + 20)), (200, 200, 210, 255))
draw = ImageDraw.Draw(sheet)
for i, f in enumerate(files):
    x, y = (i % cols) * cell, (i // cols) * (cell + 20)
    # 透過が分かるようにグレーの市松を敷く
    for cy in range(0, cell, 20):
        for cx in range(0, cell, 20):
            if (cx + cy) // 20 % 2:
                draw.rectangle([x + cx, y + cy, x + cx + 19, y + cy + 19], fill=(175, 175, 190, 255))
    im = Image.open(os.path.join(folder, f)).convert("RGBA")
    im.thumbnail((cell - 10, cell - 10))
    sheet.alpha_composite(im, (x + (cell - im.width) // 2, y + (cell - im.height) // 2))
    draw.text((x + 4, y + cell + 4), f, fill=(0, 0, 0, 255))
sheet.save(out)
