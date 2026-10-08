using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer, IChainedDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).GetHeight(property, label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawerChain.Start(this).Draw(position, property, label);

        float IChainedDrawer.GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next)
        {
            var height = EditorGUIUtility.singleLineHeight;
            var attr = (ButtonAttribute)attribute;
            if (attr.showOriginal)
                height += next.GetHeight(property, label);
            return height;
        }

        void IChainedDrawer.OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next)
        {
            var attr = (ButtonAttribute)attribute;

            var lh = EditorGUIUtility.singleLineHeight;
            var buttonsPos = new Rect(position.x, position.y, position.width, lh);
            var origPosition = new Rect(position.x, position.y + lh, position.width, position.height - lh);

            // Disabled on top of whatever is disabled around it, never enabled against it.
            using (new EditorGUI.DisabledScope(!(Application.isPlaying ? attr.player : attr.editor)))
                DrawButtons(buttonsPos, property, attr);

            if (attr.showOriginal)
                next.Draw(origPosition, property, label);
        }

        private static void DrawButtons(Rect buttonsPos, SerializedProperty property, ButtonAttribute attr)
        {
            var count = System.Math.Min(attr.m_labels.Length, attr.m_methods.Length);
            var width = buttonsPos.width / count;
            for (int i = 0; i < count; ++i)
            {
                string style;
                if (count == 1)
                    style = "Button";
                else if (i == 0)
                    style = "ButtonLeft";
                else if (i == count - 1)
                    style = "ButtonRight";
                else
                    style = "ButtonMid";

                var pos = new Rect(buttonsPos.x + width * i, buttonsPos.y, width, buttonsPos.height);
                if (GUI.Button(pos, attr.m_labels[i], style))
                {
                    var objs = property.serializedObject.targetObjects;
                    foreach (var obj in objs)
                    {
                        var method = obj.GetType().GetMethod(attr.m_methods[i],
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, null,
                            new System.Type[0], null);
                        if (method != null)
                            method.Invoke(obj, null);
                        else
                            Debug.LogError("Cannot find method called " + attr.m_methods[i]);
                    }
                }
            }
        }
    }
}
