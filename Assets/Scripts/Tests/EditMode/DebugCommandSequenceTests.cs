#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Live2DCardRogue.Common.DebugMenu;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class DebugCommandSequenceTests
    {
        // 文字列を1文字ずつ打ち、並びが揃った回数を返す
        private static int CountMatches(DebugCommandSequence sequence, string typed)
        {
            int matches = 0;
            foreach (var c in typed)
            {
                if (sequence.Input(c)) matches++;
            }
            return matches;
        }

        [Test]
        public void Input_MatchesOnTheLastDigitOfTheSequence()
        {
            var sequence = new DebugCommandSequence("0707");

            Assert.IsFalse(sequence.Input('0'));
            Assert.IsFalse(sequence.Input('7'));
            Assert.IsFalse(sequence.Input('0'));
            Assert.IsTrue(sequence.Input('7'));
        }

        // 前に余計な数字を打っていても、直近が並びと一致すればよい
        [Test]
        public void Input_IgnoresDigitsTypedBefore()
        {
            Assert.AreEqual(1, CountMatches(new DebugCommandSequence("0707"), "1230707"));
        }

        [Test]
        public void Input_DoesNotMatchAWrongOrder()
        {
            Assert.AreEqual(0, CountMatches(new DebugCommandSequence("0707"), "0770"));
        }

        // 揃ったら入力を空にするので、"070707" で二度反応しない（表示してすぐ消えることがない）
        [Test]
        public void Input_DoesNotReuseOverlappingDigits()
        {
            Assert.AreEqual(1, CountMatches(new DebugCommandSequence("0707"), "070707"));
        }

        // 2回打てば2回反応する（表示 → 非表示）
        [Test]
        public void Input_MatchesAgainWhenTypedAgain()
        {
            Assert.AreEqual(2, CountMatches(new DebugCommandSequence("0707"), "07070707"));
        }

        // 隅のタップ: 並びの最初から最後までが制限時間に収まったときだけ揃う
        [Test]
        public void Input_WithADuration_MatchesWithinIt()
        {
            var sequence = new DebugCommandSequence("0707", maxDuration: 2f);

            Assert.IsFalse(sequence.Input('0', 10.0f));
            Assert.IsFalse(sequence.Input('7', 10.5f));
            Assert.IsFalse(sequence.Input('0', 11.0f));
            Assert.IsTrue(sequence.Input('7', 11.9f));
        }

        [Test]
        public void Input_WithADuration_DoesNotMatchWhenTooSlow()
        {
            var sequence = new DebugCommandSequence("0707", maxDuration: 2f);

            sequence.Input('0', 10f);
            sequence.Input('7', 11f);
            sequence.Input('0', 12f);
            Assert.IsFalse(sequence.Input('7', 12.5f));
        }

        // 遅すぎた最初の1回は捨てて、そこから続けて打ち直せばよい
        [Test]
        public void Input_WithADuration_MatchesAgainAfterASlowStart()
        {
            var sequence = new DebugCommandSequence("0707", maxDuration: 2f);

            sequence.Input('0', 0f);
            sequence.Input('7', 5f);
            sequence.Input('0', 5.5f);
            sequence.Input('7', 6f);
            sequence.Input('0', 6.5f);
            Assert.IsTrue(sequence.Input('7', 7f));
        }

        // 途中で隅でない所（'x'）を触ったら途切れる
        [Test]
        public void Input_IsBrokenByAnotherInput()
        {
            Assert.AreEqual(0, CountMatches(new DebugCommandSequence("0707", maxDuration: 2f), "07x07"));
        }
    }
}
#endif
