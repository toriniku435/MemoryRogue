using System;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.BattleScene.Models
{
    // プレイヤーが今「指している」ユニット。バトルフィールド上のユニット表示で強調するためだけの状態。
    // 指す手段は複数ある（対象の候補一覧にカーソルを合わせる、実行リストのカードにカーソルを合わせる）ので、
    // 手段ごとの Presenter が書き、各ユニットの Presenter が読む
    public sealed class TargetFocusModel
    {
        public UnitStatusModel FocusedTarget { get; private set; }

        public event Action<UnitStatusModel> OnFocusedTargetChanged;

        public void Focus(UnitStatusModel target)
        {
            if (FocusedTarget == target) return;

            FocusedTarget = target;
            OnFocusedTargetChanged?.Invoke(FocusedTarget);
        }

        // 指していたのがそのユニットのときだけ外す。
        // 隣の候補へカーソルを移すと、新しい方の Enter の後に古い方の Exit が来ることがあり、
        // 無条件に外すと今指している方まで消えてしまうため
        public void Unfocus(UnitStatusModel target)
        {
            if (FocusedTarget != target) return;
            Focus(null);
        }

        public void Clear()
        {
            Focus(null);
        }
    }
}
