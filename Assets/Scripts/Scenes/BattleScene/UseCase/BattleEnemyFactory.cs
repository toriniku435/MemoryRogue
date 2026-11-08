using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.BattleScene.UseCase
{
    // 敵データから、戦闘で使う敵（状態込み）を手前から順に組み立てる
    public static class BattleEnemyFactory
    {
        /// <param name="count">組み立てる数（画面に出せる数で切ることがある）</param>
        /// <param name="strength">深い階層・後半のステージの上乗せ。最大HPの倍率と、攻撃ダメージを増やす状態効果で当てはめる</param>
        public static List<BattleEnemy> Create(IReadOnlyList<EnemyData> enemyDataList, int count, EnemyStrength strength)
        {
            var enemies = new List<BattleEnemy>(count);
            for (int i = 0; i < count; i++)
            {
                // 敵はスタミナを持たない（行動量はEnemyDataの行動回数で決まる）
                var status = new UnitStatusModel(strength.ApplyToMaxHp(enemyDataList[i].MaxHp), 0);
                if (strength.AttackBonus > 0) status.AddStatusEffect(new LayerBoostStatusEffect(strength.AttackBonus));
                foreach (var statusEffect in enemyDataList[i].CreateInitialStatusEffects())
                {
                    status.AddStatusEffect(statusEffect);
                }
                enemies.Add(new BattleEnemy(enemyDataList[i], status));
            }
            return enemies;
        }

        /// <summary>
        /// 敵が生成できる味方の枠（生成カードごとに1体）。表示の枠も戦闘の始めに決まるので、生成で増やすのではなく
        /// 倒れた状態の敵を先に用意し、生成で戦場に戻す（EnemyPartyModel.Summon）
        /// </summary>
        public static List<BattleEnemy> CreateReserves(IReadOnlyList<BattleEnemy> enemies, EnemyStrength strength)
        {
            var reserves = new List<BattleEnemy>();
            var seen = new HashSet<EnemyData>();
            foreach (var enemy in enemies)
            {
                foreach (var card in enemy.Data.Cards)
                {
                    if (card == null) continue;
                    foreach (var effect in card.Effects)
                    {
                        if (!(effect is SummonEnemyCardEffect summon) || summon.Enemy == null || !seen.Add(summon.Enemy)) continue;

                        int maxHp = System.Math.Max(1, strength.ApplyToMaxHp(summon.Hp));
                        var status = new UnitStatusModel(maxHp, 0);
                        // 生成されるまでは戦場にいないので、倒れた状態で始める（ログ・表示の購読より前なので何も出ない）
                        status.ChangeHp(HpChangeType.TakePiercingDamage, -maxHp);
                        reserves.Add(new BattleEnemy(summon.Enemy, status, isReserve: true));
                    }
                }
            }
            return reserves;
        }
    }
}
