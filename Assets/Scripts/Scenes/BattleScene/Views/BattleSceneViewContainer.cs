using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    public sealed class BattleSceneViewContainer : MonoBehaviour
    {
        [SerializeField] private Transform _handParent;
        [SerializeField] private BattleCardView _cardViewPrefab;

        // 実行リスト（積んだカードを並べる場所）
        [SerializeField] private Transform _commandListParent;
        [SerializeField] private BattleCommandCardView _commandCardViewPrefab;
        [SerializeField] private CommonButton _executeButton;
        [SerializeField] private CommonText _executeButtonLabel;

        [SerializeField] private CardPileCountView _deckCountView;
        [SerializeField] private CardPileCountView _discardCountView;
        // パージ（除外）されたコマンドの数
        [SerializeField] private CardPileCountView _purgeCountView;
        // パージするバッファを選んでいる間、そのコマンドの上に出すパネル
        [SerializeField] private BattlePurgePickerView _purgePicker;
        // 手札の列の見出し（画面上は「バッファ」）
        [SerializeField] private CommonText _bufferLabel;
        // バッファが空のとき、手札の場所に出す説明
        [SerializeField] private BattleBufferEmptyView _bufferEmptyView;

        [SerializeField] private BattleUnitStatusView _playerStatusView;

        // 手前から順に並べる。この並びが敵の行動順とEnemyDataの割り当て順になる
        [SerializeField] private BattleUnitStatusView[] _enemyStatusViews;

        // 対象選択中に、選んだカードの上に出す対象の候補一覧
        [SerializeField] private BattleTargetPickerView _targetPickerView;

        // 何も無い所のクリックを受ける、画面いっぱいの透明な板（対象選択の解除に使う）
        [SerializeField] private BattleCancelAreaView _cancelArea;

        // カーソルを合わせたカードの詳細を大きく出すパネル
        [SerializeField] private CardDetailView _cardDetailView;

        // 行動中に上部中央に出すログ
        [SerializeField] private BattleLogFeedView _logFeedView;
        // メニュー（オプションとあきらめる）を開くボタン（ログボタンの上）。
        // 元はオプションを直接開くボタンだったので、シーンの参照を保つために元のフィールド名を引き継いでいる
        [FormerlySerializedAs("_optionsButton")]
        [SerializeField] private CommonButton _menuButton;
        [FormerlySerializedAs("_optionsButtonLabel")]
        [SerializeField] private CommonText _menuButtonLabel;
        // ウインドウを生成する親（WindowUseCase）。ウインドウはシーンに置かない
        [SerializeField] private RectTransform _popupCanvas;

        // 戦闘全体のログを開くボタン（左下）
        [SerializeField] private CommonButton _logButton;
        [SerializeField] private CommonText _logButtonLabel;

        public BattleCardView CardViewPrefab => _cardViewPrefab;
        public Transform HandParent => _handParent;
        public Transform CommandListParent => _commandListParent;
        public BattleCommandCardView CommandCardViewPrefab => _commandCardViewPrefab;
        public CommonButton ExecuteButton => _executeButton;
        public CommonText ExecuteButtonLabel => _executeButtonLabel;
        public CardPileCountView DeckCountView => _deckCountView;
        public CardPileCountView DiscardCountView => _discardCountView;
        public CardPileCountView PurgeCountView => _purgeCountView;
        public BattlePurgePickerView PurgePicker => _purgePicker;
        public CommonText BufferLabel => _bufferLabel;
        public BattleBufferEmptyView BufferEmptyView => _bufferEmptyView;
        public BattleUnitStatusView PlayerStatusView => _playerStatusView;
        public IReadOnlyList<BattleUnitStatusView> EnemyStatusViews => _enemyStatusViews;
        public BattleTargetPickerView TargetPickerView => _targetPickerView;
        public BattleCancelAreaView CancelArea => _cancelArea;
        public CardDetailView CardDetailView => _cardDetailView;
        public BattleLogFeedView LogFeedView => _logFeedView;
        public CommonButton MenuButton => _menuButton;
        public CommonText MenuButtonLabel => _menuButtonLabel;
        public RectTransform PopupCanvas => _popupCanvas;
        public CommonButton LogButton => _logButton;
        public CommonText LogButtonLabel => _logButtonLabel;
    }
}
