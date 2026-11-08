using System;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.WindowSystem
{
    // チャレンジの選択（ノーマル / ハード / マスター / エンドレス）
    public sealed class ChallengeSelectInitData : WindowInitData
    {
        public override WindowId Id => WindowId.ChallengeSelect;

        /// <summary>並べるチャレンジ</summary>
        public ChallengeCatalog Catalog { get; }

        /// <summary>始めてよいかを確かめる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>チャレンジを始めると決めた</summary>
        public Action<ChallengeCatalog.Entry> OnChallengeChosen { get; }

        /// <summary>そのチャレンジを踏破したことがあるか</summary>
        public Func<ChallengeType, bool> IsCleared { get; }

        /// <summary>そのチャレンジの最高到達階層（まだ遊んでいなければ 0）</summary>
        public Func<ChallengeType, int> GetBestLayer { get; }

        public ChallengeSelectInitData(ChallengeCatalog catalog, WindowHandler<ConfirmShowData> confirm, Action<ChallengeCatalog.Entry> onChallengeChosen, Func<ChallengeType, bool> isCleared, Func<ChallengeType, int> getBestLayer)
        {
            Catalog = catalog;
            Confirm = confirm;
            OnChallengeChosen = onChallengeChosen;
            IsCleared = isCleared;
            GetBestLayer = getBestLayer;
        }
    }
}
