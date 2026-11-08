using System;
using Live2DCardRogue.Common.Statuses;
using UnityEditor;

namespace Live2DCardRogue.Editor
{
    // [SerializeReference] な StatusEffectDefinitionBase フィールドに、派生型を選ぶドロップダウンを追加する
    [CustomPropertyDrawer(typeof(StatusEffectDefinitionBase), true)]
    public sealed class StatusEffectDefinitionDrawer : SerializeReferenceDrawerBase
    {
        protected override Type BaseType => typeof(StatusEffectDefinitionBase);
    }
}
