using Live2DCardRogue.Common.Views;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public class FrameAnimationPlayerTests
    {
        // アイドル4コマ・行動4コマ・被弾2コマ、どれも 10fps（1コマ 0.1 秒）
        private static FrameAnimationPlayer Create(int hitCount = 2)
        {
            return new FrameAnimationPlayer(4, 4, hitCount, 10f, 10f, 10f);
        }

        private const float Frame = 0.1f;
        // 浮動小数の誤差でコマの境目を取りこぼさないよう、少しだけ長めに進める
        private const float Step = Frame + 0.001f;

        [Test]
        public void Idle_LoopsBackToFirstFrame()
        {
            var player = Create();
            for (int i = 0; i < 4; i++) player.Tick(Step);

            Assert.AreEqual(UnitAnimationClip.Idle, player.Clip);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void Action_PlaysOnceThenReturnsToIdle()
        {
            var player = Create();
            player.Play(UnitAnimationClip.Action);
            for (int i = 0; i < 3; i++) player.Tick(Step);
            Assert.AreEqual(UnitAnimationClip.Action, player.Clip);
            Assert.AreEqual(3, player.FrameIndex);

            player.Tick(Step);
            Assert.AreEqual(UnitAnimationClip.Idle, player.Clip);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void Hit_InterruptsAction()
        {
            var player = Create();
            player.Play(UnitAnimationClip.Action);
            player.Tick(Step);
            player.Play(UnitAnimationClip.Hit);

            Assert.AreEqual(UnitAnimationClip.Hit, player.Clip);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void Action_DuringHit_IsIgnored()
        {
            var player = Create();
            player.Play(UnitAnimationClip.Hit);
            player.Play(UnitAnimationClip.Action);

            Assert.AreEqual(UnitAnimationClip.Hit, player.Clip);
        }

        [Test]
        public void Defeated_AfterHit_HoldsLastHitFrame()
        {
            var player = Create();
            player.Play(UnitAnimationClip.Hit);
            player.SetDefeated(true);
            for (int i = 0; i < 10; i++) player.Tick(Step);

            Assert.AreEqual(UnitAnimationClip.Hit, player.Clip);
            Assert.AreEqual(1, player.FrameIndex);
        }

        [Test]
        public void Defeated_WhileIdle_JumpsToDefeatedPose()
        {
            var player = Create();
            player.SetDefeated(true);

            Assert.AreEqual(UnitAnimationClip.Hit, player.Clip);
            Assert.AreEqual(1, player.FrameIndex);
            Assert.IsFalse(player.Tick(Step));
        }

        [Test]
        public void Defeated_IgnoresAction()
        {
            var player = Create();
            player.SetDefeated(true);
            player.Play(UnitAnimationClip.Action);

            Assert.AreEqual(UnitAnimationClip.Hit, player.Clip);
        }

        [Test]
        public void Defeated_WithoutHitFrames_HoldsFirstIdleFrame()
        {
            var player = Create(hitCount: 0);
            player.Tick(Step);
            player.SetDefeated(true);
            player.Tick(Step);

            Assert.AreEqual(UnitAnimationClip.Idle, player.Clip);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void Revived_ResumesIdle()
        {
            var player = Create();
            player.SetDefeated(true);
            player.SetDefeated(false);
            player.Tick(Step);

            Assert.AreEqual(UnitAnimationClip.Idle, player.Clip);
            Assert.AreEqual(1, player.FrameIndex);
        }

        [Test]
        public void Play_ClipWithoutFrames_IsIgnored()
        {
            var player = new FrameAnimationPlayer(4, 0, 2, 10f, 10f, 10f);
            player.Play(UnitAnimationClip.Action);

            Assert.AreEqual(UnitAnimationClip.Idle, player.Clip);
        }
    }
}
