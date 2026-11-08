using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class EnemyPartyModelTests
    {
        private static BattleEnemy NewEnemy(int maxHp = 30)
        {
            // EnemyData も Inspector 専用なので、テストからは空のインスタンスで足りる
            // (このモデルが見るのは BattleEnemy.Status だけ)
            return new BattleEnemy(
                ScriptableObject.CreateInstance<EnemyData>(),
                new UnitStatusModel(maxHp: maxHp, maxStamina: 0));
        }

        private static void Defeat(BattleEnemy enemy)
        {
            enemy.Status.ChangeHp(HpChangeType.TakeAttackDamage, -enemy.Status.CurrentHp);
        }

        [Test]
        public void FindNextLivingTarget_ReturnsTheNextEnemyInOrder()
        {
            var first = NewEnemy();
            var second = NewEnemy();
            var party = new EnemyPartyModel(new[] { first, second });

            Assert.AreSame(second.Status, party.FindNextLivingTarget(first.Status));
        }

        // 末尾まで行ったら手前に戻る
        [Test]
        public void FindNextLivingTarget_WrapsAroundToTheFront()
        {
            var first = NewEnemy();
            var second = NewEnemy();
            var party = new EnemyPartyModel(new[] { first, second });

            Assert.AreSame(first.Status, party.FindNextLivingTarget(second.Status));
        }

        [Test]
        public void FindNextLivingTarget_SkipsDefeatedEnemies()
        {
            var first = NewEnemy();
            var second = NewEnemy();
            var third = NewEnemy();
            var party = new EnemyPartyModel(new[] { first, second, third });
            Defeat(second);

            Assert.AreSame(third.Status, party.FindNextLivingTarget(first.Status));
        }

        // 他に生きている敵が居なければ null。呼び出し側は元の対象のままにする
        [Test]
        public void FindNextLivingTarget_ReturnsNull_WhenNoOtherEnemyIsAlive()
        {
            var first = NewEnemy();
            var second = NewEnemy();
            var party = new EnemyPartyModel(new[] { first, second });
            Defeat(first);
            Defeat(second);

            Assert.IsNull(party.FindNextLivingTarget(first.Status));
        }

        // 1体しか居なくても自分自身は返さない(倒れているので)
        [Test]
        public void FindNextLivingTarget_SingleDefeatedEnemy_ReturnsNull()
        {
            var only = NewEnemy();
            var party = new EnemyPartyModel(new[] { only });
            Defeat(only);

            Assert.IsNull(party.FindNextLivingTarget(only.Status));
        }

        // 敵でないユニット(プレイヤー自身など)を渡されても何も返さない
        [Test]
        public void FindNextLivingTarget_UnknownUnit_ReturnsNull()
        {
            var party = new EnemyPartyModel(new[] { NewEnemy() });
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            Assert.IsNull(party.FindNextLivingTarget(player));
        }

        [Test]
        public void AreAllDefeated_IsTrueOnlyWhenEveryEnemyIsDown()
        {
            var first = NewEnemy();
            var second = NewEnemy();
            var party = new EnemyPartyModel(new[] { first, second });

            Assert.IsFalse(party.AreAllDefeated);

            Defeat(first);
            Assert.IsFalse(party.AreAllDefeated);

            Defeat(second);
            Assert.IsTrue(party.AreAllDefeated);
        }
    }
}
