using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class TargetFocusModelTests
    {
        private TargetFocusModel _model;
        private UnitStatusModel _unit;
        private UnitStatusModel _otherUnit;

        [SetUp]
        public void SetUp()
        {
            _model = new TargetFocusModel();
            _unit = new UnitStatusModel(10, 0);
            _otherUnit = new UnitStatusModel(10, 0);
        }

        [Test]
        public void Focus_NotifiesChange()
        {
            UnitStatusModel notified = null;
            _model.OnFocusedTargetChanged += target => notified = target;

            _model.Focus(_unit);

            Assert.AreSame(_unit, _model.FocusedTarget);
            Assert.AreSame(_unit, notified);
        }

        [Test]
        public void Focus_DoesNotNotify_WhenUnchanged()
        {
            _model.Focus(_unit);
            int notifyCount = 0;
            _model.OnFocusedTargetChanged += _ => notifyCount++;

            _model.Focus(_unit);

            Assert.AreEqual(0, notifyCount);
        }

        [Test]
        public void Unfocus_ClearsOnlyTheFocusedUnit()
        {
            _model.Focus(_unit);

            _model.Unfocus(_otherUnit);
            Assert.AreSame(_unit, _model.FocusedTarget);

            _model.Unfocus(_unit);
            Assert.IsNull(_model.FocusedTarget);
        }

        // 隣の候補へ移ったとき、新しい方の Enter → 古い方の Exit の順で来ても新しい方が残る
        [Test]
        public void MovingBetweenOptions_KeepsTheNewFocus_EvenIfExitArrivesLate()
        {
            _model.Focus(_unit);

            _model.Focus(_otherUnit);
            _model.Unfocus(_unit);

            Assert.AreSame(_otherUnit, _model.FocusedTarget);
        }
    }
}
