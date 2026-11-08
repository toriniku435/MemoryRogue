using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.Cards
{
    // コマンドの役割。自動でデッキを組むときの枠分けに使う
    public enum CardRole
    {
        /// <summary>相手を削る（攻撃・ウイルス）</summary>
        Attack,
        /// <summary>身を守る（ブロック）</summary>
        Defense,
        /// <summary>それ以外（回復・強化・手札やメモリーの操作・妨害）</summary>
        Support,
    }

    // 自動でデッキを組むときの枠。役割ごとの枚数の合計がデッキの枚数になる
    public sealed class AutoDeckRule
    {
        public AutoDeckRule(int attackCount, int defenseCount, int supportCount)
        {
            AttackCount = System.Math.Max(0, attackCount);
            DefenseCount = System.Math.Max(0, defenseCount);
            SupportCount = System.Math.Max(0, supportCount);
        }

        public int AttackCount { get; }
        public int DefenseCount { get; }
        public int SupportCount { get; }
        public int DeckSize => AttackCount + DefenseCount + SupportCount;

        public int CountOf(CardRole role)
        {
            switch (role)
            {
                case CardRole.Attack: return AttackCount;
                case CardRole.Defense: return DefenseCount;
                default: return SupportCount;
            }
        }
    }

    // 所持コマンドから、戦えるデッキを自動で組む（キャンペーン開始時のファーストビルド）。
    //   1. 役割（攻撃・防御・補助）ごとに、枠の数まで良いものから入れる
    //   2. 枠を埋めきれなかった役割があれば、残りの中から良いものでデッキの枚数まで埋める
    // 「良いもの」はレアリティが高い順、同じならコストが低い順、それも同じなら所持に並んだ順。
    // 乱数は使わないので、同じ所持からは必ず同じデッキになる
    public static class AutoDeckBuilder
    {
        /// <summary>
        /// 効果の種類から役割を決める。攻撃かウイルスを1つでも持てば攻撃、そうでなくブロックを持てば防御、ほかは補助。
        /// （攻撃と回復を併せ持つコマンドは攻撃に数える）
        /// </summary>
        public static CardRole RoleOf(CardData card)
        {
            var effects = card != null ? card.Effects : null;
            if (effects == null) return CardRole.Support;

            if (effects.Any(effect => effect is AttackCardEffectBase || effect is PoisonCardEffectBase)) return CardRole.Attack;
            if (effects.Any(effect => effect is BlockCardEffectBase)) return CardRole.Defense;
            return CardRole.Support;
        }

        /// <param name="owned">所持コマンド。同じコマンドはその数だけ並ぶ。null 要素は無視する</param>
        /// <returns>組んだデッキ。所持が足りなければデッキの枚数より少なくなる</returns>
        public static CardData[] Build(IReadOnlyList<CardData> owned, AutoDeckRule rule)
        {
            if (owned == null || rule == null) return new CardData[0];

            // 良い順に並べた候補（同じコマンドの複数枚は別々の候補）
            var ranked = owned
                .Select((card, index) => (card, index))
                .Where(x => x.card != null)
                .OrderByDescending(x => (int)x.card.Rarity)
                .ThenBy(x => x.card.Cost)
                .ThenBy(x => x.index)
                .ToList();

            // デッキに入れられる枚数に上限があるコマンドは、上限を超えた分を候補から外す
            var copies = new Dictionary<CardData, int>();
            ranked = ranked.Where(x =>
            {
                copies.TryGetValue(x.card, out int count);
                copies[x.card] = count + 1;
                return x.card.MaxCopiesInDeck <= 0 || count < x.card.MaxCopiesInDeck;
            }).ToList();

            var picked = new List<(CardData card, int index)>();
            foreach (CardRole role in System.Enum.GetValues(typeof(CardRole)))
            {
                picked.AddRange(ranked.Where(x => RoleOf(x.card) == role).Take(rule.CountOf(role)));
            }

            var rest = ranked.Except(picked).ToList();
            picked.AddRange(rest.Take(rule.DeckSize - picked.Count));

            return picked.Select(x => x.card).ToArray();
        }
    }
}
