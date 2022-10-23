using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SeweralIdeas.Drawers.Editor
{
    public static class EditorExtension
    {
        public static int DrawBitMaskField(Rect position, int mask, System.Type enumType, bool isFlagsEnum, GUIContent label)
        {
            var itemNames = System.Enum.GetNames(enumType);
            var _itemValues = System.Enum.GetValues(enumType);

            var itemValues = new int[_itemValues.Length];
            for (int i = 0; i < itemValues.Length; ++i)
            {
                int _val = (int)System.Convert.ToInt32(_itemValues.GetValue(i));
                itemValues[i] = isFlagsEnum ? _val : 1 << _val;
            }

            int val = mask;
            int maskVal = 0;
            for (int i = 0; i < itemValues.Length; i++)
            {
                var arrVal = itemValues[i];
                if (arrVal != 0)
                {
                    if ((val & arrVal) == arrVal)
                        maskVal |= 1 << i;
                }
                else if (arrVal == 0)
                    maskVal |= 1 << i;
            }

            int newMaskVal = EditorGUI.MaskField(position, label, maskVal, itemNames);
            int changes = maskVal ^ newMaskVal;

            for (int i = 0; i < itemValues.Length; i++)
            {
                if ((changes & (1 << i)) != 0)            // has this list item changed?
                {
                    var arrVal = itemValues[i];
                    if ((newMaskVal & (1 << i)) != 0)     // has it been set?
                    {
                        if (arrVal == 0)           // special case: if "0" is set, just set the val to 0
                        {
                            val = 0;
                            break;
                        }
                        else
                            val |= arrVal;
                    }
                    else                                  // it has been reset
                    {
                        val &= ~arrVal;
                    }
                }
            }
            return val;
        }
    }

    [CustomPropertyDrawer(typeof(BitMaskAttribute))]
    public class EnumBitMaskPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var typeAttr = attribute as BitMaskAttribute;
            // Add the actual int value behind the field name
            prop.intValue = EditorExtension.DrawBitMaskField(position, prop.intValue, typeAttr.enumType, typeAttr.isFlagsEnum, label);
        }
    }
}