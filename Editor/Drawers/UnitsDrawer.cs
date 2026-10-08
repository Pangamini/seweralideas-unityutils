using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(UnitsAttribute))]
    public class UnitsDrawer : PropertyDrawer, IChainedDrawer
    {
        private const float LabelWidth = 64;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.GetHeight(property, label);

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next)
        {
            var unitsAttribute = (UnitsAttribute)attribute;
            Rect propertyRect = new Rect(position.x, position.y, position.width - LabelWidth, position.height);
            Rect labelRect = new Rect(position.xMax - LabelWidth, position.y, LabelWidth, EditorGUIUtility.singleLineHeight);
            next.Draw(propertyRect, property, label);
            GUI.Label(labelRect, unitsAttribute.units);
        }
    }
}
