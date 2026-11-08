"""ドット単位で描くための小道具。キャラクターのコマ送りアニメに使う。

svgkit が「図形を描いて低解像度化する」のに対し、こちらは最初から 1 ドット = 1 画素のキャンバスに
アンチエイリアス無しで置いていき、最後に最近傍で拡大する。コマの間で形が 1 ドットもぶれないので、
コマ送りアニメに向いている。

描き方の流れ:
    s = Sprite(64, 64)
    s.rect(...)            # 本体（フチを付けたいもの）を描く
    s.outline(INK)         # 本体の外周に 1 ドットのフチを付ける
    s.fx.rect(...)         # 効果（光・ビーム）はフチの後に fx 層へ。フチは付かない
    s.save("Characters/Player/Player_idle_0.png", scale=8)
"""
import os
from PIL import Image, ImageDraw

from svgkit import ART_OUT


def rgba(color, alpha=255):
    """"#RRGGBB" か (r, g, b[, a]) を RGBA のタプルにする"""
    if isinstance(color, tuple):
        return color if len(color) == 4 else (*color, alpha)
    c = color.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), alpha)


class Layer:
    """1 枚の透明なドット画。座標はすべて両端を含む（rect(0, 0, 1, 1) は 2x2 ドット）"""

    def __init__(self, width, height):
        self.image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        # 以降の描画をずらす量（体ごと上下させる・のけぞらせるのに使う）
        self.dx = 0
        self.dy = 0

    def _p(self, x, y):
        return x + self.dx, y + self.dy

    def px(self, x, y, color, alpha=255):
        x, y = self._p(x, y)
        if 0 <= x < self.image.width and 0 <= y < self.image.height:
            self.image.putpixel((x, y), rgba(color, alpha))

    def rect(self, x0, y0, x1, y1, color, alpha=255):
        (x0, y0), (x1, y1) = self._p(x0, y0), self._p(x1, y1)
        self.draw.rectangle([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], fill=rgba(color, alpha))

    def ellipse(self, x0, y0, x1, y1, color, alpha=255):
        (x0, y0), (x1, y1) = self._p(x0, y0), self._p(x1, y1)
        self.draw.ellipse([x0, y0, x1, y1], fill=rgba(color, alpha))

    def ring(self, x0, y0, x1, y1, color, alpha=255):
        (x0, y0), (x1, y1) = self._p(x0, y0), self._p(x1, y1)
        self.draw.ellipse([x0, y0, x1, y1], outline=rgba(color, alpha), width=1)

    def poly(self, points, color, alpha=255):
        self.draw.polygon([self._p(x, y) for x, y in points], fill=rgba(color, alpha))

    def line(self, x0, y0, x1, y1, color, alpha=255):
        self.draw.line([self._p(x0, y0), self._p(x1, y1)], fill=rgba(color, alpha), width=1)

    def pixels(self, points, color, alpha=255):
        for x, y in points:
            self.px(x, y, color, alpha)

    def clear(self, x, y):
        x, y = self._p(x, y)
        if 0 <= x < self.image.width and 0 <= y < self.image.height:
            self.image.putpixel((x, y), (0, 0, 0, 0))

    def at(self, dx=0, dy=0):
        """描画のずらし量を設定する（呼び出し側で元に戻す）"""
        self.dx, self.dy = dx, dy
        return self


class Sprite(Layer):
    """本体の層（フチを付ける）と、その上に重ねる効果の層 fx（フチを付けない）を持つ 1 コマ"""

    def __init__(self, width, height):
        super().__init__(width, height)
        self.under = Layer(width, height)  # 足元の影など、本体より下に敷くもの
        self.fx = Layer(width, height)

    def outline(self, color):
        """本体の外周（上下左右で隣が透明なところ）に 1 ドットのフチを付ける"""
        src = self.image.copy()
        w, h = src.size
        pix = src.load()
        ink = rgba(color)
        for y in range(h):
            for x in range(w):
                if pix[x, y][3] != 0:
                    continue
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h and pix[nx, ny][3] == 255:
                        self.image.putpixel((x, y), ink)
                        break

    def shift_rows(self, y0, y1, amount):
        """y0..y1 の行を横にずらす（グリッチ）。本体の層だけに効く"""
        w = self.image.width
        band = self.image.crop((0, y0, w, y1 + 1))
        self.image.paste((0, 0, 0, 0), (0, y0, w, y1 + 1))
        self.image.paste(band, (amount, y0))

    def tint(self, color, amount):
        """本体の色を color に amount (0..1) だけ寄せる（被弾の白飛びなど）"""
        r, g, b, _ = rgba(color)
        pix = self.image.load()
        for y in range(self.image.height):
            for x in range(self.image.width):
                pr, pg, pb, pa = pix[x, y]
                if pa:
                    pix[x, y] = (
                        round(pr + (r - pr) * amount),
                        round(pg + (g - pg) * amount),
                        round(pb + (b - pb) * amount),
                        pa,
                    )

    def composed(self):
        out = self.under.image.copy()
        out.alpha_composite(self.image)
        out.alpha_composite(self.fx.image)
        return out

    def save(self, relative_path, scale=8):
        out = os.path.join(ART_OUT, relative_path)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        image = self.composed()
        image.resize((image.width * scale, image.height * scale), Image.NEAREST).save(out)
        return out
