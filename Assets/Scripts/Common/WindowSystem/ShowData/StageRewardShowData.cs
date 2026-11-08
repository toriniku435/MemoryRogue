using System;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 報酬ウインドウを開くときに渡すもの
    public sealed class StageRewardShowData : WindowShowData
    {
        public StageReward Reward { get; }

        /// <summary>報酬の下に添える1行（どの階層か、上乗せの割合など）</summary>
        public string Detail { get; }

        /// <summary>受け取った後のトークン（数え上げの行き先）</summary>
        public int TotalAfter { get; }

        /// <summary>受け取った（ウインドウは閉じ始めている）</summary>
        public Action OnClaimed { get; }

        public StageRewardShowData(StageReward reward, string detail, int totalAfter, Action onClaimed)
        {
            Reward = reward;
            Detail = detail;
            TotalAfter = totalAfter;
            OnClaimed = onClaimed;
        }
    }
}
