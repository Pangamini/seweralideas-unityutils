using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(Tween))]
    public abstract class TweenComponent : MonoBehaviour
    {
        public Tween Tween { get; private set; }
        
        protected virtual void Awake()
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
        
        protected virtual void OnValueChanged(float t) { }
    }
}
