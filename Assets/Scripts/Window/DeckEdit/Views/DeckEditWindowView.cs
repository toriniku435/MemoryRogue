using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // デッキ編集（コマンドビルド）のウインドウ（Assets/Prefabs/DeckEditWindow.prefab）。
    // 所持リスト・デッキリスト・選んだコマンドの拡大表示の3つと、おまかせ・キャンセル・保存のボタン。
    // デッキリストの上にデッキの番号のタブ（_deckTabs）。デッキの見出しの横には、選んでいるデッキがアクティブなら
    // アクティブの札、そうでなければ同じ場所にアクティブにするボタンを出す。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // 背面のクリックでは閉じない（編集中の誤操作で捨てないように）。閉じるのはキャンセルか保存だけで、閉じるのは Presenter
    //
    // すべて表示のときは、カードをもう一方のリストへドラッグして1個ずつ移せる。ここでは指に付いて動く写し（ゴースト）と、
    // どちらのパネルの上で離したかの判定だけを受け持ち、移してよいかと実際に移すのは Presenter（OnCardDroppedToDeck / ToOwned）
    public sealed class DeckEditWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private CommonText _titleText;

        [SerializeField] private CommonScrollList _ownedList;
        [SerializeField] private CommonScrollList _deckList;
        [SerializeField] private DeckEditSubView _subView;

        [SerializeField] private CommonText _ownedHeaderLabel;
        [SerializeField] private CommonText _deckHeaderLabel;
        [SerializeField] private DeckEditCountView _deckCountView;

        [SerializeField] private CommonButton _cancelButton;
        [SerializeField] private CommonText _cancelLabel;
        [SerializeField] private CommonButton _saveButton;
        [SerializeField] private CommonText _saveLabel;
        [SerializeField] private CommonButton _autoButton;
        [SerializeField] private CommonText _autoLabel;

        // デッキの番号のタブ（01〜）と、デッキの見出しの横に出すアクティブの札・アクティブにするボタン（同じ場所に、どちらか一方）
        [SerializeField] private DeckSlotTabView[] _deckTabs;
        [SerializeField] private GameObject _activeLabel;
        [SerializeField] private CommonText _activeLabelText;
        [SerializeField] private CommonButton _activateButton;
        [SerializeField] private CommonText _activateLabel;
        // デッキを空にするボタン（ゴミ箱のアイコン。デッキの見出しの右）
        [SerializeField] private CommonButton _clearButton;

        // 表示のしかたの切り替え（すべて / デッキ / 所持）。並びは DeckEditLayout の順
        [SerializeField] private CommonButton[] _layoutButtons;
        [SerializeField] private CommonText[] _layoutLabels;
        [SerializeField] private RectTransform _ownedPanel;
        [SerializeField] private RectTransform _deckPanel;
        [SerializeField] private RectTransform _deckTabsRoot;
        // 所持リストだけを出すときに、所持パネルの見出しの右に出す「デッキ 02　12/20」
        [SerializeField] private GameObject _deckSummary;
        [SerializeField] private CommonText _deckSummaryName;
        [SerializeField] private DeckEditCountView _deckSummaryCount;
        // 次に戦う敵の詳細を開くボタン（右上。マップの「敵詳細」と同じウインドウを開く。押されたら何をするかは EnemyDetailPresenter が決める）
        [SerializeField] private CommonButton _enemyDetailButton;
        [SerializeField] private CommonText _enemyDetailLabel;
        // ドラッグ中に、落とせる側のパネルに重ねる光る縁（未設定でもよい）。ポインタが乗ると濃くなる
        [SerializeField] private Graphic _ownedDropHighlight;
        [SerializeField] private Graphic _deckDropHighlight;
        [SerializeField] private float _dropHighlightIdleAlpha = 0.35f;
        [SerializeField] private float _dropHighlightHoverAlpha = 1f;
        [SerializeField] private float _dragGhostAlpha = 0.85f;
        [SerializeField] private Color _activeLayoutColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _inactiveLayoutColor = new Color(0.55f, 0.62f, 0.72f, 1f);

        // すべて表示のときの置き場所（最初に覚える）。片方だけのときは、2つのパネルを合わせた範囲に広げる
        private Vector2 _ownedPosition, _ownedSize, _deckPosition, _deckSize, _tabsPosition;
        private bool _layoutCaptured;
        private DeckEditLayout _layout = DeckEditLayout.Both;

        // ドラッグ中のカードと、どちらのリストから引いたか。ゴーストは最初のドラッグで行の写しから作って使い回す
        private CardData _dragCard;
        private bool _dragFromDeck;
        private DeckEditCardView _dragGhost;
        private CanvasGroup _dragGhostGroup;

        /// <summary>キャンセルが押された</summary>
        public event Action OnCancelClicked;

        /// <summary>保存が押された</summary>
        public event Action OnSaveClicked;

        /// <summary>おまかせが押された</summary>
        public event Action OnAutoClicked;

        /// <summary>アクティブにするが押された</summary>
        public event Action OnActivateClicked;

        /// <summary>デッキを空にするが押された</summary>
        public event Action OnClearClicked;

        /// <summary>デッキの番号のタブが押された（デッキの番号）</summary>
        public event Action<int> OnDeckTabClicked;

        /// <summary>表示のしかたの切り替えが押された</summary>
        public event Action<DeckEditLayout> OnLayoutClicked;

        /// <summary>所持リストのカードがデッキのパネルへドラッグされた</summary>
        public event Action<CardData> OnCardDroppedToDeck;

        /// <summary>デッキリストのカードが所持のパネルへドラッグされた</summary>
        public event Action<CardData> OnCardDroppedToOwned;

        public CommonScrollList OwnedList => _ownedList;
        public CommonButton EnemyDetailButton => _enemyDetailButton;
        public CommonText EnemyDetailLabel => _enemyDetailLabel;
        public CommonScrollList DeckList => _deckList;
        public DeckEditSubView SubView => _subView;
        public DeckEditCountView DeckCountView => _deckCountView;

        /// <summary>並べてあるデッキのタブの数</summary>
        public int DeckTabCount => _deckTabs.Length;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _cancelButton.onClick.AddListener(HandleCancel);
            _saveButton.onClick.AddListener(HandleSave);
            _autoButton.onClick.AddListener(HandleAuto);
            _activateButton.onClick.AddListener(HandleActivate);
            _clearButton.onClick.AddListener(HandleClear);
            foreach (var tab in _deckTabs) tab.OnClicked += HandleDeckTabClicked;
            for (int i = 0; i < _layoutButtons.Length; i++)
            {
                var layout = (DeckEditLayout)i;
                _layoutButtons[i].onClick.AddListener(() => HandleLayoutClicked(layout));
            }
            CaptureLayout();
        }

        private void OnDestroy()
        {
            _cancelButton.onClick.RemoveListener(HandleCancel);
            _saveButton.onClick.RemoveListener(HandleSave);
            _autoButton.onClick.RemoveListener(HandleAuto);
            _activateButton.onClick.RemoveListener(HandleActivate);
            _clearButton.onClick.RemoveListener(HandleClear);
            foreach (var tab in _deckTabs) tab.OnClicked -= HandleDeckTabClicked;
            foreach (var button in _layoutButtons) button.onClick.RemoveAllListeners();
        }

        /// <param name="layoutLabels">表示のしかたのボタンの文字（DeckEditLayout の順）</param>
        public void SetLayoutTexts(string[] layoutLabels)
        {
            for (int i = 0; i < _layoutLabels.Length && i < layoutLabels.Length; i++) _layoutLabels[i].text = layoutLabels[i];
        }

        /// <summary>
        /// 表示のしかたを変える。片方のリストだけのときは、そのパネルを2つのパネルを合わせた範囲に広げ、
        /// リストの列数を幅に合わせる。デッキの番号のタブは見えているパネルの上に寄せる
        /// </summary>
        public void SetLayout(DeckEditLayout layout)
        {
            CaptureLayout();
            CancelCardDrag();
            _layout = layout;
            float left = _ownedPosition.x - _ownedSize.x / 2f;
            float right = _deckPosition.x + _deckSize.x / 2f;
            var widePosition = new Vector2((left + right) / 2f, _deckPosition.y);
            var wideSize = new Vector2(right - left, _deckSize.y);
            bool ownedWide = layout == DeckEditLayout.OwnedOnly;
            bool deckWide = layout == DeckEditLayout.DeckOnly;

            _ownedPanel.gameObject.SetActive(!deckWide);
            _deckPanel.gameObject.SetActive(!ownedWide);
            _deckSummary.SetActive(ownedWide);

            Place(_ownedPanel, ownedWide ? widePosition : _ownedPosition, ownedWide ? wideSize : _ownedSize);
            Place(_deckPanel, deckWide ? widePosition : _deckPosition, deckWide ? wideSize : _deckSize);
            _deckTabsRoot.anchoredPosition = new Vector2(layout == DeckEditLayout.Both ? _tabsPosition.x : widePosition.x, _tabsPosition.y);

            if (!deckWide) _ownedList.FitColumnsToWidth();
            if (!ownedWide) _deckList.FitColumnsToWidth();

            for (int i = 0; i < _layoutLabels.Length; i++) _layoutLabels[i].color = i == (int)layout ? _activeLayoutColor : _inactiveLayoutColor;
        }

        /// <summary>所持リストだけを出すときの「デッキ 02　12/20」</summary>
        public void ShowDeckSummary(string deckName, int count, int max)
        {
            _deckSummaryName.text = deckName;
            _deckSummaryCount.SetCount(count, max);
        }

        private void CaptureLayout()
        {
            if (_layoutCaptured) return;
            _layoutCaptured = true;
            _ownedPosition = _ownedPanel.anchoredPosition;
            _ownedSize = _ownedPanel.sizeDelta;
            _deckPosition = _deckPanel.anchoredPosition;
            _deckSize = _deckPanel.sizeDelta;
            _tabsPosition = _deckTabsRoot.anchoredPosition;
        }

        private static void Place(RectTransform panel, Vector2 position, Vector2 size)
        {
            panel.anchoredPosition = position;
            panel.sizeDelta = size;
        }

        public void SetTexts(string title, string ownedHeader, string cancel, string save, string auto, string activeLabel, string activate)
        {
            _titleText.text = title;
            _ownedHeaderLabel.text = ownedHeader;
            _cancelLabel.text = cancel;
            _saveLabel.text = save;
            _autoLabel.text = auto;
            _activeLabelText.text = activeLabel;
            _activateLabel.text = activate;
        }

        /// <summary>
        /// 選んでいるデッキの見出しと、デッキのタブを塗る。見出しの横は、選んでいるデッキがアクティブなら札、
        /// そうでなければアクティブにするボタン（個数は問わない。確かめるのは保存のとき）
        /// </summary>
        public void ShowDecks(string deckHeader, int selectedIndex, int activeIndex)
        {
            _deckHeaderLabel.text = deckHeader;
            for (int i = 0; i < _deckTabs.Length; i++) _deckTabs[i].Show(i, i == selectedIndex, i == activeIndex);
            _activeLabel.SetActive(selectedIndex == activeIndex);
            _activateButton.gameObject.SetActive(selectedIndex != activeIndex);
        }

        public void Open()
        {
            _dialog.PlayOpen();
        }

        public void Close()
        {
            CancelCardDrag();
            _dialog.PlayClose(null);
        }

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void HideImmediate()
        {
            CancelCardDrag();
            _dialog.HideImmediate();
        }

        /// <summary>
        /// リストのカードのドラッグを始める。すべて表示のときだけ（片方のリストしか見えないときは移す先が無い）。
        /// 始めなかったら false（カードはスクロールとして扱う）
        /// </summary>
        public bool TryBeginCardDrag(DeckEditCardView source, PointerEventData eventData)
        {
            if (!IsOpen || _layout != DeckEditLayout.Both || source.CardData == null) return false;

            bool fromDeck = source.transform.IsChildOf(_deckList.transform);
            if (!fromDeck && !source.transform.IsChildOf(_ownedList.transform)) return false;

            _dragCard = source.CardData;
            _dragFromDeck = fromDeck;

            ShowGhost(source);
            DragCard(eventData);
            return true;
        }

        /// <summary>ドラッグ中：ゴーストをポインタに付けて動かし、落とせる側の縁を光らせる</summary>
        public void DragCard(PointerEventData eventData)
        {
            if (_dragCard == null) return;

            var parent = (RectTransform)_dragGhost.transform.parent;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var world))
            {
                _dragGhost.transform.position = world;
            }

            var target = _dragFromDeck ? _ownedPanel : _deckPanel;
            var highlight = _dragFromDeck ? _ownedDropHighlight : _deckDropHighlight;
            SetHighlightAlpha(highlight, IsOver(target, eventData) ? _dropHighlightHoverAlpha : _dropHighlightIdleAlpha);
        }

        /// <summary>ドラッグを終える。もう一方のパネルの上で離したら、そちらへ移すよう伝える</summary>
        public void EndCardDrag(PointerEventData eventData)
        {
            if (_dragCard == null) return;

            var card = _dragCard;
            bool fromDeck = _dragFromDeck;
            bool dropped = IsOpen && IsOver(fromDeck ? _ownedPanel : _deckPanel, eventData);
            CancelCardDrag();

            if (!dropped) return;
            if (fromDeck) OnCardDroppedToOwned?.Invoke(card);
            else OnCardDroppedToDeck?.Invoke(card);
        }

        /// <summary>何も移さずにドラッグをやめる（ゴーストと縁を消す）</summary>
        public void CancelCardDrag()
        {
            _dragCard = null;
            if (_dragGhost != null) _dragGhost.gameObject.SetActive(false);
            SetHighlightVisible(_ownedDropHighlight, false);
            SetHighlightVisible(_deckDropHighlight, false);
        }

        // ゴーストはドラッグした行の写し。入力は受けない（下のパネルの判定の邪魔をしないように）
        private void ShowGhost(DeckEditCardView source)
        {
            if (_dragGhost == null)
            {
                _dragGhost = Instantiate(source, _dialog.transform);
                _dragGhost.name = "DragGhost";
                _dragGhostGroup = _dragGhost.GetComponent<CanvasGroup>();
                if (_dragGhostGroup == null) _dragGhostGroup = _dragGhost.gameObject.AddComponent<CanvasGroup>();
                _dragGhostGroup.blocksRaycasts = false;
                _dragGhostGroup.interactable = false;
            }

            var rect = (RectTransform)_dragGhost.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = ((RectTransform)source.transform).rect.size;
            rect.localScale = Vector3.one;
            _dragGhostGroup.alpha = _dragGhostAlpha;

            // 動くのは1個ずつなので、枚数は x1 と見せる
            _dragGhost.Bind(source.CardData, 1, false, null);
            _dragGhost.gameObject.SetActive(true);
            _dragGhost.transform.SetAsLastSibling();

            SetHighlightVisible(_dragFromDeck ? _ownedDropHighlight : _deckDropHighlight, true);
        }

        private static bool IsOver(RectTransform panel, PointerEventData eventData)
        {
            return panel.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(panel, eventData.position, eventData.pressEventCamera);
        }

        private void SetHighlightVisible(Graphic highlight, bool visible)
        {
            if (highlight == null) return;
            highlight.gameObject.SetActive(visible);
            if (visible) SetHighlightAlpha(highlight, _dropHighlightIdleAlpha);
        }

        private static void SetHighlightAlpha(Graphic highlight, float alpha)
        {
            if (highlight == null) return;
            var color = highlight.color;
            color.a = alpha;
            highlight.color = color;
        }

        // 閉じている途中の押下は受け付けない（保存してから閉じ終わるまでに、もう一度押されても何もしない）
        private void HandleCancel()
        {
            if (IsOpen) OnCancelClicked?.Invoke();
        }

        private void HandleSave()
        {
            if (IsOpen) OnSaveClicked?.Invoke();
        }

        private void HandleAuto()
        {
            if (IsOpen) OnAutoClicked?.Invoke();
        }

        private void HandleActivate()
        {
            if (IsOpen) OnActivateClicked?.Invoke();
        }

        private void HandleClear()
        {
            if (IsOpen) OnClearClicked?.Invoke();
        }

        /// <summary>デッキを空にするボタンを押せるか（空のデッキでは押せない）</summary>
        public void SetClearInteractable(bool interactable)
        {
            _clearButton.interactable = interactable;
        }

        private void HandleDeckTabClicked(int index)
        {
            if (IsOpen) OnDeckTabClicked?.Invoke(index);
        }

        private void HandleLayoutClicked(DeckEditLayout layout)
        {
            if (IsOpen) OnLayoutClicked?.Invoke(layout);
        }
    }
}
