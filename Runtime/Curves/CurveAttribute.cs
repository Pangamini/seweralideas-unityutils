using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    public class CurveAttribute : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;

        public CurveAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
