using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    public class ButtonAttribute : PropertyAttribute
    {
        public string[] m_labels;
        public string[] m_methods;
        public bool editor = true;
        public bool player = true;
        public bool showOriginal = true;

        public ButtonAttribute(string[] labels, string[] methods)
        {
            m_labels = labels;
            m_methods = methods;
        }


        public ButtonAttribute(string[] methods) : this(methods, methods)
        {
        }

        public ButtonAttribute(string label, string method)
        {
            m_labels = new string[] { label };
            m_methods = new string[] { method };
        }

        public ButtonAttribute(string method) : this(method, method)
        {
        }
    }
}