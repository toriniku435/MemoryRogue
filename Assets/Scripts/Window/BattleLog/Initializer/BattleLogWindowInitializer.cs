using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.BattleLog.Presenters;
using Live2DCardRogue.Window.BattleLog.Views;

namespace Live2DCardRogue.Window.BattleLog.Initializer
{
    // 戦闘全体のログ（BattleLogWindow.prefab）のコンポジションルート
    public sealed class BattleLogWindowInitializer : WindowInitializerBase<BattleLogInitData, BattleLogWindowPresenter>
    {
        [SerializeField] private BattleLogWindowView _view;

        protected override BattleLogWindowPresenter Initialize(BattleLogInitData data)
        {
            return new BattleLogWindowPresenter(data.Log, _view, data.Formatter);
        }
    }
}
