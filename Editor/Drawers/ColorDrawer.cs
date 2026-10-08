using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ColorAttribute))]
    public class ColorDrawer : PropertyDrawer, IChainedDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next) =>
            next.GetHeight(property, label);

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = ((ColorAttribute)attribute).backgroundColor;
            try
            {
                next.Draw(position, property, label);
            }
            finally
            {
                GUI.backgroundColor = old;
            }
        }
    }
}
