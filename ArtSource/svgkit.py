"""SVG を組み立てて PNG に書き出すための小道具。形のヘルパーはすべて SVG 断片の文字列を返す。"""
import io
import math
import os
import re
import resvg_py

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(ROOT)
ART_OUT = os.path.join(PROJECT, "Assets", "Art")


def document(width, height, body, defs=""):
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
        f'viewBox="0 0 {width} {height}"><defs>{defs}</defs>{body}</svg>'
    )


def render(svg_text, relative_path, scale=1.0, pixel=1):
    """Assets/Art/ 以下の relative_path に PNG を書く。scale で設計サイズより大きく書き出せる。

    pixel > 1 のときはドット絵風にする: 1/pixel の解像度でアンチエイリアス無しに描き、
    最近傍で元の大きさへ拡大する（1ドット = pixel 画素のブロックになる）
    """
    out = os.path.join(ART_OUT, relative_path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    if pixel <= 1:
        png = bytes(resvg_py.svg_to_bytes(svg_string=svg_text, zoom=scale))
    else:
        from PIL import Image
        low = bytes(resvg_py.svg_to_bytes(svg_string=svg_text, zoom=scale / pixel, shape_rendering="crisp_edges"))
        image = Image.open(io.BytesIO(low))
        full = _svg_size(svg_text, scale)
        buffer = io.BytesIO()
        image.resize(full, Image.NEAREST).save(buffer, "PNG")
        png = buffer.getvalue()
    with open(out, "wb") as f:
        f.write(png)
    return out


def _svg_size(svg_text, scale):
    m = re.search(r'width="([\d.]+)" height="([\d.]+)"', svg_text)
    return round(float(m.group(1)) * scale), round(float(m.group(2)) * scale)


def attrs(**kw):
    return " ".join(f'{k.rstrip("_").replace("_", "-")}="{v}"' for k, v in kw.items() if v is not None)


def heart_path(cx, cy, size):
    """中心(cx, cy)、幅 size のハート"""
    s = size / 2.0
    return (
        f"M {cx} {cy + s * 0.9} "
        f"C {cx - s * 0.2} {cy + s * 0.7} {cx - s} {cy + s * 0.2} {cx - s} {cy - s * 0.25} "
        f"C {cx - s} {cy - s * 0.75} {cx - s * 0.45} {cy - s} {cx} {cy - s * 0.55} "
        f"C {cx + s * 0.45} {cy - s} {cx + s} {cy - s * 0.75} {cx + s} {cy - s * 0.25} "
        f"C {cx + s} {cy + s * 0.2} {cx + s * 0.2} {cy + s * 0.7} {cx} {cy + s * 0.9} Z"
    )


def heart(cx, cy, size, **kw):
    return f'<path d="{heart_path(cx, cy, size)}" {attrs(**kw)}/>'


def sparkle_path(cx, cy, r, thin=0.18):
    """4方向に尖ったキラキラ"""
    t = r * thin
    return (
        f"M {cx} {cy - r} Q {cx + t} {cy - t} {cx + r} {cy} "
        f"Q {cx + t} {cy + t} {cx} {cy + r} Q {cx - t} {cy + t} {cx - r} {cy} "
        f"Q {cx - t} {cy - t} {cx} {cy - r} Z"
    )


def sparkle(cx, cy, r, thin=0.18, **kw):
    return f'<path d="{sparkle_path(cx, cy, r, thin)}" {attrs(**kw)}/>'


def star_path(cx, cy, r, inner=0.45, points=5, rotation=-90):
    pts = []
    for i in range(points * 2):
        rr = r if i % 2 == 0 else r * inner
        a = math.radians(rotation + i * 180 / points)
        pts.append(f"{cx + rr * math.cos(a):.2f},{cy + rr * math.sin(a):.2f}")
    return "M " + " L ".join(pts) + " Z"


def star(cx, cy, r, inner=0.45, **kw):
    return f'<path d="{star_path(cx, cy, r, inner)}" {attrs(**kw)}/>'


def rrect(x, y, w, h, r, **kw):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" ry="{r}" {attrs(**kw)}/>'


def circle(cx, cy, r, **kw):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" {attrs(**kw)}/>'


def ellipse(cx, cy, rx, ry, **kw):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" {attrs(**kw)}/>'


def linear(id_, stops, x1=0, y1=0, x2=0, y2=1):
    s = "".join(
        f'<stop offset="{o}" stop-color="{c}" stop-opacity="{a}"/>' for o, c, a in _stops(stops)
    )
    return f'<linearGradient id="{id_}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>'


def radial(id_, stops, cx=0.5, cy=0.5, r=0.5):
    s = "".join(
        f'<stop offset="{o}" stop-color="{c}" stop-opacity="{a}"/>' for o, c, a in _stops(stops)
    )
    return f'<radialGradient id="{id_}" cx="{cx}" cy="{cy}" r="{r}">{s}</radialGradient>'


def _stops(stops):
    for st in stops:
        if len(st) == 2:
            yield st[0], st[1], 1
        else:
            yield st


def blur(id_, std):
    return (
        f'<filter id="{id_}" x="-50%" y="-50%" width="200%" height="200%">'
        f'<feGaussianBlur stdDeviation="{std}"/></filter>'
    )


def bow(cx, cy, size, fill, stroke=None, stroke_width=0, knot=None):
    """リボン結び。size は左右の端から端までの幅"""
    s = size / 2.0
    sw = stroke_width
    k = knot or fill
    loop_l = (
        f"M {cx} {cy} C {cx - s * 0.35} {cy - s * 0.75} {cx - s} {cy - s * 0.7} {cx - s} {cy - s * 0.1} "
        f"C {cx - s} {cy + s * 0.45} {cx - s * 0.4} {cy + s * 0.4} {cx} {cy} Z"
    )
    loop_r = (
        f"M {cx} {cy} C {cx + s * 0.35} {cy - s * 0.75} {cx + s} {cy - s * 0.7} {cx + s} {cy - s * 0.1} "
        f"C {cx + s} {cy + s * 0.45} {cx + s * 0.4} {cy + s * 0.4} {cx} {cy} Z"
    )
    tail_l = f"M {cx - s * 0.08} {cy + s * 0.05} L {cx - s * 0.55} {cy + s * 0.95} L {cx - s * 0.3} {cy + s * 0.85} L {cx - s * 0.2} {cy + s * 1.05} Z"
    tail_r = f"M {cx + s * 0.08} {cy + s * 0.05} L {cx + s * 0.55} {cy + s * 0.95} L {cx + s * 0.3} {cy + s * 0.85} L {cx + s * 0.2} {cy + s * 1.05} Z"
    st = f'stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round"' if stroke else ""
    parts = [
        f'<path d="{tail_l}" fill="{fill}" {st}/>',
        f'<path d="{tail_r}" fill="{fill}" {st}/>',
        f'<path d="{loop_l}" fill="{fill}" {st}/>',
        f'<path d="{loop_r}" fill="{fill}" {st}/>',
        f'<ellipse cx="{cx}" cy="{cy}" rx="{s * 0.2}" ry="{s * 0.24}" fill="{k}" {st}/>',
    ]
    return "".join(parts)


def scallop_band(x0, x1, y, radius, fill, holes=None, direction=1):
    """レースの縁取り。y の線から direction 方向（1=下）へ半円を並べる。holes を渡すと透かし穴を開ける"""
    out = []
    d = radius * 2
    n = int(math.ceil((x1 - x0) / d)) + 1
    for i in range(n):
        cx = x0 + i * d + radius
        out.append(
            f'<path d="M {cx - radius} {y} A {radius} {radius} 0 0 {0 if direction > 0 else 1} {cx + radius} {y} Z" fill="{fill}"/>'
        )
        if holes:
            out.append(circle(cx, y + direction * radius * 0.45, radius * 0.22, fill=holes))
    return "".join(out)


def polka(x, y, w, h, step, r, fill, opacity=1.0, offset=True):
    out = []
    rows = int(h / step) + 2
    cols = int(w / step) + 2
    for j in range(rows):
        for i in range(cols):
            ox = step / 2 if (offset and j % 2) else 0
            out.append(circle(x + i * step + ox, y + j * step, r, fill=fill, opacity=opacity))
    return "".join(out)


# ---------------- サイバー/ポップ用 ----------------

def chamfer_path(x, y, w, h, c):
    """四隅を斜めに切り落とした矩形"""
    return (
        f"M {x + c} {y} L {x + w - c} {y} L {x + w} {y + c} L {x + w} {y + h - c} "
        f"L {x + w - c} {y + h} L {x + c} {y + h} L {x} {y + h - c} L {x} {y + c} Z"
    )


def chamfer(x, y, w, h, c, **kw):
    return f'<path d="{chamfer_path(x, y, w, h, c)}" {attrs(**kw)}/>'


def hexagon_path(cx, cy, r, pointy=True):
    pts = []
    for i in range(6):
        a = math.radians(60 * i + (-90 if pointy else 0))
        pts.append(f"{cx + r * math.cos(a):.2f},{cy + r * math.sin(a):.2f}")
    return "M " + " L ".join(pts) + " Z"


def hexagon(cx, cy, r, pointy=True, **kw):
    return f'<path d="{hexagon_path(cx, cy, r, pointy)}" {attrs(**kw)}/>'


def halftone(x, y, w, h, step, rmax, fill, opacity=1.0, origin=(0, 0)):
    """網点。origin（0..1 の相対位置）に近いほど点が大きい"""
    out = []
    ox, oy = x + w * origin[0], y + h * origin[1]
    far = math.hypot(w, h)
    j = 0
    yy = y
    while yy <= y + h:
        xx = x + (step / 2 if j % 2 else 0)
        while xx <= x + w:
            r = rmax * max(0.0, 1 - math.hypot(xx - ox, yy - oy) / far * 1.4)
            if r > 0.6:
                out.append(circle(round(xx, 1), round(yy, 1), round(r, 2), fill=fill, opacity=opacity))
            xx += step
        yy += step
        j += 1
    return "".join(out)


def grid(x, y, w, h, step, stroke, width=1, opacity=1.0):
    lines = []
    gx = x
    while gx <= x + w:
        lines.append(f"M {gx} {y} L {gx} {y + h}")
        gx += step
    gy = y
    while gy <= y + h:
        lines.append(f"M {x} {gy} L {x + w} {gy}")
        gy += step
    return f'<path d="{" ".join(lines)}" stroke="{stroke}" stroke-width="{width}" opacity="{opacity}" fill="none"/>'


def hard_shadow(id_, dx, dy, color):
    """ポップな「ずらした影」。ぼかさずに輪郭ごと色を付けて置く"""
    return (
        f'<filter id="{id_}" x="-20%" y="-20%" width="140%" height="140%">'
        f'<feDropShadow dx="{dx}" dy="{dy}" stdDeviation="0" flood-color="{color}" flood-opacity="1"/></filter>'
    )


def neon(id_, std, color):
    """ネオンのにじみ"""
    return (
        f'<filter id="{id_}" x="-50%" y="-50%" width="200%" height="200%">'
        f'<feDropShadow dx="0" dy="0" stdDeviation="{std}" flood-color="{color}" flood-opacity="1"/></filter>'
    )


def bolt_path(cx, cy, s):
    """稲妻。s は高さ"""
    k = s / 180.0
    pts = [(15, -90), (-45, 10), (-5, 10), (-25, 90), (45, -20), (5, -20), (30, -90)]
    return "M " + " L ".join(f"{cx + px * k:.1f} {cy + py * k:.1f}" for px, py in pts) + " Z"


def chevrons(x, y, size, count, fill, gap=None, **kw):
    """>>> の並び"""
    gap = gap or size * 0.55
    out = []
    for i in range(count):
        cx = x + i * gap
        out.append(
            f'<path d="M {cx} {y - size / 2} L {cx + size * 0.45} {y - size / 2} L {cx + size * 0.9} {y} '
            f'L {cx + size * 0.45} {y + size / 2} L {cx} {y + size / 2} L {cx + size * 0.45} {y} Z" fill="{fill}" {attrs(**kw)}/>'
        )
    return "".join(out)
