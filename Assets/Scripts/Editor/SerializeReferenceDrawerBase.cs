using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Live2DCardRogue.Editor
{
    // [SerializeReference] フィールドに、選べる具象型のドロップダウンを出すドロワーの共通実装。
    // 対象の基底型ごとに [CustomPropertyDrawer] を付けた薄いサブクラスを作って BaseType を返すだけでよい
    // (Unityのドロワー解決はフィールドの宣言型で行われるため、基底型1つにつき1クラス必要)。
    public abstract class SerializeReferenceDrawerBase : PropertyDrawer
    {
        private const string NoneLabel = "(未設定)";

        // ドロップダウンに並べる具象型の基底となる型(抽象クラスでもインターフェースでもよい)
        protected abstract Type BaseType { get; }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(label.text + ": " + GetTypeDisplayName(property)), FocusType.Keyboard))
            {
                ShowTypeMenu(property);
            }

            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;

                float y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                foreach (var child in EnumerateChildren(property))
                {
                    float childHeight = EditorGUI.GetPropertyHeight(child, true);
                    var childRect = new Rect(position.x, y, position.width, childHeight);
                    EditorGUI.PropertyField(childRect, child, true);
                    y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                foreach (var child in EnumerateChildren(property))
                {
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
                }
            }

            return height;
        }

        // マネージド参照が選択されている型の直下フィールドを、余計な折りたたみ無しで列挙する
        private static IEnumerable<SerializedProperty> EnumerateChildren(SerializedProperty property)
        {
            var endProperty = property.GetEndProperty();
            var child = property.Copy();

            if (!child.NextVisible(true)) yield break;

            while (!SerializedProperty.EqualContents(child, endProperty))
            {
                yield return child.Copy();
                if (!child.NextVisible(false)) break;
            }
        }

        private static string GetTypeDisplayName(SerializedProperty property)
        {
            string typeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(typeName)) return NoneLabel;

            int lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
        }

        private void ShowTypeMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            string currentTypeName = GetTypeDisplayName(property);

            menu.AddItem(new GUIContent(NoneLabel), currentTypeName == NoneLabel, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            var concreteTypes = TypeCache.GetTypesDerivedFrom(BaseType)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name);

            foreach (var type in concreteTypes)
            {
                var selectedType = type;
                menu.AddItem(new GUIContent(selectedType.Name), selectedType.Name == currentTypeName, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(selectedType);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
