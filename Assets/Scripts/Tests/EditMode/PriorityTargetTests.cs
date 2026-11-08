using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // 優先ターゲット: クリックだけでコマンドを使うときに狙う敵。敵ユニットのクリックで選び直す
    public sealed class PriorityTargetTests
    {
        private UnitStatusModel _front;
        private UnitStatusModel _back;
        private UnitStatusModel _player;
        private PriorityTargetModel _model;

        private IReadOnlyList<UnitStatusModel> Living() => new[] { _front, _back }.Where(e => e.CurrentHp > 0).ToList();

        [SetUp]
        public void SetUp()
        {
            _front = new UnitStatusModel(50, 0);
            _back = new UnitStatusModel(50, 0);
            _player = new UnitStatusModel(100, 10);
            _model = new PriorityTargetModel(Living);
        }

        [Test]
        public void Current_IsTheFrontEnemy_UntilOneIsChosen()
        {
            Assert.AreSame(_front, _model.Current);
        }

        [Test]
        public void Select_ChangesTheTarget_AndRaisesTheEvent()
        {
            int raised = 0;
            _model.OnChanged += () => raised++;

            Assert.IsTrue(_model.Select(_back));
            Assert.AreSame(_back, _model.Current);
            Assert.AreEqual(1, raised);

            Assert.IsFalse(_model.Select(_back), "同じ敵を選び直しても何も起きない");
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Select_IgnoresThePlayerAndDefeatedEnemies()
        {
            Assert.IsFalse(_model.Select(_player));
            _back.ChangeHp(Live2DCardRogue.Common.Statuses.HpChangeType.TakePiercingDamage, -50);
            Assert.IsFalse(_model.Select(_back));
            Assert.AreSame(_front, _model.Current);
        }

        [Test]
        public void ChosenEnemyDefeated_FallsBackToTheFrontLivingEnemy()
        {
            _model.Select(_back);
            _back.ChangeHp(Live2DCardRogue.Common.Statuses.HpChangeType.TakePiercingDamage, -50);

            Assert.AreSame(_front, _model.Current);
            Assert.AreEqual(1, _model.LivingCount);
        }

        [Test]
        public void ClickToPlay_AimsAtThePriorityTarget()
        {
            var attack = ScriptableObject.CreateInstance<CardData>();
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", 10);
            SetPrivateField(attack, "_id", "Attack");
            SetPrivateField(attack, "_effects", new CardEffectBase[] { effect });

            var useCase = new BattleCommandRig(
                _player, new CardPileModel(new CardData[0]), new CardPileModel(new[] { attack }), new CardPileModel(new CardData[0]),
                new CardCommandModel(3), new CardSelectionModel(), _ => null, Living, null, 0, () => _model.Current);

            _model.Select(_back);
            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(attack, preferFrontTarget: true));
            useCase.Execute.ExecuteNext();

            Assert.AreEqual(50, _front.CurrentHp);
            Assert.AreEqual(40, _back.CurrentHp);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = null;
            for (var type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
