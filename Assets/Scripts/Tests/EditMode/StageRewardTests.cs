using System.Reflection;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class StageRewardTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static StageData NewStage(int victoryTokens, CardData victoryCard, int defeatTokens)
        {
            var stage = ScriptableObject.CreateInstance<StageData>();
            SetPrivateField(stage, "_victoryTokens", victoryTokens);
            SetPrivateField(stage, "_victoryCard", victoryCard);
            SetPrivateField(stage, "_defeatTokens", defeatTokens);
            return stage;
        }

        [Test]
        public void CreateReward_Victory_GivesTokensAndTheCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            var stage = NewStage(100, card, 200);

            var reward = stage.CreateReward(isVictory: true);

            Assert.IsTrue(reward.IsVictory);
            Assert.AreEqual(100, reward.Tokens);
            Assert.AreSame(card, reward.Card);
        }

        // 負けたときはトークンだけ（カードは付かない）
        [Test]
        public void CreateReward_Defeat_GivesOnlyTokens()
        {
            var stage = NewStage(100, ScriptableObject.CreateInstance<CardData>(), 200);

            var reward = stage.CreateReward(isVictory: false);

            Assert.IsFalse(reward.IsVictory);
            Assert.AreEqual(200, reward.Tokens);
            Assert.IsNull(reward.Card);
        }

        [Test]
        public void CreateReward_Victory_WithNoCardSet_GivesNoCard()
        {
            var stage = NewStage(100, null, 200);

            Assert.IsNull(stage.CreateReward(isVictory: true).Card);
        }

        [Test]
        public void StageReward_NegativeTokensBecomeZero()
        {
            Assert.AreEqual(0, new StageReward(true, -5, null).Tokens);
        }
    }
}
