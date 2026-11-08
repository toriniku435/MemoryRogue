using System;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 次に戦う相手（今いるレイヤーの敵）の詳細
    public sealed class EnemyDetailInitData : WindowInitData
    {
        public override WindowId Id => WindowId.EnemyDetail;

        /// <summary>次に戦うステージ（開くたびに聞く）。全部踏破した後は null</summary>
        public Func<StagePlan> CurrentStage { get; }

        public EnemyDetailInitData(Func<StagePlan> currentStage)
        {
            CurrentStage = currentStage;
        }
    }
}
