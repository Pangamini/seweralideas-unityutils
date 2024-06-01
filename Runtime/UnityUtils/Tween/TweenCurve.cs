using System;
using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.Pooling;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public class TweenCurve : TweenComponent
    {
        [SerializeField]
        private UnityEvent<float> m_onValueChanged;
        
        [SerializeField]
        private AnimationCurve m_curve;
        
        public event UnityAction<float> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }
        
        protected override sealed void OnValueChanged(float t)
        {
            float newValue = m_curve.Evaluate(t);
            m_onValueChanged.Invoke(newValue);
        }
    }
}