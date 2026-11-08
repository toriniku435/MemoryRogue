using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardPreviewModelTests
    {
        private CardPreviewModel _model;
        private CardData _card;
        private CardData _otherCard;
        private readonly object _ownerA = new object();
        private readonly object _ownerB = new object();

        [SetUp]
        public void SetUp()
        {
            _model = new CardPreviewModel();
            _card = ScriptableObject.CreateInstance<CardData>();
            _otherCard = ScriptableObject.CreateInstance<CardData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_card);
            Object.DestroyImmediate(_otherCard);
        }

        [Test]
        public void Show_NotifiesTheCard()
        {
            CardData notified = null;
            _model.OnChanged += card => notified = card;

            _model.Show(_ownerA, _card);

            Assert.AreSame(_card, _model.Card);
            Assert.AreSame(_card, notified);
        }

        [Test]
        public void Hide_ByAnotherOwner_IsIgnored()
        {
            _model.Show(_ownerA, _card);

            _model.Hide(_ownerB);

            Assert.AreSame(_card, _model.Card);
        }

        // 同じカードの別の1枚へ移ったとき、新しい方の Enter → 古い方の Exit の順で来ても消えない
        [Test]
        public void MovingBetweenCopiesOfTheSameCard_KeepsItShown_EvenIfExitArrivesLate()
        {
            _model.Show(_ownerA, _card);

            _model.Show(_ownerB, _card);
            _model.Hide(_ownerA);

            Assert.AreSame(_card, _model.Card);
        }

        [Test]
        public void Hide_ByTheShowingOwner_Clears()
        {
            _model.Show(_ownerA, _card);
            _model.Show(_ownerB, _otherCard);

            _model.Hide(_ownerB);

            Assert.IsNull(_model.Card);
        }
    }
}
