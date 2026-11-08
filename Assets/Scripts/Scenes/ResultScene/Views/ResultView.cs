using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.ResultScene.Views
{
    // リザルトの演出。成績は受け取って見せるだけで、計算はしない。
    //   1. 内訳が1行ずつすべり込む（残りトークン → バックアップブースト）
    //   2. スコアが 0 から数え上がる
    //   3. 間を置いて、ランクがザザッとデコードされて現れる
    //      （横一線から開き、文字がスクランブルしながら左から確定する。シアン/マゼンタの残像と走査線が走る）
    //   4. 「タイトルへ」ボタンが出る
    // 重さで見せる（落として揺らす）のではなく、デジタルに素早く組み上がる見せ方にしている。
    // ランクが高いほど、溜め・デコードの長さ・グリッチの強さ、確定したときの輪・放射光・虹色が豪華になる（4段階。TierOf）。
    // 演出中は画面のどこをクリックしても、ランクのデコードまで飛ばせる（ランクが現れる瞬間は見せる）。
    //
    // チャレンジ（PlayEndless。名前はエンドレスしか無かったころのまま）も同じ演出で見せる: 内訳はクリアした階層と最高記録、スコアの行は出さず、
    // ランクの代わりに到達した階層の数字をデコードする（深いほど豪華）。終わったら画面のどこをタップしてもタイトルへ戻れる。
    //
    // シーン遷移の覆いが明けてから始める。時間は実時間で進め、1フレームの上限を付けている
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private ClickArea _skipArea;

        [Header("内訳")]
        [SerializeField] private CanvasGroup _tokenRow;
        [SerializeField] private CommonText _tokenCountText;
        [SerializeField] private CommonText _tokenPointsText;
        [SerializeField] private CanvasGroup _backupRow;
        [SerializeField] private CommonText _backupCountText;
        [SerializeField] private CommonText _backupPointsText;
        [SerializeField] private CanvasGroup _scoreRow;
        [SerializeField] private TokenCounterView _scoreCounter;

        [Header("ランク")]
        [SerializeField] private CanvasGroup _rankCaption;
        [SerializeField] private RectTransform _rankRoot;
        [SerializeField] private CanvasGroup _rankGroup;
        [SerializeField] private CommonText _rankText;
        // デコード中に左右へずれる残像（ランクの文字と同じ大きさの、色違いの写し）
        [SerializeField] private CommonText _rankGhostCyan;
        [SerializeField] private CommonText _rankGhostMagenta;
        // デコード中にランクの上を上から下へ走る線
        [SerializeField] private Image _scanBar;
        [SerializeField] private Image _rankRays;
        [SerializeField] private Image[] _rankRings;
        // 画面全体の閃光（入力は受けない）
        [SerializeField] private Image _screenFlash;

        [SerializeField] private GameObject _backButton;
        // 新記録のときに最高記録の行に添える色
        [SerializeField] private Color _newRecordColor = new Color(1f, 0.9f, 0f, 1f);

        [Header("ランクの色（E, D, C, B, A, S, SS, SSS）")]
        [SerializeField] private Color[] _rankColors =
        {
            new Color(0.54f, 0.58f, 0.66f, 1f),
            new Color(0.61f, 0.71f, 0.79f, 1f),
            new Color(0f, 0.94f, 1f, 1f),
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
            new Color(1f, 1f, 1f, 1f),
        };

        [Header("間合い")]
        [SerializeField][Min(0f)] private float _startDelay = 0.3f;
        [SerializeField][Min(0f)] private float _rowInterval = 0.35f;
        [SerializeField][Min(0.01f)] private float _rowFadeDuration = 0.15f;

        // 段階（0:E〜D, 1:C〜B, 2:A〜S, 3:SS〜SSS）ごとの豪華さ
        private static readonly float[] SuspenseByTier = { 0.3f, 0.45f, 0.7f, 1f };
        private static readonly float[] DecodeByTier = { 0.3f, 0.45f, 0.65f, 0.9f };
        private static readonly float[] JitterByTier = { 4f, 6f, 10f, 14f };
        private static readonly float[] FlashByTier = { 0f, 0.08f, 0.15f, 0.25f };
        private static readonly int[] RingsByTier = { 0, 1, 2, 3 };

        private const string RankScrambleChars = "EDCBAS#%&/<>01";
        private const string NumberScrambleChars = "0123456789#%&/<>";
        private const float ScrambleStep = 0.035f;
        private const float OpenDuration = 0.12f;
        private const float GhostOffset = 8f;
        private const float RowSlide = 40f;
        private const float RingDuration = 0.7f;
        private const float RingInterval = 0.09f;
        private const float FlashDuration = 0.35f;
        private const float RaysSpeed = 16f;
        // A以上は、確定したあとも時々だけ短くグリッチする（タイトルロゴと同じ考え方）
        private const float IdleGlitchInterval = 3f;
        private const float IdleGlitchDuration = 0.12f;

        private enum Phase { Idle, WaitForScreen, Rows, Counting, Suspense, Decode, Settle, Done }

        private Phase _phase;
        private float _time;
        private float _phaseTime;
        private float _revealedTime = -1f;
        private float _scoreRowTime = -1f;
        // デコードして見せる大きな文字（ランク、エンドレスは到達した階層）とその色
        private string _rankString;
        private Color _rankColor;
        private string _scrambleChars = RankScrambleChars;
        private int _tier;
        private int _score;
        // スコアの行を出すか（エンドレスは出さない）
        private bool _hasScore;
        // 演出が終わったあと、画面のタップで OnTapAfterFinished を知らせるか（エンドレス）
        private bool _tapToContinue;
        private Vector2 _rankTextOrigin;
        private Action _onFinished;

        /// <summary>演出が終わったあとに画面がタップされた（PlayEndless のときだけ）</summary>
        public event Action OnTapAfterFinished;
        private readonly System.Random _random = new System.Random();
        private readonly StringBuilder _scramble = new StringBuilder();
        // 行の定位置（最初に動かす前の位置を覚えておく）
        private readonly Dictionary<RectTransform, Vector2> _rowBases = new Dictionary<RectTransform, Vector2>();

        private void Awake()
        {
            _skipArea.OnClicked += Skip;
            _rankTextOrigin = _rankText.rectTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            _skipArea.OnClicked -= Skip;
        }

        /// <summary>成績を並べて演出を始める（シーン遷移の覆いが明けてから動き出す）。終わったら onFinished</summary>
        public void Play(CampaignResult result, Action onFinished)
        {
            _rankString = result.Rank.ToString();
            _rankColor = RankColor(result.Rank);
            _scrambleChars = RankScrambleChars;
            _tier = TierOf(result.Rank);
            _score = result.Score;
            _hasScore = true;
            _tapToContinue = false;
            _onFinished = onFinished;

            _tokenCountText.text = result.RemainingTokens.ToString();
            _tokenPointsText.text = $"+{result.TokenPoints}";
            _backupCountText.text = result.BackupBoostCount.ToString();
            _backupPointsText.text = result.BackupPenalty > 0 ? $"-{result.BackupPenalty}" : "0";
            _backupPointsText.color = _tokenPointsText.color;

            Begin();
        }

        /// <summary>
        /// エンドレスの結果を見せる。内訳はクリアした階層と最高記録、大きく出すのは到達した階層。
        /// 終わったら onFinished、そのあと画面がタップされたら OnTapAfterFinished
        /// </summary>
        public void PlayEndless(int reachedLayer, int clearedLayers, int bestLayer, bool isNewRecord, string newRecordLabel, Action onFinished)
        {
            _rankString = reachedLayer.ToString("00");
            _tier = EndlessTierOf(reachedLayer);
            // 段階ごとに、ランクの D / B / S / SSS の色を借りる
            _rankColor = _rankColors[Mathf.Clamp(_tier * 2 + 1, 0, _rankColors.Length - 1)];
            _scrambleChars = NumberScrambleChars;
            _score = 0;
            _hasScore = false;
            _tapToContinue = true;
            _onFinished = onFinished;

            _tokenCountText.text = Mathf.Max(0, clearedLayers).ToString();
            _tokenPointsText.text = "";
            _backupCountText.text = $"LAYER {bestLayer:00}";
            _backupPointsText.text = isNewRecord ? newRecordLabel : "";
            _backupPointsText.color = _newRecordColor;

            Begin();
        }

        /// <summary>エンドレスの到達階層の豪華さ: 10階層ごとに1段（0〜3）</summary>
        public static int EndlessTierOf(int reachedLayer)
        {
            return Mathf.Clamp((reachedLayer - 1) / 10, 0, 3);
        }

        private void Begin()
        {
            _scoreRow.gameObject.SetActive(_hasScore);
            SetRow(_tokenRow, 0f);
            SetRow(_backupRow, 0f);
            SetRow(_scoreRow, 0f);
            _scoreCounter.SetValue(0);
            _rankCaption.alpha = 0f;
            _rankGroup.alpha = 0f;
            _rankRoot.localScale = Vector3.one;
            SetRankText(_rankString, _rankColor);
            SetGhosts(0f, 0f);
            _scanBar.gameObject.SetActive(false);
            _rankRays.color = WithAlpha(_rankColor, 0f);
            _screenFlash.color = WithAlpha(Color.white, 0f);
            foreach (var ring in _rankRings) ring.gameObject.SetActive(false);
            _backButton.SetActive(false);
            _skipArea.gameObject.SetActive(true);

            _phase = Phase.WaitForScreen;
            _time = 0f;
            _revealedTime = -1f;
            _scoreRowTime = -1f;
        }

        /// <summary>0:E〜D, 1:C〜B, 2:A〜S, 3:SS〜SSS</summary>
        public static int TierOf(CampaignRank rank)
        {
            if (rank >= CampaignRank.SS) return 3;
            if (rank >= CampaignRank.A) return 2;
            if (rank >= CampaignRank.C) return 1;
            return 0;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);

            if (_phase == Phase.WaitForScreen)
            {
                if (SceneTransitionEffect.IsTransitioning) return;
                _time += dt;
                if (_time < _startDelay) return;
                EnterPhase(Phase.Rows);
            }

            if (_phase == Phase.Idle) return;
            _phaseTime += dt;

            switch (_phase)
            {
                case Phase.Rows:
                    SetRow(_tokenRow, _phaseTime / _rowFadeDuration);
                    SetRow(_backupRow, (_phaseTime - _rowInterval) / _rowFadeDuration);
                    if (_phaseTime >= _rowInterval * 2f) StartCounting();
                    break;
                case Phase.Suspense:
                    _rankCaption.alpha = Mathf.Clamp01(_phaseTime / 0.2f);
                    if (_phaseTime >= SuspenseByTier[_tier]) StartDecode();
                    break;
                case Phase.Decode:
                    UpdateDecode(_phaseTime);
                    if (_phaseTime >= DecodeByTier[_tier]) Reveal();
                    break;
                case Phase.Settle:
                    if (_phaseTime >= 0.25f) Finish();
                    break;
            }

            // スコアの行は数え始めたときに出す（スコア0なら数え上げが一瞬で終わるので、段階とは別に進める）
            if (_scoreRowTime >= 0f)
            {
                _scoreRowTime += dt;
                SetRow(_scoreRow, _scoreRowTime / _rowFadeDuration);
            }

            UpdateRevealedEffects(dt);
        }

        private void EnterPhase(Phase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
        }

        private void StartCounting()
        {
            // スコアの無い結果（エンドレス）は、数え上げずに溜めへ
            if (!_hasScore)
            {
                EnterPhase(Phase.Suspense);
                return;
            }
            EnterPhase(Phase.Counting);
            if (_scoreRowTime < 0f) _scoreRowTime = 0f;
            _scoreCounter.CountTo(_score, () => EnterPhase(Phase.Suspense));
        }

        private void StartDecode()
        {
            EnterPhase(Phase.Decode);
            _rankCaption.alpha = 1f;
            _scanBar.gameObject.SetActive(true);
            _scanBar.color = WithAlpha(_rankColor, 0.8f);
            AudioUseCase.PlaySe(SeId.RankReveal);
        }

        // 横一線から開き、スクランブルした文字が左から確定していく。残像と走査線を横に走らせる
        private void UpdateDecode(float t)
        {
            float duration = DecodeByTier[_tier];
            float open = Mathf.Clamp01(t / OpenDuration);
            _rankRoot.localScale = new Vector3(1f, Mathf.Lerp(0.04f, 1f, 1f - (1f - open) * (1f - open)), 1f);

            int step = Mathf.FloorToInt(t / ScrambleStep);
            _rankGroup.alpha = step % 3 == 0 ? 0.45f : 1f;

            // 後半で左から1文字ずつ確定する
            int locked = Mathf.FloorToInt(_rankString.Length * Mathf.Clamp01((t - duration * 0.4f) / (duration * 0.6f)));
            SetRankText(Scrambled(locked, step), _rankColor);

            // 格子に吸着させた横ずれ。確定に近づくほど小さくなる
            float amp = JitterByTier[_tier] * (1f - Mathf.Clamp01(t / duration));
            float jitter = Mathf.Round(Noise(step) * amp / 2f) * 2f;
            _rankText.rectTransform.anchoredPosition = _rankTextOrigin + new Vector2(jitter, 0f);
            SetGhosts(GhostOffset + Mathf.Abs(jitter), step % 2 == 0 ? 0.7f : 0.35f);

            // 走査線は上から下へ
            var area = _rankRoot.rect;
            float y = Mathf.Lerp(area.yMax, area.yMin, Mathf.Clamp01(t / duration));
            _scanBar.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        // ランクが確定した瞬間。揺らさず、控えめな閃光と、段階に応じた輪・放射光を出す
        private void Reveal()
        {
            _rankRoot.localScale = Vector3.one;
            _rankGroup.alpha = 1f;
            _rankCaption.alpha = 1f;
            _rankText.rectTransform.anchoredPosition = _rankTextOrigin;
            SetRankText(_rankString, _rankColor);
            SetGhosts(0f, 0f);
            _scanBar.gameObject.SetActive(false);
            _revealedTime = 0f;

            AudioUseCase.PlaySe(_tier >= 2 ? SeId.Reward : SeId.Confirm);

            for (int i = 0; i < _rankRings.Length; i++)
            {
                _rankRings[i].gameObject.SetActive(i < RingsByTier[_tier]);
            }

            EnterPhase(Phase.Settle);
        }

        private void UpdateRevealedEffects(float dt)
        {
            if (_revealedTime < 0f) return;
            _revealedTime += dt;
            float t = _revealedTime;

            _screenFlash.color = WithAlpha(Color.white, FlashByTier[_tier] * (1f - Mathf.Clamp01(t / FlashDuration)));

            // 広がる輪（段階が上がるほど本数が増える）
            for (int i = 0; i < _rankRings.Length; i++)
            {
                var ring = _rankRings[i];
                if (!ring.gameObject.activeSelf) continue;
                float rt = Mathf.Clamp01((t - i * RingInterval) / RingDuration);
                float s = Mathf.Lerp(0.3f, 2.6f + i * 0.4f, 1f - (1f - rt) * (1f - rt));
                ring.rectTransform.localScale = new Vector3(s, s, 1f);
                ring.color = WithAlpha(_rankColor, rt <= 0f ? 0f : (1f - rt));
            }

            if (_tier < 2) return;

            // A以上は放射光が回り続け、時々だけ短くグリッチする
            _rankRays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * RaysSpeed);
            _rankRays.color = WithAlpha(_rankRays.color, Mathf.Clamp01(t / 0.3f) * (_tier >= 3 ? 0.45f : 0.3f));

            float cycle = Mathf.Repeat(t, IdleGlitchInterval);
            if (t > 1f && cycle < IdleGlitchDuration)
            {
                int step = Mathf.FloorToInt(t / ScrambleStep);
                float jitter = Mathf.Round(Noise(step) * JitterByTier[_tier] * 0.5f / 2f) * 2f;
                _rankText.rectTransform.anchoredPosition = _rankTextOrigin + new Vector2(jitter, 0f);
                SetGhosts(GhostOffset, 0.5f);
            }
            else
            {
                _rankText.rectTransform.anchoredPosition = _rankTextOrigin;
                SetGhosts(0f, 0f);
            }

            // SS以上は文字と放射光が虹色に移ろう
            var color = _tier >= 3 ? Color.HSVToRGB(Mathf.Repeat(t * 0.35f, 1f), 0.55f, 1f) : _rankColor;
            _rankText.color = color;
            _rankRays.color = WithAlpha(color, _rankRays.color.a);
        }

        // 演出を飛ばす。溜めまでは飛ばし、ランクのデコードは見せる。
        // エンドレスは、演出が終わったあとのタップを知らせる（タイトルへ戻る）
        private void Skip()
        {
            switch (_phase)
            {
                case Phase.Done:
                    if (_tapToContinue) OnTapAfterFinished?.Invoke();
                    break;
                case Phase.WaitForScreen:
                case Phase.Rows:
                case Phase.Counting:
                case Phase.Suspense:
                    AudioUseCase.PlaySe(SeId.Click);
                    SetRow(_tokenRow, 1f);
                    SetRow(_backupRow, 1f);
                    _scoreRowTime = _rowFadeDuration;
                    SetRow(_scoreRow, 1f);
                    // 数え上げの完了通知（Suspense へ進む）は付けずに、最後の値にする
                    _scoreCounter.CountTo(_score);
                    _scoreCounter.Finish();
                    StartDecode();
                    break;
            }
        }

        private void Finish()
        {
            _phase = Phase.Done;
            // エンドレスはタップでタイトルへ戻れるように、画面全体のクリックを受けたままにする
            _skipArea.gameObject.SetActive(_tapToContinue);
            _backButton.SetActive(true);
            _onFinished?.Invoke();
        }

        // locked 文字目までは本物、残りは step ごとに入れ替わるでたらめな文字
        private string Scrambled(int locked, int step)
        {
            _scramble.Clear();
            for (int i = 0; i < _rankString.Length; i++)
            {
                if (i < locked)
                {
                    _scramble.Append(_rankString[i]);
                    continue;
                }
                int index = (step * 7 + i * 13 + _random.Next(_scrambleChars.Length)) % _scrambleChars.Length;
                _scramble.Append(_scrambleChars[index]);
            }
            return _scramble.ToString();
        }

        private void SetRankText(string text, Color color)
        {
            _rankText.text = text;
            _rankText.color = color;
            _rankGhostCyan.text = text;
            _rankGhostMagenta.text = text;
        }

        // 残像を左右に offset だけずらして、alpha で出す（0 なら隠す）
        private void SetGhosts(float offset, float alpha)
        {
            _rankGhostCyan.rectTransform.anchoredPosition = _rankTextOrigin + new Vector2(-offset, 0f);
            _rankGhostMagenta.rectTransform.anchoredPosition = _rankTextOrigin + new Vector2(offset, 0f);
            _rankGhostCyan.alpha = alpha;
            _rankGhostMagenta.alpha = alpha;
        }

        // 行を左からすべり込ませる（0:見えない 〜 1:Inspector で置いた定位置）
        private void SetRow(CanvasGroup row, float progress)
        {
            float p = Mathf.Clamp01(progress);
            float before = row.alpha;
            row.alpha = p;

            var rt = (RectTransform)row.transform;
            if (!_rowBases.TryGetValue(rt, out var basePosition))
            {
                basePosition = rt.anchoredPosition;
                _rowBases[rt] = basePosition;
            }
            rt.anchoredPosition = basePosition + Vector2.left * ((1f - p) * (1f - p) * RowSlide);

            if (before <= 0f && p > 0f) AudioUseCase.PlaySe(SeId.TokenTick);
        }

        // step ごとに決まる -1〜1 の値（同じ step の間は同じずれを保つ）
        private static float Noise(int step)
        {
            return Mathf.Sin(step * 12.9898f) * 43758.5453f % 1f;
        }

        private Color RankColor(CampaignRank rank)
        {
            return _rankColors[Mathf.Clamp((int)rank, 0, _rankColors.Length - 1)];
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
