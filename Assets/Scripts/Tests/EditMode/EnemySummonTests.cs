using System.Collections.Generic;
using System.Reflection;
using Live2DCardRogue.BattleScene.Data;
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
    // 敵の生成: 生成カードを持つ敵の分だけ倒れた枠が用意され、生成で戦場に戻り、倒されたらまた戻せる
    public sealed class EnemySummonTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = null;
            for (var t = target.GetType(); t != null && field == null; t = t.BaseType)
                field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static EnemyData NewEnemy(string id, int maxHp, params CardData[] cards)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(enemy, "_id", id);
            SetPrivateField(enemy, "_maxHp", maxHp);
            SetPrivateField(enemy, "_cards", cards);
            return enemy;
        }

        private static CardData NewCard(string id, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static CardData NewSummonCard(EnemyData summoned, int hp)
        {
            var effect = new SummonEnemyCardEffect();
            SetPrivateField(effect, "_enemy", summoned);
            SetPrivateField(effect, "_hp", hp);
            return NewCard("Summon", effect);
        }

        private EnemyData _minion;
        private CardData _summonCard;
        private EnemyData _boss;

        [SetUp]
        public void SetUp()
        {
            _minion = NewEnemy("Minion", 80);
            _summonCard = NewSummonCard(_minion, 40);
            _boss = NewEnemy("Boss", 300, _summonCard);
        }

        private EnemyPartyModel NewParty()
        {
            var enemies = BattleEnemyFactory.Create(new[] { _boss }, 1, EnemyStrength.None);
            enemies.AddRange(BattleEnemyFactory.CreateReserves(enemies, EnemyStrength.None));
            return new EnemyPartyModel(enemies);
        }

        [Test]
        public void Reserve_IsCreatedDefeated_WithTheSummonHp()
        {
            var party = NewParty();

            Assert.AreEqual(2, party.Enemies.Count);
            var reserve = party.Enemies[1];
            Assert.IsTrue(reserve.IsReserve);
            Assert.IsTrue(reserve.IsDefeated);
            Assert.AreEqual(40, reserve.Status.MaxHp);
            Assert.AreEqual(0, party.CountLivingAllies(party.Enemies[0]), "倒れた枠は味方に数えない");
            Assert.IsFalse(party.AreAllDefeated);
        }

        [Test]
        public void Summon_BringsTheReserveBack_AndCanRepeatAfterItIsDefeated()
        {
            var party = NewParty();
            var reserve = party.Enemies[1];
            BattleEnemy summoned = null;
            party.OnSummoned += e => summoned = e;

            Assert.IsTrue(party.Summon(_minion));
            Assert.AreSame(reserve, summoned);
            Assert.AreEqual(40, reserve.Status.CurrentHp);
            Assert.IsFalse(party.Summon(_minion), "出ている間は出せない");

            reserve.Status.ChangeHp(HpChangeType.TakePiercingDamage, -40);
            Assert.IsTrue(party.Summon(_minion));
            Assert.AreEqual(40, reserve.Status.CurrentHp);
        }

        // 生成は回復ではない: 回復反転があってもダメージにならず、状態効果も消える
        [Test]
        public void Revive_IgnoresHealInterventions_AndClearsStatusEffects()
        {
            var unit = new UnitStatusModel(40, 0);
            unit.ChangeHp(HpChangeType.TakePiercingDamage, -40);
            unit.AddStatusEffect(new HealReversalStatusEffect(turns: 2));
            bool revived = false;
            bool hpChanged = false;
            unit.OnRevived += () => revived = true;
            unit.OnHpChanged += (_, __) => hpChanged = true;

            unit.Revive();

            Assert.AreEqual(40, unit.CurrentHp);
            Assert.IsEmpty(unit.StatusEffects);
            Assert.IsTrue(revived);
            Assert.IsFalse(hpChanged, "回復の数字やログを出さない");
        }

        // 敵が生成カードを使うと、実行する側が生成を頼む
        [Test]
        public void ExecuteEnemyCard_WithASummonEffect_AsksToSummon()
        {
            var party = NewParty();
            var player = new UnitStatusModel(100, 10);
            var rig = new BattleCommandRig(player, new CardPileModel(new CardData[0]), new CardPileModel(new CardData[0]),
                new CardPileModel(new CardData[0]), new CardCommandModel(3), new CardSelectionModel(), _ => null);
            var execute = new BattleCommandExecuteUseCase(player, rig.Piles, rig.Commands,
                new BattleTargetResolver(player, _ => null, null, null), rig.Tokens, new CommandHistoryModel(), party.Summon);

            execute.ExecuteEnemyCard(_summonCard, party.Enemies[0].Status, player);

            Assert.IsFalse(party.Enemies[1].IsDefeated);
            Assert.AreEqual(100, player.CurrentHp, "生成はプレイヤーに何もしない");
        }

        [Test]
        public void TurnsAlone_CountsOnlyWhileNoAllyIsAlive()
        {
            var enemy = new BattleEnemy(_boss, new UnitStatusModel(300, 0));

            enemy.CountTurnEnd(hasLivingAlly: false);
            enemy.CountTurnEnd(hasLivingAlly: false);
            Assert.AreEqual(2, enemy.TurnsAlone);

            enemy.CountTurnEnd(hasLivingAlly: true);
            Assert.AreEqual(0, enemy.TurnsAlone);
        }
    }
}
