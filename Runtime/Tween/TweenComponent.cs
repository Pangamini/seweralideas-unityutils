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

        protected virtual void Awake()
        {
            Tween = GetComponent<Tween>();
        }
        
        protected virtual void Start()
        {
            Tween.ValueChanged += OnValueChanged;
            OnValueChanged(Tween.Progress);
        }

        protected virtual void OnDestroy()
        {
            if(Tween == null)
                Debug.LogError($"{this} Tween is null", this);
            Tween.ValueChanged -= OnValueChanged;
            Tween = null;
        }
        
        protected virtual void OnValueChanged(float progress) { }
    }
}