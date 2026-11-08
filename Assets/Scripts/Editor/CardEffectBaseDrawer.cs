using System;
using Live2DCardRogue.Common.Models.Cards;
using UnityEditor;

namespace Live2DCardRogue.Editor
{
    // [SerializeReference] な CardEffectBase フィールドに、派生型を選ぶドロップダウンを追加する
    [CustomPropertyDrawer(typeof(CardEffectBase), true)]
    public sealed class CardEffectBaseDrawer : SerializeReferenceDrawerBase
    {
        protected override Type BaseType => typeof(CardEffectBase);
    }
}
