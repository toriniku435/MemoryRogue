namespace Live2DCardRogue.Common.Views
{
    public enum UnitAnimationClip
    {
        Idle,
        Action,
        Hit,
    }

    // コマ送りアニメで「今どのクリップの何コマ目か」だけを決める。時間は外から渡すので、描画にも Unity にも依存しない。
    //   ・アイドルはループ
    //   ・行動・被弾は1回再生してアイドルへ戻る（被弾は行動を上書きする）
    //   ・倒れている間は被弾の最後のコマで止まる（被弾のコマが無ければアイドルの1コマ目）
    public sealed class FrameAnimationPlayer
    {
        private readonly int[] _frameCounts = new int[3];
        private readonly float[] _frameDurations = new float[3];
        private float _elapsed;

        public UnitAnimationClip Clip { get; private set; } = UnitAnimationClip.Idle;
        public int FrameIndex { get; private set; }
        public bool IsDefeated { get; private set; }

        public FrameAnimationPlayer(int idleCount, int actionCount, int hitCount, float idleFps, float actionFps, float hitFps)
        {
            Set(UnitAnimationClip.Idle, idleCount, idleFps);
            Set(UnitAnimationClip.Action, actionCount, actionFps);
            Set(UnitAnimationClip.Hit, hitCount, hitFps);
        }

        private void Set(UnitAnimationClip clip, int count, float fps)
        {
            _frameCounts[(int)clip] = count < 0 ? 0 : count;
            _frameDurations[(int)clip] = fps > 0f ? 1f / fps : float.MaxValue;
        }

        private int Count(UnitAnimationClip clip) => _frameCounts[(int)clip];

        public void Play(UnitAnimationClip clip)
        {
            if (Count(clip) == 0) return;
            if (IsDefeated && clip != UnitAnimationClip.Hit) return;
            // 被弾中の行動は、被弾を最後まで見せる
            if (clip == UnitAnimationClip.Action && Clip == UnitAnimationClip.Hit) return;

            Clip = clip;
            FrameIndex = 0;
            _elapsed = 0f;
        }

        public void SetDefeated(bool isDefeated)
        {
            if (IsDefeated == isDefeated) return;
            IsDefeated = isDefeated;

            if (!isDefeated)
            {
                Clip = UnitAnimationClip.Idle;
                FrameIndex = 0;
                _elapsed = 0f;
            }
            else if (Clip != UnitAnimationClip.Hit)
            {
                // 被弾を再生中ならその終わりで止まる。そうでなければすぐ倒れた姿にする
                HoldDefeatedPose();
            }
        }

        /// <summary>時間を進める。コマが変わったら true</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return false;
            if (IsDefeated && IsHoldingDefeatedPose()) return false;

            int count = Count(Clip);
            if (count == 0)
            {
                if (Clip == UnitAnimationClip.Idle) return false;
                ReturnToIdle();
                return true;
            }

            _elapsed += deltaTime;
            float duration = _frameDurations[(int)Clip];
            bool changed = false;
            while (_elapsed >= duration)
            {
                _elapsed -= duration;
                changed = true;
                if (FrameIndex + 1 < count)
                {
                    FrameIndex++;
                }
                else if (Clip == UnitAnimationClip.Idle)
                {
                    FrameIndex = 0;
                }
                else
                {
                    ReturnToIdle();
                    if (IsDefeated) return true;
                }
            }
            return changed;
        }

        private void ReturnToIdle()
        {
            _elapsed = 0f;
            if (IsDefeated)
            {
                HoldDefeatedPose();
                return;
            }
            Clip = UnitAnimationClip.Idle;
            FrameIndex = 0;
        }

        private void HoldDefeatedPose()
        {
            int hitCount = Count(UnitAnimationClip.Hit);
            Clip = hitCount > 0 ? UnitAnimationClip.Hit : UnitAnimationClip.Idle;
            FrameIndex = hitCount > 0 ? hitCount - 1 : 0;
            _elapsed = 0f;
        }

        private bool IsHoldingDefeatedPose()
        {
            int hitCount = Count(UnitAnimationClip.Hit);
            return hitCount > 0
                ? Clip == UnitAnimationClip.Hit && FrameIndex == hitCount - 1
                : Clip == UnitAnimationClip.Idle && FrameIndex == 0;
        }
    }
}
