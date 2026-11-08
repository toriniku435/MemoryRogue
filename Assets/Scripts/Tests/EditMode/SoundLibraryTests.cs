using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // Resources/SoundLibrary.asset が SeId / BgmId と揃っているか。
    // 割り当て漏れも名前の打ち間違いも、実行時には「音が鳴らない」だけでエラーにならない
    public sealed class SoundLibraryTests
    {
        private const string LibraryPath = "Assets/Resources/SoundLibrary.asset";

        private SoundLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            Assert.IsNotNull(_library, $"{LibraryPath} が読めません");
        }

        [Test]
        public void EverySeIdHasAClip()
        {
            var missing = Ids<SeId>().Where(id => _library.FindSe(id) == null).ToList();
            Assert.IsEmpty(missing, "音が割り当てられていない SeId: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryBgmIdHasAClip()
        {
            var missing = Ids<BgmId>().Where(id => _library.FindBgm(id) == null).ToList();
            Assert.IsEmpty(missing, "音が割り当てられていない BgmId: " + string.Join(", ", missing));
        }

        [Test]
        public void SeEntriesAreKnownAndUnique()
        {
            AssertKnownAndUnique<SeId>(_library.SeEntries.Select(e => e?.Id), "SE");
        }

        [Test]
        public void BgmEntriesAreKnownAndUnique()
        {
            AssertKnownAndUnique<BgmId>(_library.BgmEntries.Select(e => e?.Id), "BGM");
        }

        [Test]
        public void EverySeIdHasAUsageLabel()
        {
            AssertUsageLabels<SeId>();
        }

        [Test]
        public void EveryBgmIdHasAUsageLabel()
        {
            AssertUsageLabels<BgmId>();
        }

        [Test]
        public void FindSeIgnoresUnknownAndDuplicateRows()
        {
            var library = ScriptableObject.CreateInstance<SoundLibrary>();
            var first = AudioClip.Create("first", 1, 1, 44100, false);
            var second = AudioClip.Create("second", 1, 1, 44100, false);
            try
            {
                var entries = new[]
                {
                    new SoundLibrary.SeEntry { Id = "NoSuchSound", Clip = second },
                    new SoundLibrary.SeEntry { Id = nameof(SeId.Hit), Clip = first },
                    new SoundLibrary.SeEntry { Id = nameof(SeId.Hit), Clip = second },
                };
                typeof(SoundLibrary).GetField("_se", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(library, entries);

                Assert.AreSame(first, library.FindSe(SeId.Hit).Clip);
                Assert.IsNull(library.FindSe(SeId.Heal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(library);
            }
        }

        private static IEnumerable<TId> Ids<TId>() where TId : struct, Enum
        {
            return Enum.GetValues(typeof(TId)).Cast<TId>().Where(id => id.ToString() != "None");
        }

        private static void AssertKnownAndUnique<TId>(IEnumerable<string> ids, string label) where TId : struct, Enum
        {
            var list = ids.ToList();
            var unknown = list.Where(id => !Enum.TryParse(id, out TId _) || id == "None").ToList();
            Assert.IsEmpty(unknown, $"{label} に enum に無い名前の行があります: " + string.Join(", ", unknown));

            var duplicates = list.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(duplicates, $"{label} に同じ名前の行が複数あります: " + string.Join(", ", duplicates));
        }

        private static void AssertUsageLabels<TId>() where TId : struct, Enum
        {
            var missing = Ids<TId>()
                .Where(id => typeof(TId).GetField(id.ToString()).GetCustomAttribute<SoundUsageAttribute>() == null)
                .ToList();
            Assert.IsEmpty(missing, $"{typeof(TId).Name} に SoundUsage が付いていない値: " + string.Join(", ", missing));
        }
    }
}
