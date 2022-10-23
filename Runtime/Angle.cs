using UnityEngine;

namespace SeweralIdeas.Drawers
{
    public class AngleAttribute : PropertyAttribute
    {
        public bool radians;
        public AngleAttribute(bool radians)
        {
            this.radians = radians;
        }
    }
}