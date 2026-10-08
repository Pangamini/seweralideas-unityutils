using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(FlattenAttribute))]
    public class FlattenDrawer : PropertyDrawer, IChainedDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.Draw(position, TryGetOnlyChild(property, out SerializedProperty child) ? child : property, label);

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.GetHeight(TryGetOnlyChild(property, out SerializedProperty child) ? child : property, label);

        private static bool TryGetOnlyChild(SerializedProperty property, out SerializedProperty child)
        {
            child = null;
            if (property.propertyType != SerializedPropertyType.Generic || property.isArray)
                return false;

            SerializedProperty end = property.GetEndProperty();
            SerializedProperty it = property.Copy();
            if (!it.NextVisible(true) || SerializedProperty.EqualContents(it, end))
                return false;

            SerializedProperty first = it.Copy();
            if (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end))
                return false;

            child = first;
            return true;
        }
    }
}
