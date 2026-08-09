using UnityEditor;
using UnityEditor.UI;
using SeweralIdeas.UnityUtils.UI;

namespace SeweralIdeas.UnityUtils.Editor
{
    public abstract class LayoutGroupWithNavigationEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _navigateA;
        private SerializedProperty _navigateB;

        protected abstract string NavigateAPropertyName { get; }
        protected abstract string NavigateBPropertyName { get; }

        protected override void OnEnable()
        {
            base.OnEnable();
            _navigateA = serializedObject.FindProperty(NavigateAPropertyName);
            _navigateB = serializedObject.FindProperty(NavigateBPropertyName);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_navigateA);
            EditorGUILayout.PropertyField(_navigateB);
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HorizontalLayoutGroupWithNavigation), true)]
    [CanEditMultipleObjects]
    public class HorizontalLayoutGroupWithNavigationEditor : LayoutGroupWithNavigationEditor
    {
        protected override string NavigateAPropertyName => "_navigateUp";
        protected override string NavigateBPropertyName => "_navigateDown";
    }

    [CustomEditor(typeof(VerticalLayoutGroupWithNavigation), true)]
    [CanEditMultipleObjects]
    public class VerticalLayoutGroupWithNavigationEditor : LayoutGroupWithNavigationEditor
    {
        protected override string NavigateAPropertyName => "_navigateLeft";
        protected override string NavigateBPropertyName => "_navigateRight";
    }
}
