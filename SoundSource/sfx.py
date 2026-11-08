"""操作に反応する効果音。ファイル名は se_<SeId の小文字>.wav（Unity 側の SeId と対応）。

どれも短く（多くは 0.2 秒以下）、連打しても耳障りにならない音量と帯域にしてある。
高さは C メジャー系にそろえ、確定は上がる音、取り消しは下がる音、エラーは濁った低い音。
"""
import numpy as np
from synth import *


def hover():
    # カーソルが乗った：ごく短く小さいチック
    x = square(2400, 0.025, duty=0.25) * decay_env(seconds(0.025), 0.006)
    return highpass(x, 900) * 0.35


def click():
    # 押した：2つの音が素早く上がるブリップ
    a = square(midi_to_freq(84), 0.03, duty=0.25) * decay_env(seconds(0.03), 0.012)
    b = square(midi_to_freq(91), 0.05, duty=0.25) * decay_env(seconds(0.05), 0.02)
    x = np.concatenate([a, b])
    return mix(x, noise(0.01, 1) * 0.15) * 0.6


def confirm():
    # 確定：上がる3音のアルペジオ（矩形波にノコギリを少し足して厚く）と短いエコー
    out = silence(0.0)
    for i, m in enumerate([72, 76, 79, 84]):
        d = 0.05 if i < 3 else 0.16
        tone = square(midi_to_freq(m), d, duty=0.5) * 0.6 + saw(midi_to_freq(m) * 1.003, d) * 0.3
        tone *= adsr(len(tone), 0.002, 0.03, 0.5, 0.02 if i < 3 else 0.12)
        out = np.concatenate([out, tone])
    return echo(lowpass(out, 7000), 0.09, 0.3, 3) * 0.55


def cancel():
    # 取り消し：下がる2音（三角波でやわらかく）
    a = triangle(midi_to_freq(79), 0.06) * adsr(seconds(0.06), 0.002, 0.03, 0.6, 0.02)
    b = triangle(midi_to_freq(72), 0.11) * adsr(seconds(0.11), 0.002, 0.04, 0.5, 0.07)
    x = np.concatenate([a, b])
    sq = np.concatenate([square(midi_to_freq(79), 0.06, 0.125), square(midi_to_freq(72), 0.11, 0.125)]) * 0.15
    return mix(x, sq * decay_env(len(sq), 0.05)) * 0.6


def error():
    # 使えない：濁った低い2連のブザー（ビットクラッシュでざらつかせる）
    one = (square(165, 0.07, 0.5) + square(174, 0.07, 0.5)) * 0.5 * adsr(seconds(0.07), 0.002, 0.02, 0.8, 0.02)
    gap = silence(0.035)
    x = np.concatenate([one, gap, one])
    return bitcrush(lowpass(x, 2500), bits=5, hold=3) * 0.55


def card_select():
    # カードを選んだ：上へすべるきらめき（サイン波のスイープ＋細かいノイズ）
    n = seconds(0.1)
    body = sine(sweep(700, 1700, 0.1), 0.1) * decay_env(n, 0.05)
    tip = square(sweep(1400, 3200, 0.1), 0.1, 0.25) * decay_env(n, 0.02) * 0.3
    sparkle = highpass(noise(0.1, 7), 5000) * decay_env(n, 0.03) * 0.25
    return mix(body, tip, sparkle) * 0.5


def memory_write():
    # 命令をメモリへ書き込む：データが流れ込む細かいブリップの列、最後に「カチッ」
    rng = np.random.default_rng(11)
    out = silence(0.0)
    for i in range(9):
        m = 84 + int(rng.integers(-5, 8))
        d = 0.014
        out = np.concatenate([out, square(midi_to_freq(m), d, 0.25) * decay_env(seconds(d), 0.006) * (0.4 + i * 0.04)])
    latch = mix(square(midi_to_freq(96), 0.04, 0.5) * decay_env(seconds(0.04), 0.01), highpass(noise(0.02, 2), 3000) * 0.5)
    return mix(np.concatenate([out, latch]), silence(0.2)) * 0.5


def card_return():
    # 手札へ戻す：下へすべる音（ローパスをかけながら）
    x = square(sweep(900, 380, 0.12), 0.12, 0.5) * decay_env(seconds(0.12), 0.06)
    return sweep_lowpass(x, 6000, 900) * 0.55


def command_execute():
    # 命令を実行：下へ落ちるザップと、ノイズの弾け
    n = seconds(0.2)
    zap = saw(sweep(1600, 160, 0.2), 0.2) * decay_env(n, 0.08)
    zap = sweep_lowpass(zap, 9000, 1500)
    burst = bandpass(noise(0.2, 5), 1500, 8000) * decay_env(n, 0.03)
    return soft_clip(mix(zap * 0.7, burst * 0.5), 1.5) * 0.55


def window_open():
    # ウインドウが開く：ノイズが上へ抜けるうねりと、やわらかい和音のきらめき
    n = seconds(0.26)
    whoosh = noise(0.26, 4) * adsr(n, 0.12, 0.08, 0.3, 0.06)
    whoosh = sweep_lowpass(whoosh, 800, 7000) * 0.5
    chime = silence(0.26)
    for i, m in enumerate([79, 84, 88]):
        t = triangle(midi_to_freq(m), 0.18) * decay_env(seconds(0.18), 0.07) * 0.35
        place(chime, t, seconds(0.06 + i * 0.03))
    return reverb(mix(whoosh, chime), 0.4, 0.2) * 0.6


def window_close():
    # ウインドウが閉じる：一線に潰れて下がる音と、電子の粒が散るパチパチ
    n = seconds(0.16)
    collapse = square(sweep(1200, 180, 0.16), 0.16, 0.5) * decay_env(n, 0.07)
    collapse = sweep_lowpass(collapse, 7000, 900) * 0.6
    rng = np.random.default_rng(21)
    crackle = silence(0.4)
    for _ in range(14):
        start = seconds(0.1 + rng.uniform(0, 0.25))
        d = rng.uniform(0.008, 0.02)
        blip = square(midi_to_freq(int(rng.integers(88, 104))), d, 0.25) * decay_env(seconds(d), 0.005)
        place(crackle, blip * rng.uniform(0.15, 0.35), start)
    return mix(collapse, crackle) * 0.6


def transition():
    # シーン切り替えのグリッチ：途切れ途切れのノイズと、ビットクラッシュした鳴き
    rng = np.random.default_rng(31)
    out = silence(0.45)
    t = 0.0
    while t < 0.38:
        d = rng.uniform(0.015, 0.05)
        if rng.random() < 0.55:
            s = bandpass(noise(d, int(rng.integers(0, 999))), 800, 9000) * 0.6
        else:
            s = square(sweep(rng.uniform(300, 2000), rng.uniform(300, 3000), d, "lin"), d, 0.25) * 0.4
        place(out, s * adsr(len(s), 0.001, 0.005, 0.8, 0.004), seconds(t))
        t += d + rng.uniform(0.0, 0.02)
    tail = sine(sweep(900, 90, 0.2), 0.2) * decay_env(seconds(0.2), 0.08) * 0.4
    place(out, tail, seconds(0.25))
    return bitcrush(out, bits=6, hold=2) * 0.5

def reward():
    # 勝利の報酬：上がりきる短いファンファーレ（矩形波のアルペジオ→伸ばす和音）。電子のきらめきを散らす
    out = silence(0.95)
    for i, m in enumerate([72, 76, 79, 84, 88]):
        d = 0.07
        tone = square(midi_to_freq(m), d, duty=0.25) * 0.5 * adsr(seconds(d), 0.002, 0.03, 0.6, 0.02)
        place(out, tone, seconds(i * 0.06))
    held = mix(*[
        (square(midi_to_freq(m), 0.5, duty=0.5) * 0.25 + saw(midi_to_freq(m) * 1.004, 0.5) * 0.18) * adsr(seconds(0.5), 0.005, 0.12, 0.55, 0.3)
        for m in [84, 88, 91]
    ])
    place(out, held, seconds(0.3))
    rng = np.random.default_rng(7)
    for _ in range(10):
        d = 0.02
        blip = triangle(midi_to_freq(int(rng.integers(96, 108))), d) * decay_env(seconds(d), 0.01)
        place(out, blip * 0.25, seconds(0.32 + rng.uniform(0, 0.45)))
    return echo(lowpass(out, 8000), 0.11, 0.3, 3) * 0.55


def backup_boost():
    # 敗北時の支援：電源が入り直すように下から持ち上がるうねりと、やわらかい和音（励ます音。警告にはしない）
    n = seconds(0.45)
    rise = triangle(sweep(180, 720, 0.45), 0.45) * adsr(n, 0.2, 0.1, 0.6, 0.15) * 0.45
    rise = sweep_lowpass(rise, 900, 5000)
    chime = silence(0.8)
    for i, m in enumerate([72, 76, 79]):
        t = triangle(midi_to_freq(m), 0.4) * decay_env(seconds(0.4), 0.2) * 0.3
        place(chime, t, seconds(0.35 + i * 0.07))
    return reverb(mix(rise, chime), 0.45, 0.25) * 0.6


def token_tick():
    # トークンの数字が増えていく音：ごく短い高いブリップ（連打されても耳につかない）
    d = 0.025
    return square(midi_to_freq(96), d, 0.25) * decay_env(seconds(d), 0.01) * 0.35


def rank_reveal():
    # リザルトのランクがデコードされて現れる「ザザッ」：帯域の違うノイズの短い断片を刻んで並べ、最後にカチッと留める
    rng = np.random.default_rng(31)
    out = silence(0.42)
    t = 0.0
    while t < 0.3:
        d = rng.uniform(0.015, 0.04)
        burst = lowpass(noise(d, int(rng.integers(0, 1000))), rng.uniform(1500, 7000)) * rng.uniform(0.25, 0.5)
        place(out, burst * adsr(len(burst), 0.001, 0.004, 0.8, 0.004), seconds(t))
        t += d + rng.uniform(0.005, 0.02)
    latch = square(midi_to_freq(88), 0.04, 0.25) * decay_env(seconds(0.04), 0.015) * 0.4
    place(out, latch, seconds(0.32))
    return bitcrush(out, bits=6, hold=2) * 0.7



def install_charge():
    # チップの読み込み：低いうなりが「ぐぐーっ」と上がりながら溜まっていく。
    # 読み込み（1.3秒）とエピックの溜め（0.45秒）を覆う長さで、終わりは高いところで唸り続ける（放つときに止める）
    d = 1.9
    n = seconds(d)
    t = np.arange(n) / SR
    # 前半で大きく上がり、後半は高いところで粘る
    curve = 1.0 - (1.0 - np.minimum(t / 1.3, 1.0)) ** 2
    freq = 90.0 * (4.5 ** curve)
    # だんだん速く、深くなる揺れ（溜まっていく緊張）
    wobble = 1.0 + 0.012 * (0.3 + curve) * np.sin(2 * np.pi * (5 + 14 * curve) * t)
    body = saw(freq * wobble, d) * 0.45 + square(freq * 2.0 * wobble, d, duty=0.3) * 0.18 + sine(freq * 0.5, d) * 0.35
    body = sweep_lowpass(body, 500, 5200)
    riser = sweep_lowpass(noise(d, 12), 900, 9000) * (0.05 + 0.3 * curve)
    env = np.minimum(1.0, t / 0.12) * (0.55 + 0.45 * curve)
    return soft_clip(mix(body, riser) * env, 1.3) * 0.5


def _promote(root, shimmer):
    # 光の色が1段上がる：上へ弾ける和音の打撃と、きらめきの粒
    out = silence(0.7)
    hit = mix(*[
        (square(midi_to_freq(m), 0.35, duty=0.5) * 0.3 + saw(midi_to_freq(m) * 1.004, 0.35) * 0.22) * adsr(seconds(0.35), 0.002, 0.06, 0.4, 0.2)
        for m in (root, root + 4, root + 7, root + 12)
    ])
    place(out, hit, 0)
    whoosh = sweep_lowpass(noise(0.18, 3), 1500, 9000) * decay_env(seconds(0.18), 0.06) * 0.35
    place(out, whoosh, 0)
    rng = np.random.default_rng(root)
    for _ in range(shimmer):
        dd = 0.025
        blip = triangle(midi_to_freq(int(rng.integers(root + 19, root + 31))), dd) * decay_env(seconds(dd), 0.012)
        place(out, blip * 0.25, seconds(0.05 + rng.uniform(0, 0.4)))
    return echo(lowpass(out, 9000), 0.09, 0.3, 3) * 0.55


def install_promote():
    # レアへの昇格
    return _promote(72, 6)


def install_promote_epic():
    # エピックへの昇格：より高く、きらめきを多く
    return _promote(79, 16) * 1.1


def card_flip():
    # コマンドをめくる：シャカッと擦れる短いノイズを2つ重ねるだけ（音程のあるブリップは入れない。30枚続けてめくっても耳につかないように）
    out = silence(0.12)
    for i, (start, d, low) in enumerate([(0.0, 0.05, 3000), (0.04, 0.07, 4500)]):
        n = seconds(d)
        shaka = bandpass(noise(d, 9 + i), low, 11000) * adsr(n, 0.004, 0.015, 0.45, 0.035)
        place(out, shaka * (0.5 if i == 0 else 0.4), seconds(start))
    return out * 1.2



def enemy_action():
    # 敵の行動：プレイヤーの実行音（落ちるザップ）とは別の、濁った敵意のある「ブズッ」。
    # 少しずらした低い矩形波2本を下げ、ビットクラッシュでざらつかせる
    n = seconds(0.18)
    a = square(sweep(520, 140, 0.18), 0.18, 0.5)
    b = square(sweep(548, 150, 0.18), 0.18, 0.25)
    body = (a + b) * 0.5 * adsr(n, 0.002, 0.05, 0.5, 0.08)
    grit = bandpass(noise(0.18, 41), 900, 5000) * decay_env(n, 0.04) * 0.4
    return bitcrush(lowpass(mix(body, grit), 5000), bits=5, hold=2) * 0.5


def hit():
    # 攻撃が当たった：短いノイズの弾けと、低く落ちる打撃
    n = seconds(0.14)
    thump = sine(sweep(160, 45, 0.14), 0.14) * decay_env(n, 0.06)
    snap = lowpass(noise(0.14, 43), 3500) * decay_env(n, 0.025)
    return soft_clip(mix(thump * 0.9, snap * 0.6), 1.4) * 0.6


def heal():
    # 回復：やわらかく上がる3音と、細かいきらめき
    out = silence(0.3)
    for i, m in enumerate([76, 81, 88]):
        t = triangle(midi_to_freq(m), 0.14) * adsr(seconds(0.14), 0.004, 0.05, 0.5, 0.08) * 0.5
        place(out, t, seconds(i * 0.045))
    sparkle = highpass(noise(0.3, 44), 6000) * decay_env(seconds(0.3), 0.08) * 0.12
    return reverb(mix(out, sparkle), 0.3, 0.2) * 0.55


def virus_tick():
    # ウイルス・ワームの継続ダメージ：データが腐るような、ぐずぐずした3つの泡（ビットクラッシュ）
    out = silence(0.24)
    for i, (f0, f1) in enumerate([(300, 520), (260, 460), (220, 380)]):
        d = 0.06
        blip = square(sweep(f0, f1, d, "lin"), d, 0.35) * adsr(seconds(d), 0.003, 0.02, 0.5, 0.02)
        place(out, blip * (0.7 - i * 0.12), seconds(i * 0.06))
    return bitcrush(lowpass(out, 3000), bits=4, hold=3) * 0.5


# SeId（Unity 側の enum）→ 生成関数。ファイル名は se_<キー>.wav
SOUNDS = {
    "hover": hover,
    "click": click,
    "confirm": confirm,
    "cancel": cancel,
    "error": error,
    "card_select": card_select,
    "memory_write": memory_write,
    "card_return": card_return,
    "command_execute": command_execute,
    "window_open": window_open,
    "window_close": window_close,
    "transition": transition,
    "reward": reward,
    "backup_boost": backup_boost,
    "token_tick": token_tick,
    "rank_reveal": rank_reveal,
    "install_charge": install_charge,
    "install_promote": install_promote,
    "install_promote_epic": install_promote_epic,
    "card_flip": card_flip,
    "enemy_action": enemy_action,
    "hit": hit,
    "heal": heal,
    "virus_tick": virus_tick,
}


def build():
    for name, fn in SOUNDS.items():
        x = fade(fn(), 0.001, 0.008)
        write_wav(np.clip(x, -1, 1), f"SE/se_{name}.wav")
