using System.Reflection;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // フォレンジック: 倒した敵の数でダメージが増える。数えるのは EnemyPartyModel（生成で戻った敵は倒すたびに数える）
    public sealed class DefeatCountAttackTests
    {
        private static DefeatCountAttackCardEffect NewEffect(int damage, int perDefeat)
        {
            var effect = new DefeatCountAttackCardEffect();
            var type = typeof(DefeatCountAttackCardEffect);
            type.GetField("_damage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, damage);
            type.GetField("_damagePerDefeat", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, perDefeat);
            return effect;
        }

        private static BattleEnemy NewEnemy(int hp)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            return new BattleEnemy(data, new UnitStatusModel(hp, 0));
        }

        private static void Defeat(UnitStatusModel unit) => unit.ChangeHp(HpChangeType.TakePiercingDamage, -unit.CurrentHp);

        [Test]
        public void Damage_AddsThePerDefeatBonusForEachDefeatedEnemy()
        {
            var user = new UnitStatusModel(100, 10);
            var target = new UnitStatusModel(200, 0);

            NewEffect(6, 10).ExecuteWithContext(user, target, new CardExecutionContext(0, null, defeatedEnemyCount: 3));

            Assert.AreEqual(200 - 36, target.CurrentHp);
        }

        // 敵が使うなど事情の分からない場面では、基本ダメージだけ
        [Test]
        public void WithoutContext_DealsTheBaseDamageOnly()
        {
            var user = new UnitStatusModel(100, 10);
            var target = new UnitStatusModel(200, 0);

            NewEffect(6, 10).Execute(user, target);

            Assert.AreEqual(194, target.CurrentHp);
        }

        [Test]
        public void Party_CountsEachDefeat_IncludingAReturnedEnemy_ButNotAReserveCreatedDefeated()
        {
            var front = NewEnemy(30);
            var reserve = NewEnemy(20);
            Defeat(reserve.Status);
            var party = new EnemyPartyModel(new[] { front, reserve });
            Assert.AreEqual(0, party.DefeatedCount, "倒れた状態で用意した枠は数えない");

            reserve.Status.Revive();
            Defeat(reserve.Status);
            Assert.AreEqual(1, party.DefeatedCount);

            reserve.Status.Revive();
            Defeat(reserve.Status);
            Defeat(front.Status);
            Assert.AreEqual(3, party.DefeatedCount);

            // 倒れたあとのHP変動（0のまま）では増えない
            front.Status.ChangeHp(HpChangeType.TakePiercingDamage, -5);
            Assert.AreEqual(3, party.DefeatedCount);
        }
    }
}
