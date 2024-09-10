using System;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomPropertyDrawer(typeof(SerializableGuid))]
    public class SerializableGuidDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var aProp = property.FindPropertyRelative("m_a");
            var bProp = property.FindPropertyRelative("m_b");
            var cProp = property.FindPropertyRelative("m_c");
            var dProp = property.FindPropertyRelative("m_d");
            var eProp = property.FindPropertyRelative("m_e");
            var fProp = property.FindPropertyRelative("m_f");
            var gProp = property.FindPropertyRelative("m_g");
            var hProp = property.FindPropertyRelative("m_h");
            var iProp = property.FindPropertyRelative("m_i");
            var jProp = property.FindPropertyRelative("m_j");
            var kProp = property.FindPropertyRelative("m_k");
            
            var guid = new SerializableGuid(
                aProp.intValue, 
                (short)bProp.intValue, 
                (short)cProp.intValue, 
                (byte)dProp.intValue, 
                (byte)eProp.intValue, 
                (byte)fProp.intValue, 
                (byte)gProp.intValue, 
                (byte)hProp.intValue, 
                (byte)iProp.intValue, 
                (byte)jProp.intValue, 
                (byte)kProp.intValue);

            string oldText = guid.ToString();
            string newText = EditorGUI.DelayedTextField(position, label, oldText);
            if(!string.Equals(oldText, newText, StringComparison.Ordinal))
            {
                if(SerializableGuid.TryParse(newText, out guid))
                {
                    aProp.intValue = guid.A;
                    bProp.intValue = guid.B;
                    cProp.intValue = guid.C;
                    dProp.intValue = guid.D;
                    eProp.intValue = guid.E;
                    fProp.intValue = guid.F;
                    gProp.intValue = guid.G;
                    hProp.intValue = guid.H;
                    iProp.intValue = guid.I;
                    jProp.intValue = guid.J;
                    kProp.intValue = guid.K;
                }
            }
        }
    }
}
