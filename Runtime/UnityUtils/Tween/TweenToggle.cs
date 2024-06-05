using System;
using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public class TweenToggle : TweenComponent
    {
        public enum Operator
        {
            Greater,
            GreaterEqual,
            Less,
            LessEqual
        }

        
        [SerializeField]
        private UnityEvent<bool> m_onValueChanged;
        
        [SerializeField]
        private UnityEvent m_onTrue;
        
        [SerializeField]
        private UnityEvent m_onFalse;
        
        [SerializeField]
        private Operator m_operator;

        [SerializeField] 
        private float m_rightValue = 0.5f;

        private bool m_hasValue = false;
        private bool m_value;
        
        public event UnityAction<bool> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }

        public event UnityAction OnTrue
        {
            add => m_onTrue.AddListener(value);
            remove => m_onTrue.RemoveListener(value);
        }

        public event UnityAction OnFalse
        {
            add => m_onFalse.AddListener(value);
            remove => m_onFalse.RemoveListener(value);
        }

        protected override void OnValueChanged(float progress)
        {
            bool newValue = m_operator switch
            {
                Operator.Greater => progress > m_rightValue,
                Operator.Less => progress < m_rightValue,
                Operator.GreaterEqual => progress >= m_rightValue,
                Operator.LessEqual => progress <= m_rightValue,
                _ => throw new IndexOutOfRangeException()
            };

            if (m_hasValue && newValue == m_value)
                return;
            
            m_value = newValue;
            m_hasValue = true;
            m_onValueChanged.Invoke(m_value);
            if (m_value)
            {
                m_onTrue.Invoke();
            }
            else
            {
                m_onFalse.Invoke();
            }
        }
    }
}
