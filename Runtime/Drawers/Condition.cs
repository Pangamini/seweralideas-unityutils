using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    public class ConditionAttribute : PropertyAttribute
    {
        public string Condition;
        public bool   Invert;

        public ConditionAttribute(string value)
        {
            Condition = value;
        }
    }
}