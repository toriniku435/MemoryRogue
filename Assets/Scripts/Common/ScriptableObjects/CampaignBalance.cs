using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // キャンペーンのゲームバランス。調整に使う数値とステージの並びはここ1か所に集める（コードの定数やシーンの Inspector に散らさない）。
    //   ステージ: エピソードごとの、踏破するステージの並び（CampaignData。エピソード1から順に）。難易度ごとに別のステージ（敵・報酬）を並べられる
    //   開始時: トークン・最大HP・メモリー上限・所持コマンド（ファーストビルドの前から持っている分）
    //   ファーストビルド: キャンペーンの最初に開けるチップと個数・確定で入るコマンド・自動で組むデッキの役割ごとの枚数
    //   スコアとランク: トークン1あたりの点・バックアップブースト1回の減点・ランクの閾値・エンディングの分かれ目
    //   ユニット強化: 1段の上がり幅・上限・値段（基礎 + 増分 × 今の段）
    //   チップ: 全チップの値段にかける基礎係数（各チップの値段は、中身と一緒に CardPackData が持つ）
    //   シングル購入: コマンドを1つずつ選んで買うときのレアリティごとの値段と、1種類あたりの上限（値段にはチップと同じ係数がかかる）
    // ステージごとの敵と勝敗の報酬は StageData が持つ。難易度を変えるなら、別の StageData を並べた CampaignData を指す。
    // 別の難しさやモードを作るときは、このアセットを複製して差し替える（1つの難易度 = このアセット1つ）
    [CreateAssetMenu(fileName = "CampaignBalance", menuName = "Campaign/CampaignBalance")]
    public sealed class CampaignBalance : ScriptableObject
    {
        [Header("ステージ（エピソード1から順に）")]
        /// <summary>エピソードごとの、踏破するステージの並び。マップのマスの数は、遊んでいるエピソードのステージ数で決まる</summary>
        [SerializeField] private CampaignData[] _episodes;

        [Header("開始時")]
        [SerializeField][Min(0)] private int _startTokens = 500;
        [SerializeField][Min(1)] private int _startMaxHp = 100;
        [SerializeField][Min(0)] private int _startMaxMemory = 10;
        /// <summary>ファーストビルドの前から持っている所持コマンド。同じコマンドを複数持つなら、その数だけ並べる。デッキはこれとファーストビルドの中から自動で組む</summary>
        [SerializeField] private CardData[] _startOwnedCards = new CardData[0];

        [Header("ファーストビルド（キャンペーンの最初にチップを開け、そこから自動でデッキを組む）")]
        [SerializeField] private CardPackData _firstBuildChip;
        [SerializeField][Min(0)] private int _firstBuildChipCount = 10;
        /// <summary>
        /// 必ず入るコマンド。チップから出る数（チップの封入数 × 個数）のうち、この数だけが確定枠になり、残りだけを抽選する。
        /// 引きが悪くても最低限戦えるようにするためのもの
        /// </summary>
        [SerializeField] private CardData[] _firstBuildGuaranteed = new CardData[0];
        /// <summary>自動で組むデッキの役割ごとの枚数。合計はデッキの規定枚数（マップのデッキ編集の DeckEditConstants.MaxDeckSize）にそろえる</summary>
        [SerializeField][Min(0)] private int _autoDeckAttackCount = 10;
        [SerializeField][Min(0)] private int _autoDeckDefenseCount = 6;
        [SerializeField][Min(0)] private int _autoDeckSupportCount = 4;

        [Header("スコアとランク")]
        [SerializeField][Min(0)] private int _pointsPerToken = 1;
        /// <summary>1回の支援で得られるトークンより大きくしておく（わざと負けて稼ぐと必ず損をするように）</summary>
        [SerializeField][Min(0)] private int _penaltyPerBackupBoost = 1000;
        /// <summary>ランクごとの最低スコア。E, D, C, B, A, S, SS, SSS の順（上がるほど大きく）</summary>
        [SerializeField] private int[] _rankMinScores = { 0, 150, 350, 600, 900, 1200, 1600, 2000 };
        [SerializeField] private CampaignRank _bestRouteFrom = CampaignRank.S;
        [SerializeField] private CampaignRank _normalRouteFrom = CampaignRank.B;

        [Header("ユニット強化（次の1段の値段 = 基礎 + 増分 × 今の段）")]
        [SerializeField][Min(1)] private int _hpStep = 20;
        [SerializeField][Min(1)] private int _hpLimit = 300;
        [SerializeField][Min(0)] private int _hpBaseCost = 40;
        [SerializeField][Min(0)] private int _hpCostPerLevel = 10;
        [SerializeField][Min(1)] private int _memoryStep = 1;
        [SerializeField][Min(0)] private int _memoryLimit = 15;
        [SerializeField][Min(0)] private int _memoryBaseCost = 60;
        [SerializeField][Min(0)] private int _memoryCostPerLevel = 40;

        [Header("チップ")]
        /// <summary>全チップの値段にかける係数。値段は CardPackData の値段 × これ（四捨五入）</summary>
        [SerializeField][Min(0f)] private float _chipPriceMultiplier = 1f;

        [Header("シングル購入（コマンドを選んで1つずつ買う。チップより少し高め）")]
        [SerializeField][Min(0)] private int _singleNormalPrice = 40;
        [SerializeField][Min(0)] private int _singleRarePrice = 90;
        [SerializeField][Min(0)] private int _singleEpicPrice = 180;
        /// <summary>1種類のコマンドをキャンペーンを通して買える数</summary>
        [SerializeField][Min(0)] private int _singleLimitPerCommand = 3;

        /// <summary>エピソードの数（空欄の要素も数える。エピソード選択のボタンの数とは別）</summary>
        public int EpisodeCount => _episodes?.Length ?? 0;

        /// <summary>そのエピソード（0始まり）のステージの並び。範囲外・空欄なら null</summary>
        public CampaignData GetEpisode(int episodeIndex)
        {
            if (_episodes == null || episodeIndex < 0 || episodeIndex >= _episodes.Length) return null;
            return _episodes[episodeIndex];
        }

        /// <summary>そのエピソード（0始まり）に、遊べるステージがあるか</summary>
        public bool HasEpisode(int episodeIndex)
        {
            var episode = GetEpisode(episodeIndex);
            return episode != null && episode.CreateStages().Count > 0;
        }
        public int StartTokens => _startTokens;
        public int StartMaxHp => _startMaxHp;
        public int StartMaxMemory => _startMaxMemory;
        public CardData[] StartOwnedCards => _startOwnedCards ?? new CardData[0];
        public CardPackData FirstBuildChip => _firstBuildChip;
        public int FirstBuildChipCount => _firstBuildChipCount;
        public float ChipPriceMultiplier => _chipPriceMultiplier;

        public CampaignScoreRule CreateScoreRule()
        {
            return new CampaignScoreRule(_pointsPerToken, _penaltyPerBackupBoost, _rankMinScores, _bestRouteFrom, _normalRouteFrom);
        }

        public AutoDeckRule CreateAutoDeckRule()
        {
            return new AutoDeckRule(_autoDeckAttackCount, _autoDeckDefenseCount, _autoDeckSupportCount);
        }

        /// <summary>
        /// ファーストビルドのチップを開ける。出る数はチップの封入数 × 個数で、そのうち確定枠（_firstBuildGuaranteed）を先に入れ、残りを抽選する。
        /// チップが未設定なら空（呼び出し側はそのまま使える）
        /// </summary>
        public CardData[] OpenFirstBuild(System.Random random)
        {
            if (_firstBuildChip == null || _firstBuildChipCount <= 0) return new CardData[0];

            int total = _firstBuildChip.CardCount * _firstBuildChipCount;
            var cards = new List<CardData>(total);
            foreach (var card in _firstBuildGuaranteed ?? new CardData[0])
            {
                if (card == null || cards.Count >= total) continue;
                cards.Add(card);
            }
            cards.AddRange(_firstBuildChip.OpenCards(total - cards.Count, random));
            return cards.ToArray();
        }

        public SinglePurchaseRule CreateSinglePurchaseRule()
        {
            return new SinglePurchaseRule(_singleNormalPrice, _singleRarePrice, _singleEpicPrice, _singleLimitPerCommand);
        }

        public UnitUpgradeRule CreateUnitUpgradeRule()
        {
            return new UnitUpgradeRule(_hpStep, _hpLimit, _hpBaseCost, _hpCostPerLevel, _memoryStep, _memoryLimit, _memoryBaseCost, _memoryCostPerLevel);
        }

        // Inspector で壊れた値にしない: 閾値はランクの数だけ、E は 0、上のランクほど大きく
        private void OnValidate()
        {
            int rankCount = Enum.GetValues(typeof(CampaignRank)).Length;
            if (_rankMinScores == null || _rankMinScores.Length != rankCount)
            {
                var fixedScores = new int[rankCount];
                for (int i = 0; i < rankCount; i++) fixedScores[i] = _rankMinScores != null && i < _rankMinScores.Length ? _rankMinScores[i] : 0;
                _rankMinScores = fixedScores;
            }
            _rankMinScores[0] = 0;
            for (int i = 1; i < rankCount; i++)
            {
                if (_rankMinScores[i] <= _rankMinScores[i - 1]) _rankMinScores[i] = _rankMinScores[i - 1] + 1;
            }
            if (_normalRouteFrom > _bestRouteFrom) _normalRouteFrom = _bestRouteFrom;
        }
    }
}
