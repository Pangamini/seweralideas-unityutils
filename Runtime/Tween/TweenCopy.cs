using UnityEngine;

namespace SeweralIdeas.Tweening
{
    /// <summary>
    /// A <see cref="TweenBase"/> that has the value of another one, so that the <see cref="TweenComponent"/>s of this object can follow
    /// a tween that is somewhere else. It passes on what the other tween sends to its listeners, which for a <see cref="Tween"/> is the value after its curve.
    /// </summary>
    [DefaultExecutionOrder(ExecOrder)]
    public class TweenCopy : TweenBase
    {
        [Tooltip("The tween to copy. Components next to this one follow its value.")]
        [SerializeField] private TweenBase _copied;

        private float _value;
        private bool  _initialized;
        private bool  _subscribed;
        private bool  _notifying; // a loop of copies (this one is copied by what it copies) would not end

        public TweenBase Copied
        {
            get => _copied;
            set
            {
                if(_copied == value)
                    return;

                Unsubscribe();
                _copied = value;
                if(isActiveAndEnabled)
                    Subscribe();
            }
        }

        public override float Progress
        {
            get
            {
                EnsureInitialized();
                return _value;
            }
        }

        public override void EnsureInitialized()
        {
            if(_initialized)
                return;
            _initialized = true;

            if(_copied != null && _copied != this)
            {
                _copied.EnsureInitialized();
                _value = _copied.Progress;
            }
        }

        protected virtual void OnEnable()
        {
            EnsureInitialized();
            Subscribe();
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if(_subscribed || _copied == null || _copied == this)
                return;

            _subscribed = true;
            _copied.EnsureInitialized();
            _copied.ValueChanged += OnCopiedChanged;
            OnCopiedChanged(_copied.Progress); // the value it has now, for the components that follow this one
        }

        private void Unsubscribe()
        {
            if(!_subscribed)
                return;

            _subscribed = false;
            if(_copied != null)
                _copied.ValueChanged -= OnCopiedChanged;
        }

        private void OnCopiedChanged(float value)
        {
            if(_notifying)
                return;

            _value = value;
            _notifying = true;
            try
            {
                NotifyValueChanged(value);
            }
            finally
            {
                _notifying = false;
            }
        }
    }
}
