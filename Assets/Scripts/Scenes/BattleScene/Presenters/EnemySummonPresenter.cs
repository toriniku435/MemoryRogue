using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 生成された敵の枠を表に出す。生成されるまでの枠は戦闘の始めに隠してある（BattleSceneInitializer.CreateEnemies）。
    // HP・状態効果の描き直しはその敵の UnitPresenter が OnRevived で行うので、ここは出すだけ
    public sealed class EnemySummonPresenter : PresenterBase
    {
        private readonly EnemyPartyModel _party;
        // 敵の並びと同じ順の View
        private readonly IReadOnlyList<BattleUnitStatusView> _views;

        public EnemySummonPresenter(EnemyPartyModel party, IReadOnlyList<BattleUnitStatusView> views)
        {
            _party = party;
            _views = views;
        }

        protected override void OnInitialize()
        {
            _party.OnSummoned += HandleSummoned;
        }

        protected override void OnDispose()
        {
            _party.OnSummoned -= HandleSummoned;
        }

        protected override void OnLocaleChanged()
        {
        }

        private void HandleSummoned(BattleEnemy enemy)
        {
            for (int i = 0; i < _party.Enemies.Count && i < _views.Count; i++)
            {
                if (_party.Enemies[i] != enemy) continue;
                _views[i].gameObject.SetActive(true);
                AudioUseCase.PlaySe(SeId.MemoryWrite);
                return;
            }
        }
    }
}
