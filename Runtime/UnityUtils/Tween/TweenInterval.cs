using UnityEngine;
using UnityEngine.Events;
namespace SeweralIdeas.UnityUtils
{
    public class TweenInterval : TweenComponent
    {
        [SerializeField]
        private UnityEvent<float> m_onValueChanged;
        
        [SerializeField]
        private Vector2 m_interval = new Vector2(0,1);
        
        public event UnityAction<float> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }
        
        protected override sealed void OnValueChanged(float progress)
        {
            float newValue = Mathf.LerpUnclamped(m_interval.x, m_interval.y, progress);
            m_onValueChanged.Invoke(newValue);
        }
    }
}