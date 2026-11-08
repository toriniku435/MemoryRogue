using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using UnityEngine;

namespace Live2DCardRogue.Common.Presenters
{
    // バトルログの記録（BattleLogEntry）を、画面に出す1行（BattleLogLine）にする。
    // 文章の型はローカライズテーブル（UIText の Log*）にあり、ここは穴を埋めるだけ。
    // 表示するたびに作り直すので、言語を切り替えればログも訳し直される。
    //
    // 同じ敵が2体以上いるときは、名前の後ろに A・B… を付けて区別する（並び順＝手前から）
    public sealed class BattleLogFormatter
    {
        private readonly UnitStatusModel _player;
        private readonly Sprite _playerIcon;
        private readonly Dictionary<UnitStatusModel, (EnemyData Data, string Suffix, Sprite Icon)> _enemies =
            new Dictionary<UnitStatusModel, (EnemyData Data, string Suffix, Sprite Icon)>();

        /// <param name="enemies">手前から順の敵（ステータス・データ・顔）</param>
        public BattleLogFormatter(UnitStatusModel player, Sprite playerIcon, IReadOnlyList<(UnitStatusModel Status, EnemyData Data, Sprite Icon)> enemies)
        {
            _player = player;
            _playerIcon = playerIcon;

            var counts = new Dictionary<EnemyData, int>();
            foreach (var enemy in enemies)
            {
                counts[enemy.Data] = counts.TryGetValue(enemy.Data, out int c) ? c + 1 : 1;
            }
            var used = new Dictionary<EnemyData, int>();
            foreach (var enemy in enemies)
            {
                string suffix = "";
                if (counts[enemy.Data] > 1)
                {
                    int index = used.TryGetValue(enemy.Data, out int u) ? u : 0;
                    used[enemy.Data] = index + 1;
                    suffix = ((char)('A' + index)).ToString();
                }
                _enemies[enemy.Status] = (enemy.Data, suffix, enemy.Icon);
            }
        }

        public BattleLogLine Format(BattleLogEntry entry)
        {
            var side = SideOf(entry.Actor);
            var icon = IconOf(entry.Subject);

            switch (entry.Kind)
            {
                case BattleLogKind.PlayerTurn:
                    return Header(string.Format(Ui(UiTextKey.LogPlayerTurn), entry.Amount));
                case BattleLogKind.EnemyTurn:
                    return Header(Ui(UiTextKey.LogEnemyTurn));
                case BattleLogKind.CardUsed:
                    string card = entry.Card != null ? LocalizationHelper.GetCardName(entry.Card) : "?";
                    string text = entry.Target == null
                        ? string.Format(Ui(UiTextKey.LogCardUsed), NameOf(entry.Subject), card)
                        : string.Format(Ui(UiTextKey.LogCardUsedOn), NameOf(entry.Subject), card, NameOf(entry.Target));
                    return new BattleLogLine(text, side, 0, icon);
                case BattleLogKind.Stunned:
                    return new BattleLogLine(string.Format(Ui(UiTextKey.LogStunned), NameOf(entry.Subject)), side, 0, icon);
                case BattleLogKind.Damage:
                    return Result(side, icon, UiTextKey.LogDamage, NameOf(entry.Subject), entry.Amount);
                case BattleLogKind.Heal:
                    return Result(side, icon, UiTextKey.LogHeal, NameOf(entry.Subject), entry.Amount);
                // 原因の状態効果が分かれば「ウイルスで」と名前で書く。分からなければ「継続効果で」
                case BattleLogKind.DamageOverTime:
                    return entry.Effect != null
                        ? Result(side, icon, UiTextKey.LogDamageFrom, NameOf(entry.Subject), entry.Amount, EffectName(entry))
                        : Result(side, icon, UiTextKey.LogDamageOverTime, NameOf(entry.Subject), entry.Amount);
                case BattleLogKind.HealOverTime:
                    return entry.Effect != null
                        ? Result(side, icon, UiTextKey.LogHealFrom, NameOf(entry.Subject), entry.Amount, EffectName(entry))
                        : Result(side, icon, UiTextKey.LogHealOverTime, NameOf(entry.Subject), entry.Amount);
                case BattleLogKind.StatusGained:
                    return Result(side, icon, UiTextKey.LogStatusGained, NameOf(entry.Subject), EffectName(entry), entry.Amount);
                case BattleLogKind.StatusChanged:
                    string signed = entry.Amount > 0 ? $"+{entry.Amount}" : entry.Amount.ToString();
                    return Result(side, icon, UiTextKey.LogStatusChanged, NameOf(entry.Subject), EffectName(entry), signed);
                case BattleLogKind.StatusRemoved:
                    return Result(side, icon, UiTextKey.LogStatusRemoved, NameOf(entry.Subject), EffectName(entry));
                case BattleLogKind.Defeated:
                    return Result(side, icon, UiTextKey.LogDefeated, NameOf(entry.Subject));
                case BattleLogKind.CardPurged:
                    return Result(side, icon, UiTextKey.LogCardPurged, entry.Card != null ? LocalizationHelper.GetCardName(entry.Card) : "?");
                case BattleLogKind.CardReturned:
                    return Result(side, icon, UiTextKey.LogCardReturned, entry.Card != null ? LocalizationHelper.GetCardName(entry.Card) : "?");
                case BattleLogKind.DeckExhausted:
                    return Header(Ui(UiTextKey.LogDeckExhausted));
                case BattleLogKind.StatusDetail:
                    // 状態効果ごとの詳しい説明の書式に、記録した時点の値（{0}: 値・残りターン、{1}: 効きの大きさ）を入れる
                    string detail = entry.Effect != null
                        ? string.Format(LocalizationHelper.GetStatusEffectLogDetail(entry.Effect), entry.Amount, entry.EffectAmount)
                        : "";
                    return new BattleLogLine(detail, side, 2, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry.Kind, "表示の仕方が決まっていないログの種類です");
            }
        }

        private static BattleLogLine Header(string text)
        {
            return new BattleLogLine(text, BattleLogSide.Neutral, 0, null);
        }

        private static BattleLogLine Result(BattleLogSide side, Sprite icon, UiTextKey key, params object[] args)
        {
            return new BattleLogLine(string.Format(Ui(key), args), side, 1, icon);
        }

        private BattleLogSide SideOf(UnitStatusModel unit)
        {
            if (unit == null) return BattleLogSide.Neutral;
            return unit == _player ? BattleLogSide.Player : BattleLogSide.Enemy;
        }

        private Sprite IconOf(UnitStatusModel unit)
        {
            if (unit == null) return null;
            if (unit == _player) return _playerIcon;
            return _enemies.TryGetValue(unit, out var enemy) ? enemy.Icon : null;
        }

        private string NameOf(UnitStatusModel unit)
        {
            if (unit == null) return "?";
            if (unit == _player) return Ui(UiTextKey.LogYou);
            if (!_enemies.TryGetValue(unit, out var enemy) || enemy.Data == null) return "?";
            return LocalizationHelper.GetEnemyName(enemy.Data) + enemy.Suffix;
        }

        private static string EffectName(BattleLogEntry entry)
        {
            return entry.Effect != null ? LocalizationHelper.GetStatusEffectName(entry.Effect) : "?";
        }

        private static string Ui(UiTextKey key)
        {
            return LocalizationHelper.GetUiText(key);
        }
    }
}
