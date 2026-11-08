using UnityEngine;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.BattleScene.Data
{
    // 対象の候補一覧に並べうるユニット1体。どのユニットが今選べるかは、その都度 UseCase に聞く
    public sealed class BattleTargetCandidate
    {
        public UnitStatusModel Status { get; }
        public Sprite Portrait { get; }

        public BattleTargetCandidate(UnitStatusModel status, Sprite portrait)
        {
            Status = status;
            Portrait = portrait;
        }
    }
}
