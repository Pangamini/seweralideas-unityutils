using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Drawers
{
    public class EnumMapAttribute : PropertyAttribute
    {
        public System.Type enumType;
        public EnumMapAttribute(System.Type enumType)
        {
            this.enumType = enumType;
        }
    }
}