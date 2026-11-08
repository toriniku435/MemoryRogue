using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.MapScene.Models;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Live2DCardRogue.Common.SceneTransition;

namespace Live2DCardRogue.Tests.EditMode
{
    // エンドレス: 階層の組み方（EndlessBalance）、マップに差し込むルール、ページの範囲、敵の強さの上乗せ、成績
    public sealed class EndlessModeTests
    {
        private const string BalancePath = "Assets/ScriptableObjects/EndlessBalance.asset";

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static EnemyData NewEnemy(string id, int maxHp = 100)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(enemy, "_id", id);
            SetPrivateField(enemy, "_maxHp", maxHp);
            return enemy;
        }

        private static EndlessBalance.EnemyEntry NewEntry(EnemyData enemy, int fromLayer, bool canAppearAlone = true)
        {
            var entry = new EndlessBalance.EnemyEntry();
            SetPrivateField(entry, "_enemy", enemy);
            SetPrivateField(entry, "_fromLayer", fromLayer);
            SetPrivateField(entry, "_canAppearAlone", canAppearAlone);
            return entry;
        }

        // 1階層目から出る A、5階層目から出る B、単独では出ない Support（10階層目から2体）
        private static EndlessBalance NewBalance(out EnemyData a, out EnemyData b, out EnemyData support)
        {
            a = NewEnemy("A");
            b = NewEnemy("B");
            support = NewEnemy("Support");
            var balance = ScriptableObject.CreateInstance<EndlessBalance>();
            SetPrivateField(balance, "_enemies", new[] { NewEntry(a, 1), NewEntry(b, 5), NewEntry(support, 1, canAppearAlone: false) });
            SetPrivateField(balance, "_twoEnemiesFromLayer", 10);
            SetPrivateField(balance, "_twoEnemiesChancePercent", 100);
            SetPrivateField(balance, "_layersPerTier", 10);
            SetPrivateField(balance, "_hpPercentPerTier", 25);
            SetPrivateField(balance, "_attackBonusPerTier", 2);
            SetPrivateField(balance, "_victoryTokensBase", 100);
            SetPrivateField(balance, "_victoryTokensPerLayer", 10);
            return balance;
        }

        // ---------------- EndlessBalance ----------------

        // 同じシードと階層なら、何度組んでも同じ編成になる（戦闘から戻っても、再開しても変わらない）
        [Test]
        public void CreateStage_IsDeterministicForTheSameSeedAndLayer()
        {
            var balance = NewBalance(out _, out _, out _);
            for (int index = 0; index < 40; index++)
            {
                var first = balance.CreateStage(99, index);
                var second = balance.CreateStage(99, index);
                CollectionAssert.AreEqual(first.Enemies, second.Enemies, $"layer {index + 1}");
            }
        }

        // 強さは10階層ごとに1段上がる（1〜10階層は上乗せ無し）
        [Test]
        public void CreateStrength_StepsUpEveryTenLayers()
        {
            var balance = NewBalance(out _, out _, out _);

            Assert.AreEqual(100, balance.CreateStrength(0).HpPercent);
            Assert.AreEqual(0, balance.CreateStrength(9).AttackBonus);
            Assert.AreEqual(125, balance.CreateStrength(10).HpPercent);
            Assert.AreEqual(2, balance.CreateStrength(10).AttackBonus);
            Assert.AreEqual(150, balance.CreateStrength(29).HpPercent);
            Assert.AreEqual(6, balance.CreateStrength(30).AttackBonus);
        }

        // 1体で出るのは単独で出てよい敵だけ、出始める階層より前には出ない。2体は決めた階層から
        [Test]
        public void CreateStage_RespectsFromLayerAloneAndTwoEnemies()
        {
            var balance = NewBalance(out _, out var b, out var support);
            for (int seed = 0; seed < 30; seed++)
            {
                for (int index = 0; index < 9; index++)
                {
                    var enemies = balance.CreateStage(seed, index).Enemies;
                    Assert.AreEqual(1, enemies.Length, $"layer {index + 1}");
                    CollectionAssert.DoesNotContain(enemies, support);
                    if (index + 1 < 5) CollectionAssert.DoesNotContain(enemies, b);
                }
                Assert.AreEqual(2, balance.CreateStage(seed, 9).Enemies.Length);
            }
        }

        // 勝利のトークンは階層が深いほど増える。負けたときの支援は無い
        [Test]
        public void CreateStage_RewardGrowsWithLayerAndDefeatGivesNothing()
        {
            var balance = NewBalance(out _, out _, out _);

            Assert.AreEqual(100, balance.CreateStage(1, 0).CreateReward(true).Tokens);
            Assert.AreEqual(190, balance.CreateStage(1, 9).CreateReward(true).Tokens);
            Assert.AreEqual(0, balance.CreateStage(1, 9).CreateReward(false).Tokens);
            Assert.IsNull(balance.CreateStage(1, 9).CreateReward(false).Card);
        }

        // 実際のアセット: 敵が設定されていて、どの階層も1〜2体（戦闘画面に並ぶのは2体まで）で組める
        [Test]
        public void Asset_BuildsOneOrTwoEnemiesOnEveryLayer()
        {
            var balance = AssetDatabase.LoadAssetAtPath<EndlessBalance>(BalancePath);
            Assert.IsNotNull(balance, BalancePath);
            for (int index = 0; index < 100; index++)
            {
                var plan = balance.CreateStage(2026, index);
                Assert.That(plan.Enemies.Length, Is.InRange(1, 2), $"layer {index + 1}");
                Assert.IsTrue(plan.Enemies.All(enemy => enemy != null));
            }
        }

        // ---------------- MapRunRule ----------------

        [Test]
        public void CampaignRule_HasAnEnd()
        {
            var stage = ScriptableObject.CreateInstance<StageData>();
            var rule = new CampaignMapRule(new[] { stage, stage, stage }, null);

            Assert.IsTrue(rule.HasStory);
            Assert.IsFalse(rule.EndsOnDefeat);
            Assert.IsFalse(rule.IsCompleted(2));
            Assert.IsTrue(rule.IsCompleted(3));
            Assert.IsNull(rule.GetStage(3));
            Assert.AreEqual("LAYER 02 / 03", rule.LayerText(1));
            Assert.AreEqual("LAYER 03 / 03", rule.LayerText(3));
            Assert.AreEqual(3, rule.SavedStageCount);
        }

        [Test]
        public void EndlessChallenge_NeverEnds_AndReusesTheSamePlan()
        {
            var rule = new ChallengeMapRule(NewBalance(out _, out _, out _), 7, 0);

            Assert.IsFalse(rule.HasStory);
            Assert.IsTrue(rule.EndsOnDefeat);
            Assert.IsFalse(rule.IsCompleted(9999));
            Assert.AreEqual("LAYER 37", rule.LayerText(36));
            Assert.AreEqual(0, rule.SavedStageCount);
            Assert.AreSame(rule.GetStage(12), rule.GetStage(12));
            Assert.IsNull(rule.GetStage(-1));
        }

        // 階層数のあるチャレンジ（ノーマル 30 など）は、その階層で踏破になる。物語は無く、負けたら終わり
        [Test]
        public void LimitedChallenge_EndsAtItsLayerCount()
        {
            var rule = new ChallengeMapRule(NewBalance(out _, out _, out _), 7, 30);

            Assert.AreEqual(30, rule.StageCount);
            Assert.IsTrue(rule.EndsOnDefeat);
            Assert.IsFalse(rule.IsCompleted(29));
            Assert.IsTrue(rule.IsCompleted(30));
            Assert.IsNotNull(rule.GetStage(29));
            Assert.IsNull(rule.GetStage(30));
            Assert.AreEqual("LAYER 12 / 30", rule.LayerText(11));
            Assert.AreEqual(30, rule.SavedStageCount);
        }

        // ---------------- MapPageWindow ----------------

        // 出しておくのは今のページと前後のページだけ
        [Test]
        public void PageWindow_CoversTheCurrentAndNeighbouringPages()
        {
            var first = MapPageWindow.Of(0, 1, 10, null);
            Assert.AreEqual(0, first.First);
            Assert.AreEqual(20, first.End);

            var deep = MapPageWindow.Of(7, 1, 10, null);
            Assert.AreEqual(60, deep.First);
            Assert.AreEqual(90, deep.End);
            Assert.IsTrue(deep.Contains(75));
            Assert.IsFalse(deep.Contains(59));
            Assert.IsFalse(deep.Contains(90));
        }

        // 部屋の数が決まっていれば、それを超えない
        [Test]
        public void PageWindow_StopsAtTheStageCount()
        {
            var window = MapPageWindow.Of(0, 1, 10, 10);
            Assert.AreEqual(0, window.First);
            Assert.AreEqual(10, window.End);
        }

        // ---------------- 敵の強さ ----------------

        [Test]
        public void EnemyStrength_ScalesMaxHp()
        {
            Assert.AreEqual(130, EnemyStrength.None.ApplyToMaxHp(130));
            Assert.AreEqual(162, new EnemyStrength(125, 0).ApplyToMaxHp(130));
            Assert.AreEqual(1, new EnemyStrength(1, 0).ApplyToMaxHp(10));
        }

        // 階層ブーストは攻撃ダメージだけを増やす
        [Test]
        public void LayerBoost_AddsToAttackDamageOnly()
        {
            var boost = new LayerBoostStatusEffect(4);

            Assert.AreEqual(10, boost.OnDealtValue(DealtValueType.AttackDamage, 6));
            Assert.AreEqual(6, boost.OnDealtValue(DealtValueType.Block, 6));
            Assert.AreEqual(6, boost.OnDealtValue(DealtValueType.Poison, 6));
        }

        // 階層ブーストは筋力と別の効果なので、敵が筋力を積んでも打ち消し合わない
        [Test]
        public void LayerBoost_StacksWithStrength()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new LayerBoostStatusEffect(2));
            unit.AddStatusEffect(new StrengthStatusEffect(3));

            Assert.AreEqual(2, unit.StatusEffects.Count);
        }

        // ---------------- 成績 ----------------

        [Test]
        public void ChallengeResult_UpdatesTheBestOnlyWhenDeeper()
        {
            var record = new ChallengeResultTransitionData(ChallengeType.Endless, 23, false, 15);
            Assert.IsTrue(record.IsNewRecord);
            Assert.AreEqual(23, record.BestLayer);

            var notRecord = new ChallengeResultTransitionData(ChallengeType.Normal, 9, false, 15);
            Assert.IsFalse(notRecord.IsNewRecord);
            Assert.AreEqual(15, notRecord.BestLayer);
        }
    }
}
