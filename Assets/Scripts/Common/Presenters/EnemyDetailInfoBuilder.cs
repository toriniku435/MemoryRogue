using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.Presenters
{
    // 敵詳細ウインドウに見せる敵1種類ぶん（EnemyDetailInfo）を、今の言語で組み立てる。
    // マップの敵詳細とタイトルのエネミーコレクションが使う。
    // 数字（HP・行動回数）・最初から持っている効果・コマンドとその説明はデータから、特徴の文は EnemyDescription テーブルから引くので、
    // 敵を調整しても表示がずれない。強さの上乗せ（エンドレスの深い階層）があれば、HPは上乗せ後の値にし、階層ブーストを効果に並べる
    public static class EnemyDetailInfoBuilder
    {
        /// <summary>手前から順の編成を、同じ敵をまとめて並べる（「x2」を添える）</summary>
        public static List<EnemyDetailInfo> CreateGrouped(IEnumerable<EnemyData> lineup, EnemyStrength strength)
        {
            return lineup
                .Where(enemy => enemy != null)
                .GroupBy(enemy => enemy)
                .Select(group => Create(group.Key, group.Count(), strength))
                .ToList();
        }

        /// <summary>
        /// まだ出会っていない敵（エネミーコレクション）。見た目は黒い影にするので立ち絵は渡し、名前・数字・特徴・コマンドは伏せる
        /// </summary>
        public static EnemyDetailInfo CreateUnknown(EnemyData enemy)
        {
            return new EnemyDetailInfo(
                enemy.Visual != null && enemy.Visual.Icon != null ? enemy.Visual.Icon : enemy.Portrait,
                enemy.Visual,
                UnknownName,
                1,
                "HP ???",
                LocalizationHelper.GetUiText(UiTextKey.EnemyUnknown),
                "",
                new EnemyDetailCommandInfo[0],
                isDiscovered: false);
        }

        /// <summary>まだ出会っていない敵の名前</summary>
        public const string UnknownName = "？？？";

        public static EnemyDetailInfo Create(EnemyData enemy, int count, EnemyStrength strength)
        {
            strength ??= EnemyStrength.None;
            string actions = enemy.MinActionCount == enemy.MaxActionCount
                ? enemy.MaxActionCount.ToString()
                : $"{enemy.MinActionCount}-{enemy.MaxActionCount}";
            string stats = $"HP {strength.ApplyToMaxHp(enemy.MaxHp)}   {string.Format(LocalizationHelper.GetUiText(UiTextKey.EnemyActionCount), actions)}";

            // 同じコマンドを複数枚持っていても（出やすさの調整）、1回だけ載せる
            var commands = (enemy.Cards ?? new CardData[0])
                .Where(card => card != null)
                .Distinct()
                .Select(card => new EnemyDetailCommandInfo(card.Illustration, LocalizationHelper.GetCardName(card), LocalizationHelper.GetCardDescription(card)))
                .ToList();

            var effects = enemy.CreateInitialStatusEffects().Select(LocalizationHelper.GetStatusEffectName).ToList();
            if (strength.AttackBonus > 0)
            {
                effects.Add($"{LocalizationHelper.GetStatusEffectName(new LayerBoostStatusEffect(strength.AttackBonus))} +{strength.AttackBonus}");
            }

            return new EnemyDetailInfo(
                enemy.Visual != null && enemy.Visual.Icon != null ? enemy.Visual.Icon : enemy.Portrait,
                enemy.Visual,
                LocalizationHelper.GetEnemyName(enemy),
                count,
                stats,
                LocalizationHelper.GetEnemyDescription(enemy),
                string.Join(" / ", effects),
                commands);
        }
    }
}
