using System;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.Reward.Views
{
    // 戦闘から戻ってきたときの報酬ウインドウ（Assets/Prefabs/StageRewardWindow.prefab）。
    //   勝ったとき: // ACCESS GRANTED。カード1枚が放射光を背負って横一線から開き、トークンが数え上がる
    //   負けたとき: // CONNECTION LOST。バックアップブーストとして支援トークンだけが数え上がる
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // 演出中は背面のどこをクリックしても最後まで飛ばせる。受け取るボタンは演出が終わってから出す。
    //
    // シーン遷移の覆いが明けるまでは開かない（覆われている間に演出が流れてしまうため）。
    // 押されたことを伝えるだけで、閉じた後に何をするかは Presenter が決める
    public sealed class StageRewardView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        // 演出中だけクリックを受けて飛ばす背面（ウインドウの後ろの暗い板）
        [SerializeField] private ClickArea _skipArea;
        [SerializeField] private RectTransform _window;

        [SerializeField] private CommonText _decoText;
        // 右上の補足（LAYER 03 / 10）
        [SerializeField] private CommonText _detailText;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonText _messageText;

        [Header("カード（勝ったときだけ）")]
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private CardView _cardView;
        // カードの縁の光（レアリティの色）
        [SerializeField] private Image _cardGlow;
        // 開いた瞬間にカードを白く光らせる板
        [SerializeField] private Image _cardFlash;

        [Header("トークン")]
        [SerializeField] private RectTransform _tokenPanel;
        [SerializeField] private CommonText _tokenLabel;
        [SerializeField] private TokenCounterView _tokenCounter;
        [SerializeField] private CommonText _totalText;

        // 報酬の後ろで回る放射光
        [SerializeField] private Image _rays;

        [SerializeField] private CommonButton _claimButton;
        [SerializeField] private CommonText _claimLabel;

        [Header("色")]
        // レアリティごとの光の色（ノーマル・レア・エピック）。カード購入の開封演出とそろえる
        [SerializeField] private Color[] _tierColors =
        {
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
        };
        [SerializeField] private Color _victoryTitleColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _defeatTitleColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _victoryDecoColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _defeatDecoColor = new Color(1f, 0.18f, 0.77f, 1f);
        // カードが無いときの放射光（トークンの金）と、負けたときの放射光（支援の緑）
        [SerializeField] private Color _tokenRaysColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _backupRaysColor = new Color(0.62f, 1f, 0.2f, 1f);
        [SerializeField][Range(0f, 1f)] private float _raysAlpha = 0.28f;

        [Header("並べ方")]
        [SerializeField] private Vector2 _victoryWindowSize = new Vector2(1080f, 760f);
        [SerializeField] private Vector2 _defeatWindowSize = new Vector2(900f, 640f);
        [SerializeField] private Vector2 _cardPosition = new Vector2(-220f, -30f);
        [SerializeField] private Vector2 _tokenPositionBesideCard = new Vector2(230f, -30f);
        [SerializeField] private Vector2 _tokenPositionAlone = new Vector2(0f, -30f);

        [Header("間合い")]
        // 画面が見えるようになってから開くまで
        [SerializeField][Min(0f)] private float _openDelay = 0.2f;
        // 開き始めてから報酬が現れるまで（枠が開き切るのを待つ）
        [SerializeField][Min(0f)] private float _revealDelay = 0.25f;
        [SerializeField][Min(0.01f)] private float _cardOpenDuration = 0.22f;
        [SerializeField][Min(0.01f)] private float _flashDuration = 0.4f;
        // 報酬が現れてからトークンを数え始めるまで
        [SerializeField][Min(0f)] private float _countDelay = 0.4f;
        [SerializeField] private float _raysSpeed = 18f;
        [SerializeField][Min(0.01f)] private float _glowPulseSpeed = 3f;

        private enum Phase { Idle, WaitForScreen, Sequence, Done }

        private Phase _phase;
        private float _time;
        private float _openAt;
        private bool _revealed;
        private bool _countStarted;
        private StageReward _reward;
        private Color _glowColor;

        /// <summary>受け取るボタンが押された</summary>
        public event Action OnClaimClicked;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _claimButton.onClick.AddListener(HandleClaim);
            _skipArea.OnClicked += Skip;
        }

        private void OnDestroy()
        {
            _claimButton.onClick.RemoveListener(HandleClaim);
            _skipArea.OnClicked -= Skip;
        }

        /// <summary>
        /// 報酬を見せる。シーン遷移の覆いが明けてから開く。
        /// 文字は SetTexts で別に渡す（言語が変わったときに引き直せるように）
        /// </summary>
        /// <param name="detail">右上の補足（どのステージか）</param>
        /// <param name="totalAfter">受け取った後の所持トークン</param>
        public void Open(StageReward reward, string detail, int totalAfter)
        {
            _reward = reward;
            bool hasCard = reward.IsVictory && reward.Card != null;

            _decoText.text = reward.IsVictory ? "// ACCESS GRANTED  ::  LOOT DECRYPTED" : "// CONNECTION LOST  ::  BACKUP BOOST";
            _decoText.color = reward.IsVictory ? _victoryDecoColor : _defeatDecoColor;
            _titleText.color = reward.IsVictory ? _victoryTitleColor : _defeatTitleColor;
            _detailText.text = detail;
            _window.sizeDelta = hasCard ? _victoryWindowSize : _defeatWindowSize;

            _cardRoot.gameObject.SetActive(hasCard);
            if (hasCard)
            {
                _cardRoot.anchoredPosition = _cardPosition;
                _cardView.SetCardData(reward.Card);
                _glowColor = TierColor((int)reward.Card.Rarity);
            }
            _tokenPanel.anchoredPosition = hasCard ? _tokenPositionBesideCard : _tokenPositionAlone;
            _totalText.text = $"TOTAL  {totalAfter}";

            var raysColor = hasCard ? _glowColor : (reward.IsVictory ? _tokenRaysColor : _backupRaysColor);
            _rays.rectTransform.anchoredPosition = hasCard ? _cardPosition : _tokenPositionAlone;
            _rays.color = WithAlpha(raysColor, 0f);

            _tokenCounter.SetValue(0);
            _claimButton.gameObject.SetActive(false);
            _revealed = false;
            _countStarted = false;
            ApplyReveal(0f);

            _phase = Phase.WaitForScreen;
            _time = 0f;
            _openAt = -1f;
        }

        public void SetTexts(string title, string message, string tokenLabel, string claimLabel)
        {
            _titleText.text = title;
            _messageText.text = message;
            _tokenLabel.text = tokenLabel;
            _claimLabel.text = claimLabel;
        }

        /// <summary>言語が変わったときに、カードの文字を引き直す</summary>
        public void RefreshCard()
        {
            if (_cardRoot.gameObject.activeSelf && _cardView.CardData != null) _cardView.SetCardData(_cardView.CardData);
        }

        /// <summary>閉じる動きを流し、閉じ終わったら onClosed を呼ぶ</summary>
        public void Close(Action onClosed)
        {
            _phase = Phase.Idle;
            _dialog.PlayClose(onClosed);
        }

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void HideImmediate()
        {
            _phase = Phase.Idle;
            _dialog.HideImmediate();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_phase == Phase.WaitForScreen)
            {
                if (SceneTransitionEffect.IsTransitioning) return;
                if (_openAt < 0f) _openAt = _time + _openDelay;
                _time += dt;
                if (_time < _openAt) return;

                _dialog.PlayOpen();
                _phase = Phase.Sequence;
                _time = 0f;
                return;
            }

            if (_phase == Phase.Idle) return;

            _time += dt;
            _rays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_time * _raysSpeed);

            if (_phase == Phase.Sequence)
            {
                if (!_revealed && _time >= _revealDelay)
                {
                    _revealed = true;
                    AudioUseCase.PlaySe(_reward.IsVictory ? SeId.Reward : SeId.BackupBoost);
                }
                if (_revealed) ApplyReveal(_time - _revealDelay);

                if (!_countStarted && _time >= _revealDelay + _countDelay)
                {
                    _countStarted = true;
                    _tokenCounter.CountTo(_reward.Tokens, FinishSequence);
                }
            }

            // 開き終えたカードの縁は、レアリティの色でゆっくり脈打つ
            if (_phase == Phase.Done && _cardRoot.gameObject.activeSelf)
            {
                float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(_time * _glowPulseSpeed));
                _cardGlow.color = WithAlpha(_glowColor, pulse);
            }
        }

        // 報酬が現れる動き（t は現れ始めてからの秒数）。カードは横一線からチラつきながら開き、白く光ってから落ち着く
        private void ApplyReveal(float t)
        {
            float open = Mathf.Clamp01(t / _cardOpenDuration);
            float eased = 1f - (1f - open) * (1f - open);
            bool flicker = open < 1f && Mathf.FloorToInt(t * 40f) % 3 == 0;

            _cardRoot.localScale = new Vector3(1f, Mathf.Lerp(0.03f, 1f, eased), 1f);
            _cardView.SetActive(t > 0f);
            _cardGlow.color = WithAlpha(_glowColor, t > 0f ? (flicker ? 0.3f : 1f) : 0f);

            float flash = t <= 0f ? 0f : 1f - Mathf.Clamp01(t / _flashDuration);
            _cardFlash.color = WithAlpha(Color.white, flash * flash);

            _rays.color = WithAlpha(_rays.color, Mathf.Clamp01(t / 0.3f) * _raysAlpha);
        }

        // 演出を最後まで飛ばす（背面のどこをクリックしても）
        private void Skip()
        {
            if (_phase != Phase.Sequence) return;
            AudioUseCase.PlaySe(SeId.Click);
            if (!_revealed)
            {
                _revealed = true;
                AudioUseCase.PlaySe(_reward.IsVictory ? SeId.Reward : SeId.BackupBoost);
            }
            ApplyReveal(10f);
            if (!_countStarted)
            {
                _countStarted = true;
                _tokenCounter.CountTo(_reward.Tokens, FinishSequence);
            }
            _tokenCounter.Finish();
        }

        private void FinishSequence()
        {
            if (_phase != Phase.Sequence) return;
            _phase = Phase.Done;
            _claimButton.gameObject.SetActive(true);
        }

        // 閉じる途中の二度押しは伝えない
        private void HandleClaim()
        {
            if (IsOpen && _phase == Phase.Done) OnClaimClicked?.Invoke();
        }

        private Color TierColor(int tier)
        {
            return _tierColors[Mathf.Clamp(tier, 0, _tierColors.Length - 1)];
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
