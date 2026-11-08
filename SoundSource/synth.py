"""音の部品。波形・エンベロープ・フィルター・エフェクトと、WAV の書き出し。

方向性: チップチューン × シンセウェーブの中間。
  チップ側 : 矩形波（デューティ比を変える）、三角波、ノイズ、軽いビットクラッシュ
  シンセ側 : デチューンしたノコギリ波の厚いパッド、ローパス、エコー、簡単なリバーブ
どの音もここの部品をコードで組み合わせて作る（既存の曲・音源・サンプルは一切使わない）。
"""
import os
import wave

import numpy as np

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Audio")


# ---------------- 基本 ----------------

def seconds(n):
    return int(round(n * SR))


def silence(duration):
    return np.zeros(seconds(duration))


def midi_to_freq(midi):
    return 440.0 * 2.0 ** ((np.asarray(midi, dtype=float) - 69.0) / 12.0)


def phase_of(freq, n):
    """周波数（定数か、サンプルごとの配列）から位相を積み上げる。スイープやビブラートはこれで作る"""
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,))
    return 2.0 * np.pi * np.cumsum(f) / SR


# ---------------- 波形 ----------------

def sine(freq, duration):
    n = seconds(duration)
    return np.sin(phase_of(freq, n))


def square(freq, duration, duty=0.5):
    n = seconds(duration)
    ph = (phase_of(freq, n) / (2 * np.pi)) % 1.0
    return np.where(ph < duty, 1.0, -1.0)


def triangle(freq, duration):
    n = seconds(duration)
    ph = (phase_of(freq, n) / (2 * np.pi)) % 1.0
    return 4.0 * np.abs(ph - 0.5) - 1.0


def saw(freq, duration):
    n = seconds(duration)
    ph = (phase_of(freq, n) / (2 * np.pi)) % 1.0
    return 2.0 * ph - 1.0


def supersaw(freq, duration, voices=5, detune=0.012, seed=0):
    """デチューンしたノコギリ波を重ねた厚い音（シンセウェーブのパッド）"""
    rng = np.random.default_rng(seed)
    out = np.zeros(seconds(duration))
    for i in range(voices):
        d = 1.0 + detune * (i - (voices - 1) / 2) / max(1, (voices - 1) / 2)
        n = seconds(duration)
        ph = (phase_of(np.asarray(freq) * d, n) / (2 * np.pi) + rng.random()) % 1.0
        out += 2.0 * ph - 1.0
    return out / voices


def noise(duration, seed=0):
    return np.random.default_rng(seed).uniform(-1.0, 1.0, seconds(duration))


def sweep(start, end, duration, curve="exp"):
    """start→end へ変わる周波数の配列"""
    n = seconds(duration)
    x = np.linspace(0.0, 1.0, n)
    if curve == "exp":
        return start * (end / start) ** x
    return start + (end - start) * x


# ---------------- エンベロープ ----------------

def adsr(n, attack=0.005, decay=0.05, sustain=0.6, release=0.05):
    """n サンプル分。release は末尾の中に含める"""
    a, d, r = seconds(attack), seconds(decay), seconds(release)
    env = np.full(n, sustain, dtype=float)
    a = min(a, n)
    env[:a] = np.linspace(0.0, 1.0, a, endpoint=False) if a > 0 else env[:a]
    d_end = min(n, a + d)
    if d_end > a:
        env[a:d_end] = np.linspace(1.0, sustain, d_end - a, endpoint=False)
    r = min(r, n)
    if r > 0:
        env[n - r:] *= np.linspace(1.0, 0.0, r)
    return env


def decay_env(n, time):
    """指数的に減衰する（打楽器・ブリップ用）"""
    t = np.arange(n) / SR
    return np.exp(-t / max(1e-4, time))


# ---------------- フィルター（周波数領域。止まっている音色向け） ----------------

def _fft_filter(x, response):
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / SR)
    return np.fft.irfft(spectrum * response(freqs), n=len(x))


def lowpass(x, cutoff, order=2):
    return _fft_filter(x, lambda f: 1.0 / np.sqrt(1.0 + (f / cutoff) ** (2 * order)))


def highpass(x, cutoff, order=2):
    return _fft_filter(x, lambda f: 1.0 / np.sqrt(1.0 + (cutoff / np.maximum(f, 1e-6)) ** (2 * order)))


def bandpass(x, low, high):
    return highpass(lowpass(x, high), low)


def sweep_lowpass(x, start, end):
    """カットオフが時間で動くローパス（1次）。短い効果音向け（長い音には遅い）"""
    cut = np.linspace(start, end, len(x))
    alpha = 1.0 - np.exp(-2.0 * np.pi * cut / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += alpha[i] * (x[i] - acc)
        y[i] = acc
    return y


# ---------------- エフェクト ----------------

def echo(x, delay, feedback=0.35, taps=4):
    d = seconds(delay)
    out = np.concatenate([x, np.zeros(d * taps)])
    for k in range(1, taps + 1):
        out[d * k:d * k + len(x)] += x * (feedback ** k)
    return out


def reverb(x, size=1.0, mix=0.25, seed=3):
    """小さなリバーブ（ランダムな遅延の減衰を重ね、高域を丸める）"""
    rng = np.random.default_rng(seed)
    tail = seconds(1.2 * size)
    out = np.concatenate([x, np.zeros(tail)])
    wet = np.zeros_like(out)
    for i in range(18):
        d = seconds(rng.uniform(0.02, 0.09) * size * (1 + i * 0.35))
        g = 0.55 * (0.82 ** i)
        wet[d:d + len(x)] += x * g * (1 if i % 2 == 0 else -1)
    wet = lowpass(wet, 4500)
    return out * (1 - mix) + wet * mix


def bitcrush(x, bits=6, hold=2):
    """ビット数を落とし、サンプルを間引いて保持する（チップっぽいざらつき）"""
    levels = 2 ** (bits - 1)
    y = np.round(x * levels) / levels
    if hold > 1:
        y = np.repeat(y[::hold], hold)[:len(x)]
    return y


def fade(x, fade_in=0.002, fade_out=0.01):
    y = x.copy()
    a, b = min(len(y), seconds(fade_in)), min(len(y), seconds(fade_out))
    if a:
        y[:a] *= np.linspace(0, 1, a)
    if b:
        y[-b:] *= np.linspace(1, 0, b)
    return y


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def place(buffer, sound, start_sample):
    """buffer の start_sample から sound を足す（はみ出しは切る）"""
    if start_sample >= len(buffer):
        return
    end = min(len(buffer), start_sample + len(sound))
    buffer[start_sample:end] += sound[:end - start_sample]


def normalize(x, peak=0.89):
    m = np.max(np.abs(x))
    return x if m == 0 else x * (peak / m)


def soft_clip(x, drive=1.2):
    return np.tanh(x * drive) / np.tanh(drive)


# ---------------- 書き出し ----------------

def write_wav(samples, relative_path):
    """samples: モノラルなら1次元、ステレオなら (n, 2)。-1..1 の範囲"""
    path = os.path.join(ROOT, relative_path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = np.clip(samples, -1.0, 1.0)
    channels = 1 if data.ndim == 1 else data.shape[1]
    pcm = (data * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    return path
