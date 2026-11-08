using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // コマ送りのドット絵で IUnitVisual を実装する。Image のスプライトを差し替えていくだけ。
    // SpriteSheetUnitVisualDefinition.Create が GameObject ごと作って Setup する
    [RequireComponent(typeof(Image))]
    public sealed class SpriteSheetUnitVisual : MonoBehaviour, IUnitVisual
    {
        // UI の演出と同じく時間は unscaled。1フレームが長くても（ヒッチ・エディタのフォーカス外）コマを飛ばし過ぎない
        private const float MaxDeltaTime = 0.1f;

        private Image _image;
        private IReadOnlyList<Sprite> _idle;
        private IReadOnlyList<Sprite> _action;
        private IReadOnlyList<Sprite> _hit;
        private FrameAnimationPlayer _player;

        public void Setup(IReadOnlyList<Sprite> idle, IReadOnlyList<Sprite> action, IReadOnlyList<Sprite> hit,
            float idleFps, float actionFps, float hitFps)
        {
            _image = GetComponent<Image>();
            _idle = idle;
            _action = action;
            _hit = hit;
            _player = new FrameAnimationPlayer(idle.Count, action.Count, hit.Count, idleFps, actionFps, hitFps);
            ApplyFrame();
        }

        public void PlayAction()
        {
            if (_player == null) return;
            _player.Play(UnitAnimationClip.Action);
            ApplyFrame();
        }

        public void PlayHit()
        {
            if (_player == null) return;
            _player.Play(UnitAnimationClip.Hit);
            ApplyFrame();
        }

        public void SetDefeated(bool isDefeated)
        {
            if (_player == null) return;
            _player.SetDefeated(isDefeated);
            ApplyFrame();
        }

        public void SetColor(Color color)
        {
            if (_image != null) _image.color = color;
        }

        private void Update()
        {
            if (_player != null && _player.Tick(Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime)))
            {
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            var frames = _player.Clip switch
            {
                UnitAnimationClip.Action => _action,
                UnitAnimationClip.Hit => _hit,
                _ => _idle,
            };
            if (frames == null || _player.FrameIndex >= frames.Count) return;

            var sprite = frames[_player.FrameIndex];
            if (sprite != null) _image.sprite = sprite;
        }
    }
}
