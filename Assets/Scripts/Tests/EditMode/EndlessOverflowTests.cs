using System.Reflection;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.MapScene.Models;
using Live2DCardRogue.MapScene.Views;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // エンドレスの階層がとても深くなっても（int の上限付近でも）、階層から計算する値があふれて
    // マイナスになったり小さくなったりしないこと
    public sealed class EndlessOverflowTests
    {
        private const int Deepest = CampaignProgressRepository.MaxStageIndex;

        [TearDown]
        public void TearDown()
        {
            CampaignProgressRepository.Reset();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static EndlessBalance NewBalance()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(enemy, "_id", "A");
            var entry = new EndlessBalance.EnemyEntry();
            SetPrivateField(entry, "_enemy", enemy);
            SetPrivateField(entry, "_fromLayer", 1);
            SetPrivateField(entry, "_canAppearAlone", true);
            var balance = ScriptableObject.CreateInstance<EndlessBalance>();
            SetPrivateField(balance, "_enemies", new[] { entry });
            return balance;
        }

        // 進行は上限の1つ手前で止まる（表示の「階層 + 1」が int に収まる）。読み込んだ値も同じく止める
        [Test]
        public void Progress_StopsBeforeIntMax()
        {
            CampaignProgressRepository.Restore(Deepest, 0);
            CampaignProgressRepository.AdvanceStage();
            Assert.AreEqual(Deepest, CampaignProgressRepository.CurrentStageIndex);

            CampaignProgressRepository.Restore(int.MaxValue, 0);
            Assert.AreEqual(Deepest, CampaignProgressRepository.CurrentStageIndex);
        }

        // 敵の強さは深いほど上がるだけで、あふれて下がらない
        [Test]
        public void Strength_NeverWrapsAround()
        {
            var balance = NewBalance();
            var shallow = balance.CreateStrength(1000);
            var deepest = balance.CreateStrength(Deepest);

            Assert.Greater(deepest.HpPercent, 0);
            Assert.GreaterOrEqual(deepest.HpPercent, shallow.HpPercent);
            Assert.GreaterOrEqual(deepest.AttackBonus, shallow.AttackBonus);
            Assert.Greater(deepest.ApplyToMaxHp(150), 0);
        }

        // 勝利のトークンも深いほど増えるだけで、上限で止まる
        [Test]
        public void VictoryTokens_ClampAtIntMax()
        {
            var balance = NewBalance();

            Assert.AreEqual(int.MaxValue, balance.VictoryTokensOf(Deepest));
            Assert.GreaterOrEqual(balance.VictoryTokensOf(Deepest), balance.VictoryTokensOf(1000));
            Assert.AreEqual(int.MaxValue, balance.CreateStage(1, Deepest).CreateReward(true).Tokens);
        }

        // マイニングの上乗せは、大きな額に掛けても上限で止まる
        [Test]
        public void TokenBonus_ClampsAtIntMax()
        {
            var reward = new StageReward(true, int.MaxValue / 2, null).WithTokenBonus(150);
            Assert.AreEqual(int.MaxValue, reward.Tokens);
        }

        // 階層ブーストは、大きな上乗せを足しても上限で止まる
        [Test]
        public void LayerBoost_ClampsAtIntMax()
        {
            var boost = new LayerBoostStatusEffect(int.MaxValue - 5);
            Assert.AreEqual(int.MaxValue, boost.OnDealtValue(DealtValueType.AttackDamage, 100));
        }

        // ページの範囲は、上限付近でも int に収まる
        [Test]
        public void PageWindow_StaysInsideInt()
        {
            int page = Deepest / 10;
            var window = MapPageWindow.Of(page, 1, 10, null);

            Assert.LessOrEqual(window.First, window.End);
            Assert.IsTrue(window.Contains(Deepest));
            Assert.Greater(window.First, 0);
        }

        // 部屋のアドレスは下4桁だけ（256階層ごとに戻る）。とても深くてもマイナスにならない
        [Test]
        public void Address_KeepsFourHexDigits()
        {
            Assert.AreEqual("0x0100", MapCellView.FormatAddress(1));
            Assert.AreEqual("0xFF00", MapCellView.FormatAddress(255));
            Assert.AreEqual("0x0000", MapCellView.FormatAddress(256));
            Assert.AreEqual("0xFF00", MapCellView.FormatAddress(int.MaxValue));
        }

        // 階層の表示（LAYER 37）は、上限でもマイナスにならない
        [Test]
        public void LayerText_NeverWrapsAround()
        {
            var rule = new ChallengeMapRule(NewBalance(), 1, 0);
            Assert.AreEqual("LAYER 2147483648", rule.LayerText(int.MaxValue));
        }
    }
}
