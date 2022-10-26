using UnityEngine;
namespace SeweralIdeas.UnityUtils.Drawers
{
    public class BitMaskAttribute : PropertyAttribute
    {
        public System.Type enumType;
        public bool isFlagsEnum;
        public BitMaskAttribute(System.Type enumType, bool isFlagsEnum = true)
        {
            this.enumType = enumType;
            this.isFlagsEnum = isFlagsEnum;
        }
    }
}