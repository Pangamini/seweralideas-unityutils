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
        public string displayValue;

        public CurveAttribute(float min, float max, float displayHeight, string xFormat = "N2", string yFormat = "N2", string displayValue = null)
        {
            this.min = min;
            this.max = max;
            this.displayHeight = displayHeight;
            this.xFormat = xFormat;
            this.yFormat = yFormat;
            this.displayValue = displayValue;
        }
    
        public CurveAttribute(float min, float max, string xFormat = "N2", string yFormat = "N2", string displayValue = null) 
            : this(min, max, 128, xFormat, yFormat, displayValue) { }
    }
}
