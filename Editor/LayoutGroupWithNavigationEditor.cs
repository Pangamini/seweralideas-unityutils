using UnityEditor;
using UnityEditor.UI;
using SeweralIdeas.UnityUtils.UI;

namespace SeweralIdeas.UnityUtils.Editor
{
    public abstract class LayoutGroupWithNavigationEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _navigateA;
        private SerializedProperty _navigateB;
        private SerializedProperty _inheritFrom;

        protected abstract string NavigateAPropertyName { get; }
        protected abstract string NavigateBPropertyName { get; }

        protected override void OnEnable()
        {
            base.OnEnable();
            _navigateA = serializedObject.FindProperty(NavigateAPropertyName);
            _navigateB = serializedObject.FindProperty(NavigateBPropertyName);
            _inheritFrom = serializedObject.FindProperty("_inheritFrom");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            // (With a selectable to inherit from, the two explicit ones are hidden by their [Condition])
            EditorGUILayout.PropertyField(_inheritFrom);
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
