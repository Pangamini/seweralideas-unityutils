using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Drawers
{
    public class ColorAttribute : PropertyAttribute
    {
        public readonly Color backgroundColor;

        public ColorAttribute(string background)
        {
            ColorUtility.TryParseHtmlString(background, out backgroundColor);
        }
    }
}