using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.EnemyCollection.Presenters
{
    // エネミーのコレクションのウインドウを受け持つPresenter。マップの敵詳細と同じウインドウ（EnemyDetailWindowView）で、
    // 左の列に出会う敵をすべて並べ（多ければスクロールする）、クリックした敵の詳細を右に出す。開いたときは一番上の敵を選ぶ。
    // 中身は EnemyDetailInfoBuilder が組む（強さの上乗せは無し。敵そのものの値を見せる）。
    // まだ出会っていない敵は、影と「？？？」で伏せる（選ぶことはできる）。出会ったかは開くたびに聞き直す
    public sealed class EnemyCollectionPresenter : PresenterBase, IWindowPresenter
    {
        private readonly EnemyDetailWindowView _view;
        private readonly EnemyCollectionData _data;
        private readonly Func<ISet<string>> _getDiscoveredIds;
        private ISet<string> _discoveredIds = new HashSet<string>();

        private List<EnemyDetailInfo> _enemies = new List<EnemyDetailInfo>();
        private int _selectedIndex;

        /// <param name="getDiscoveredIds">一度でも戦ったことがある敵の ID（開くたびに聞き直す）</param>
        public EnemyCollectionPresenter(EnemyDetailWindowView view, EnemyCollectionData data, Func<ISet<string>> getDiscoveredIds)
        {
            _view = view;
            _data = data;
            _getDiscoveredIds = getDiscoveredIds;
        }

        protected override void OnInitialize()
        {
            _view.OnCloseRequested += Close;
            _view.OnEnemyClicked += HandleEnemyClicked;
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _view.OnCloseRequested -= Close;
            _view.OnEnemyClicked -= HandleEnemyClicked;
        }

        // 文字はどれもローカライズテーブル由来なので、開いていれば中身ごと引き直す（選んでいる敵はそのまま）
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_view.IsOpen) Rebuild();
        }

        public bool IsOpen => _view.IsOpen;

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        public void Open()
        {
            _selectedIndex = 0;
            _discoveredIds = _getDiscoveredIds.Invoke() ?? new HashSet<string>();
            if (!Rebuild()) return;
            _view.Open();
        }

        private void ApplyLabels()
        {
            _view.SetTexts(
                LocalizationHelper.GetUiText(UiTextKey.EnemyCollection),
                LocalizationHelper.GetUiText(UiTextKey.EnemyCommands),
                LocalizationHelper.GetUiText(UiTextKey.Close));
        }

        // 敵を並べ直して、選んでいる敵を出す。並べる敵がいなければ false
        private bool Rebuild()
        {
            if (_data == null)
            {
                Debug.LogError("TitleSceneInitializer に EnemyCollectionData が設定されていません");
                return false;
            }

            _enemies = new List<EnemyDetailInfo>();
            foreach (var enemy in _data.Enemies)
            {
                _enemies.Add(_discoveredIds.Contains(enemy.Id)
                    ? EnemyDetailInfoBuilder.Create(enemy, 1, EnemyStrength.None)
                    : EnemyDetailInfoBuilder.CreateUnknown(enemy));
            }
            if (_enemies.Count == 0) return false;

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _enemies.Count - 1);
            _view.SetEnemies(_enemies);
            _view.ShowSelected(_selectedIndex, _enemies[_selectedIndex]);
            return true;
        }

        private void HandleEnemyClicked(int index)
        {
            if (index < 0 || index >= _enemies.Count || index == _selectedIndex) return;
            _selectedIndex = index;
            _view.ShowSelected(_selectedIndex, _enemies[_selectedIndex]);
        }

        public void Close()
        {
            _view.Close();
        }
    }
}
