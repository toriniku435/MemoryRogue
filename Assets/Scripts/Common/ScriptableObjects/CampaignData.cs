using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Conversation;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // キャンペーン1本分のステージ並び。マップのマス数はこのアセットの要素数で決まるので、
    // ステージを増減したいときはこのアセットを編集するだけでよい（コード側に固定の段数は無い）
    [CreateAssetMenu(fileName = "NewCampaignData", menuName = "Campaign/CampaignData")]
    public class CampaignData : ScriptableObject
    {
        /// <summary>1マス目から順に並べる</summary>
        [SerializeField] private StageData[] _stages;

        [Header("エピソードを始めてマップに着いた直後に流す会話（チュートリアルの確認の前）。空欄なら流さない")]
        [SerializeField] private ConversationData _openingConversation;

        /// <summary>エピソードを始めてマップに着いた直後に流す会話（無ければ null）</summary>
        public ConversationData OpeningConversation => _openingConversation;

        /// <summary>
        /// 踏破する順のステージ列。Inspectorで空欄のまま残された要素は落とすので、
        /// 呼び出し側はnullチェック無しでそのまま使える（マスの数もこの結果の長さで決める）
        /// </summary>
        public IReadOnlyList<StageData> CreateStages()
        {
            var stages = new List<StageData>();
            if (_stages == null) return stages;

            foreach (var stage in _stages)
            {
                if (stage != null) stages.Add(stage);
            }

            return stages;
        }
    }
}
