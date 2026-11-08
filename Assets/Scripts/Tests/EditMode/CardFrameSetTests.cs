using System.Reflection;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardFrameSetTests
    {
        private CardFrameSet _frameSet;
        private Sprite _normal;
        private Sprite _rare;
        private Sprite _epic;

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static Sprite NewSprite(string name)
        {
            var sprite = Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.zero);
            sprite.name = name;
            return sprite;
        }

        [SetUp]
        public void SetUp()
        {
            _frameSet = ScriptableObject.CreateInstance<CardFrameSet>();
            _normal = NewSprite("normal");
            _rare = NewSprite("rare");
            _epic = NewSprite("epic");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var sprite in new[] { _normal, _rare, _epic })
            {
                Object.DestroyImmediate(sprite.texture);
                Object.DestroyImmediate(sprite);
            }
            Object.DestroyImmediate(_frameSet);
        }

        [TestCase(CardRarity.Normal, "normal")]
        [TestCase(CardRarity.Rare, "rare")]
        [TestCase(CardRarity.Epic, "epic")]
        public void GetFrame_ReturnsFrameOfThatRarity(CardRarity rarity, string expectedName)
        {
            SetPrivateField(_frameSet, "_normal", _normal);
            SetPrivateField(_frameSet, "_rare", _rare);
            SetPrivateField(_frameSet, "_epic", _epic);

            Assert.AreEqual(expectedName, _frameSet.GetFrame(rarity).name);
        }

        [Test]
        public void GetFrame_FallsBackToNormal_WhenThatRarityIsUnset()
        {
            SetPrivateField(_frameSet, "_normal", _normal);

            Assert.AreSame(_normal, _frameSet.GetFrame(CardRarity.Rare));
            Assert.AreSame(_normal, _frameSet.GetFrame(CardRarity.Epic));
        }

        [Test]
        public void GetFrame_ReturnsNull_WhenNothingIsSet()
        {
            Assert.IsNull(_frameSet.GetFrame(CardRarity.Epic));
        }
    }
}
