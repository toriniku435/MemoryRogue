using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 敵詳細ウインドウ（マップの敵詳細、タイトルのエネミーコレクション）で見せる敵1種類ぶん。
    // 文字は EnemyDetailInfoBuilder が言語に合わせて組み立て済み
    public sealed class EnemyDetailInfo
    {
        public Sprite Icon { get; }
        // 動く見た目。null なら Icon の1枚絵
        public UnitVisualDefinition Visual { get; }
        public string Name { get; }
        // 同じ敵が何体出るか
        public int Count { get; }
        public string Stats { get; }
        public string Trait { get; }
        // 最初から持っている効果（無効化など）。無ければ空
        public string Immunities { get; }
        public IReadOnlyList<EnemyDetailCommandInfo> Commands { get; }
        // 出会ったことがあるか。無ければ見た目は黒い影、文字は伏せたもの（EnemyDetailInfoBuilder.CreateUnknown）
        public bool IsDiscovered { get; }

        public EnemyDetailInfo(Sprite icon, UnitVisualDefinition visual, string name, int count, string stats, string trait, string immunities,
            IReadOnlyList<EnemyDetailCommandInfo> commands, bool isDiscovered = true)
        {
            IsDiscovered = isDiscovered;
            Icon = icon;
            Visual = visual;
            Name = name;
            Count = count;
            Stats = stats;
            Trait = trait;
            Immunities = immunities;
            Commands = commands;
        }
    }

    // 敵が使うコマンド1つぶん（絵・名前・効果の説明）
    public sealed class EnemyDetailCommandInfo
    {
        public Sprite Illustration { get; }
        public string Name { get; }
        public string Description { get; }

        public EnemyDetailCommandInfo(Sprite illustration, string name, string description)
        {
            Illustration = illustration;
            Name = name;
            Description = description;
        }
    }
}
