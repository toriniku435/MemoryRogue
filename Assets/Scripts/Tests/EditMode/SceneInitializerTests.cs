using System;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.SceneTransition;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // どれも壊れてもエラーにならず、遷移で渡したデータが黙って無視されるだけなので、型の形で確かめる
    public sealed class SceneInitializerTests
    {
        private static Type[] SceneInitializerTypes()
        {
            return typeof(SceneInitializerBase<>).Assembly.GetTypes()
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type) && type.Name.EndsWith("SceneInitializer"))
                .ToArray();
        }

        [Test]
        public void SceneInitializersExist()
        {
            Assert.IsNotEmpty(SceneInitializerTypes());
        }

        [Test]
        public void EverySceneInitializer_DerivesFromSceneInitializerBase()
        {
            foreach (var type in SceneInitializerTypes())
            {
                Assert.IsTrue(DerivesFromBase(type), $"{type.Name} が SceneInitializerBase<TData> を継承していません");
            }
        }

        // 派生側に Start があると Unity はそちらだけを呼び、基底での取り出しが飛ばされる
        [Test]
        public void NoSceneInitializer_DeclaresItsOwnStart()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var type in SceneInitializerTypes())
            {
                Assert.IsNull(type.GetMethod("Start", flags, null, Type.EmptyTypes, null), $"{type.Name} が Start を定義しています。Initialize に書いてください");
            }
        }

        private static bool DerivesFromBase(Type type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SceneInitializerBase<>)) return true;
            }
            return false;
        }
    }
}
