using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(Tween))]
    [DefaultExecutionOrder(Tween.ExecOrder - 1)]
    public abstract class TweenComponent : MonoBehaviour
    {
        public Tween Tween { get; private set; }
        
        protected virtual void Start()
        {
            Tween = GetComponent<Tween>();
            Tween.ValueChanged += OnValueChanged;
            OnValueChanged(Tween.Progress);
        }

        protected void OnDestroy()
        {
            Tween.ValueChanged -= OnValueChanged;
            Tween = null;
        }
        
        protected virtual void OnValueChanged(float progress) { }
    }
}
