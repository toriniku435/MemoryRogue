using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Conversation;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // キャンペーンの1ステージ分。「誰と戦うか」と、勝敗ごとに受け取るもの。
    // CardData/EnemyDataと同じく、フィールドは全てprivateで読み取り専用アクセサ経由
    [CreateAssetMenu(fileName = "NewStageData", menuName = "Campaign/StageData")]
    public class StageData : ScriptableObject
    {
        [SerializeField] private string _id;

        /// <summary>手前から順に並べる。この並びが敵の行動順になる</summary>
        [SerializeField] private EnemyData[] _enemies;

        [Header("勝利報酬（トークンとカード1枚）")]
        [SerializeField][Min(0)] private int _victoryTokens;
        [SerializeField] private CardData _victoryCard;

        [Header("敗北時の支援（トークンのみ。勝利時の2倍程度が目安）")]
        [SerializeField][Min(0)] private int _defeatTokens;

        [Header("敵の強さの上乗せ（ストーリー後半を少し強くする。100% / 0 で上乗せ無し）")]
        [SerializeField][Min(1)] private int _enemyHpPercent = 100;
        [SerializeField][Min(0)] private int _enemyAttackBonus;

        [Header("勝ってマップに戻った直後に流す会話（報酬の前）。空欄なら流さない")]
        [SerializeField] private ConversationData _clearConversation;

        [Header("戦闘中に流す BGM（ボス戦なら BossBattle）")]
        [SerializeField] private BgmId _bgm = BgmId.Battle;

        public string Id => _id;

        /// <summary>勝ってマップに戻った直後に流す会話（無ければ null）</summary>
        public ConversationData ClearConversation => _clearConversation;

        /// <summary>
        /// この結果で受け取るもの。勝てばトークンとカード、負ければ支援のトークンだけ。
        /// カードが空欄のままなら、勝ってもカードは付かない（null）
        /// </summary>
        public StageReward CreateReward(bool isVictory)
        {
            return isVictory
                ? new StageReward(true, _victoryTokens, _victoryCard)
                : new StageReward(false, _defeatTokens, null);
        }

        /// <summary>このステージの敵の強さの上乗せ（Inspector の値。100% / 0 なら EnemyStrength.None）</summary>
        public EnemyStrength Strength => _enemyHpPercent == 100 && _enemyAttackBonus == 0
            ? EnemyStrength.None
            : new EnemyStrength(_enemyHpPercent, _enemyAttackBonus);

        /// <summary>マップが見る形にする</summary>
        public StagePlan CreatePlan()
        {
            return new StagePlan(CreateEnemyLineup(), Strength, CreateReward(true), CreateReward(false), _clearConversation, _bgm);
        }

        /// <summary>
        /// このステージの敵編成。Inspectorで空欄のまま残された要素は落とすので、
        /// 呼び出し側はnullチェック無しでそのまま使える
        /// </summary>
        public EnemyData[] CreateEnemyLineup()
        {
            if (_enemies == null) return new EnemyData[0];

            var lineup = new List<EnemyData>(_enemies.Length);
            foreach (var enemy in _enemies)
            {
                if (enemy != null) lineup.Add(enemy);
            }

            return lineup.ToArray();
        }
    }
}
