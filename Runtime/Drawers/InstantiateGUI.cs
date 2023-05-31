using System;
using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    public class InstantiateGUI : PropertyAttribute
    {
        public InstantiateGUI(Type baseType)
        {
            BaseType = baseType;
        }

        public Type BaseType { get; }
    }
}
