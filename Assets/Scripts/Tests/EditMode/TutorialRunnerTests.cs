using Live2DCardRogue.Common.Tutorial;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // チュートリアルの手の進み方
    public sealed class TutorialRunnerTests
    {
        private static TutorialStep Click(string key) => new TutorialStep(null, key);
        private static TutorialStep WaitFor(string eventName, string key = "Say") => new TutorialStep("Anchor", key, TutorialAdvance.Event, eventName);

        [Test]
        public void ClickSteps_AdvanceOnNext_AndFinishCompleted()
        {
            var runner = new TutorialRunner(new[] { Click("A"), Click("B") });
            bool? completed = null;
            runner.OnFinished += c => completed = c;

            runner.Start();
            Assert.AreEqual("A", runner.Current.TextKey);
            runner.Next();
            Assert.AreEqual("B", runner.Current.TextKey);
            runner.Next();

            Assert.IsTrue(runner.IsFinished);
            Assert.AreEqual(true, completed);
        }

        // 出来事を待つ手はクリックでは進まず、ほかの出来事でも進まない
        [Test]
        public void EventSteps_AdvanceOnlyOnTheirEvent()
        {
            var runner = new TutorialRunner(new[] { WaitFor(TutorialEventNames.CardQueued), Click("After") });
            runner.Start();

            runner.Next();
            runner.HandleEvent(TutorialEventNames.ExecuteRequested);
            Assert.AreEqual(0, runner.Index);

            runner.HandleEvent(TutorialEventNames.CardQueued);
            Assert.AreEqual("After", runner.Current.TextKey);
        }

        // クリックで進む手に、出来事は効かない（先に操作しても手順が飛ばない）
        [Test]
        public void ClickSteps_IgnoreEvents()
        {
            var runner = new TutorialRunner(new[] { Click("A"), Click("B") });
            runner.Start();

            runner.HandleEvent(TutorialEventNames.CardQueued);

            Assert.AreEqual(0, runner.Index);
        }

        [Test]
        public void Skip_FinishesNotCompleted_AndStopsAdvancing()
        {
            var runner = new TutorialRunner(new[] { Click("A"), Click("B") });
            bool? completed = null;
            runner.OnFinished += c => completed = c;
            runner.Start();

            runner.Skip();
            runner.Next();

            Assert.AreEqual(false, completed);
            Assert.AreEqual(0, runner.Index);
        }

        // 手数の表示は、何かを言う手だけを数える
        [Test]
        public void CountTextSteps_SkipsWaitOnlySteps()
        {
            var runner = new TutorialRunner(new[] { Click("A"), WaitFor(TutorialEventNames.PlayerTurnStarted, key: null), Click("B") });

            Assert.AreEqual(1, runner.CountTextStepsUpTo(1));
            Assert.AreEqual(2, runner.CountTextStepsUpTo(2));
        }
    }
}
