using System.Linq;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Data;
using Live2DCardRogue.MapScene.Models;

namespace Live2DCardRogue.MapScene.UseCase
{
    // マップで、ラン（ストーリー / チャレンジ1回分）の状態を書き換えるところ。
    // 初めて来たときの持ち物を配り、戦闘の結果・デッキ編集・買い物・ユニット強化をリポジトリへ書き戻し、節目で保存する。
    // 画面は持たない（何を見せるかは MapArrival などで返す）
    public sealed class MapRunUseCase
    {
        private readonly CampaignBalance _balance;
        private readonly MapRunRule _rule;
        private readonly CardPackData[] _shopPacks;

        public MapRunUseCase(CampaignBalance balance, MapRunRule rule, CardPackData[] shopPacks)
        {
            _balance = balance;
            _rule = rule;
            _shopPacks = shopPacks;
        }

        // マップのチュートリアル中と踏破した後は保存しない（チュートリアルは次も最初から。踏破後はクリア記録を書いて進捗を消すので、
        // その前に終了したら最終ステージの前から再開できるようにする）
        private bool CanSave => CampaignTutorialRepository.Stage != CampaignTutorialStage.Map && !_rule.IsCompleted(CampaignProgressRepository.CurrentStageIndex);

        /// <summary>
        /// キャンペーンに入って最初にマップへ来たときだけ、ファーストビルドのチップを開け、所持コマンド（最初から持っている分 + 開けた分）から
        /// 自動でデッキを組む。開けたコマンドを返す（開けていなければ空。再開したときも空）
        /// </summary>
        public CardData[] InitializeDeckIfNeeded()
        {
            if (!CampaignDeckRepository.IsEmpty) return new CardData[0];

            var firstBuild = _balance.OpenFirstBuild(new System.Random());
            var owned = _balance.StartOwnedCards.Where(card => card != null).Concat(firstBuild).ToArray();
            var deck = AutoDeckBuilder.Build(owned, _balance.CreateAutoDeckRule());
            CampaignDeckRepository.Initialize(deck, owned);
            Debug.Log($"--- ファーストビルド: チップ{_balance.FirstBuildChipCount}個から{firstBuild.Length}個 → デッキ{deck.Length}枚 / 所持{owned.Length}枚で開始 ---");
            return firstBuild;
        }

        /// <summary>キャンペーンに入って最初にマップへ来たときだけ、初期のトークンと、強化していないユニットの値を配る</summary>
        public void InitializeTokensAndUnitIfNeeded()
        {
            if (!CampaignTokenRepository.IsInitialized)
            {
                CampaignTokenRepository.Initialize(_balance.StartTokens);
                Debug.Log($"--- トークン{CampaignTokenRepository.Tokens}で開始 ---");
            }
            if (!CampaignUnitRepository.IsInitialized) CampaignUnitRepository.Initialize(_balance.StartMaxHp, _balance.StartMaxMemory);
        }

        /// <summary>報酬や買い物の結果を反映したあとのトークンとユニットの値で、ユニット強化を始める</summary>
        public UnitUpgradeModel CreateUnitUpgradeModel()
        {
            return new UnitUpgradeModel(CampaignTokenRepository.Tokens, CampaignUnitRepository.MaxHp, CampaignUnitRepository.MaxMemory,
                _balance.StartMaxHp, _balance.StartMaxMemory, _balance.CreateUnitUpgradeRule());
        }

        /// <summary>
        /// 戦ったステージの報酬（負けたときは支援）を受け取り、勝ったら次の部屋へ進む。負けたら同じ部屋のまま。
        /// result が null（戦闘から戻ってきたのでない）なら何もしない
        /// </summary>
        public MapArrival ApplyBattleResult(BattleResult result)
        {
            if (result == null) return MapArrival.None;

            // 戦闘中にコマンドで払ったトークンは、勝っても負けても戻らない
            if (result.TokensSpent > 0)
            {
                CampaignTokenRepository.Set(CampaignTokenRepository.Tokens - result.TokensSpent);
                Debug.Log($"--- 戦闘で払ったトークン -{result.TokensSpent}（残り{CampaignTokenRepository.Tokens}）---");
            }

            // 進む前の位置が、いま戦ってきたステージ
            int stageIndex = CampaignProgressRepository.CurrentStageIndex;
            var stage = _rule.GetStage(stageIndex);

            // 最後のステージ（チャレンジは最後の階層）に勝った: もう先が無いので報酬は渡さない。
            // 進行だけ進め、移動の演出もせず、会話があれば流してからエンディング / リザルトへ進む
            if (result.IsVictory && stage != null && _rule.IsCompleted(stageIndex + 1))
            {
                CampaignProgressRepository.AdvanceStage();
                Debug.Log("--- 最後のステージに勝利（報酬は無し）---");
                return new MapArrival(null, null, stage, completedNow: true);
            }

            StageReward reward = null;
            if (stage != null)
            {
                reward = stage.CreateReward(result.IsVictory);
                // マイニングの上乗せは勝ったときだけ（負けたときの支援には乗せない）
                if (result.IsVictory) reward = reward.WithTokenBonus(result.TokenBonusPercent);
                CampaignTokenRepository.Add(reward.Tokens);
                if (reward.Card != null) CampaignDeckRepository.AddOwnedCards(new[] { reward.Card });
                Debug.Log($"--- {(result.IsVictory ? "報酬" : "支援")}: トークン+{reward.Tokens}（計{CampaignTokenRepository.Tokens}）{(reward.Card != null ? $" / カード {reward.Card.Id}" : "")} ---");
            }

            if (result.IsVictory)
            {
                CampaignProgressRepository.AdvanceStage();
                Debug.Log($"--- 勝利したので次のマスへ（{CampaignProgressRepository.CurrentStageIndex + 1}マス目）---");
                return new MapArrival(stageIndex, reward, stage, completedNow: false);
            }

            // チャレンジは負けたらリザルトへ進むので、ここへは戻ってこない（戻ってきても支援は無い）
            if (!_rule.EndsOnDefeat)
            {
                CampaignProgressRepository.RecordBackupBoost();
                Debug.Log($"--- 敗北したのでマスはそのまま（{CampaignProgressRepository.CurrentStageIndex + 1}マス目 / バックアップブースト{CampaignProgressRepository.BackupBoostCount}回目）---");
            }
            return new MapArrival(null, reward, null, completedNow: false);
        }

        /// <summary>デッキ編集で保存した: キャンペーンの保存先へ書き戻して、進捗を保存する</summary>
        public void ApplyDeckSaved(CardData[][] decks, int activeIndex)
        {
            CampaignDeckRepository.SetDecks(decks, activeIndex);
            Debug.Log($"--- デッキを更新（アクティブはデッキ{activeIndex + 1:00}、{CampaignDeckRepository.Deck.Length}枚）---");
            SaveIfAllowed();
        }

        /// <summary>マーケットを開いたときの状態。チップの値段の係数とシングル購入の決まりはゲームバランスから、トークンと持ち物は保存先から</summary>
        public CardShopModel CreateCardShopModel()
        {
            return new CardShopModel(_shopPacks, CampaignTokenRepository.Tokens, new System.Random(), _balance.ChipPriceMultiplier,
                _balance.CreateSinglePurchaseRule(), CampaignDeckRepository.PurchasedSingles, CampaignDeckRepository.OwnedCards);
        }

        /// <summary>
        /// 買ったコマンドを所持に加え、残りトークンを書き戻して、進捗を保存する。
        /// シングル購入は1種類あたりの上限をキャンペーンを通して数えるので、何を買ったかも残す
        /// </summary>
        public void ApplyShopPurchase(CardData[] purchased, CardData[] singles, int remainingTokens)
        {
            CampaignDeckRepository.AddOwnedCards(purchased);
            CampaignDeckRepository.AddPurchasedSingles(singles);
            CampaignTokenRepository.Set(remainingTokens);
            Debug.Log($"--- {purchased.Length}個購入: {string.Join(", ", purchased.Select(card => card.Id))}（所持{CampaignDeckRepository.OwnedCards.Length}個 / 残りトークン{remainingTokens}）---");
            SaveIfAllowed();
        }

        /// <summary>ユニット強化を確定した: トークンとユニットの値を書き戻して、進捗を保存する</summary>
        public void ApplyUnitUpgrade(int tokens, int maxHp, int maxMemory)
        {
            CampaignTokenRepository.Set(tokens);
            CampaignUnitRepository.Set(maxHp, maxMemory);
            SaveProgress();
            Debug.Log($"--- ユニット強化: HP{maxHp} / メモリー{maxMemory}（残りトークン{tokens}）---");
        }

        /// <summary>保存してよい節目なら保存する（チュートリアル中・踏破後は保存しない）</summary>
        public void SaveIfAllowed()
        {
            if (CanSave) SaveProgress();
        }

        public void SaveProgress()
        {
            CampaignSaveService.Default.SaveProgress(_rule.SavedStageCount);
            Debug.Log($"--- 進捗を保存（{CampaignSaveService.Default.Directory}）---");
        }
    }
}
