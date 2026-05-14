using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// Makes the ScrollRect track a selected child by scrolling to make the selected child visible.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectNavigator : MonoBehaviour
    {
        [SerializeField] private bool       _trackSelectedChildren;
        
        private ScrollRect _scrollRect;
        private GameObject _previousSelected;

        private void OnSelectionChanged(GameObject selected)
        {
            if(selected == null)
                return;
            
            if(selected.transform is not RectTransform rt)
                return;
            
            if(!selected.transform.IsChildOf(_scrollRect.content))
                return;

            ScrollTo(rt);
        }

        protected void Awake() => _scrollRect = GetComponent<ScrollRect>();

        protected void Update()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if(selected != _previousSelected)
                OnSelectionChanged(selected);
            
            _previousSelected = selected;
        }

        public void ScrollTo(RectTransform target) => ScrollTo(_scrollRect, target);

        public static void ScrollTo(ScrollRect scrollRect, RectTransform target, float topMargin = 0f, float bottomMargin = 0f)
        {
            Canvas.ForceUpdateCanvases();

            Vector2 targetPivot = (Vector2)scrollRect.viewport.InverseTransformPoint(target.position);
            Rect    viewport   = scrollRect.viewport.rect;
            Rect    item       = target.rect;

            float visibleTop    = viewport.yMax - topMargin;
            float visibleBottom = viewport.yMin + bottomMargin;

            float itemTop    = targetPivot.y + item.yMax;
            float itemBottom = targetPivot.y + item.yMin;

            Vector2 offset = Vector2.zero;

            if (itemBottom < visibleBottom)
                offset.y = visibleBottom - itemBottom;
            else if (itemTop > visibleTop)
                offset.y = -(itemTop - visibleTop);

            scrollRect.content.anchoredPosition += offset;
        }
    }
}