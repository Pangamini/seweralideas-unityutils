using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    public class UnitsAttribute : PropertyAttribute
    {
        public string units;

        public UnitsAttribute(string units)
        {
            this.units = units;
        }
    }
}
