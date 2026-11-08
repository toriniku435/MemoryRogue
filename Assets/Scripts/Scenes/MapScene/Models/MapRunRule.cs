using System.Collections.Generic;
using Live2DCardRogue.Common.Conversation;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.MapScene.Models
{
    // マップに差し込むルール。ストーリーとチャレンジで違うのは
    //   - 部屋の数（決まっている / 終わりが無い。チャレンジは種類ごとに 30・60・100 階層か、終わり無し）
    //   - 各部屋の中身（アセットの StageData / シードと階層から組む）
    //   - 物語（会話・チュートリアル・エンディング）があるか。物語の無いモードは、負けたらマップへは戻らずリザルトへ進む
    // だけなので、それをここに閉じ込め、マップの画面・ウインドウ・保存の流れは共用する
    public abstract class MapRunRule
    {
        /// <summary>部屋の数。終わりが無ければ null</summary>
        public abstract int? StageCount { get; }

        /// <summary>
        /// 物語（会話・チュートリアル・エンディング・バックアップブースト）があるか。
        /// 無いモード（チャレンジ）は、一度負けたらマップへは戻らずリザルトへ進み、踏破してもリザルトへ進む
        /// </summary>
        public abstract bool HasStory { get; }

        /// <summary>エピソードの始まりに流す会話（無ければ null）</summary>
        public abstract ConversationData OpeningConversation { get; }

        /// <summary>その部屋（0始まり）の中身。範囲外なら null</summary>
        public abstract StagePlan GetStage(int stageIndex);

        /// <summary>一度負けたら終わりか（マップへは戻らずリザルトへ）</summary>
        public bool EndsOnDefeat => !HasStory;

        /// <summary>保存して、タイトルの再開の確認に出すための部屋の数（終わりが無ければ 0）</summary>
        public int SavedStageCount => StageCount ?? 0;

        /// <summary>全部の部屋を踏破したか（終わりが無ければ常に false）</summary>
        public bool IsCompleted(int stageIndex)
        {
            return StageCount.HasValue && stageIndex >= StageCount.Value;
        }

        /// <summary>その部屋があるか</summary>
        public bool HasStage(int stageIndex)
        {
            return stageIndex >= 0 && !IsCompleted(stageIndex);
        }

        /// <summary>今の深さの表示（LAYER 03 / 10、終わりが無ければ LAYER 37）</summary>
        public string LayerText(int stageIndex)
        {
            // 1始まりにする +1 は long で（int の上限付近でもあふれないように）
            long next = (long)stageIndex + 1;
            if (!StageCount.HasValue) return $"LAYER {next:00}";
            int count = StageCount.Value;
            long layer = next < count ? next : count;
            return $"LAYER {layer:00} / {count:00}";
        }
    }

    // ストーリー: エピソードのステージ（アセット）を順に踏破する
    public sealed class CampaignMapRule : MapRunRule
    {
        private readonly IReadOnlyList<StageData> _stages;
        private readonly ConversationData _openingConversation;

        public CampaignMapRule(IReadOnlyList<StageData> stages, ConversationData openingConversation)
        {
            _stages = stages ?? new StageData[0];
            _openingConversation = openingConversation;
        }

        public override int? StageCount => _stages.Count;

        public override bool HasStory => true;

        public override ConversationData OpeningConversation => _openingConversation;

        public override StagePlan GetStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Count) return null;
            return _stages[stageIndex].CreatePlan();
        }
    }

    // チャレンジ（ノーマル / ハード / マスター / エンドレス）: 各階層の敵をシードから乱数で組み、10階層ごとに強くなる。
    // 階層数が決まっていれば（0 より大きければ）そこで踏破、0 なら負けるまで終わらない（エンドレス）
    public sealed class ChallengeMapRule : MapRunRule
    {
        private readonly EndlessBalance _balance;
        private readonly int _seed;
        private readonly int _layerCount;
        // 同じ階層は同じ中身になるので、一度組んだものは使い回す（報酬のコマンドの抽選をやり直さない）
        private readonly Dictionary<int, StagePlan> _cache = new Dictionary<int, StagePlan>();

        /// <param name="layerCount">階層数。0 なら終わりが無い</param>
        public ChallengeMapRule(EndlessBalance balance, int seed, int layerCount)
        {
            _balance = balance;
            _seed = seed;
            _layerCount = layerCount < 0 ? 0 : layerCount;
        }

        public override int? StageCount => _layerCount > 0 ? _layerCount : (int?)null;

        public override bool HasStory => false;

        public override ConversationData OpeningConversation => null;

        public override StagePlan GetStage(int stageIndex)
        {
            if (stageIndex < 0 || _balance == null || IsCompleted(stageIndex)) return null;
            if (!_cache.TryGetValue(stageIndex, out var plan))
            {
                plan = _balance.CreateStage(_seed, stageIndex);
                _cache.Add(stageIndex, plan);
            }
            return plan;
        }
    }
}
