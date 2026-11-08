using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // 2026-10-04 に足したコマンド（ゼロデイ・リドゥ・フラッシュ・スワップアウト・デフラグ・クラウドバースト・
    // パイプライン・ブルートフォース・リフレクション・フォーマット）と敵の新しい行動、重ね掛けの新ルール
    public sealed class NewCommandSetTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = null;
            for (var type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static T Effect<T>(params (string field, int value)[] values) where T : CardEffectBase, new()
        {
            var effect = new T();
            foreach (var (field, value) in values) SetPrivateField(effect, field, value);
            return effect;
        }

        private static CardData NewCard(string id, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_cost", 0);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static CardData Attack(int damage) => NewCard("Attack", Effect<BasicAttackCardEffect>(("_damage", damage)));
        private static CardData Filler(string id = "Filler") => NewCard(id, Effect<BasicBlockCardEffect>(("_block", 1)));

        private sealed class Battle
        {
            public UnitStatusModel Player;
            public UnitStatusModel Enemy;
            public CardPileModel Hand, Purge;
            public BattleCommandRig UseCase;

            public void Queue(CardData card, UnitStatusModel target)
            {
                Assert.IsTrue(UseCase.Queue.TrySelectCard(card));
                Assert.IsTrue(UseCase.Queue.SelectTarget(target));
            }

            public void ExecuteAll()
            {
                while (UseCase.Execute.ExecuteNext()) { }
            }
        }

        private static Battle NewBattle(CardData[] hand, int tokens = 0)
        {
            var b = new Battle
            {
                Player = new UnitStatusModel(100, 10),
                Enemy = new UnitStatusModel(100, 0),
                Hand = new CardPileModel(hand),
                Purge = new CardPileModel(new CardData[0]),
            };
            b.UseCase = new BattleCommandRig(
                b.Player, new CardPileModel(new CardData[0]), b.Hand, new CardPileModel(new CardData[0]),
                new CardCommandModel(3), new CardSelectionModel(), _ => null,
                () => new[] { b.Enemy }.Where(e => e.CurrentHp > 0).ToList(), b.Purge, tokens);
            return b;
        }

        // ---------------- ゼロデイ ----------------

        [Test]
        public void ZeroDay_InfectsEveryUnit_TheUserIncluded()
        {
            var zeroDay = NewCard("ZeroDay", Effect<AllUnitsPoisonCardEffect>(("_poison", 8)));
            var b = NewBattle(new[] { zeroDay });
            Assert.IsFalse(zeroDay.TargetsOpponent, "対象は選ばない（自分側）");

            b.Queue(zeroDay, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(8, b.Player.StatusEffects.Single(e => e.Id == PoisonStatusEffect.EffectId).Value);
            Assert.AreEqual(8, b.Enemy.StatusEffects.Single(e => e.Id == PoisonStatusEffect.EffectId).Value);
        }

        [Test]
        public void ZeroDay_WithoutContext_InfectsTargetAndUser()
        {
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);
            Effect<AllUnitsPoisonCardEffect>(("_poison", 8)).Execute(user, target);

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(1, target.StatusEffects.Count);
        }

        // ---------------- リドゥ ----------------

        [Test]
        public void Redo_RepeatsThePreviousCommandOnItsTarget()
        {
            var attack = Attack(10);
            var redo = NewCard("Redo", new RedoCardEffect());
            var b = NewBattle(new[] { attack, redo });

            b.Queue(attack, b.Enemy);
            b.Queue(redo, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(80, b.Enemy.CurrentHp);
            Assert.IsTrue(redo.RepeatsPrevious);
        }

        [Test]
        public void Redo_TwiceInARow_RepeatsTheLastNonRedoCommand()
        {
            var attack = Attack(10);
            var b = NewBattle(new[] { attack, NewCard("Redo", new RedoCardEffect()), NewCard("Redo", new RedoCardEffect()) });

            b.Queue(attack, b.Enemy);
            b.Queue(b.Hand.Cards[0], b.Player);
            b.Queue(b.Hand.Cards[0], b.Player);
            b.ExecuteAll();

            Assert.AreEqual(70, b.Enemy.CurrentHp, "リドゥはリドゥを繰り返さず、その前のアタックを繰り返す");
        }

        [Test]
        public void Redo_WithNothingBefore_DoesNothing()
        {
            var redo = NewCard("Redo", new RedoCardEffect());
            var b = NewBattle(new[] { redo });

            b.Queue(redo, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(100, b.Enemy.CurrentHp);
            Assert.AreEqual(100, b.Player.CurrentHp);
        }

        [Test]
        public void Redo_FirstInTheTurn_DoesNotRepeatLastTurnsCommand()
        {
            var attack = Attack(10);
            var redo = NewCard("Redo", new RedoCardEffect());
            var b = NewBattle(new[] { attack, redo });

            b.Queue(attack, b.Enemy);
            b.ExecuteAll();
            b.UseCase.TurnEnd.EndPlayerTurn();
            Assert.AreEqual(90, b.Enemy.CurrentHp);

            b.Hand.AddCard(redo);
            b.Queue(redo, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(90, b.Enemy.CurrentHp, "前のターンのコマンドは繰り返さない");
        }

        [Test]
        public void Redo_OfATokenCommand_PaysAgain()
        {
            var cloudBurst = CloudBurst();
            var redo = NewCard("Redo", new RedoCardEffect());
            var b = NewBattle(new[] { cloudBurst, redo }, tokens: 200);

            b.Queue(cloudBurst, b.Enemy);
            b.Queue(redo, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(0, b.Enemy.CurrentHp);
            Assert.AreEqual(0, b.UseCase.Tokens.Tokens);
            Assert.AreEqual(200, b.UseCase.Tokens.Spent);
        }

        [Test]
        public void Redo_OfATokenCommand_FizzlesWithoutEnoughTokens()
        {
            var cloudBurst = CloudBurst();
            var redo = NewCard("Redo", new RedoCardEffect());
            var b = NewBattle(new[] { cloudBurst, redo }, tokens: 150);

            b.Queue(cloudBurst, b.Enemy);
            b.Queue(redo, b.Player);
            b.ExecuteAll();

            Assert.AreEqual(50, b.Enemy.CurrentHp, "繰り返しは不発");
            Assert.AreEqual(50, b.UseCase.Tokens.Tokens);
            Assert.AreEqual(100, b.UseCase.Tokens.Spent);
        }

        // ---------------- フラッシュ・パイプライン（バッファの数） ----------------

        [Test]
        public void Flush_AddsTheBonus_OnlyWhenTheBufferIsEmpty()
        {
            var flush = NewCard("Flush", Effect<FlushAttackCardEffect>(("_damage", 8), ("_bonusDamage", 16)));
            var withBuffer = NewBattle(new[] { flush, Filler() });
            withBuffer.Queue(flush, withBuffer.Enemy);
            withBuffer.ExecuteAll();
            Assert.AreEqual(92, withBuffer.Enemy.CurrentHp);

            var empty = NewBattle(new[] { flush });
            empty.Queue(flush, empty.Enemy);
            empty.ExecuteAll();
            Assert.AreEqual(76, empty.Enemy.CurrentHp);
        }

        [Test]
        public void Pipeline_DealsDamagePerCommandLeftInTheBuffer()
        {
            var pipeline = NewCard("Pipeline", Effect<BufferCountAttackCardEffect>(("_damagePerBuffer", 2)));
            var b = NewBattle(new[] { pipeline, Filler("A"), Filler("B"), Filler("C") });

            b.Queue(pipeline, b.Enemy);
            b.ExecuteAll();

            Assert.AreEqual(94, b.Enemy.CurrentHp);
        }

        // ---------------- スワップアウト・デフラグ（パージした数） ----------------

        [Test]
        public void SwapOut_GainsBlockPerPurgedCommand()
        {
            var swapOut = NewCard("SwapOut", Effect<PurgeBlockCardEffect>(("_purgeCount", 2), ("_blockPerPurge", 15)));
            var b = NewBattle(new[] { swapOut, Filler("A"), Filler("B"), Filler("C") });

            b.UseCase.Queue.TrySelectCard(swapOut);
            b.UseCase.Queue.SelectTarget(b.Player);
            Assert.IsTrue(b.UseCase.Queue.ConfirmPurgeChoice(new[] { b.Hand.Cards[1], b.Hand.Cards[2] }));
            b.ExecuteAll();

            Assert.AreEqual(30, b.Player.BlockValue);
            Assert.AreEqual(2, b.Purge.Count);
        }

        [Test]
        public void Defrag_HealsPerPurgedCommand()
        {
            var defrag = NewCard("Defrag", Effect<PurgeHealCardEffect>(("_purgeCount", 2), ("_healPerPurge", 15)));
            var b = NewBattle(new[] { defrag, Filler("A"), Filler("B") });
            b.Player.ChangeHp(HpChangeType.TakeAttackDamage, -50);

            b.UseCase.Queue.TrySelectCard(defrag);
            b.UseCase.Queue.SelectTarget(b.Player);
            b.UseCase.Queue.ConfirmPurgeChoice(new[] { b.Hand.Cards[1] });
            b.ExecuteAll();

            Assert.AreEqual(65, b.Player.CurrentHp);
        }

        // ---------------- クラウドバースト（トークン） ----------------

        private static CardData CloudBurst() => NewCard("CloudBurst",
            Effect<PayTokensCardEffect>(("_tokenCost", 100)),
            Effect<PiercingAttackCardEffect>(("_damage", 50)));

        [Test]
        public void CloudBurst_PaysTokensWhenQueued_AndPiercesBlock()
        {
            var cloudBurst = CloudBurst();
            var b = NewBattle(new[] { cloudBurst }, tokens: 150);
            b.Enemy.AddStatusEffect(new BlockStatusEffect(20));

            b.Queue(cloudBurst, b.Enemy);
            Assert.AreEqual(50, b.UseCase.Tokens.Tokens);
            Assert.AreEqual(100, b.UseCase.Tokens.Spent);

            b.ExecuteAll();
            Assert.AreEqual(50, b.Enemy.CurrentHp);
            Assert.AreEqual(100, b.UseCase.Tokens.Spent, "実行しても払ったまま");
        }

        [Test]
        public void CloudBurst_ReturnedToHand_RefundsTheTokens()
        {
            var cloudBurst = CloudBurst();
            var b = NewBattle(new[] { cloudBurst }, tokens: 150);

            b.Queue(cloudBurst, b.Enemy);
            b.UseCase.Queue.ReturnCommandToHand(0);

            Assert.AreEqual(150, b.UseCase.Tokens.Tokens);
            Assert.AreEqual(0, b.UseCase.Tokens.Spent);
        }

        [Test]
        public void CloudBurst_CannotBeSelected_WithoutEnoughTokens()
        {
            var cloudBurst = CloudBurst();
            Assert.AreEqual(100, cloudBurst.TokenCost);
            Assert.IsFalse(NewBattle(new[] { cloudBurst }, tokens: 99).UseCase.Queue.CanSelectCard(cloudBurst));
            Assert.IsTrue(NewBattle(new[] { cloudBurst }, tokens: 100).UseCase.Queue.CanSelectCard(cloudBurst));
        }

        // ---------------- ブルートフォース（確率） ----------------

        private sealed class FixedRandom : System.Random
        {
            private readonly int _value;
            public FixedRandom(int value) { _value = value; }
            public override int Next(int maxValue) => _value;
        }

        [Test]
        public void BruteForce_HitsOrMisses_ByTheRoll()
        {
            var original = ChanceAttackCardEffect.Random;
            try
            {
                var effect = Effect<ChanceAttackCardEffect>(("_chancePercent", 50), ("_damage", 30));
                var target = new UnitStatusModel(100, 0);

                ChanceAttackCardEffect.Random = new FixedRandom(49);
                effect.Execute(new UnitStatusModel(100, 0), target);
                Assert.AreEqual(70, target.CurrentHp);

                ChanceAttackCardEffect.Random = new FixedRandom(50);
                effect.Execute(new UnitStatusModel(100, 0), target);
                Assert.AreEqual(70, target.CurrentHp, "外れたら何も起きない");
            }
            finally
            {
                ChanceAttackCardEffect.Random = original;
            }
        }

        // ---------------- リフレクション ----------------

        [Test]
        public void Reflection_DealsTheUsersBlock_ThenHalvesIt()
        {
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);
            user.AddStatusEffect(new BlockStatusEffect(13));

            new BlockStrikeCardEffect().Execute(user, target);

            Assert.AreEqual(87, target.CurrentHp);
            Assert.AreEqual(6, user.BlockValue, "13 の半分（切り捨て）が残る");
        }

        [Test]
        public void Reflection_WithOneBlock_SpendsIt()
        {
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);
            user.AddStatusEffect(new BlockStatusEffect(1));

            new BlockStrikeCardEffect().Execute(user, target);

            Assert.AreEqual(99, target.CurrentHp);
            Assert.AreEqual(0, user.BlockValue);
        }

        // ---------------- フォーマット ----------------

        [Test]
        public void Format_ClearsCommandEffectsFromEveryUnit_ButKeepsTraits()
        {
            var format = NewCard("Format", new FormatCardEffect());
            var b = NewBattle(new[] { format });
            b.Player.AddStatusEffect(new StrengthStatusEffect(3));
            b.Player.AddStatusEffect(new BlockStatusEffect(10));
            b.Enemy.AddStatusEffect(new PoisonStatusEffect(4));
            b.Enemy.AddStatusEffect(new BlockStatusEffect(5));
            b.Enemy.AddStatusEffect(new KillImmunityStatusEffect());
            b.Enemy.AddStatusEffect(new LayerBoostStatusEffect(2));

            b.Queue(format, b.Player);
            b.ExecuteAll();

            Assert.IsEmpty(b.Player.StatusEffects);
            CollectionAssert.AreEquivalent(
                new[] { KillImmunityStatusEffect.EffectId, LayerBoostStatusEffect.EffectId },
                b.Enemy.StatusEffects.Select(e => e.Id));
        }

        [Test]
        public void Traits_AreMarked_AndCommandEffectsAreNot()
        {
            Assert.IsTrue(new VirusImmunityStatusEffect().IsTrait);
            Assert.IsTrue(new CorruptionImmunityStatusEffect().IsTrait);
            Assert.IsTrue(new KillImmunityStatusEffect().IsTrait);
            Assert.IsTrue(new LayerBoostStatusEffect(1).IsTrait);
            Assert.IsFalse(new StrengthStatusEffect(1).IsTrait);
            Assert.IsFalse(new BlockStatusEffect(1).IsTrait);
            Assert.IsFalse(new PoisonStatusEffect(1).IsTrait);
        }

        // ---------------- 敵の新しい行動 ----------------

        [Test]
        public void TargetHpRatioAttack_DealsPercentOfTheTargetsCurrentHp_AtLeastOne()
        {
            var effect = Effect<TargetHpRatioAttackCardEffect>(("_percent", 20));
            var target = new UnitStatusModel(100, 0);
            target.ChangeHp(HpChangeType.TakeAttackDamage, -50);

            effect.Execute(new UnitStatusModel(100, 0), target);
            Assert.AreEqual(40, target.CurrentHp);

            var weak = new UnitStatusModel(100, 0);
            weak.ChangeHp(HpChangeType.TakeAttackDamage, -97);
            effect.Execute(new UnitStatusModel(100, 0), weak);
            Assert.AreEqual(2, weak.CurrentHp, "端数で0になっても最低1");
        }

        // ---------------- 重ね掛け: 強化・継続ダメージは大きい方 +1 ----------------

        [Test]
        public void Worm_Duplicated_KeepsTheLargerPlusOne()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new WormStatusEffect(2));
            unit.AddStatusEffect(new WormStatusEffect(2));

            Assert.AreEqual(3, unit.StatusEffects.Single().Value);
        }

        [Test]
        public void Regeneration_Duplicated_StillKeepsTheLarger()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new RegenerationStatusEffect(3));
            unit.AddStatusEffect(new RegenerationStatusEffect(2));

            Assert.AreEqual(3, unit.StatusEffects.Single().Value, "回復は今回のルール変更の対象外");
        }

        // ---------------- ストーリー後半の敵の強さ ----------------

        [Test]
        public void StageData_Strength_IsNoneByDefault_AndFollowsTheFields()
        {
            var stage = ScriptableObject.CreateInstance<StageData>();
            Assert.AreSame(EnemyStrength.None, stage.Strength);

            SetPrivateField(stage, "_enemyHpPercent", 120);
            SetPrivateField(stage, "_enemyAttackBonus", 2);
            Assert.AreEqual(120, stage.Strength.HpPercent);
            Assert.AreEqual(2, stage.Strength.AttackBonus);
        }

        [Test]
        // ストーリーは上乗せ無し（2026-10-08。素の数値からレベルデザインする）
        public void StoryStages_HaveNoStrengthBoost()
        {
            for (int i = 1; i <= 10; i++)
            {
                var stage = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>($"Assets/ScriptableObjects/Stages/Stage{i:00}.asset");
                Assert.IsNotNull(stage, $"Stage{i:00}");
                Assert.AreSame(EnemyStrength.None, stage.Strength, $"Stage{i:00}");
            }
        }
    }
}
