using UnityEditor;
using UnityEditor.UI;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Editor
{
    // CommonButton は uGUI の Button の Inspector をそのまま使い、その下に操作音の欄だけを足す
    [CustomEditor(typeof(CommonButton), true)]
    [CanEditMultipleObjects]
    public sealed class CommonButtonEditor : ButtonEditor
    {
        private SerializedProperty _clickSe;
        private SerializedProperty _hoverSe;

        protected override void OnEnable()
        {
            base.OnEnable();
            _clickSe = serializedObject.FindProperty("_clickSe");
            _hoverSe = serializedObject.FindProperty("_hoverSe");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("操作音", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_clickSe);
            EditorGUILayout.PropertyField(_hoverSe);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
