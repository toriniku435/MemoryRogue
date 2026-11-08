using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.EnemyDetail.Presenters
{
    // 敵詳細のウインドウを受け持つ（開くのは呼ぶ側が Handler の Show で）。
    // 見せるのは次に戦うステージ（今いるレイヤー）の敵。開いたときは先頭の敵を選び、左の列のクリックで選び直す。
    // 見せる中身は EnemyDetailInfoBuilder が組む（タイトルのエネミーコレクションと共通）。
    // 同じ敵が複数体いるステージは1枠にまとめて「x2」と添える。
    // マップの「敵詳細」ボタンからも、コマンドビルドのウインドウの右上からも、同じ Handler で開く
    public sealed class EnemyDetailPresenter : PresenterBase, IWindowPresenter
    {
        private readonly EnemyDetailWindowView _window;
        // 次に戦うステージ（今いるレイヤー）。全部踏破した後は null
        private readonly Func<StagePlan> _currentStage;

        private List<EnemyDetailInfo> _enemies = new List<EnemyDetailInfo>();
        private int _selectedIndex;

        public EnemyDetailPresenter(EnemyDetailWindowView window, Func<StagePlan> currentStage)
        {
            _window = window;
            _currentStage = currentStage;
        }

        public bool IsOpen => _window.IsOpen;

        public void HideImmediate()
        {
            _window.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _window.OnCloseRequested += Close;
            _window.OnEnemyClicked += HandleEnemyClicked;
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _window.OnCloseRequested -= Close;
            _window.OnEnemyClicked -= HandleEnemyClicked;
        }

        // 文字はどれもローカライズテーブル由来なので、開いていれば中身ごと引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_window.IsOpen) Rebuild();
        }

        private void ApplyLabels()
        {
            _window.SetTexts(
                LocalizationHelper.GetUiText(UiTextKey.EnemyDetail),
                LocalizationHelper.GetUiText(UiTextKey.EnemyCommands),
                LocalizationHelper.GetUiText(UiTextKey.Close));
        }

        public void Open()
        {
            if (_window.IsOpen) return;
            _selectedIndex = 0;
            if (!Rebuild()) return;
            _window.Open();
        }

        public void Close()
        {
            _window.Close();
        }

        private void HandleEnemyClicked(int index)
        {
            if (index < 0 || index >= _enemies.Count || index == _selectedIndex) return;
            _selectedIndex = index;
            _window.ShowSelected(_selectedIndex, _enemies[_selectedIndex]);
        }

        // 今いるレイヤーの敵を組み立て直して、選んでいる敵を出す。見せる敵がいなければ false
        private bool Rebuild()
        {
            var stage = _currentStage.Invoke();
            if (stage == null) return false;

            // 手前から順の並びを保ったまま、同じ敵をまとめる
            _enemies = EnemyDetailInfoBuilder.CreateGrouped(stage.Enemies, stage.Strength);
            if (_enemies.Count == 0) return false;

            _selectedIndex = UnityEngine.Mathf.Clamp(_selectedIndex, 0, _enemies.Count - 1);
            _window.SetEnemies(_enemies);
            _window.ShowSelected(_selectedIndex, _enemies[_selectedIndex]);
            return true;
        }
    }
}
