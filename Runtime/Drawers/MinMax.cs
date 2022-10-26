using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    public class MinMaxSliderAttribute : PropertyAttribute
    {

        public readonly float max;
        public readonly float min;

        public MinMaxSliderAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}