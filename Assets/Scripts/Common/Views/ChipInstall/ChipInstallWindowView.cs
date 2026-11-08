using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // チップを開けた結果のウインドウ（チップ購入の結果、キャンペーン開始時のファーストビルド）。出たカードを並べ、カーソルを合わせたカードを右側に大きく出す。
    // 何を開けたか・どこで使うかは知らない。タイトルとボタンの文字は呼び出し側が入れる。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition に任せる（ウインドウの出入りはどれもそろえる）。
    // ここで足しているのは中身の動き（開封演出）：
    //   1. インストール: パック（チップ）をソケットに差し込む。光は中身の一番高いレアリティへ昇格していく（ChipInstallView）
    //   2. 裏向きのカードが届き、レアリティの低い順に1枚ずつめくる。レア以上は光を溜めてからめくる
    //   3. すべてめくれたら閉じるボタンが出る
    // 演出中はどこをクリックしても最後まで飛ばせる（待たせない）。閉じるとカードが逆順に潰れて消える。
    //
    // カードの並べ方はスクリプトで決める（登場の動きと喧嘩しないよう Layout Group は使わない）。
    // 枚数が多いときは、すべてが枠に収まるまで縮める
    public sealed class ChipInstallWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _transition;
        [SerializeField] private RectTransform _cardArea;
        [SerializeField] private ChipInstallCardView _cardPrefab;
        [SerializeField] private CardDetailView _detailView;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;
        [SerializeField] private ChipInstallView _install;
        // 演出中だけ有効な、クリックで飛ばすための領域（カードより手前に置く）
        [SerializeField] private ClickArea _skipArea;
        // めくったときに散る粒（シミュレーションはワールド座標。カードの位置へ動かしてから出す）
        [SerializeField] private ParticleSystem _revealBurst;

        [Header("レアリティごとの光の色（ノーマル・レア・エピック）")]
        [SerializeField] private Color[] _tierColors =
        {
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
        };

        [Header("めくる間合い")]
        // 裏向きのカードが届き終わってから、最初の1枚をめくるまで
        [SerializeField][Min(0f)] private float _firstRevealDelay = 0.25f;
        [SerializeField][Min(0f)] private float _revealInterval = 0.14f;
        [SerializeField][Min(0f)] private float _rareCharge = 0.35f;
        [SerializeField][Min(0f)] private float _epicCharge = 0.8f;

        [SerializeField][Min(0f)] private float _spacing = 18f;
        [SerializeField][Min(0.1f)] private float _maxCardScale = 0.8f;
        // これより小さく並べるときは、カードの説明文を隠す（ドット絵フォントが読めなくなる大きさ）
        [SerializeField][Min(0f)] private float _minDescriptionScale = 0.67f;
        // 開き始めてから最初のカードが届くまで（枠が開き切るのを待つ）
        [SerializeField][Min(0f)] private float _firstCardDelay = 0.15f;
        // カードが1枚ずつ届く間隔
        [SerializeField][Min(0f)] private float _cardInterval = 0.07f;
        // 閉じるときにカードが消えていく間隔（届いたときの逆順）
        [SerializeField][Min(0f)] private float _cardCloseInterval = 0.015f;

        private readonly List<ChipInstallCardView> _cardViews = new List<ChipInstallCardView>();
        private readonly List<CardData> _cards = new List<CardData>();

        private bool _isSequencing;
        private bool _cardsShown;
        private float _sequenceTime;
        private float _sequenceEnd = float.MaxValue;

        private void Awake()
        {
            if (_skipArea != null) _skipArea.OnClicked += Skip;
        }

        private void OnDestroy()
        {
            if (_skipArea != null) _skipArea.OnClicked -= Skip;
        }

        public CommonText TitleText => _titleText;
        public CommonButton CloseButton => _closeButton;
        public CommonText CloseLabel => _closeLabel;

        /// <param name="onHovered">カードにカーソルが乗った</param>
        /// <param name="onUnhovered">カードからカーソルが外れた</param>
        public void Show(IReadOnlyList<CardData> cards, Action<CardData> onHovered, Action<CardData> onUnhovered)
        {
            _transition.PlayOpen();
            _detailView.gameObject.SetActive(false);

            // レアリティの低い順に並べ、良いカードほど後でめくる（同じレアリティは出た順のまま）
            _cards.Clear();
            _cards.AddRange(cards.Select((card, index) => (card, index)).OrderBy(x => (int)x.card.Rarity).ThenBy(x => x.index).Select(x => x.card));
            cards = _cards;

            while (_cardViews.Count < cards.Count)
            {
                _cardViews.Add(Instantiate(_cardPrefab, _cardArea));
            }

            Vector2 cardSize = ((RectTransform)_cardPrefab.transform).rect.size;
            float scale = FitScale(cards.Count, cardSize, out int columns);

            for (int i = 0; i < _cardViews.Count; i++)
            {
                var view = _cardViews[i];
                bool active = i < cards.Count;
                view.gameObject.SetActive(active);
                if (!active) continue;

                var card = cards[i];
                view.Initialize(card, () => onHovered.Invoke(card), () => onUnhovered.Invoke(card));
                view.RectTransform.localScale = new Vector3(scale, scale, 1f);
                view.SetDescriptionVisible(scale >= _minDescriptionScale);
                view.RectTransform.anchoredPosition = PositionOf(i, cards.Count, columns, cardSize * scale);
                // カードはインストールが終わるまで出さない
                view.gameObject.SetActive(false);
            }

            _closeButton.gameObject.SetActive(false);
            if (_skipArea != null) _skipArea.gameObject.SetActive(true);
            _isSequencing = true;
            _cardsShown = false;
            _sequenceEnd = float.MaxValue;

            int maxTier = cards.Count == 0 ? 0 : cards.Max(card => (int)card.Rarity);
            _install.Play(maxTier, StartReveal);
        }

        // インストールが終わったら、裏向きのカードを届け、低いレアリティから順にめくる
        private void StartReveal()
        {
            _install.Hide();
            ShowCards(faceDown: true);

            int count = _cards.Count;
            float time = _firstCardDelay + count * _cardInterval + _firstRevealDelay;
            for (int i = 0; i < count; i++)
            {
                var view = _cardViews[i];
                int tier = (int)_cards[i].Rarity;
                float charge = tier >= 2 ? _epicCharge : (tier == 1 ? _rareCharge : 0f);
                var color = TierColor(tier);
                view.PlayReveal(time, charge, color, tier > 0, () => HandleCardFlipped(view, tier));
                time += charge + _revealInterval;
            }

            _sequenceTime = 0f;
            _sequenceEnd = time + 0.3f;
        }

        private void ShowCards(bool faceDown)
        {
            if (_cardsShown) return;
            _cardsShown = true;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cardViews[i].gameObject.SetActive(true);
                _cardViews[i].PlayAppear(faceDown ? _firstCardDelay + i * _cardInterval : 0f, faceDown);
            }
        }

        private void HandleCardFlipped(ChipInstallCardView view, int tier)
        {
            // めくる音（シャカッと擦れる音だけ）
            AudioUseCase.PlaySe(SeId.CardFlip);
            if (_revealBurst == null) return;
            _revealBurst.transform.position = view.transform.position;
            var emit = new ParticleSystem.EmitParams { startColor = TierColor(tier), applyShapeToPosition = true };
            _revealBurst.Emit(emit, tier >= 2 ? 70 : (tier == 1 ? 30 : 8));
        }

        // 演出を最後まで飛ばす（どこをクリックしても）
        private void Skip()
        {
            if (!_isSequencing) return;
            AudioUseCase.PlaySe(SeId.Click);
            _install.Skip();
            ShowCards(faceDown: false);
            for (int i = 0; i < _cards.Count; i++)
            {
                int tier = (int)_cards[i].Rarity;
                _cardViews[i].RevealImmediately(TierColor(tier), tier > 0);
            }
            FinishSequence();
        }

        private void FinishSequence()
        {
            _isSequencing = false;
            _sequenceEnd = float.MaxValue;
            if (_skipArea != null) _skipArea.gameObject.SetActive(false);
            _closeButton.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isSequencing || _sequenceEnd == float.MaxValue) return;
            _sequenceTime += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            if (_sequenceTime >= _sequenceEnd) FinishSequence();
        }

        private Color TierColor(int tier)
        {
            return _tierColors[Mathf.Clamp(tier, 0, _tierColors.Length - 1)];
        }

        /// <summary>開いていて、閉じる途中でもない</summary>
        public bool IsOpen => gameObject.activeSelf && !_transition.IsClosing;

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void Hide()
        {
            _isSequencing = false;
            _install.Hide();
            _transition.HideImmediate();
        }

        /// <summary>閉じる動きを流して閉じる</summary>
        public void Close()
        {
            if (!gameObject.activeSelf || _transition.IsClosing) return;

            int order = 0;
            for (int i = _cardViews.Count - 1; i >= 0; i--)
            {
                if (!_cardViews[i].gameObject.activeSelf) continue;
                _cardViews[i].PlayDisappear(order * _cardCloseInterval);
                order++;
            }
            _detailView.Hide();
            _transition.PlayClose(null);
        }

        public void ShowDetail(CardData cardData)
        {
            _detailView.Show(cardData);
        }

        public void HideDetail()
        {
            _detailView.Hide();
        }

        /// <summary>言語が変わったときに、並んでいるカードの文字を引き直す</summary>
        public void RefreshTexts()
        {
            foreach (var view in _cardViews)
            {
                if (view.gameObject.activeSelf && view.CardData != null) view.SetCardData(view.CardData);
            }
            if (_detailView.gameObject.activeSelf && _detailView.CardData != null) _detailView.SetCardData(_detailView.CardData);
        }

        // 枠に全部が収まる並べ方のうち、カードが一番大きくなる列数を選ぶ
        private float FitScale(int count, Vector2 cardSize, out int columns)
        {
            Vector2 area = _cardArea.rect.size;
            columns = Mathf.Max(1, count);
            float best = 0f;
            for (int c = 1; c <= Mathf.Max(1, count); c++)
            {
                int rows = Mathf.CeilToInt(count / (float)c);
                float sx = (area.x - (c - 1) * _spacing) / (c * cardSize.x);
                float sy = (area.y - (rows - 1) * _spacing) / (rows * cardSize.y);
                float s = Mathf.Min(sx, sy, _maxCardScale);
                if (s > best)
                {
                    best = s;
                    columns = c;
                }
            }
            return best;
        }

        // 枠の中央を基準に、行ごとに中央揃えで並べる
        private Vector2 PositionOf(int index, int count, int columns, Vector2 size)
        {
            int rows = Mathf.CeilToInt(count / (float)columns);
            int row = index / columns;
            int column = index % columns;
            int inRow = Mathf.Min(columns, count - row * columns);

            float x = (column - (inRow - 1) / 2f) * (size.x + _spacing);
            float y = ((rows - 1) / 2f - row) * (size.y + _spacing);
            return new Vector2(x, y);
        }
    }
}
