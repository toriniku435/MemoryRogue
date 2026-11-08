"""全アートを書き出す。 python ArtSource/build.py [background ui cards characters effects]

引数なしなら全部。出力先は Assets/Art/ 以下で、ファイル名は固定。
同じ名前で上書きするので、Unity 側の GUID もインポート設定（9-slice の枠など）もそのまま残る。
"""
import importlib
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

GROUPS = ["background", "ui", "cards", "characters", "effects"]

if __name__ == "__main__":
    targets = sys.argv[1:] or GROUPS
    for name in targets:
        module = importlib.import_module(f"assets.{name}")
        module.build()
        print(f"built: {name}")
