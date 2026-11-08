using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // エンドレスのゲームバランス。敵の候補と、階層が深くなるほど強くなる量、勝利の報酬をここに集める。
    // 開始時の持ち物・ファーストビルド・ユニット強化・マーケットは、ストーリーの CampaignBalance をそのまま使う。
    //
    // 各階層の中身（敵の編成・報酬のコマンド）は「シード + 階層」から毎回同じものを組む（CreateStage）。
    // そのため戦闘から戻っても、中断して再開しても、同じ階層は同じ編成になり、保存するのはシードだけでよい
    [CreateAssetMenu(fileName = "EndlessBalance", menuName = "Campaign/EndlessBalance")]
    public sealed class EndlessBalance : ScriptableObject
    {
        [Serializable]
        public sealed class EnemyEntry
        {
            [SerializeField] private EnemyData _enemy;
            /// <summary>この階層（1始まり）から候補に入る</summary>
            [SerializeField][Min(1)] private int _fromLayer = 1;
            /// <summary>1体だけで出てよいか（味方を回復する敵のように、単独では役目の無い敵は外す）</summary>
            [SerializeField] private bool _canAppearAlone = true;

            public EnemyData Enemy => _enemy;
            public int FromLayer => _fromLayer;
            public bool CanAppearAlone => _canAppearAlone;
        }

        [Header("敵の候補（各階層で、ここから乱数で選ぶ）")]
        [SerializeField] private EnemyEntry[] _enemies = new EnemyEntry[0];
        /// <summary>この階層（1始まり）から2体で出ることがある（戦闘画面に並べられるのは2体まで）</summary>
        [SerializeField][Min(1)] private int _twoEnemiesFromLayer = 10;
        [SerializeField][Range(0, 100)] private int _twoEnemiesChancePercent = 60;

        [Header("強さの上乗せ（この階層数ごとに1段上がる）")]
        [SerializeField][Min(1)] private int _layersPerTier = 10;
        /// <summary>1段ごとに敵の最大HPに足す割合（%）</summary>
        [SerializeField][Min(0)] private int _hpPercentPerTier = 25;
        /// <summary>1段ごとに敵の攻撃ダメージに足す値</summary>
        [SerializeField][Min(0)] private int _attackBonusPerTier = 2;

        [Header("勝利報酬（トークン = 基礎 + 階層ごとの増分 × (階層 - 1)。負けたらリザルトなので支援は無い）")]
        [SerializeField][Min(0)] private int _victoryTokensBase = 100;
        [SerializeField][Min(0)] private int _victoryTokensPerLayer = 10;
        /// <summary>報酬のコマンドを1個引くチップ。強さの段ごとに並べる（段が並びを超えたら最後のもの）。空欄なら報酬のコマンドは無し</summary>
        [SerializeField] private CardPackData[] _rewardChipsByTier = new CardPackData[0];

        public int LayersPerTier => _layersPerTier;

        /// <summary>その階層（0始まり）の強さの段（0 から）</summary>
        public int TierOf(int stageIndex)
        {
            return Mathf.Max(0, stageIndex) / Mathf.Max(1, _layersPerTier);
        }

        // 深い階層ほど大きくなる値は long で計算し、int の上限で止める（とても深い階層でも、あふれて小さく・マイナスにならないように）
        public EnemyStrength CreateStrength(int stageIndex)
        {
            long tier = TierOf(stageIndex);
            return new EnemyStrength(ClampToInt(100L + tier * _hpPercentPerTier), ClampToInt(tier * _attackBonusPerTier));
        }

        /// <summary>その階層（0始まり）の勝利のトークン</summary>
        public int VictoryTokensOf(int stageIndex)
        {
            return ClampToInt(_victoryTokensBase + (long)_victoryTokensPerLayer * Mathf.Max(0, stageIndex));
        }

        private static int ClampToInt(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            return value < 0 ? 0 : (int)value;
        }

        /// <summary>
        /// その階層（0始まり）の中身を組む。同じシードと階層なら、いつ呼んでも同じものになる。
        /// 候補の敵が1体もいなければ、敵のいない（すぐ勝つ）ステージになる
        /// </summary>
        public StagePlan CreateStage(int seed, int stageIndex)
        {
            var random = new System.Random(MixSeed(seed, stageIndex));
            // 階層は 1 始まり（進行は int の上限の1つ手前で止まるので、+1 してもあふれない）
            int layer = stageIndex < int.MaxValue ? stageIndex + 1 : stageIndex;

            int count = layer >= _twoEnemiesFromLayer && random.Next(100) < _twoEnemiesChancePercent ? 2 : 1;
            // 1体なら、単独で出てよい敵から選ぶ。2体なら誰でもよい
            var candidates = CandidatesFor(layer, aloneOnly: count == 1);
            var enemies = new List<EnemyData>(count);
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                enemies.Add(candidates[random.Next(candidates.Count)]);
            }

            var chip = RewardChipFor(stageIndex);
            var drawn = chip != null ? chip.OpenCards(1, random) : new CardData[0];
            var victory = new StageReward(true, VictoryTokensOf(stageIndex), drawn.Length > 0 ? drawn[0] : null);

            return new StagePlan(enemies.ToArray(), CreateStrength(stageIndex), victory, new StageReward(false, 0, null));
        }

        // その階層で出せる敵。条件に合うものが無ければ（設定の漏れ）、候補の全員から選ぶ
        private List<EnemyData> CandidatesFor(int layer, bool aloneOnly)
        {
            var all = new List<EnemyData>();
            var available = new List<EnemyData>();
            foreach (var entry in _enemies ?? new EnemyEntry[0])
            {
                if (entry == null || entry.Enemy == null) continue;
                all.Add(entry.Enemy);
                if (entry.FromLayer <= layer && (!aloneOnly || entry.CanAppearAlone)) available.Add(entry.Enemy);
            }
            return available.Count > 0 ? available : all;
        }

        private CardPackData RewardChipFor(int stageIndex)
        {
            if (_rewardChipsByTier == null || _rewardChipsByTier.Length == 0) return null;
            return _rewardChipsByTier[Mathf.Min(TierOf(stageIndex), _rewardChipsByTier.Length - 1)];
        }

        // シードと階層を1つの乱数の種にまとめる（隣り合う階層で似た並びにならないように混ぜる）
        private static int MixSeed(int seed, int stageIndex)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= (uint)stageIndex * 2246822519u;
                h ^= h >> 15;
                h *= 3266489917u;
                h ^= h >> 13;
                return (int)h;
            }
        }
    }
}
