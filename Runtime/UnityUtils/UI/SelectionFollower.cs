#nullable enable
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransformFollower))]
    [DisallowMultipleComponent]
    public class SelectionFollower : MonoBehaviour
    {
        private RectTransformFollower        _follower = null!;
        private EventSystemSelectionTrigger? _selectionTrigger;
        
        protected void Awake()
        {
            _follower = GetComponent<RectTransformFollower>();
        }

        protected void OnEnable()
        {
            _selectionTrigger = EventSystemSelectionTrigger.Get();
            if(_selectionTrigger == null)
                return;
            
            _selectionTrigger.OnSelectionChanged.AddListener(OnSelectionChanged);
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
            _follower.Destination = selection.GetComponent<RectTransform>();
        }
    }
}
