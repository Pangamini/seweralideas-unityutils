using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaRect : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public UnityEvent OnSafeAreaChanged;

        private DrivenRectTransformTracker _tracker = new();

        protected void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        protected void OnEnable()
        {
            _tracker.Clear();
            _tracker.Add(
                this,
                _rectTransform,
                DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
        }

        protected void OnDisable()
        {
            _tracker.Clear();
        }

        protected void Update() => ApplySafeArea();

        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if(_rectTransform.anchorMin == anchorMin && _rectTransform.anchorMax == anchorMax && _rectTransform.offsetMin == Vector2.zero && _rectTransform.offsetMax == Vector2.zero)
                return;
            
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            OnSafeAreaChanged?.Invoke();
        }
    }
}
