"""キャラクターのドット絵とコマ送りアニメ（64x64 ドット、8 倍に拡大して書き出す）。

キャラクター 1 体につき、次のファイルを書き出す:
    Characters/<Name>.png                 立ち絵（アイドルの 1 コマ目）。対象一覧やログのアイコンにも使う
    Characters/<Name>/<Name>_idle_<n>.png   アイドル（ループ）
    Characters/<Name>/<Name>_action_<n>.png 行動（1 回再生してアイドルへ戻る）
    Characters/<Name>/<Name>_hit_<n>.png    被弾（1 回再生してアイドルへ戻る）

全員「右向き」で描く。敵はバトル画面で Portrait の x スケール -1 によって左向きになる。
これらを並べたアニメの定義は Assets/ScriptableObjects/UnitVisuals/ の SpriteSheetUnitVisualDefinition で、
コマ数を変えたらそちらのコマの並びも合わせる（Unity 側の UnitVisualDefinitionBuilder で組み直せる）。
"""
from palette import *
from pixelkit import Sprite

SIZE = 64
SCALE = 8
GROUND = 61  # 足の裏の行


def pad(s, color, alpha=110):
    """足元のホログラムの台座（本体より下に敷く）"""
    s.under.ellipse(14, GROUND - 2, 49, GROUND + 2, color, alpha)
    s.under.ring(14, GROUND - 2, 49, GROUND + 2, color, 200)


# ------------------------------------------------------------------ プレイヤー: サイバーな女の子（ショートカット）

SKIN = "#FFDCC8"
SKIN_SH = "#E8A694"
HAIR = "#E4ECFF"
HAIR_SH = "#9AA8DA"
HAIR_DEEP = "#6470B0"
JACKET = "#F2F6FF"
JACKET_SH = "#AEB9DC"
INNER = "#262650"
SHORTS = "#30306A"
SOCK = "#2A2A5E"
BOOT = "#DDE6FF"
BOOT_SH = "#8C98C8"
EYE = "#19C8FF"
EYE_DEEP = "#1060C0"
HEADSET = "#3A3A7A"
MOUTH = "#C0506A"


def girl(eyes="open", arm="rest", bob=0, lean=0, hair=0, mouth="closed"):
    """eyes: open / closed / hurt, arm: rest / up / forward, bob: 体の上下, lean: 上半身の前後, hair: 毛先の揺れ"""
    s = Sprite(SIZE, SIZE)
    pad(s, CYAN_DEEP)

    # 脚（動かない）
    for x0 in (25, 32):
        s.rect(x0, 46, x0 + 3, 48, SKIN)
        s.rect(x0, 49, x0 + 3, 55, SOCK)
        s.rect(x0, 50, x0 + 3, 50, CYAN)
        s.rect(x0 - 1, 56, x0 + 4, 58, BOOT)
        s.rect(x0 - 1, 59, x0 + 5, 60, BOOT)
        s.rect(x0 - 1, 61, x0 + 5, 61, BOOT_SH)
        s.rect(x0 - 1, 56, x0 + 4, 56, CYAN)

    # 上半身（bob で上下、lean で前後）
    s.at(lean, bob)
    s.rect(24, 42, 36, 46, SHORTS)
    s.rect(24, 42, 36, 42, MAGENTA)
    s.rect(29, 42, 31, 42, YELLOW)

    # 後ろの腕
    s.rect(18, 32, 21, 41, JACKET_SH)
    s.rect(18, 41, 21, 41, CYAN_DEEP)
    s.rect(18, 42, 21, 44, SKIN_SH)

    # 胴（前を開けたオーバーサイズのジャケット）
    s.rect(26, 31, 34, 41, INNER)
    s.rect(29, 35, 31, 36, CYAN)
    s.px(30, 35, WHITE)
    s.rect(21, 31, 27, 43, JACKET)
    s.rect(33, 31, 39, 43, JACKET)
    s.rect(22, 30, 38, 30, JACKET)
    s.rect(21, 31, 22, 43, JACKET_SH)
    s.rect(21, 43, 39, 43, JACKET_SH)
    s.rect(27, 32, 27, 43, CYAN)
    s.rect(33, 32, 33, 43, CYAN)
    s.rect(35, 38, 37, 38, MAGENTA)  # 胸ポケットのライン
    s.rect(28, 29, 32, 30, SKIN_SH)  # 首

    # 前の腕
    if arm == "rest":
        s.rect(39, 32, 42, 40, JACKET)
        s.rect(41, 32, 42, 40, JACKET_SH)
        s.rect(39, 41, 42, 41, CYAN)
        s.rect(39, 42, 42, 44, SKIN)
    elif arm == "up":
        s.rect(39, 31, 43, 34, JACKET)
        s.rect(41, 26, 44, 31, JACKET)
        s.rect(41, 25, 44, 25, CYAN)
        s.rect(41, 21, 44, 24, SKIN)
    elif arm == "forward":
        s.rect(38, 32, 50, 35, JACKET)
        s.rect(38, 35, 50, 35, JACKET_SH)
        s.rect(50, 32, 50, 35, CYAN)
        s.rect(51, 32, 54, 35, SKIN)
        s.rect(55, 33, 55, 34, SKIN)

    # 頭（体と一緒に動く）
    # 後ろ髪
    s.ellipse(19, 5, 41, 29, HAIR)
    s.rect(19, 16, 24, 28, HAIR)
    s.rect(19, 12, 21, 28, HAIR_SH)
    # 顔
    s.rect(23, 15, 37, 27, SKIN)
    s.rect(25, 28, 35, 28, SKIN)
    s.rect(27, 29, 33, 29, SKIN)
    s.rect(23, 26, 24, 27, SKIN_SH)
    # 前髪（ぎざぎざの毛先）
    s.rect(22, 11, 38, 15, HAIR)
    s.pixels([(23, 16), (24, 16), (25, 17), (27, 16), (28, 16), (31, 16), (32, 17), (35, 16), (36, 16), (37, 17)], HAIR)
    s.rect(26, 8, 34, 9, WHITE)  # 天使の輪（ハイライト）
    s.rect(23, 10, 25, 10, HAIR_SH)
    # 前髪の筋（束に見せる影）
    for x, y1 in ((26, 15), (30, 16), (34, 15)):
        s.rect(x, 12, x, y1, HAIR_SH)
    s.pixels([(24, 17), (29, 17), (33, 16), (36, 17)], HAIR_SH)
    # 右側の横髪（顔の手前）
    s.rect(37, 14, 39, 23 + hair, HAIR)
    s.rect(38, 22 + hair, 39, 23 + hair, CYAN)
    # 左側の横髪（ショートボブの裾）。毛先はシアン
    s.rect(19, 26 + hair, 24, 26 + hair, HAIR)
    s.pixels([(19, 27 + hair), (20, 27 + hair), (22, 27 + hair), (23, 27 + hair), (19, 28 + hair), (22, 28 + hair)], CYAN)
    s.pixels([(19, 29 + hair), (22, 29 + hair)], CYAN_DEEP)
    s.clear(21, 28 + hair)
    s.clear(24, 27 + hair)
    s.rect(20, 12, 20, 25, HAIR_DEEP)

    # ヘッドセット（耳当てとアンテナ）
    s.rect(20, 17, 24, 23, HEADSET)
    s.rect(21, 18, 23, 22, CYAN)
    s.px(22, 20, WHITE)
    s.rect(19, 10, 20, 16, HEADSET)
    s.px(19, 9, MAGENTA)
    s.rect(21, 6, 36, 6, HEADSET)  # ヘッドバンド
    s.rect(21, 7, 21, 16, HEADSET)

    # 目（右向きなので右寄り）
    for x0 in (28, 33):
        if eyes == "open":
            s.rect(x0, 18, x0 + 2, 18, INK)
            s.px(x0 + 3 if x0 == 33 else x0 - 1, 18, INK)  # 目尻のまつげ
            s.rect(x0, 19, x0 + 2, 22, EYE)
            s.rect(x0, 19, x0 + 2, 19, EYE_DEEP)
            s.rect(x0, 22, x0 + 2, 22, CYAN)
            s.px(x0, 20, WHITE)
        elif eyes == "closed":
            s.rect(x0, 21, x0 + 2, 21, INK)
        elif eyes == "hurt":
            s.pixels([(x0, 19), (x0 + 1, 20), (x0 + 2, 21)] if x0 == 28 else [(x0 + 2, 19), (x0 + 1, 20), (x0, 21)], INK)
            s.pixels([(x0, 21)] if x0 == 28 else [(x0 + 2, 21)], INK)
    s.px(27, 24, "#FF9CB0")
    s.px(37, 24, "#FF9CB0")
    if mouth == "closed":
        s.rect(32, 25, 33, 25, MOUTH)
    elif mouth == "open":
        s.rect(32, 25, 33, 26, MOUTH)
        s.rect(32, 25, 33, 25, INK)
    s.at(0, 0)

    s.outline(INK)
    return s


def girl_frames():
    idle = []
    # 2 秒ほどで 1 周。体が 1 ドット沈んで戻り、途中で 1 回まばたき
    for i, (bob, hair, eyes) in enumerate([
        (0, 0, "open"), (0, 0, "open"), (1, 1, "open"), (1, 1, "open"),
        (0, 0, "open"), (0, 0, "closed"), (1, 1, "open"), (1, 1, "open"),
    ]):
        idle.append(girl(eyes=eyes, bob=bob, hair=hair))

    action = []
    # 0: 振りかぶる（手元に光）
    s = girl(arm="up", lean=-1, bob=1)
    s.fx.pixels([(42, 19), (43, 18), (44, 19), (43, 20)], CYAN)
    s.fx.px(43, 19, WHITE)
    action.append(s)
    # 1: 手を突き出す（小さな輪）
    s = girl(arm="forward", lean=1, mouth="open")
    s.fx.ring(55, 30, 61, 38, CYAN)
    s.fx.px(58, 34, WHITE)
    action.append(s)
    # 2: ホログラムが広がる
    s = girl(arm="forward", lean=1, mouth="open")
    s.fx.ring(51, 26, 63, 42, CYAN)
    s.fx.ring(54, 29, 62, 39, MAGENTA)
    s.fx.pixels([(58, 32), (57, 34), (59, 34), (58, 36)], WHITE)
    s.fx.pixels([(48, 26), (47, 42), (62, 24), (61, 44)], CYAN)
    action.append(s)
    # 3: 残光
    s = girl(arm="forward")
    s.fx.ring(52, 27, 63, 41, CYAN_DEEP, 160)
    s.fx.pixels([(49, 25), (62, 23), (46, 43)], CYAN_DEEP)
    action.append(s)

    hit = []
    # 0: のけぞる（グリッチで行がずれ、白く飛ぶ）
    s = girl(eyes="hurt", lean=-2, mouth="open")
    s.shift_rows(17, 21, 2)
    s.shift_rows(37, 39, -2)
    s.tint(WHITE, 0.45)
    hit.append(s)
    # 1: 戻りかけ
    s = girl(eyes="hurt", lean=-1, bob=1)
    s.shift_rows(33, 34, 1)
    hit.append(s)

    return {"idle": idle, "action": action, "hit": hit}


# ------------------------------------------------------------------ 敵（どれも右向きで描く。画面では左右反転される）

def glitch_hit(make, rows_a, rows_b):
    """被弾の 2 コマ: 1 コマ目はのけぞり＋行ずれ＋白飛び、2 コマ目は戻りかけ"""
    s = make(lean=-2, hurt=True)
    s.shift_rows(*rows_a, 2)
    s.shift_rows(*rows_b, -2)
    s.tint(WHITE, 0.45)
    t = make(lean=-1, hurt=True)
    t.shift_rows(rows_b[0], rows_b[0] + 1, 1)
    return [s, t]


# --- トロイの木馬: 車輪の付いた箱型の木馬。胴の扉からペイロードを撃つ
TRO = MAGENTA
TRO_SH = MAGENTA_DEEP
TRO_DARK = "#5A0848"


def trojan(lean=0, bob=0, nod=0, wheel=0, hatch=False, hurt=False, eye=YELLOW):
    s = Sprite(SIZE, SIZE)
    pad(s, MAGENTA_DEEP)
    # 台車と車輪（動かない）
    s.rect(10, 48, 48, 51, "#3A3A7A")
    s.rect(10, 48, 48, 48, CYAN)
    for cx in (16, 42):
        s.ellipse(cx - 5, 51, cx + 5, 61, "#26264E")
        s.ring(cx - 5, 51, cx + 5, 61, CYAN_DEEP)
        s.px(cx, 56, YELLOW)
        if wheel == 0:
            s.pixels([(cx, 53), (cx, 59)], CYAN_DEEP)
        else:
            s.pixels([(cx - 3, 56), (cx + 3, 56)], CYAN_DEEP)

    s.at(lean, bob)
    # 脚
    for x0 in (14, 22, 34, 41):
        s.rect(x0, 42, x0 + 3, 47, TRO_SH)
    # 尻尾（ケーブル）
    s.rect(7, 29, 11, 30, TRO_SH)
    s.rect(6, 31, 7, 35, TRO_SH)
    s.px(6, 36, CYAN)
    # 胴
    s.rect(11, 27, 45, 42, TRO)
    s.rect(11, 38, 45, 42, TRO_SH)
    s.rect(11, 27, 45, 27, "#FF8ADE")
    # パネルの継ぎ目
    for x in (19, 37):
        s.rect(x, 28, x, 41, TRO_SH)
    # 胴の扉
    if hatch:
        s.rect(22, 30, 34, 39, INK)
        s.rect(23, 31, 33, 38, "#2A0030")
        s.pixels([(25, 33), (28, 35), (31, 32), (26, 37), (32, 36)], LIME)
        s.rect(34, 30, 36, 39, TRO_DARK)  # 開いた扉
    else:
        s.rect(22, 30, 34, 39, TRO_DARK)
        s.rect(23, 31, 33, 38, TRO_SH)
        s.rect(27, 34, 29, 35, YELLOW)
    # 首と頭（うなずく）
    s.at(lean, bob + nod)
    s.rect(37, 16, 45, 30, TRO)
    s.rect(37, 16, 38, 30, TRO_SH)
    s.rect(38, 9, 53, 20, TRO)
    s.rect(49, 14, 57, 22, TRO)
    s.rect(49, 21, 57, 22, TRO_SH)
    s.rect(38, 9, 53, 9, "#FF8ADE")
    s.rect(40, 5, 42, 8, TRO)  # 耳
    s.px(41, 4, TRO)
    # たてがみ（シアンのぎざぎざ）
    s.pixels([(36, 10), (35, 12), (36, 14), (35, 16), (36, 18), (35, 20), (36, 22), (35, 24)], CYAN)
    s.rect(36, 9, 37, 25, CYAN_DEEP)
    # 目と鼻
    if hurt:
        s.pixels([(47, 12), (48, 13), (47, 14)], INK)
    else:
        s.rect(46, 12, 48, 14, INK)
        s.rect(47, 12, 48, 13, eye)
    s.rect(55, 17, 56, 18, TRO_DARK)
    s.rect(50, 20, 55, 20, TRO_DARK)  # 口
    s.at(0, 0)
    s.outline(INK)
    return s


def trojan_frames():
    idle = [
        trojan(wheel=0), trojan(nod=1, wheel=0),
        trojan(nod=1, wheel=1, eye=WHITE), trojan(wheel=1),
    ]
    action = []
    action.append(trojan(lean=-2, nod=-1))                  # 後ろへ溜める
    s = trojan(lean=2, nod=1, hatch=True, wheel=1)            # 扉が開き、ペイロードが飛び出す
    s.fx.rect(40, 33, 44, 35, LIME)
    s.fx.pixels([(47, 32), (50, 34), (46, 36)], LIME)
    action.append(s)
    s = trojan(lean=3, nod=1, hatch=True)
    s.fx.rect(48, 32, 53, 36, LIME)
    s.fx.rect(49, 33, 52, 35, WHITE)
    s.fx.pixels([(56, 30), (58, 34), (56, 38), (60, 36), (61, 31)], LIME)
    action.append(s)
    action.append(trojan(lean=1, wheel=1))
    hit = glitch_hit(lambda lean, hurt: trojan(lean=lean, nod=-1, hurt=hurt), (9, 16), (32, 36))
    return {"idle": idle, "action": action, "hit": hit}


# --- ルートキット: 地面から生えたフードの影。根（ケーブル）を伸ばして刺す
ROOT = "#C4540E"
ROOT_SH = "#7A2E08"
ROOT_DARK = "#3A1606"


def rootkit(lean=0, bob=0, sway=0, reach=0, hurt=False, glow=ORANGE):
    s = Sprite(SIZE, SIZE)
    pad(s, "#8A3A08")
    # 地面に張った根（sway で先が揺れる）
    for x0, x1, y in ((8, 20, 60), (40, 55, 60), (12, 22, 57), (38, 50, 57)):
        s.rect(x0, y, x1, y, ROOT_SH)
    s.pixels([(7, 59 + sway), (56, 59 - sway), (11, 56), (51, 56 + sway)], ROOT_SH)
    s.pixels([(14, 60), (47, 60), (18, 57), (43, 57)], ORANGE)

    s.at(lean, bob)
    # 体（すそが根に分かれるフード）
    s.poly([(20, 58), (18, 36), (22, 18), (32, 9), (42, 18), (46, 36), (44, 58)], ROOT)
    s.poly([(20, 58), (18, 36), (22, 18), (26, 14), (24, 36), (26, 58)], ROOT_SH)
    s.rect(32, 10, 33, 12, "#FF9A48")
    s.px(34, 13, "#FF9A48")
    # すその根の切れ込み
    for x in (25, 31, 38):
        s.rect(x, 52, x + 1, 58, ROOT_DARK)
    # フードの中の闇と目
    s.poly([(26, 34), (26, 22), (32, 16), (40, 22), (41, 34), (33, 38)], ROOT_DARK)
    s.poly([(28, 32), (28, 23), (32, 19), (38, 23), (39, 32), (33, 35)], INK)
    if hurt:
        s.pixels([(31, 25), (32, 26), (31, 27), (37, 25), (36, 26), (37, 27)], glow)
    else:
        s.rect(31, 25, 32, 27, glow)
        s.rect(36, 25, 37, 27, glow)
        s.px(31, 25, WHITE)
        s.px(36, 25, WHITE)
    # 胸の回路の光
    s.rect(31, 42, 34, 42, ORANGE)
    s.rect(32, 43, 33, 48, ORANGE)
    s.rect(28, 46, 31, 46, ORANGE)
    s.rect(34, 45, 37, 45, ORANGE)
    s.px(28, 47, YELLOW)
    s.px(37, 44, YELLOW)
    # 伸ばす根（右へ）
    if reach:
        s.rect(44, 40, 44 + reach, 42, ROOT)
        s.rect(44, 42, 44 + reach, 42, ROOT_SH)
        s.poly([(45 + reach, 38), (49 + reach, 41), (45 + reach, 44)], ROOT)
    s.at(0, 0)
    s.outline(INK)
    return s


def rootkit_frames():
    idle = [
        rootkit(sway=0), rootkit(bob=1, sway=1),
        rootkit(bob=1, sway=1, glow=YELLOW), rootkit(sway=0),
    ]
    action = [rootkit(lean=-2, bob=1, glow=YELLOW)]
    s = rootkit(lean=1, reach=6)
    action.append(s)
    s = rootkit(lean=2, reach=12)
    s.fx.pixels([(61, 38), (62, 41), (61, 44), (63, 40)], ORANGE)
    s.fx.pixels([(62, 36), (63, 45)], YELLOW)
    action.append(s)
    action.append(rootkit(lean=1, reach=4))
    hit = glitch_hit(lambda lean, hurt: rootkit(lean=lean, hurt=hurt), (22, 28), (44, 47))
    return {"idle": idle, "action": action, "hit": hit}


# --- スパイウェア: 浮かぶ単眼のドローン。レンズからスキャンの光を当てる
SPY = YELLOW
SPY_SH = "#C8A000"
SPY_DARK = "#6A5400"


def spyware(lean=0, bob=0, blink=False, prop=0, hurt=False, lens=MAGENTA, look=0):
    s = Sprite(SIZE, SIZE)
    # 浮いているので影は小さめ
    s.under.ellipse(22, GROUND - 1, 42, GROUND + 1, "#8A7A00", 110)

    s.at(lean, bob)
    # アンテナ
    s.rect(31, 6, 32, 13, "#3A3A7A")
    s.rect(30, 4, 33, 6, CYAN if prop % 2 == 0 else MAGENTA)
    # 横のプロペラ（アーム）
    s.rect(10, 22, 18, 24, "#3A3A7A")
    s.rect(45, 22, 53, 24, "#3A3A7A")
    if prop % 2 == 0:
        s.rect(6, 19, 18, 20, CYAN_DEEP)
        s.rect(45, 19, 57, 20, CYAN_DEEP)
    else:
        s.rect(9, 19, 15, 20, CYAN)
        s.rect(48, 19, 54, 20, CYAN)
    # 本体（丸い殻）
    s.ellipse(15, 12, 48, 45, SPY)
    s.ellipse(15, 12, 48, 45, SPY)
    s.poly([(18, 36), (24, 43), (32, 45), (40, 43), (46, 36), (46, 40), (38, 46), (26, 46), (17, 40)], SPY_SH)
    s.rect(24, 14, 31, 15, "#FFF59A")
    # 目（レンズ）
    s.ellipse(21, 18, 42, 39, INK)
    if blink or hurt:
        s.rect(22, 28, 41, 29, SPY_DARK)
        if hurt:
            s.pixels([(28, 24), (30, 26), (34, 31), (36, 33)], MAGENTA)
    else:
        s.ellipse(23, 20, 40, 37, "#1A1A44")
        s.ellipse(27 + look, 24, 36 + look, 33, lens)
        s.ellipse(29 + look, 26, 34 + look, 31, INK)
        s.rect(28 + look, 25, 29 + look, 26, WHITE)
    # 下の脚（カメラの三脚のような爪）
    for x0, x1 in ((22, 20), (32, 32), (41, 43)):
        s.line(x0, 44, x1, 50, "#3A3A7A")
    s.at(0, 0)
    s.outline(INK)
    return s


def spyware_frames():
    idle = [
        spyware(bob=0, prop=0), spyware(bob=-1, prop=1),
        spyware(bob=-1, prop=0, blink=True), spyware(bob=0, prop=1, look=-2),
    ]
    action = [spyware(lean=-1, prop=0, look=3, lens=YELLOW)]
    for i, reach in enumerate((10, 20)):
        s = spyware(lean=1, prop=i % 2 + 1, look=3, lens=WHITE)
        # 右へ広がるスキャンの光
        s.fx.poly([(41, 28), (41 + reach, 28 - reach // 2), (41 + reach, 30 + reach // 2), (41, 30)], YELLOW, 110)
        for y in range(28 - reach // 2, 31 + reach // 2, 3):
            s.fx.px(41 + reach, y, WHITE)
        action.append(s)
    s = spyware(prop=0, look=2)
    s.fx.pixels([(56, 22), (60, 29), (57, 36)], YELLOW)
    action.append(s)
    hit = glitch_hit(lambda lean, hurt: spyware(lean=lean, bob=1, hurt=hurt), (24, 30), (40, 43))
    return {"idle": idle, "action": action, "hit": hit}


# --- ボットネット: バイザーの付いた親機と、線でつながった子機たち
BOT = VIOLET
BOT_SH = "#5530B0"
BOT_LIGHT = "#B89CFF"


def botnet(lean=0, bob=0, orbit=0, hurt=False, fire=0, visor=CYAN):
    s = Sprite(SIZE, SIZE)
    pad(s, BOT_SH)
    # 子機の位置（orbit で回る。fire のときは右に集まる）
    ring = [(12, 18), (10, 40), (52, 12), (54, 38)]
    if fire:
        ring = [(46 + fire, 14), (48 + fire, 44), (56 + fire // 2, 22), (56 + fire // 2, 36)]
    shift = [(0, orbit), (orbit, 0), (0, -orbit), (-orbit, 0)]
    minis = [(x + dx, y + dy) for (x, y), (dx, dy) in zip(ring, shift)]

    # 親機と子機をつなぐ線（フチを付けないよう下の層に）
    for x, y in minis:
        s.under.line(32 + lean, 30 + bob, x + lean, y + bob, BOT_LIGHT, 150)

    s.at(lean, bob)
    # 脚（二本の支柱）
    s.rect(24, 46, 27, 60, "#3A3A7A")
    s.rect(37, 46, 40, 60, "#3A3A7A")
    s.rect(22, 58, 29, 60, BOT_SH)
    s.rect(35, 58, 42, 60, BOT_SH)
    # 胴
    s.rect(20, 34, 44, 47, BOT)
    s.rect(20, 44, 44, 47, BOT_SH)
    for x in range(23, 42, 4):
        s.rect(x, 38, x + 1, 41, LIME if (x // 4 + orbit) % 2 else BOT_SH)
    # 頭（四角いモニター）
    s.rect(18, 14, 46, 33, BOT)
    s.rect(18, 14, 46, 15, BOT_LIGHT)
    s.rect(18, 30, 46, 33, BOT_SH)
    s.rect(31, 9, 32, 13, "#3A3A7A")
    s.rect(30, 7, 33, 8, LIME)
    # バイザー
    s.rect(22, 19, 44, 27, INK)
    if hurt:
        s.pixels([(28, 21), (30, 23), (28, 25), (40, 21), (38, 23), (40, 25)], MAGENTA)
    else:
        s.rect(24, 22, 43, 24, visor)
        s.rect(34 + orbit, 21, 38 + orbit, 25, WHITE)
    s.at(0, 0)

    # 子機（小さな四角いボット）
    for x, y in minis:
        s.rect(x - 3 + lean, y - 3 + bob, x + 3 + lean, y + 3 + bob, BOT)
        s.rect(x - 3 + lean, y + 2 + bob, x + 3 + lean, y + 3 + bob, BOT_SH)
        s.rect(x - 1 + lean, y - 1 + bob, x + 1 + lean, y + bob, LIME if not hurt else MAGENTA)
    s.outline(INK)
    return s


def botnet_frames():
    idle = [botnet(orbit=0), botnet(orbit=1, bob=1), botnet(orbit=2, bob=1), botnet(orbit=1)]
    action = [botnet(lean=-1, orbit=2, visor=LIME)]
    s = botnet(fire=2, visor=LIME)
    action.append(s)
    s = botnet(lean=1, fire=4, visor=WHITE)
    s.fx.rect(58, 28, 63, 30, LIME)
    s.fx.pixels([(61, 25), (62, 33), (60, 20), (60, 38)], LIME)
    action.append(s)
    action.append(botnet(orbit=1))
    hit = glitch_hit(lambda lean, hurt: botnet(lean=lean, orbit=3, hurt=hurt), (19, 26), (38, 42))
    return {"idle": idle, "action": action, "hit": hit}


# ------------------------------------------------------------------ 書き出し

CHARACTERS = {
    "Player": girl_frames,
    "Trojan": trojan_frames,
    "Rootkit": rootkit_frames,
    "Spyware": spyware_frames,
    "Botnet": botnet_frames,
}


def save_character(name, frames):
    for anim, sprites in frames.items():
        for i, s in enumerate(sprites):
            s.save(f"Characters/{name}/{name}_{anim}_{i}.png", SCALE)
    frames["idle"][0].save(f"Characters/{name}.png", SCALE)


def build():
    for name, make in CHARACTERS.items():
        save_character(name, make())
