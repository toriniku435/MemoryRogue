using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models.Cards;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/CardData")]
    public class CardData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField][Min(0)] private int _cost;

        /// <summary>パックから出る当たりやすさに使う。重み付けはパック側(CardPackData)が持つ</summary>
        [SerializeField] private CardRarity _rarity;
        [SerializeReference] private CardEffectBase[] _effects;

        /// <summary>カード中央の窓に出す絵。未設定なら窓は空のまま表示される</summary>
        [SerializeField] private Sprite _illustration;

        /// <summary>使用後、捨て札ではなくパージ（除外）される。この戦闘の間は山札に戻ってこない</summary>
        [SerializeField] private bool _purgeOnUse;

        /// <summary>デッキに入れられる枚数の上限。0 なら上限なし（所持している分だけ入れられる）</summary>
        [SerializeField][Min(0)] private int _maxCopiesInDeck;


        public string Id => _id;
        public int Cost => _cost;
        public CardRarity Rarity => _rarity;
        public CardEffectBase[] Effects => _effects;
        public bool PurgeOnUse => _purgeOnUse;
        public int MaxCopiesInDeck => _maxCopiesInDeck;

        /// <summary>
        /// 説明文（Smart String）に差し込む数値。各効果が名前付きで足したものをまとめる（{damage} など）。
        /// 数値を調整すれば説明文も追従するので、説明文に数字を直接書かない（CardDescriptionTemplateTests が見張る）
        /// </summary>
        public Dictionary<string, object> CreateDescriptionValues()
        {
            var values = new Dictionary<string, object>();
            if (_effects == null) return values;
            foreach (var effect in _effects)
            {
                effect?.AddDescriptionValues(values);
            }
            return values;
        }
        public Sprite Illustration => _illustration;

        /// <summary>
        /// 敵が使うときに、味方の中から一番弱っている1体を対象にするカードかどうか（味方回復）。
        /// 1つでもそういう効果を持っていれば true。null の要素は無視する
        /// </summary>
        /// <summary>
        /// 積むときにパージするバッファ（手札）を何個まで選ぶか（バッファオーバーフロー）。そういう効果が無ければ 0
        /// </summary>
        public int HandPurgeCount
        {
            get
            {
                int count = 0;
                if (_effects == null) return count;

                foreach (var effect in _effects)
                {
                    if (effect is IHandPurgeCardEffect handPurge && handPurge.HandPurgeCount > count) count = handPurge.HandPurgeCount;
                }

                return count;
            }
        }

        /// <summary>
        /// 積むときに払うトークン（クラウドバースト）。そういう効果が無ければ 0。払えるかの判定と支払いは BattleCommandQueueUseCase / BattleCommandExecuteUseCase
        /// </summary>
        public int TokenCost
        {
            get
            {
                int cost = 0;
                if (_effects == null) return cost;
                foreach (var effect in _effects)
                {
                    if (effect is ITokenCostCardEffect token) cost += token.TokenCost;
                }
                return cost;
            }
        }

        /// <summary>一つ前のコマンドと同じ効果を出すコマンド（リドゥ）か</summary>
        public bool RepeatsPrevious
        {
            get
            {
                if (_effects == null) return false;
                foreach (var effect in _effects)
                {
                    if (effect is RedoCardEffect) return true;
                }
                return false;
            }
        }

        public bool ChoosesAlly
        {
            get
            {
                if (_effects == null) return false;

                foreach (var effect in _effects)
                {
                    if (effect != null && effect.ChoosesAlly) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 相手側のユニットを対象に選ぶカードかどうか（false なら使用者自身を選ぶ）。
        /// 1つでも相手に作用する効果を持っていれば相手を選ぶカード。
        /// Inspectorで空欄のまま残された要素があり得るので、null は無視する
        /// </summary>
        public bool TargetsOpponent
        {
            get
            {
                if (_effects == null) return false;

                foreach (var effect in _effects)
                {
                    if (effect != null && effect.TargetsOpponent) return true;
                }

                return false;
            }
        }
    }
}
