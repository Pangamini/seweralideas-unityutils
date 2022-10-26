using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    public class ConditionAttribute : PropertyAttribute
    {
        public string condition;
        public bool invert;

        public ConditionAttribute(string value)
        {
            condition = value;
        }
    }
}