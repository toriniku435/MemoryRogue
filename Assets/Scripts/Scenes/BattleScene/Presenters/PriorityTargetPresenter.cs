using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 優先ターゲットの印を、その敵の頭上にだけ出す。生きている敵が1体以下なら選ぶ意味が無いので出さない。
    // 選んだ敵が倒れたら、モデルが返す次の敵（一番手前）へ印が移る
    public sealed class PriorityTargetPresenter : PresenterBase
    {
        private readonly PriorityTargetModel _model;
        private readonly IReadOnlyList<UnitStatusModel> _enemies;
        private readonly IReadOnlyList<BattleUnitStatusView> _views;

        // 敵と View は同じ並び（手前から）
        public PriorityTargetPresenter(PriorityTargetModel model, IReadOnlyList<UnitStatusModel> enemies, IReadOnlyList<BattleUnitStatusView> views)
        {
            _model = model;
            _enemies = enemies;
            _views = views;
        }

        protected override void OnInitialize()
        {
            _model.OnChanged += HandleChanged;
            foreach (var enemy in _enemies) enemy.OnHpChanged += HandleHpChanged;
            Refresh();
        }

        protected override void OnDispose()
        {
            _model.OnChanged -= HandleChanged;
            foreach (var enemy in _enemies) enemy.OnHpChanged -= HandleHpChanged;
        }

        // 文字は飾りの英字（TARGET）だけなので、言語が変わっても出し直すものは無い
        protected override void OnLocaleChanged()
        {
        }

        private void HandleChanged()
        {
            AudioUseCase.PlaySe(SeId.CardSelect);
            Refresh();
        }

        private void HandleHpChanged(int current, int max)
        {
            Refresh();
        }

        private void Refresh()
        {
            var current = _model.LivingCount >= 2 ? _model.Current : null;
            int count = System.Math.Min(_enemies.Count, _views.Count);
            for (int i = 0; i < count; i++)
            {
                _views[i].SetPriorityTarget(_enemies[i] == current);
            }
        }
    }
}
