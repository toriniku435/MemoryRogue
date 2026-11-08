using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;

namespace Live2DCardRogue.Common.Repositories
{
    // シーンをまたいで使うコマンド（CardData）と敵（EnemyData）を預かり、ID で引けるようにする。
    // 預かったものには「解放しない」印を付け、アプリを閉じるまで持つ（小さなデータなので放さない）。
    //
    // ScriptableObject は前のシーンからしか参照されていないと、C# の変数が指していてもシーン切り替えの
    // 未使用アセットの解放で消えることがある（ビルドで起きる。エディタでは再現しない）。
    // そのためシーン間は ID で渡し、実物はここが寿命を持つ。預けるのは、前のシーンが生きているうち（遷移データを作るとき）
    public static class GameAssetRepository
    {
        private static readonly Dictionary<string, CardData> Cards = new Dictionary<string, CardData>();
        private static readonly Dictionary<string, EnemyData> Enemies = new Dictionary<string, EnemyData>();

        /// <summary>コマンドを預かり、その ID を返す（null や ID の無いものは null）</summary>
        public static string Hold(CardData card)
        {
            return Hold(Cards, card, card != null ? card.Id : null);
        }

        /// <summary>敵を預かり、その ID を返す（null や ID の無いものは null）</summary>
        public static string Hold(EnemyData enemy)
        {
            return Hold(Enemies, enemy, enemy != null ? enemy.Id : null);
        }

        /// <summary>預かったコマンド。預かっていなければ null（エラーログも出す。呼ぶ側は null を飛ばすだけでよい）</summary>
        public static CardData FindCard(string id)
        {
            return Find(Cards, id);
        }

        /// <summary>預かった敵。預かっていなければ null（エラーログも出す。呼ぶ側は null を飛ばすだけでよい）</summary>
        public static EnemyData FindEnemy(string id)
        {
            return Find(Enemies, id);
        }

        private static string Hold<T>(Dictionary<string, T> held, T asset, string id) where T : ScriptableObject
        {
            if (asset == null || string.IsNullOrEmpty(id)) return null;
            asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            if (held.TryGetValue(id, out var existing) && existing != null && existing != asset)
            {
                Debug.LogError($"ID {id} の {typeof(T).Name} が2つある（{existing.name} / {asset.name}）。あとから預けた方を使う");
            }
            held[id] = asset;
            return id;
        }

        private static T Find<T>(Dictionary<string, T> held, string id) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (held.TryGetValue(id, out var asset) && asset != null) return asset;
            Debug.LogError($"{typeof(T).Name} {id} は GameAssetRepository に預けられていない（シーンをまたぐ前に Hold すること）");
            return null;
        }
    }
}
