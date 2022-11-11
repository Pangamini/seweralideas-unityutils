using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    public class CurveAttribute : PropertyAttribute
    {
        public float min;
        public float max;
        public float displayHeight;
        public string xFormat;
        public string yFormat;

        public CurveAttribute(float min, float max, float displayHeight, string xFormat = "N2", string yFormat = "N2")
        {
            this.min = min;
            this.max = max;
            this.displayHeight = displayHeight;
            this.xFormat = xFormat;
            this.yFormat = yFormat;
        }
    
        public CurveAttribute(float min, float max, string xFormat = "N2", string yFormat = "N2") : this(min, max, EditorGUIUtility.singleLineHeight * 4f, xFormat, yFormat)
        {
        }
    }
}
