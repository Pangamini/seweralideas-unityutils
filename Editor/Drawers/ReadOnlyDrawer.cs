using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer, IChainedDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.GetHeight(property, label);

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next)
        {
            using (new EditorGUI.DisabledScope(true))
                next.Draw(position, property, label);
        }
    }
}
