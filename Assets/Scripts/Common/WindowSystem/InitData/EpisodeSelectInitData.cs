using System;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ストーリーのエピソード選択
    public sealed class EpisodeSelectInitData : WindowInitData
    {
        public override WindowId Id => WindowId.EpisodeSelect;

        /// <summary>始めてよいかを確かめる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>そのエピソード（0始まり）の状態（開くたびに聞き直す）</summary>
        public Func<int, EpisodeState> GetState { get; }

        /// <summary>エピソードを始めると決めた（0始まり）</summary>
        public Action<int> OnEpisodeChosen { get; }

        /// <summary>そのエピソードのクリアで取った一番良いランク。クリアしていなければ null</summary>
        public Func<int, CampaignRank?> GetBestRank { get; }

        public EpisodeSelectInitData(WindowHandler<ConfirmShowData> confirm, Func<int, EpisodeState> getState, Action<int> onEpisodeChosen, Func<int, CampaignRank?> getBestRank)
        {
            Confirm = confirm;
            GetState = getState;
            OnEpisodeChosen = onEpisodeChosen;
            GetBestRank = getBestRank;
        }
    }
}
