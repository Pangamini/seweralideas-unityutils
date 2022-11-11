using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    public class CurveAttribute : PropertyAttribute
    {
        public float min;
        public float max;
        public float displayHeight;

        public CurveAttribute(float min, float max, float displayHeight)
        {
            this.min = min;
            this.max = max;
            this.displayHeight = displayHeight;
        }
    
        public CurveAttribute(float min, float max) : this(min, max, EditorGUIUtility.singleLineHeight * 4f)
        {
        }
    }
}
