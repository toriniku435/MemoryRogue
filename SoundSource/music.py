"""各画面の BGM（ループ）。ファイル名は bgm_<BgmId の小文字>.wav（Unity 側の BgmId と対応）。

作り方:
  - コード進行（4小節で一巡）を決め、パッド・ベース・アルペジオ・ドラム・リードを重ねる
  - メロディは、コードの音を強拍に置き、音階の音でつなぐように、シード固定の乱数で作る（毎回同じ曲になる）。
    4小節のフレーズ A・B を作り、A B A B' と並べる（B' は最後に主音へ落ち着く）
  - ループの継ぎ目で残響が途切れないよう、曲の後ろにはみ出した残響を曲の頭に重ねてから切り出す
コード進行そのものはありふれたものだが、メロディ・編成・音色はすべてここで作ったもの。
"""
import numpy as np
from synth import *

# ---------------- 和音と音階 ----------------

CHORDS = {
    "M": [0, 4, 7], "m": [0, 3, 7], "M7": [0, 4, 7, 11], "m7": [0, 3, 7, 10],
    "7": [0, 4, 7, 10], "6": [0, 4, 7, 9], "sus2": [0, 2, 7], "add9": [0, 4, 7, 14],
}

SCALES = {
    "major": [0, 2, 4, 5, 7, 9, 11],
    "minor": [0, 2, 3, 5, 7, 8, 10],
    "dorian": [0, 2, 3, 5, 7, 9, 10],
}

NOTE = {"C": 0, "C#": 1, "D": 2, "Eb": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "Ab": 8, "A": 9, "Bb": 10, "B": 11}


def chord(name, octave=4):
    root, quality = name.split(":")
    base = 12 * (octave + 1) + NOTE[root]
    return [base + i for i in CHORDS[quality]]


# ---------------- 楽器 ----------------

def inst_pad(midis, dur, bright=1.0, seed=0):
    total = dur + 0.5
    x = sum(supersaw(midi_to_freq(m), total, voices=5, detune=0.01, seed=seed + i) for i, m in enumerate(midis))
    x = x / max(1, len(midis))
    return x * adsr(len(x), 0.25, 0.3, 0.75, 0.5)


def inst_bass(m, dur):
    total = dur + 0.03
    f = midi_to_freq(m)
    x = square(f, total, 0.5) * 0.6 + saw(f * 1.004, total) * 0.4
    return x * adsr(len(x), 0.003, 0.12, 0.55, 0.03)


def inst_arp(m, dur, duty=0.25):
    total = dur + 0.02
    x = square(midi_to_freq(m), total, duty)
    return x * decay_env(len(x), 0.07) * adsr(len(x), 0.002, 0.0, 1.0, 0.01)


def inst_lead(m, dur):
    total = dur + 0.08
    n = seconds(total)
    t = np.arange(n) / SR
    vibrato = 1.0 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.15) / 0.2, 0, 1)
    f = midi_to_freq(m) * vibrato
    x = square(f, total, 0.5) * 0.65 + saw(f * 1.002, total) * 0.35
    return x * adsr(n, 0.01, 0.1, 0.7, 0.08)


def inst_bell(m, dur):
    total = dur + 0.4
    f = midi_to_freq(m)
    x = triangle(f, total) * 0.7 + sine(f * 2, total) * 0.3
    return x * decay_env(len(x), 0.35) * adsr(len(x), 0.002, 0.0, 1.0, 0.05)


def drum_kick():
    n = seconds(0.3)
    body = sine(sweep(170, 45, 0.3), 0.3) * decay_env(n, 0.11)
    click_ = highpass(noise(0.01, 1), 2000) * 0.3
    return mix(body, click_)


def drum_snare(seed=2):
    n = seconds(0.22)
    tone = sine(190, 0.22) * decay_env(n, 0.04) * 0.5
    rattle = bandpass(noise(0.22, seed), 1500, 9000) * decay_env(n, 0.07)
    return bitcrush(mix(tone, rattle), bits=7, hold=2)


def drum_hat(seed=3, open_=False):
    d = 0.18 if open_ else 0.05
    x = highpass(noise(d, seed), 7000) * decay_env(seconds(d), 0.06 if open_ else 0.015)
    return x * 0.6


def drum_clap(seed=4):
    out = silence(0.2)
    for i in range(3):
        place(out, bandpass(noise(0.02, seed + i), 1000, 6000) * decay_env(seconds(0.02), 0.006), seconds(i * 0.012))
    tail = bandpass(noise(0.15, seed + 9), 1000, 5000) * decay_env(seconds(0.15), 0.05) * 0.6
    place(out, tail, seconds(0.036))
    return out


# ---------------- 曲の組み立て ----------------

class Song:
    def __init__(self, bpm, bars):
        self.bpm = bpm
        self.bars = bars
        self.spb = 60.0 / bpm
        self.length = seconds(bars * 4 * self.spb)
        self.tail = seconds(3.0)
        self.tracks = {}

    def track(self, name, pan=0.0, send=0.0, lp=None, hp=None, gain=1.0):
        self.tracks[name] = {"buf": np.zeros(self.length + self.tail), "pan": pan, "send": send, "lp": lp, "hp": hp, "gain": gain}
        return name

    def at(self, beat):
        return seconds(beat * self.spb)

    def add(self, name, sound, beat, velocity=1.0):
        place(self.tracks[name]["buf"], sound * velocity, self.at(beat))

    def render(self):
        left = np.zeros(self.length + self.tail)
        right = np.zeros(self.length + self.tail)
        send = np.zeros(self.length + self.tail)
        for tr in self.tracks.values():
            x = tr["buf"]
            if tr["lp"]:
                x = lowpass(x, tr["lp"])
            if tr["hp"]:
                x = highpass(x, tr["hp"])
            x = x * tr["gain"]
            angle = (tr["pan"] + 1) * np.pi / 4
            left += x * np.cos(angle)
            right += x * np.sin(angle)
            send += x * tr["send"]
        wet = reverb(send, size=1.2, mix=1.0)[:len(send)]
        left += wet * 0.9
        right += np.roll(wet, seconds(0.013)) * 0.9
        stereo = np.stack([left, right], axis=1)
        # ループ：はみ出した残響を頭に重ねてから、ちょうどの長さで切る
        stereo[:self.tail] += stereo[self.length:self.length + self.tail]
        stereo = stereo[:self.length]
        stereo = soft_clip(stereo, 1.1)
        return normalize(stereo, 0.85)


def melody(rng, chords_by_bar, scale_root, scale, low, high, rhythms, start):
    """フレーズ A・B（各4小節）を作り、A B A B' に並べる。rhythms は4小節分の [(拍, 長さ), ...]"""
    pcs = [(scale_root + s) % 12 for s in SCALES[scale]]

    def phrase(offset, prev, cadence):
        notes = []
        for bar in range(4):
            chord_pcs = [m % 12 for m in chords_by_bar[(offset + bar) % len(chords_by_bar)]]
            for beat, length in rhythms[bar]:
                strong = float(beat).is_integer() and int(beat) % 2 == 0
                allowed = chord_pcs if strong else pcs
                cands = [p for p in range(low, high + 1) if p % 12 in allowed]
                if cadence and bar == 3 and beat == rhythms[3][-1][0]:
                    cands = [p for p in range(low, high + 1) if p % 12 == scale_root % 12]
                w = np.array([np.exp(-abs(c - prev) / 2.5) * (0.4 if c == prev else 1.0) for c in cands])
                prev = int(rng.choice(cands, p=w / w.sum()))
                notes.append((bar * 4 + beat, length, prev))
        return notes, prev

    a, p = phrase(0, start, False)
    b, p = phrase(4, p, False)
    b2, _ = phrase(4, p, True)
    out = []
    for i, part in enumerate([a, b, a, b2]):
        out += [(i * 16 + beat, length, m) for beat, length, m in part]
    return out


def add_melody(song, name, notes, instrument, bar_offset=0, velocity=0.8):
    for beat, length, m in notes:
        song.add(name, instrument(m, length * song.spb * 0.95), bar_offset * 4 + beat, velocity)


def arp_pattern(song, name, chords_by_bar, bars, step=0.25, octave_up=12, order=(0, 1, 2, 1), velocity=0.45, duty=0.25, start_bar=0):
    for bar in range(start_bar, bars):
        ch = chords_by_bar[bar % len(chords_by_bar)]
        tones = [m + octave_up for m in ch[:3]]
        steps = int(4 / step)
        for s in range(steps):
            m = tones[order[s % len(order)] % len(tones)]
            if s % 8 == 7:
                m += 12
            song.add(name, inst_arp(m, step * song.spb * 0.9, duty), bar * 4 + s * step, velocity)


def pad_part(song, name, chords_by_bar, bars, velocity=0.35, seed=0):
    for bar in range(bars):
        song.add(name, inst_pad(chords_by_bar[bar % len(chords_by_bar)], 4 * song.spb, seed=seed + bar % 4), bar * 4, velocity)


# ---------------- 各曲 ----------------

def title():
    # 夢の入口：ゆったりしたシンセウェーブのパッドに、チップのアルペジオとやさしいリード
    song = Song(bpm=100, bars=16)
    chords = [chord("A:m", 3), chord("F:M", 3), chord("C:M", 4), chord("G:M", 3)]
    song.track("pad", pan=0.0, send=0.35, lp=2600)
    song.track("arp", pan=0.35, send=0.25, lp=6000, gain=0.8)
    song.track("bass", pan=0.0, lp=900)
    song.track("drums", pan=0.0, send=0.08)
    song.track("lead", pan=-0.2, send=0.4, lp=5000)
    pad_part(song, "pad", chords, 16)
    arp_pattern(song, "arp", chords, 16, step=0.25, order=(0, 1, 2, 1, 2, 0, 1, 2))
    for bar in range(16):
        root = chords[bar % 4][0] - 12
        song.add("bass", inst_bass(root, 2 * song.spb * 0.95), bar * 4, 0.6)
        song.add("bass", inst_bass(root, 1.5 * song.spb * 0.95), bar * 4 + 2.5, 0.5)
        if bar >= 4:
            song.add("drums", drum_kick(), bar * 4, 0.8)
            song.add("drums", drum_kick(), bar * 4 + 2.5, 0.5)
            song.add("drums", drum_snare(), bar * 4 + 2, 0.45)
            for h in range(8):
                song.add("drums", drum_hat(seed=h), bar * 4 + h * 0.5, 0.25 if h % 2 else 0.12)
    rng = np.random.default_rng(101)
    rhythms = [[(0, 1.5), (1.5, 0.5), (2, 2)], [(0, 1), (1, 1), (2, 1.5), (3.5, 0.5)], [(0, 1.5), (1.5, 1.5), (3, 1)], [(0, 3), (3, 1)]]
    notes = melody(rng, chords, 9, "minor", 69, 84, rhythms, 76)
    # リードは後半の8小節だけ（前半はアルペジオとパッドで空気を作る）
    for beat, length, m in notes:
        if beat >= 32:
            song.add("lead", inst_lead(m, length * song.spb * 0.95), beat, 0.5)
    return song.render()


def map_():
    # サイバー空間の探索：ドリアンのベル、軽いアルペジオ、控えめなビート
    song = Song(bpm=92, bars=16)
    chords = [chord("D:m7", 3), chord("G:M", 3), chord("D:m7", 3), chord("C:M", 3)]
    song.track("pad", pan=0.0, send=0.4, lp=2000)
    song.track("arp", pan=-0.3, send=0.3, lp=5000, gain=0.7)
    song.track("bass", pan=0.0, lp=700)
    song.track("drums", pan=0.0, send=0.1)
    song.track("bell", pan=0.3, send=0.45, lp=6000)
    pad_part(song, "pad", chords, 16, velocity=0.3, seed=20)
    arp_pattern(song, "arp", chords, 16, step=0.5, order=(0, 2, 1, 2), duty=0.125, velocity=0.4)
    for bar in range(16):
        root = chords[bar % 4][0] - 12
        for b in (0, 1.5, 3):
            song.add("bass", inst_bass(root if b != 1.5 else root + 7, 0.9 * song.spb), bar * 4 + b, 0.5)
        song.add("drums", drum_kick(), bar * 4, 0.6)
        if bar >= 2:
            song.add("drums", drum_clap(), bar * 4 + 3, 0.25)
        for h in range(4):
            song.add("drums", drum_hat(seed=h + 10), bar * 4 + h + 0.5, 0.22)
    rng = np.random.default_rng(202)
    rhythms = [[(0, 1), (1, 0.5), (1.5, 1.5), (3, 1)], [(0.5, 1.5), (2, 2)], [(0, 1), (1, 1), (2, 1), (3, 1)], [(0, 2), (2, 2)]]
    notes = melody(rng, chords, 2, "dorian", 67, 86, rhythms, 74)
    for beat, length, m in notes:
        if beat >= 16:
            song.add("bell", inst_bell(m, length * song.spb * 0.9), beat, 0.45)
    return song.render()


def inst_stab(midis, dur):
    # トランスのオフビートの和音：明るいスーパーソウを短く切る
    total = dur + 0.05
    x = sum(supersaw(midi_to_freq(m), total, voices=7, detune=0.014, seed=70 + i) for i, m in enumerate(midis))
    x = x / max(1, len(midis))
    return x * adsr(len(x), 0.004, 0.09, 0.45, 0.04)


def drum_crash(seed=6):
    # サビの頭のシンバル
    n = seconds(1.4)
    return highpass(noise(1.4, seed), 5000) * decay_env(n, 0.45) * 0.55


def fx_riser(duration, seed=8):
    # ビルドアップで上がっていくノイズ（フィルターが開きながら大きくなる）
    n = seconds(duration)
    x = sweep_lowpass(noise(duration, seed), 300, 10000)
    return x * np.linspace(0.0, 1.0, n) ** 2


def sidechain(song, name, depth=0.65, release=0.45):
    """4分ごとに音量を沈めて戻す（キックに合わせてうねらせる、トランスのポンプ）"""
    buf = song.tracks[name]["buf"]
    beat = seconds(song.spb)
    curve = np.ones(beat)
    rise = int(beat * release)
    curve[:rise] = 1.0 - depth * (1.0 - np.linspace(0.0, 1.0, rise)) ** 2
    reps = int(np.ceil(len(buf) / beat))
    buf *= np.tile(curve, reps)[:len(buf)]


def battle():
    # 戦闘: トランス寄り 150BPM・ニ長調（I → V → vi → IV の前向きな進行）。32小節で A → A' → ビルドアップ → サビ と上げていく。
    # 短調は暗すぎ、ドラムは大げさだったので、長調にしてドラムは伴奏として控えめにしてある
    #   A (0-7)   : 4つ打ち、転がるベース（16分でキックの隙間を埋める）、16分のアルペジオ、リードが入る
    #   A' (8-15) : クラップとオフビートの和音が加わる
    #   Build (16-23): ベースは伸ばし、スネアの連打が加速し、ノイズが上がる。最後の半拍は無音にして溜める
    #   Drop (24-31): シンバルとともに全部入り。スーパーソウの和音をキックに合わせてうねらせ、リードは1オクターブ上
    song = Song(bpm=150, bars=32)
    chords = [chord("D:M", 3), chord("A:M", 3), chord("B:m", 3), chord("G:M", 3)]
    song.track("pad", pan=0.0, send=0.3, lp=3500, gain=0.7)
    song.track("stab", pan=0.0, send=0.25, lp=9000, gain=0.75)
    song.track("arp", pan=0.35, send=0.2, lp=7500, gain=0.55)
    song.track("bass", pan=0.0, lp=1400, gain=1.0)
    song.track("kick", pan=0.0, gain=0.9)
    song.track("drums", pan=0.0, send=0.06, gain=0.7)
    song.track("fx", pan=0.0, send=0.35, gain=0.45)
    song.track("lead", pan=-0.2, send=0.3, lp=7000, gain=0.9)
    song.track("lead2", pan=0.25, send=0.3, lp=4000, gain=0.5)

    build = range(16, 24)
    drop = range(24, 32)

    for bar in range(32):
        ch = chords[bar % 4]
        root = ch[0] - 12
        in_build = bar in build
        in_drop = bar in drop

        # パッド（ビルドアップで少しずつ大きく）
        pad_vel = (0.12 if bar < 8 else 0.2) + (0.03 * (bar - 16) if in_build else 0.0) + (0.1 if in_drop else 0.0)
        song.add("pad", inst_pad(ch, 4 * song.spb, seed=40 + bar % 4), bar * 4, pad_vel)

        # キック: ビルドアップの後半（22-23小節）は抜いて溜める
        if not (in_build and bar >= 22):
            for q in range(4):
                song.add("kick", drum_kick(), bar * 4 + q, 0.75)

        # ベース
        if in_build:
            # 2拍ずつ伸ばして、うねりを止める
            for half in range(2):
                song.add("bass", inst_bass(root, 1.9 * song.spb), bar * 4 + half * 2, 0.5)
        else:
            # 転がるベース: 各拍の2〜4つ目の16分（頭はキックに譲る）
            for q in range(4):
                for s16 in (1, 2, 3):
                    m = root + (12 if (in_drop and s16 == 3) else 0)
                    song.add("bass", inst_bass(m, 0.22 * song.spb), bar * 4 + q + s16 * 0.25, 0.4 if bar < 8 else 0.5)

        # ハイハット: オフビート（サビはオープン気味に強く）
        if not (in_build and bar >= 22):
            for q in range(4):
                song.add("drums", drum_hat(seed=q + bar, open_=in_drop), bar * 4 + q + 0.5, 0.2 if in_drop else 0.16)

        # クラップ（2・4拍）
        if (8 <= bar < 16) or in_drop:
            song.add("drums", drum_clap(seed=bar), bar * 4 + 1, 0.3)
            song.add("drums", drum_clap(seed=bar + 20), bar * 4 + 3, 0.3)

        # オフビートの和音（A'では控えめ、サビでは主役）
        if (8 <= bar < 16) or in_drop:
            stab_chord = [m + 12 for m in ch] + [ch[0] + 24]
            for q in range(4):
                song.add("stab", inst_stab(stab_chord, 0.4 * song.spb), bar * 4 + q + 0.5, 0.9 if in_drop else 0.3)

    # アルペジオ: 最初から最後まで16分で。ビルドアップ以降は1オクターブ上げて明るくする
    arp_pattern(song, "arp", chords, 8, step=0.25, order=(0, 1, 2, 1), velocity=0.22)
    arp_pattern(song, "arp", chords, 16, step=0.25, order=(0, 1, 2, 1), velocity=0.3, start_bar=8)
    arp_pattern(song, "arp", chords, 32, step=0.25, octave_up=24, order=(0, 2, 1, 2), velocity=0.3, start_bar=16)

    # ビルドアップ: スネアの連打が 4分 → 8分 → 16分 と詰まり、少しずつ大きくなっていく（主張しすぎない音量で）
    for bar in build:
        k = bar - 16
        step = 1.0 if k < 3 else 0.5 if k < 6 else 0.25
        count = int(4 / step)
        for i in range(count):
            beat = bar * 4 + i * step
            if bar == 23 and i * step >= 3.5:
                break  # サビの直前の半拍は無音にして溜める
            vel = 0.1 + 0.25 * ((k * 4 + i * step) / 32.0)
            song.add("drums", drum_snare(seed=bar * 16 + i), beat, vel)
    song.add("fx", fx_riser(8 * 4 * song.spb - 0.5 * song.spb), 16 * 4, 0.35)

    # サビの頭にだけシンバル
    song.add("fx", drum_crash(seed=24), 24 * 4, 0.45)

    # サビの和音とパッドをキックに合わせてうねらせる
    sidechain(song, "stab", depth=0.7)
    sidechain(song, "pad", depth=0.45)

    # メロディ: A・A' は中音域、サビは1オクターブ上で、下にノコギリ波を重ねて厚くする
    rng = np.random.default_rng(1500)
    verse_rhythms = [[(0, 0.75), (0.75, 0.75), (1.5, 0.5), (2, 1), (3, 0.5), (3.5, 0.5)],
                     [(0, 1.5), (1.5, 0.5), (2, 0.75), (2.75, 1.25)],
                     [(0, 0.5), (0.5, 0.5), (1, 0.75), (1.75, 0.75), (2.5, 1.5)],
                     [(0, 2.5), (3, 1)]]
    verse = melody(rng, chords, 2, "major", 62, 81, verse_rhythms, 69)
    for beat, length, m in verse:
        if beat >= 16:  # 最初の4小節は伴奏だけで始める
            song.add("lead", inst_lead(m, length * song.spb * 0.9), beat, 0.36)

    chorus_rhythms = [[(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 1), (2.5, 0.5), (3, 1)],
                      [(0, 0.75), (0.75, 0.75), (1.5, 0.5), (2, 2)],
                      [(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 0.5), (2.5, 0.5), (3, 1)],
                      [(0, 1), (1, 1), (2, 2)]]
    chorus = melody(np.random.default_rng(2400), chords, 2, "major", 69, 88, chorus_rhythms, 74)
    for beat, length, m in chorus:
        if beat >= 32:
            break  # A・B の8小節ぶん
        at = 24 * 4 + beat
        song.add("lead", inst_lead(m, length * song.spb * 0.92), at, 0.58)
        song.add("lead2", saw(midi_to_freq(m - 12), length * song.spb * 0.92 + 0.05) * adsr(seconds(length * song.spb * 0.92 + 0.05), 0.01, 0.1, 0.7, 0.05), at, 0.35)

    # サビの直前の半拍は、ノイズ以外を実際に止めて溜める（伸びている和音やベースも切る）
    gap_start, gap_end = song.at(23 * 4 + 3.5), song.at(24 * 4)
    fade_n = seconds(0.01)
    for name in ("pad", "stab", "arp", "bass", "lead", "lead2", "drums"):
        buf = song.tracks[name]["buf"]
        buf[gap_start:gap_start + fade_n] *= np.linspace(1.0, 0.0, fade_n)
        buf[gap_start + fade_n:gap_end] = 0.0

    return song.render()


def deck_edit():
    # プログラムを書く時間：メジャー7thのやわらかいパッド、三角波のゆっくりしたアルペジオ、軽いビート
    song = Song(bpm=88, bars=16)
    chords = [chord("C:M7", 3), chord("A:m7", 3), chord("F:M7", 3), chord("G:6", 3)]
    song.track("pad", pan=0.0, send=0.45, lp=1800)
    song.track("arp", pan=0.3, send=0.35, lp=4000, gain=0.8)
    song.track("bass", pan=0.0, lp=600)
    song.track("drums", pan=0.0, send=0.1, gain=0.8)
    song.track("bell", pan=-0.3, send=0.5, lp=5000)
    pad_part(song, "pad", chords, 16, velocity=0.3, seed=60)
    for bar in range(16):
        ch = chords[bar % 4]
        for s, idx in enumerate([0, 2, 1, 3, 2, 1, 3, 2]):
            m = ch[idx % len(ch)] + 12
            t = triangle(midi_to_freq(m), 0.5 * song.spb) * decay_env(seconds(0.5 * song.spb), 0.2)
            song.add("arp", t, bar * 4 + s * 0.5, 0.35)
        song.add("bass", inst_bass(ch[0] - 12, 3 * song.spb), bar * 4, 0.45)
        song.add("drums", drum_kick(), bar * 4, 0.5)
        song.add("drums", drum_kick(), bar * 4 + 2.5, 0.35)
        song.add("drums", drum_hat(seed=bar), bar * 4 + 1, 0.2)
        song.add("drums", drum_hat(seed=bar + 5), bar * 4 + 3, 0.2)
    rng = np.random.default_rng(404)
    rhythms = [[(0, 2), (2, 1), (3, 1)], [(0, 1.5), (1.5, 2.5)], [(0, 1), (1, 1), (2, 2)], [(0, 4)]]
    notes = melody(rng, chords, 0, "major", 67, 84, rhythms, 76)
    for beat, length, m in notes:
        if beat >= 32:
            song.add("bell", inst_bell(m, length * song.spb * 0.9), beat, 0.4)
    return song.render()


def shop():
    # データマーケット：弾むベース（根音と5度）、手拍子、明るいチップのリード
    song = Song(bpm=116, bars=16)
    chords = [chord("F:M", 3), chord("D:m", 3), chord("Bb:M", 3), chord("C:M", 3)]
    song.track("pad", pan=0.0, send=0.3, lp=2800, gain=0.8)
    song.track("arp", pan=-0.35, send=0.2, lp=6500, gain=0.7)
    song.track("bass", pan=0.0, lp=1000)
    song.track("drums", pan=0.0, send=0.08)
    song.track("lead", pan=0.25, send=0.3, lp=6500)
    pad_part(song, "pad", chords, 16, velocity=0.28, seed=80)
    arp_pattern(song, "arp", chords, 16, step=0.5, order=(0, 1, 2, 1), duty=0.125, velocity=0.4)
    for bar in range(16):
        root = chords[bar % 4][0] - 12
        for b, off in ((0, 0), (1, 7), (2, 12), (3, 7)):
            song.add("bass", inst_bass(root + off, 0.8 * song.spb), bar * 4 + b, 0.5)
        song.add("drums", drum_kick(), bar * 4, 0.7)
        song.add("drums", drum_kick(), bar * 4 + 2, 0.6)
        song.add("drums", drum_clap(seed=bar), bar * 4 + 1, 0.35)
        song.add("drums", drum_clap(seed=bar + 40), bar * 4 + 3, 0.35)
        for h in range(4):
            song.add("drums", drum_hat(seed=h + bar * 3), bar * 4 + h + 0.5, 0.22)
    rng = np.random.default_rng(505)
    rhythms = [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 0.5), (2.5, 0.5), (3, 1)],
               [(0, 1), (1, 1), (2, 2)],
               [(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 1), (3, 1)],
               [(0, 3), (3, 1)]]
    notes = melody(rng, chords, 5, "major", 69, 86, rhythms, 77)
    for beat, length, m in notes:
        if beat >= 16:
            song.add("lead", inst_lead(m, length * song.spb * 0.85), beat, 0.42)
    return song.render()


# BgmId（Unity 側の enum）→ 生成関数。ファイル名は bgm_<キー>.wav
SONGS = {
    "title": title,
    "map": map_,
    "battle": battle,
    "deck_edit": deck_edit,
    "shop": shop,
}


def build(names=None):
    for name, fn in SONGS.items():
        if names and name not in names:
            continue
        write_wav(fn(), f"BGM/bgm_{name}.wav")
        print(f"  bgm_{name}")
