using System.Collections.Generic;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.TitleScene.Models;
using Live2DCardRogue.Window.EpisodeSelect.Models;

namespace Live2DCardRogue.TitleScene.UseCase
{
    // タイトルのウインドウが見せる記録（クリア記録・チャレンジの記録・出会った敵）を読む。
    // エピソード選択はエピソードごとに何度も聞くので、クリア記録は Reload するまで使い回す
    public sealed class TitleRecordUseCase
    {
        private readonly CampaignBalance _balance;
        private readonly EndlessBalance _endlessBalance;
        private CampaignClearData _clearData;

        /// <param name="balance">どのエピソードにステージがあるか、ストーリーの敵（マップと同じアセット）</param>
        /// <param name="endlessBalance">中断しているチャレンジで突破した階層の敵を組み直す（記録を始める前に戦った敵も、出会ったことにするため）</param>
        public TitleRecordUseCase(CampaignBalance balance, EndlessBalance endlessBalance)
        {
            _balance = balance;
            _endlessBalance = endlessBalance;
        }

        private CampaignClearData ClearData => _clearData ??= CampaignSaveService.Default.LoadClearData();

        /// <summary>次に聞かれたときに読み直す（エピソード選択を開く前、ユーザーデータを消した後）</summary>
        public void Reload()
        {
            _clearData = null;
        }

        public EpisodeState GetEpisodeState(int index)
        {
            return EpisodeUnlockRule.GetState(index, ClearData.HasCleared, HasEpisodeContent);
        }

        /// <summary>そのエピソード（0始まり）のクリアで取った一番良いランク。クリアしていなければ null</summary>
        public CampaignRank? GetBestRank(int index)
        {
            return ClearData.BestRank(index);
        }

        /// <summary>そのチャレンジを最後の階層まで踏破したことがあるか（開くたびに読み直す）</summary>
        public bool IsChallengeCleared(ChallengeType challenge)
        {
            var record = CampaignSaveService.Default.LoadClearData().FindChallenge(challenge.ToString());
            return record != null && record.cleared;
        }

        /// <summary>そのチャレンジの最高到達階層（まだ遊んでいなければ 0）</summary>
        public int GetChallengeBestLayer(ChallengeType challenge)
        {
            var record = CampaignSaveService.Default.LoadClearData().FindChallenge(challenge.ToString());
            return record != null ? record.bestLayer : 0;
        }

        public HashSet<string> LoadDiscoveredCommandIds()
        {
            return CampaignSaveService.Default.LoadDiscoveredCommandIds();
        }

        /// <summary>一度でも戦ったことがある敵: 記録に、クリア記録と中断データから分かる分を足す</summary>
        public ISet<string> LoadDiscoveredEnemyIds()
        {
            var saves = CampaignSaveService.Default;
            var campaign = saves.TryLoadProgress(out var c) ? c : null;
            var endless = saves.TryLoadProgress(out var e, RunMode.Challenge) ? e : null;
            return EnemyDiscoveryResolver.Resolve(saves.LoadClearData(), campaign, endless, _balance, _endlessBalance);
        }

        // CampaignBalance にそのエピソードの CampaignData が無ければ、選べるようになっても準備中と伝える
        private bool HasEpisodeContent(int index)
        {
            return _balance != null && _balance.HasEpisode(index);
        }
    }
}
