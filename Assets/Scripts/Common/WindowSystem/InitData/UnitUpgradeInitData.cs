using System;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ユニット強化。モデルはマップと共有する（買い物でトークンが減ったらマップが書き換える）
    public sealed class UnitUpgradeInitData : WindowInitData
    {
        public override WindowId Id => WindowId.UnitUpgrade;

        /// <summary>強化の状態（マップと共有）</summary>
        public UnitUpgradeModel Model { get; }

        /// <summary>マップのトークン表示。確定して減った分を数え下げる</summary>
        public TokenCounterView MapTokenCounter { get; }

        /// <summary>変更を捨ててよいかを確かめる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>確定した（残りトークン, 最大HP, メモリー上限）</summary>
        public Action<int, int, int> OnUpgraded { get; }

        public UnitUpgradeInitData(UnitUpgradeModel model, TokenCounterView mapTokenCounter, WindowHandler<ConfirmShowData> confirm, Action<int, int, int> onUpgraded)
        {
            Model = model;
            MapTokenCounter = mapTokenCounter;
            Confirm = confirm;
            OnUpgraded = onUpgraded;
        }
    }
}
