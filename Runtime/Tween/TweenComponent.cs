using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Tweening
{
    [RequireComponent(typeof(TweenBase))]
    [DefaultExecutionOrder(TweenBase.ExecOrder - 1)]
    public abstract class TweenComponent : MonoBehaviour
    {
        public TweenBase Source { get; private set; }

        protected virtual void Awake()
        {
            Source = GetComponent<TweenBase>();
        }

        protected virtual void Start()
        {
            Source.ValueChanged += OnValueChanged;
            OnValueChanged(Source.Progress);
        }

        protected virtual void OnDestroy()
        {
            if(Source == null)
                Debug.LogError($"{this} Source is null", this);
            Source.ValueChanged -= OnValueChanged;
            Source = null;
        }

        protected virtual void OnValueChanged(float progress) { }
    }
}
