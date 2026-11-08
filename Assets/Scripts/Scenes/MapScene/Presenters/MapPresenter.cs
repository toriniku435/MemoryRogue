using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Models;
using Live2DCardRogue.MapScene.Views;
using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.MapScene.Presenters
{
    // マップ（サイバー空間の部屋を奥へ進んでいく経路）とボタンを受け持つPresenter。
    // 部屋の数と中身はルール（MapRunRule: ストーリー / エンドレス）が、座標は MapRouteLayout が決め、ここは並べて見せるだけ。
    //
    // 部屋と配線は今のページと前後のページの分だけを作り、範囲から外れたものはプールへ戻して使い回すので、
    // エンドレスで何百階層進んでも数は増えない。
    // 戦闘から戻ってきたときは、まず報酬ウインドウ（負けたときはバックアップブースト）を見せ、
    // 受け取るとトークンが数え上がる。勝って1つ進んでいれば、その後にプレイヤーが前の部屋から
    // パケットになって今の部屋へ移る演出を見せる。ページをまたぐときは、飛んでいる間に1ページ分スクロールする
    public sealed class MapPresenter : PresenterBase
    {
        // 表示しておくページ: 今のページと、その前後（横長の画面では隣のページが見える）
        private const int PagesAround = 1;

        private readonly MapSceneViewContainer _viewContainer;
        private readonly MapRunRule _rule;
        private readonly Action _onStageStart;
        // 最終ステージを踏破して報酬を受け取り終えた（エンディングへ進む）
        private readonly Action _onCampaignCompleted;

        // 今出している部屋と配線（何番目か → View）と、使い終えて戻したもの。配線 i は部屋 i と i + 1 をつなぐ
        private readonly Dictionary<int, MapCellView> _activeCells = new Dictionary<int, MapCellView>();
        private readonly Dictionary<int, MapLinkView> _activeLinks = new Dictionary<int, MapLinkView>();
        private readonly List<MapCellView> _cellPool = new List<MapCellView>();
        private readonly List<MapLinkView> _linkPool = new List<MapLinkView>();

        // 勝って進んできた場合の、1つ前の部屋。進んでいなければ null
        private readonly int? _arrivedFromIndex;

        // 戦闘から戻ってきたときに受け取ったもの。戦闘から戻ってきたのでなければ null。
        // リポジトリへの反映は済んでいて、ここでは見せるだけ
        private readonly StageReward _reward;

        // 報酬ウインドウを開くのを待つか（先に会話を流すとき。会話を読み終えたら ShowReward で開く）
        private readonly bool _holdReward;

        private MapRouteLayout _layout;

        // 移動の演出中だけ、見た目上の現在地を前の部屋に据え置く
        private int _displayedIndex;

        // 接続開始を確かめる（マップで共用の確認ウインドウ）
        private readonly WindowHandler<ConfirmShowData> _confirm;
        // 報酬ウインドウを開く（Initializer が報酬ウインドウの Handler の Show を渡す）
        private readonly Action<StageRewardShowData> _showReward;

        public MapPresenter(
            MapSceneViewContainer viewContainer,
            WindowHandler<ConfirmShowData> confirm,
            Action<StageRewardShowData> showReward,
            MapRunRule rule,
            Action onStageStart,
            Action onCampaignCompleted,
            int? arrivedFromIndex,
            StageReward reward,
            bool holdReward = false)
        {
            _viewContainer = viewContainer;
            _confirm = confirm;
            _showReward = showReward;
            _rule = rule;
            _onStageStart = onStageStart;
            _onCampaignCompleted = onCampaignCompleted;
            _arrivedFromIndex = arrivedFromIndex;
            _reward = reward;
            _holdReward = holdReward;
        }

        protected override void OnInitialize()
        {
            _viewContainer.StageStartButton.onClick.AddListener(HandleStageStart);

            _layout = new MapRouteLayout(((RectTransform)_viewContainer.CellViewPrefab.transform).rect.width, _rule.StageCount);
            if (!HasRooms) Debug.LogError("ステージが1つも設定されていません。CampaignBalance のステージ（CampaignData）を確認してください");

            // 進んできた場合は、移動の演出が済むまで前の部屋にいることにしておく
            int current = CampaignProgressRepository.CurrentStageIndex;
            _displayedIndex = IsTravelPending ? _arrivedFromIndex.Value : current;
            if (HasRooms) _layout.SetOrigin(_layout.PlayerCellIndex(_displayedIndex));
            Sync();
            if (HasRooms) _viewContainer.PlayerView.PlaceAt(_layout.PlayerPosition(_layout.PlayerCellIndex(_displayedIndex)));

            if (_reward == null)
            {
                _viewContainer.TokenCounter.SetValue(CampaignTokenRepository.Tokens);
                PlayArrival();
                return;
            }

            // 受け取る前の額から見せ、受け取ったら数え上げる
            _viewContainer.TokenCounter.SetValue(CampaignTokenRepository.Tokens - _reward.Tokens);
            _viewContainer.StageStartButton.interactable = false;
            if (!_holdReward) ShowReward();
        }

        /// <summary>報酬ウインドウを開く（会話を先に流したときは、読み終えてから Initializer が呼ぶ）</summary>
        public void ShowReward()
        {
            if (_reward == null) return;
            int current = CampaignProgressRepository.CurrentStageIndex;
            _showReward.Invoke(new StageRewardShowData(
                _reward,
                // マイニングでトークンを上乗せしていれば、その割合も添える
                $"{_rule.LayerText(_arrivedFromIndex ?? current)}  {(_reward.IsVictory ? "CLEAR" : "RETRY")}{(_reward.BonusPercent > 0 ? $"  MINING +{_reward.BonusPercent}%" : "")}",
                CampaignTokenRepository.Tokens,
                HandleRewardClaimed));
        }

        /// <summary>今いる部屋（見た目上の現在地）。チュートリアルの目印に使う</summary>
        public RectTransform CurrentCellTransform =>
            HasRooms && _activeCells.TryGetValue(_layout.PlayerCellIndex(_displayedIndex), out var cell) ? cell.RectTransform : null;

        private bool HasRooms => _layout.HasRooms;

        private bool IsTravelPending => _arrivedFromIndex.HasValue && HasRooms;

        // 進んできたなら、前の部屋からパケットになって今の部屋へ移る。進んでいなければ今の部屋で操作を受け付ける。
        // ページをまたぐなら、パケットが飛んでいる間に経路を1ページ分流す
        private void PlayArrival()
        {
            int current = CampaignProgressRepository.CurrentStageIndex;
            if (!IsTravelPending)
            {
                _displayedIndex = current;
                Sync();
                return;
            }

            int fromCell = _layout.PlayerCellIndex(_arrivedFromIndex.Value);
            int toCell = _layout.PlayerCellIndex(current);
            int fromPage = MapRouteLayout.PageOf(fromCell);
            int toPage = MapRouteLayout.PageOf(toCell);

            _viewContainer.StageStartButton.interactable = false;
            _viewContainer.PlayerView.PlayTravel(
                _layout.PlayerPosition(fromCell),
                _layout.PlayerPosition(toCell),
                onArrived: () =>
                {
                    _displayedIndex = current;
                    Sync();
                },
                onDeparted: () =>
                {
                    if (fromPage == toPage) return;
                    _viewContainer.RouteScroll.PlayScroll(_layout.PageOffset(fromPage), _layout.PageOffset(toPage), _viewContainer.PlayerView.TravelDuration);
                });
        }

        private void HandleRewardClaimed()
        {
            // 最終ステージを踏破したなら、先へ進む部屋は無い。トークンを数え上げたらエンディングへ
            if (_rule.IsCompleted(CampaignProgressRepository.CurrentStageIndex))
            {
                _viewContainer.TokenCounter.CountTo(CampaignTokenRepository.Tokens, _onCampaignCompleted);
                return;
            }

            _viewContainer.TokenCounter.CountTo(CampaignTokenRepository.Tokens);
            PlayArrival();
        }

        protected override void OnDispose()
        {
            _viewContainer.StageStartButton.onClick.RemoveListener(HandleStageStart);
        }

        // ボタンのラベルはローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        // 今のページと前後のページの部屋と配線を出し、範囲から外れたものはプールへ戻す
        private void RefreshRoute(int currentCell)
        {
            int page = MapRouteLayout.PageOf(currentCell);
            var range = MapPageWindow.Of(page, PagesAround, MapRouteLayout.PageSize, _rule.StageCount);

            Recycle(_activeCells, _cellPool, index => !range.Contains(index));
            // 配線は両端の部屋が出ている間だけ出す
            Recycle(_activeLinks, _linkPool, index => !range.Contains(index) || !range.Contains(index + 1));

            for (int i = range.First; i < range.End; i++)
            {
                if (!_activeCells.ContainsKey(i))
                {
                    var cell = Take(_cellPool, _viewContainer.CellViewPrefab, _viewContainer.CellParent);
                    cell.RectTransform.anchoredPosition = _layout.CellPosition(i);
                    cell.SetNumber(i + 1);
                    _activeCells.Add(i, cell);
                }
                if (range.Contains(i + 1) && !_activeLinks.ContainsKey(i))
                {
                    var link = Take(_linkPool, _viewContainer.LinkViewPrefab, _viewContainer.LinkParent);
                    link.Connect(_layout.CellPosition(i), _layout.CellPosition(i + 1));
                    _activeLinks.Add(i, link);
                }
            }

            // ページ送りのスクロール中は、流し終わりに任せる
            if (!_viewContainer.RouteScroll.IsPlaying) _viewContainer.RouteScroll.SetOffset(_layout.PageOffset(page));
        }

        private static void Recycle<T>(Dictionary<int, T> active, List<T> pool, Func<int, bool> isOutside) where T : Component
        {
            var removed = new List<int>();
            foreach (var pair in active)
            {
                if (isOutside.Invoke(pair.Key)) removed.Add(pair.Key);
            }
            foreach (var index in removed)
            {
                var view = active[index];
                view.gameObject.SetActive(false);
                pool.Add(view);
                active.Remove(index);
            }
        }

        private static T Take<T>(List<T> pool, T prefab, Transform parent) where T : Component
        {
            if (pool.Count == 0) return UnityEngine.Object.Instantiate(prefab, parent);
            var view = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            view.gameObject.SetActive(true);
            return view;
        }

        // 現在地と踏破状況を塗り直し、踏破済みならステージ開始を押せなくする。
        // 塗るのは見た目上の現在地（移動の演出中は前の部屋）
        private void Sync()
        {
            int currentIndex = _displayedIndex;

            if (HasRooms) RefreshRoute(_layout.PlayerCellIndex(currentIndex));
            foreach (var pair in _activeCells)
            {
                pair.Value.SetState(isCleared: pair.Key < currentIndex, isCurrent: pair.Key == currentIndex);
            }
            foreach (var pair in _activeLinks)
            {
                pair.Value.SetLit(pair.Key < currentIndex);
            }

            _viewContainer.StageStartButton.interactable = !_rule.IsCompleted(CampaignProgressRepository.CurrentStageIndex);
            _viewContainer.DepthLabel.text = _rule.LayerText(currentIndex);
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            _viewContainer.StageStartLabel.text = LocalizationHelper.GetUiText(UiTextKey.StageStart);
            _viewContainer.TokenLabel.text = LocalizationHelper.GetUiText(UiTextKey.Tokens);
        }

        // すぐには始めず、確認ウインドウを出す
        private void HandleStageStart()
        {
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.StageStartConfirm),
                _rule.LayerText(CampaignProgressRepository.CurrentStageIndex),
                LocalizationHelper.GetUiText(UiTextKey.Ok),
                LocalizationHelper.GetUiText(UiTextKey.Cancel),
                _onStageStart));
        }
    }
}
