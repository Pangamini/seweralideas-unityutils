#nullable enable
using SeweralIdeas.Tweening;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransformFollower))]
    [DisallowMultipleComponent]
    public class SelectionCursor : MonoBehaviour
    {
        /// <summary>What the cursor follows. A selection that does not qualify is treated as nothing being selected.</summary>
        public enum FollowMode
        {
            /// <summary>Every selected object.</summary>
            Everything,
            /// <summary>Only objects that are children (at any depth) of the <c>Root</c> transform, or the root itself.</summary>
            ChildrenOfRoot,
            /// <summary>Only objects in the same scene as the cursor.</summary>
            SameScene,
        }

        [SerializeField]
        private Tween? _hasTargetTween;

        [SerializeField]
        private FollowMode _follow = FollowMode.Everything;

        [SerializeField]
        [Condition(nameof(FollowsTransform))]
        private Transform? _root; // for ChildrenOfRoot
        
        private                                                        RectTransformFollower        _follower = null!;
        private                                                        EventSystemSelectionTrigger? _selectionTrigger;

        private bool FollowsTransform => _follow == FollowMode.ChildrenOfRoot;
        
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
            _selectionTrigger = EventSystemSelectionTrigger.GetInstance();
            if(_selectionTrigger == null)
                return;
            _selectionTrigger.Selection.Changed += OnSelectionChanged;
        }

        protected void OnDisable()
        {
            if(_selectionTrigger == null)
                return;
            
            _selectionTrigger.Selection.Changed -= OnSelectionChanged;
            _selectionTrigger = null;
        }
        
        private bool IsFollowed(GameObject selection)
        {
            switch (_follow)
            {
                case FollowMode.ChildrenOfRoot:
                    return _root != null && selection.transform.IsChildOf(_root); // true for the root itself, too
                case FollowMode.SameScene:
                    return selection.scene == gameObject.scene;
                default:
                    return true;
            }
        }

        private void OnSelectionChanged(GameObject? selection)
        {
            bool snapInstantly = _hasTargetTween ? _hasTargetTween.Progress <= 0.1f :  _follower.Destination == null;
            
            _follower.Destination = selection != null && IsFollowed(selection) ? selection.GetComponent<RectTransform>() : null;
            
            if(_hasTargetTween)
                _hasTargetTween.IsOn = _follower.Destination != null;
            
            if(snapInstantly)
                _follower.SnapToDestination();
        }
    }
}
