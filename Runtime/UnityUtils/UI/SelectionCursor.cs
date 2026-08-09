#nullable enable
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransformFollower))]
    [DisallowMultipleComponent]
    public class SelectionCursor : MonoBehaviour
    {
        [SerializeField] private Tween?                       _hasTargetTween;
        private                  RectTransformFollower        _follower = null!;
        private                  EventSystemSelectionTrigger? _selectionTrigger;
        
        protected void Awake()
        {
            _follower = GetComponent<RectTransformFollower>();
        }

        protected void Reset()
        {
            _hasTargetTween = GetComponent<Tween>();
        }

        protected void OnEnable()
        {
            _selectionTrigger = EventSystemSelectionTrigger.Get();
            if(_selectionTrigger == null)
                return;
            _selectionTrigger.OnSelectionChanged.AddListener(OnSelectionChanged);
            OnSelectionChanged(_selectionTrigger.Selection);
        }

        protected void OnDisable()
        {
            if(_selectionTrigger == null)
                return;
            
            _selectionTrigger.OnSelectionChanged.RemoveListener(OnSelectionChanged);
            _selectionTrigger = null;
        }
        
        private void OnSelectionChanged(GameObject selection)
        {
            bool snapInstantly = _hasTargetTween ? _hasTargetTween.Progress <= 0.1f :  _follower.Destination == null;
            
            _follower.Destination = selection != null? selection.GetComponent<RectTransform>() : null;
            
            if(_hasTargetTween)
                _hasTargetTween.IsOn = _follower.Destination != null;
            
            if(snapInstantly)
                _follower.SnapToDestination();
        }
    }
}
