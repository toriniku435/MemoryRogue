using System;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using UnityEditor;

namespace Live2DCardRogue.Editor
{
    // [SerializeReference] な IEnemyCardPicker フィールドに、実装型を選ぶドロップダウンを追加する
    [CustomPropertyDrawer(typeof(IEnemyCardPicker), true)]
    public sealed class EnemyCardPickerDrawer : SerializeReferenceDrawerBase
    {
        protected override Type BaseType => typeof(IEnemyCardPicker);
    }
}
