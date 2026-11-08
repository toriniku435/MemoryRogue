using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.MapScene.Views
{
    public sealed class MapSceneViewContainer : MonoBehaviour
    {
        // マスはステージ数に合わせて実行時に生成するので、置き場所とプレハブだけ持つ
        [SerializeField] private Transform _cellParent;
        [SerializeField] private MapCellView _cellViewPrefab;

        // 部屋同士をつなぐ配線（部屋より奥に描くので、置き場所を分けてある）
        [SerializeField] private Transform _linkParent;
        [SerializeField] private MapLinkView _linkViewPrefab;

        // マップ上のプレイヤー（部屋の上に立ち、進むときはパケットになって移る）
        [SerializeField] private MapPlayerView _playerView;

        // 配線・部屋・プレイヤーの親をそろって横へずらす（10部屋ごとのページ送り）
        [SerializeField] private MapRouteScrollView _routeScroll;

        // 今の深さ（LAYER 03 / 10）
        [SerializeField] private CommonText _depthLabel;

        // 次のレイヤーで戦う敵の詳細を開くボタン（左下）
        [SerializeField] private CommonButton _enemyDetailButton;
        [SerializeField] private CommonText _enemyDetailLabel;

        // チュートリアルを見直すボタン（左上）
        [SerializeField] private CommonButton _tutorialButton;
        [SerializeField] private CommonText _tutorialLabel;

        // メニュー（中断・オプション）を開くボタン（右上）
        [SerializeField] private CommonButton _menuButton;
        [SerializeField] private CommonText _menuLabel;

        [SerializeField] private CommonButton _stageStartButton;
        [SerializeField] private CommonText _stageStartLabel;
        [SerializeField] private CommonButton _deckEditButton;
        [SerializeField] private CommonText _deckEditLabel;
        [SerializeField] private CommonButton _cardShopButton;
        [SerializeField] private CommonText _cardShopLabel;

        // 持っているトークン（ヘッダーの右上）
        [SerializeField] private CommonText _tokenLabel;
        [SerializeField] private TokenCounterView _tokenCounter;

        // ウインドウを生成する親（WindowUseCase）。ウインドウはシーンに置かない
        [SerializeField] private RectTransform _popupCanvas;

        // ユニット強化を開くボタン
        [SerializeField] private CommonButton _unitUpgradeButton;
        [SerializeField] private CommonText _unitUpgradeLabel;

        public Transform CellParent => _cellParent;
        public MapCellView CellViewPrefab => _cellViewPrefab;
        public Transform LinkParent => _linkParent;
        public MapLinkView LinkViewPrefab => _linkViewPrefab;
        public MapPlayerView PlayerView => _playerView;
        public MapRouteScrollView RouteScroll => _routeScroll;
        public CommonText DepthLabel => _depthLabel;
        public CommonButton EnemyDetailButton => _enemyDetailButton;
        public CommonText EnemyDetailLabel => _enemyDetailLabel;
        public CommonButton TutorialButton => _tutorialButton;
        public CommonText TutorialLabel => _tutorialLabel;
        public CommonButton MenuButton => _menuButton;
        public CommonText MenuLabel => _menuLabel;
        public CommonButton StageStartButton => _stageStartButton;
        public CommonText StageStartLabel => _stageStartLabel;
        public CommonButton DeckEditButton => _deckEditButton;
        public CommonText DeckEditLabel => _deckEditLabel;
        public CommonButton CardShopButton => _cardShopButton;
        public CommonText CardShopLabel => _cardShopLabel;
        public CommonText TokenLabel => _tokenLabel;
        public TokenCounterView TokenCounter => _tokenCounter;
        public RectTransform PopupCanvas => _popupCanvas;
        public CommonButton UnitUpgradeButton => _unitUpgradeButton;
        public CommonText UnitUpgradeLabel => _unitUpgradeLabel;
    }
}
