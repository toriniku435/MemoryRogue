using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace Live2DCardRogue.Tests.EditMode
{
    // シーン間は ID で渡し、コマンドと敵の実物は GameAssetRepository が「解放しない」印を付けて持つ。
    // 印が無いと、ビルドでだけ「渡したはずのものが次のシーンで消えている」ことになる（エディタでは再現しない）
    public sealed class GameAssetRepositoryTests
    {
        private CardData _card;
        private EnemyData _enemy;

        [SetUp]
        public void SetUp()
        {
            _card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(_card, "_id", "GameAssetRepositoryTests_Card");
            _enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(_enemy, "_id", "GameAssetRepositoryTests_Enemy");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_card);
            Object.DestroyImmediate(_enemy);
        }

        [Test]
        public void Hold_MarksTheAssetAndFindsItById()
        {
            Assert.AreEqual(_card.Id, GameAssetRepository.Hold(_card));
            Assert.AreEqual(_enemy.Id, GameAssetRepository.Hold(_enemy));

            Assert.IsTrue((_card.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0, "解放しない印が無い");
            Assert.IsTrue((_enemy.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0, "解放しない印が無い");
            Assert.AreSame(_card, GameAssetRepository.FindCard(_card.Id));
            Assert.AreSame(_enemy, GameAssetRepository.FindEnemy(_enemy.Id));
        }

        [Test]
        public void Hold_IgnoresNull()
        {
            Assert.IsNull(GameAssetRepository.Hold((CardData)null));
            Assert.IsNull(GameAssetRepository.Hold((EnemyData)null));
            Assert.IsNull(GameAssetRepository.FindCard(null));
        }

        // 預けていない ID はリポジトリがエラーを出す（呼ぶ側は null を飛ばすだけ）
        [Test]
        public void Find_LogsAnErrorForAnIdNotHeld()
        {
            LogAssert.Expect(LogType.Error, new Regex("GameAssetRepositoryTests_Missing"));
            Assert.IsNull(GameAssetRepository.FindEnemy("GameAssetRepositoryTests_Missing"));
        }

        // 遷移データはデッキと敵を ID にして運び、同時にリポジトリに預ける（並び順と重複はそのまま）
        [Test]
        public void BattleTransitionData_CarriesIdsAndHoldsTheAssets()
        {
            var data = new BattleSceneTransitionData(null, new[] { _card, null, _card }, new[] { _enemy });

            CollectionAssert.AreEqual(new[] { _card.Id, _card.Id }, data.DeckIds);
            CollectionAssert.AreEqual(new[] { _enemy.Id }, data.EnemyIds);
            Assert.AreSame(_card, GameAssetRepository.FindCard(data.DeckIds[0]));
            Assert.AreSame(_enemy, GameAssetRepository.FindEnemy(data.EnemyIds[0]));
        }

        // ID で引き直すので、同じ ID の敵が2つあると取り違える（コマンドは CardCatalogTests が見ている）
        [Test]
        public void EnemyIds_AreUnique()
        {
            var ids = AssetDatabase.FindAssets("t:EnemyData")
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g)).Id)
                .ToList();
            CollectionAssert.AllItemsAreUnique(ids, "同じ ID の敵がある");
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }
    }
}
