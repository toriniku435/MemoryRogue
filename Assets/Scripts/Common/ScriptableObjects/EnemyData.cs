using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField][Min(1)] private int _maxHp;

        /// <summary>バトル画面の立ち絵。未設定ならシーンに置いてある絵がそのまま使われる</summary>
        [SerializeField] private Sprite _portrait;

        /// <summary>バトル画面で動く見た目（アイドル・行動・被弾）。未設定なら _portrait の1枚絵のまま</summary>
        [SerializeField] private UnitVisualDefinition _visual;

        // 敵はスタミナを持たず、1ターンの行動回数で行動量を決める
        [SerializeField][Min(0)] private int _minActionCount;
        [SerializeField][Min(0)] private int _maxActionCount;

        [SerializeField] private CardData[] _cards;
        [SerializeReference] private IEnemyCardPicker _cardPicker;

        // 戦闘開始時から持っている状態効果。
        [SerializeReference] private StatusEffectDefinitionBase[] _initialStatusEffectDefinitions;

        public string Id => _id;
        public int MaxHp => _maxHp;
        public Sprite Portrait => _portrait;
        public UnitVisualDefinition Visual => _visual;
        public int MinActionCount => _minActionCount;
        public int MaxActionCount => _maxActionCount;
        public IReadOnlyList<CardData> Cards => _cards;

        // 戦闘開始時に付与する状態効果を生成する。未設定でもnullを返さない。
        // 毎回生成するので、戦闘中の値の変化がアセット側に残ることはない
        public IReadOnlyList<IStatusEffect> CreateInitialStatusEffects()
        {
            if (_initialStatusEffectDefinitions == null)
            {
                return Array.Empty<IStatusEffect>();
            }

            var effects = new List<IStatusEffect>();
            foreach (var definition in _initialStatusEffectDefinitions)
            {
                if (definition != null)
                {
                    effects.Add(definition.Create());
                }
            }

            return effects;
        }

        // 1ターン分の行動を実行順に並べて返す。Pickerが未設定でも呼び出し側がそのままforeachできるよう、空を返す
        public IReadOnlyList<CardData> PickCards(EnemyPickContext context = default)
        {
            if (_cardPicker == null || _cards == null)
            {
                return Array.Empty<CardData>();
            }

            // Inspectorで空欄のままの要素をPicker側に持ち込ませない
            var candidates = new List<CardData>();
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    candidates.Add(card);
                }
            }

            // min > max のまま保存されていても各Pickerが破綻しないよう、ここで整えてから渡す
            int min = Mathf.Min(_minActionCount, _maxActionCount);
            return _cardPicker.Pick(candidates, min, _maxActionCount, context) ?? Array.Empty<CardData>();
        }

        private void OnValidate()
        {
            // Inspector上でmin > maxにならないように寄せる
            if (_maxActionCount < _minActionCount)
            {
                _maxActionCount = _minActionCount;
            }
        }
    }
}
