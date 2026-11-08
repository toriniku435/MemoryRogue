using System.Linq;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class BattleLogRecorderTests
    {
        private UnitStatusModel _player;
        private UnitStatusModel _enemy;
        private BattleLogModel _log;
        private BattleLogRecorder _recorder;

        [SetUp]
        public void SetUp()
        {
            _player = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            _enemy = new UnitStatusModel(maxHp: 20, maxStamina: 0);
            _log = new BattleLogModel();
            _recorder = new BattleLogRecorder(_log, new[] { _player, _enemy });
            _recorder.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        private BattleLogKind[] Kinds() => _log.Entries.Select(e => e.Kind).ToArray();

        // カードを使ったら、その後の変化はその使用者の行動として記録される（敵味方の色分けに使う）
        [Test]
        public void CardUse_ThenDamage_RecordsTheUserAsActor()
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            _recorder.RecordCardUsed(_player, card, _enemy);
            _enemy.ChangeHp(HpChangeType.TakeAttackDamage, -6);

            CollectionAssert.AreEqual(new[] { BattleLogKind.CardUsed, BattleLogKind.Damage }, Kinds());
            var damage = _log.Entries[1];
            Assert.AreSame(_player, damage.Actor);
            Assert.AreSame(_enemy, damage.Subject);
            Assert.AreEqual(6, damage.Amount);
            Assert.AreSame(_enemy, _log.Entries[0].Target);
        }

        // 自分に使ったカードは対象を書かない（「あなたの「ブロック」→ あなた」とは出さない）
        [Test]
        public void CardUsedOnSelf_HasNoTarget()
        {
            _recorder.RecordCardUsed(_player, ScriptableObject.CreateInstance<CardData>(), _player);

            Assert.IsNull(_log.Entries[0].Target);
        }

        [Test]
        public void Heal_IsRecordedAsHeal()
        {
            _player.ChangeHp(HpChangeType.TakeAttackDamage, -10);
            _player.ChangeHp(HpChangeType.Heal, 4);

            Assert.AreEqual(BattleLogKind.Heal, _log.Entries.Last().Kind);
            Assert.AreEqual(4, _log.Entries.Last().Amount);
        }

        // ブロックが吸収した分は「ブロック -n」、抜けた分だけがダメージ。使い切ったブロックが消えても重ねて書かない
        [Test]
        public void BlockAbsorption_RecordsBlockLossThenRemainingDamage()
        {
            _player.AddStatusEffect(new BlockStatusEffect(5));

            _player.ChangeHp(HpChangeType.TakeAttackDamage, -8);
            _player.StartTurn(); // 使い切ったブロックが掃除される

            // 付与には詳しい説明が続くが、0 まで使い切った減少には付けない
            CollectionAssert.AreEqual(
                new[] { BattleLogKind.StatusGained, BattleLogKind.StatusDetail, BattleLogKind.StatusChanged, BattleLogKind.Damage },
                Kinds());
            Assert.AreEqual(-5, _log.Entries[2].Amount);
            Assert.AreEqual(3, _log.Entries[3].Amount);
        }

        // ターン終了時の処理中の HP の変化は継続効果によるもので、そのターンの主の行動として書く
        [Test]
        public void TurnEnd_DamageIsRecordedAsOverTime()
        {
            _recorder.BeginTurnEnd(_player);
            _player.ChangeHp(HpChangeType.TakePoisonDamage, -2);
            _recorder.EndTurnEnd(_player);
            _player.ChangeHp(HpChangeType.TakeAttackDamage, -1);

            Assert.AreEqual(BattleLogKind.DamageOverTime, _log.Entries[0].Kind);
            Assert.AreSame(_player, _log.Entries[0].Actor);
            Assert.AreEqual(BattleLogKind.Damage, _log.Entries[1].Kind);
        }

        // 継続ダメージには、それを起こした状態効果が付く（ログで「ウイルスで2ダメージ」と書くため）
        [Test]
        public void TurnEnd_VirusDamage_RecordsTheVirusAsItsCause()
        {
            _player.AddStatusEffect(new PoisonStatusEffect(2));

            _recorder.BeginTurnEnd(_player);
            _player.EndTurn();
            _recorder.EndTurnEnd(_player);

            var damage = _log.Entries.First(e => e.Kind == BattleLogKind.DamageOverTime);
            Assert.IsInstanceOf<PoisonStatusEffect>(damage.Effect);
            Assert.AreEqual(2, damage.Amount);
        }

        // 状態効果のフックの外で起きた変化（原因が分からない）は、効果を付けない
        [Test]
        public void TurnEnd_DamageWithoutEffect_HasNoCause()
        {
            _recorder.BeginTurnEnd(_player);
            _player.ChangeHp(HpChangeType.TakePoisonDamage, -2);
            _recorder.EndTurnEnd(_player);

            Assert.IsNull(_log.Entries[0].Effect);
        }

        [Test]
        public void HpReachingZero_RecordsDefeated()
        {
            _enemy.ChangeHp(HpChangeType.TakeAttackDamage, -50);

            CollectionAssert.AreEqual(new[] { BattleLogKind.Damage, BattleLogKind.Defeated }, Kinds());
            Assert.AreEqual(20, _log.Entries[0].Amount);
        }

        // 時間制の効果は、付いたときと消えたときだけ書く（残りターンが減るたびには書かない）
        [Test]
        public void TimedEffect_CountdownIsNotRecorded()
        {
            _enemy.AddStatusEffect(new StunStatusEffect(turns: 2));
            _enemy.StartTurn();
            _enemy.EndTurn();
            _enemy.StartTurn();
            _enemy.EndTurn();

            CollectionAssert.AreEqual(new[] { BattleLogKind.StatusGained, BattleLogKind.StatusDetail, BattleLogKind.StatusRemoved }, Kinds());
        }

        // 詳しい説明には、記録した時点の値を控える（効果のインスタンスは後で値が変わるため）
        [Test]
        public void StatusDetail_KeepsTheValuesAtThatMoment()
        {
            var stun = new StunStatusEffect(turns: 2);
            _enemy.AddStatusEffect(stun);
            _player.AddStatusEffect(new MemoryBoostStatusEffect(turns: 3, amount: 4));
            _enemy.StartTurn();
            _enemy.EndTurn(); // 停止の残りターンが 1 に減る

            var stunDetail = _log.Entries.First(e => e.Kind == BattleLogKind.StatusDetail && e.Effect == stun);
            Assert.AreEqual(2, stunDetail.Amount);

            var boostDetail = _log.Entries.Last(e => e.Kind == BattleLogKind.StatusDetail);
            Assert.AreEqual(3, boostDetail.Amount);
            Assert.AreEqual(4, boostDetail.EffectAmount);
        }

        // 値が増えた（重ねがけ）ときも、増えたあとの値で説明を添える
        [Test]
        public void StatusIncrease_AddsDetailWithTheNewValue()
        {
            _player.AddStatusEffect(new BlockStatusEffect(5));
            _player.AddStatusEffect(new BlockStatusEffect(3));

            CollectionAssert.AreEqual(
                new[] { BattleLogKind.StatusGained, BattleLogKind.StatusDetail, BattleLogKind.StatusChanged, BattleLogKind.StatusDetail },
                Kinds());
            Assert.AreEqual(8, _log.Entries[3].Amount);
        }

        [Test]
        public void TurnHeaders_AndStun_AreRecorded()
        {
            _recorder.RecordPlayerTurn(3);
            _recorder.RecordEnemyTurn();
            _recorder.RecordStunned(_enemy);

            CollectionAssert.AreEqual(new[] { BattleLogKind.PlayerTurn, BattleLogKind.EnemyTurn, BattleLogKind.Stunned }, Kinds());
            Assert.AreEqual(3, _log.Entries[0].Amount);
            Assert.IsNull(_log.Entries[0].Actor);
            Assert.AreSame(_enemy, _log.Entries[2].Actor);
        }

        [Test]
        public void Dispose_StopsRecording()
        {
            _recorder.Dispose();

            _enemy.ChangeHp(HpChangeType.TakeAttackDamage, -3);

            Assert.AreEqual(0, _log.Entries.Count);
        }
    }
}
