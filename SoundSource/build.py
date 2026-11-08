"""全サウンドを書き出す。 python SoundSource/build.py [sfx] [music]

引数なしなら全部。出力先は Assets/Audio/ 以下（SE/se_*.wav, BGM/bgm_*.wav）で、ファイル名は固定。
同じ名前で上書きするので、Unity 側の GUID もインポート設定もそのまま残る（差し替えもこの名前で置けばよい）。
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import music  # noqa: E402
import sfx  # noqa: E402

if __name__ == "__main__":
    targets = sys.argv[1:] or ["sfx", "music"]
    if "sfx" in targets:
        sfx.build()
        print("built: sfx")
    if "music" in targets:
        music.build()
        print("built: music")
