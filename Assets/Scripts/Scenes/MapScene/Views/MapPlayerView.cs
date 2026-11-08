using System;
using UnityEngine;
using Live2DCardRogue.Common.SceneTransition;

namespace Live2DCardRogue.MapScene.Views
{
    // マップ上のプレイヤー。今いる部屋の上に立ち、次の部屋へ進むときは
    //   分解: 横一線に潰れながら明滅し、電子の粒になって散る
    //   転送: 光るパケットが軌跡を残しながら、格子に吸着して（カクカクと）次の部屋へ飛ぶ
    //   再構成: 着いた先で粒が弾け、横一線から開いてキャラクターに戻る
    // と見せる。見た目だけで、どこへ進むかは Presenter が決める
    public sealed class MapPlayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        // キャラクター（立ち絵と足元の輪）
        [SerializeField] private RectTransform _body;
        [SerializeField] private CanvasGroup _bodyGroup;
        // パケット（光る菱形）と、その軌跡を出すパーティクル
        [SerializeField] private RectTransform _packet;
        [SerializeField] private ParticleSystem _trail;
        [SerializeField] private ParticleSystem _burst;

        [SerializeField][Min(0.01f)] private float _dissolveDuration = 0.35f;
        [SerializeField][Min(0.01f)] private float _travelDuration = 0.7f;
        [SerializeField][Min(0.01f)] private float _assembleDuration = 0.35f;
        // 転送中の位置をこの間隔の格子に吸着させる（デジタルっぽい動き）
        [SerializeField][Min(0f)] private float _step = 12f;
        // 画面が見えるようになってから動き出すまでの間（何が起きるか目で追えるように）
        [SerializeField][Min(0f)] private float _startDelay = 0.25f;

        private enum Phase
        {
            Idle,
            // シーン遷移の演出が明けるのを待っている（覆われている間に動き出すと、分解の途中が見えないため）
            WaitForScreen,
            Dissolve,
            Travel,
            Assemble,
        }

        private Phase _phase;
        private float _time;
        private Vector2 _from;
        private Vector2 _to;
        private Action _onArrived;
        private Action _onDeparted;

        /// <summary>パケットが部屋から部屋へ飛ぶ時間（ページ送りのスクロールをこれに合わせる）</summary>
        public float TravelDuration => _travelDuration;

        public void PlaceAt(Vector2 position)
        {
            _phase = Phase.Idle;
            _root.anchoredPosition = position;
            ShowBody(1f, 1f);
            _packet.gameObject.SetActive(false);
        }

        // from の部屋から to の部屋へ転送する。分解し終えてパケットが飛び始めた時点で onDeparted を、
        // 着いてキャラクターに戻り始めた時点で onArrived を呼ぶ
        public void PlayTravel(Vector2 from, Vector2 to, Action onArrived, Action onDeparted = null)
        {
            _from = from;
            _to = to;
            _onArrived = onArrived;
            _onDeparted = onDeparted;
            _root.anchoredPosition = from;
            ShowBody(1f, 1f);
            _phase = Phase.WaitForScreen;
            _time = 0f;
        }

        private void Update()
        {
            if (_phase == Phase.Idle) return;

            _time += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            switch (_phase)
            {
                case Phase.WaitForScreen:
                {
                    if (SceneTransitionEffect.IsTransitioning)
                    {
                        _time = 0f;
                        break;
                    }
                    if (_time >= _startDelay)
                    {
                        if (_burst != null) _burst.Play();
                        Next(Phase.Dissolve);
                    }
                    break;
                }
                case Phase.Dissolve:
                {
                    // 横一線に潰れながらチラつき、粒になって消える
                    float p = Mathf.Clamp01(_time / _dissolveDuration);
                    float flicker = UnityEngine.Random.value < 0.35f ? 0.3f : 1f;
                    ShowBody(Mathf.Lerp(1f, 1.3f, p), Mathf.Lerp(1f, 0.04f, p * p), (1f - p) * flicker);
                    if (p >= 1f)
                    {
                        Next(Phase.Travel);
                        var departed = _onDeparted;
                        _onDeparted = null;
                        departed?.Invoke();
                    }
                    break;
                }
                case Phase.Travel:
                {
                    float p = Mathf.Clamp01(_time / _travelDuration);
                    float eased = p * p * (3f - 2f * p);
                    var position = Vector2.LerpUnclamped(_from, _to, eased);
                    if (_step > 0f && p < 1f)
                    {
                        var d = position - _to;
                        position = _to + new Vector2(Mathf.Round(d.x / _step) * _step, Mathf.Round(d.y / _step) * _step);
                    }
                    _root.anchoredPosition = position;
                    _packet.gameObject.SetActive(true);
                    _packet.localRotation = Quaternion.Euler(0f, 0f, _time * 720f);
                    if (_trail != null && !_trail.isEmitting) _trail.Play();
                    if (p >= 1f)
                    {
                        _packet.gameObject.SetActive(false);
                        if (_trail != null) _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        if (_burst != null) _burst.Play();
                        Next(Phase.Assemble);
                        var callback = _onArrived;
                        _onArrived = null;
                        callback?.Invoke();
                    }
                    break;
                }
                case Phase.Assemble:
                {
                    // 横一線から開いてキャラクターに戻る
                    float p = Mathf.Clamp01(_time / _assembleDuration);
                    float open = 1f - (1f - p) * (1f - p);
                    float flicker = p < 0.6f && UnityEngine.Random.value < 0.35f ? 0.4f : 1f;
                    ShowBody(Mathf.Lerp(1.3f, 1f, open), Mathf.Lerp(0.04f, 1f, open), flicker);
                    if (p >= 1f)
                    {
                        ShowBody(1f, 1f);
                        _phase = Phase.Idle;
                    }
                    break;
                }
            }
        }

        private void Next(Phase phase)
        {
            _phase = phase;
            _time = 0f;
        }

        private void ShowBody(float scaleX, float scaleY, float alpha = 1f)
        {
            _body.localScale = new Vector3(scaleX, scaleY, 1f);
            if (_bodyGroup != null) _bodyGroup.alpha = alpha;
        }
    }
}
