using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(OverrideValueAttribute))]
    public class OverrideValueDrawer : PropertyDrawer, IChainedDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            OverrideDrawer.Draw(position, property, label, (rect, valueProp) => next.Draw(rect, valueProp, GUIContent.none));

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.GetHeight(property.FindPropertyRelative("_value"), label);
    }
}
