using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// Locks the game to a fixed aspect ratio by letterboxing/pillarboxing the
    /// main camera's viewport, and shrinking any registered UI viewports to match.
    /// Builds its own canvas + black bars for the letterboxed area in code.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-1500)]
    public class AspectRatioLock : MonoBehaviour
    {
        private const string LetterboxCanvasName = "LetterboxCanvas";

        [SerializeField] private Vector2Int      _targetAspect = new(16, 9);
        [SerializeField] private Camera          _gameCamera;
        [SerializeField] private RectTransform[] _uiViewports;

        [SerializeField] private Canvas         _letterboxCanvas;
        [SerializeField] private RectTransform  _barTop;
        [SerializeField] private RectTransform  _barBottom;
        [SerializeField] private RectTransform  _barLeft;
        [SerializeField] private RectTransform  _barRight;

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private DrivenRectTransformTracker _tracker;

        public Vector2Int TargetAspect
        {
            get => _targetAspect;
            set
            {
                _targetAspect = value;
                Apply();
            }
        }

        protected void OnEnable()
        {
            if(!_gameCamera)
                _gameCamera = Camera.main;

            Apply();
        }

        protected void Update()
        {
            if(Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                Apply();
        }

        protected void OnDisable()
        {
            _tracker.Clear();
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            // Defer: creating the letterbox UI (AddComponent etc.) is not allowed during OnValidate,
            // and re-running here lets Apply()'s _tracker.Clear() drop any rects reassigned away
            // (old _uiViewports entries, old bar RectTransforms) before re-adding the current ones.
            EditorApplication.delayCall += DelayedApply;
        }

        private void DelayedApply()
        {
            if(this)
                Apply();
        }
#endif

        private void Apply()
        {
            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;

            _tracker.Clear();

            // Screen.width/height can be transiently 0 in the editor (e.g. the Game
            // view hasn't laid out yet after a domain reload, or OnValidate's
            // deferred call runs before it does). windowAspect would then divide by
            // zero, producing an Infinity or NaN Rect that CalculateViewport has no
            // way to detect - that would get written straight into every tracked
            // RectTransform's anchors and the camera's rect below.
            if(!_gameCamera || _targetAspect.x <= 0 || _targetAspect.y <= 0 || Screen.width <= 0 || Screen.height <= 0)
                return;

            EnsureLetterboxUi();

            Rect viewport = CalculateViewport();
            _gameCamera.rect = viewport;

            if(_uiViewports != null)
            {
                foreach(RectTransform viewportRect in _uiViewports)
                {
                    if(!viewportRect)
                        continue;

                    _tracker.Add(this, viewportRect, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
                    ApplyUiViewport(viewportRect, viewport);
                }
            }

            ApplyLetterboxBars(viewport, _barTop, _barBottom, _barLeft, _barRight);
        }

        private void EnsureLetterboxUi()
        {
            if(!_letterboxCanvas)
            {
                var canvasGo = new GameObject(LetterboxCanvasName, typeof(RectTransform), typeof(Canvas));
                canvasGo.transform.SetParent(transform, false);
                _letterboxCanvas = canvasGo.GetComponent<Canvas>();
                _letterboxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            if(!_barTop)    _barTop    = CreateBar(_letterboxCanvas.transform, "Top");
            if(!_barBottom) _barBottom = CreateBar(_letterboxCanvas.transform, "Bottom");
            if(!_barLeft)   _barLeft   = CreateBar(_letterboxCanvas.transform, "Left");
            if(!_barRight)  _barRight  = CreateBar(_letterboxCanvas.transform, "Right");
        }

        private static RectTransform CreateBar(Transform parent, string name)
        {
            var barGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(parent, false);

            Image image = barGo.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            return (RectTransform)barGo.transform;
        }

        private Rect CalculateViewport()
        {
            float targetAspect = (float)_targetAspect.x / _targetAspect.y;
            float windowAspect = (float)Screen.width / Screen.height;
            float scaleHeight  = windowAspect / targetAspect;

            if(scaleHeight < 1f)
                return new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);

            float scaleWidth = 1f / scaleHeight;
            return new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }

        private static void ApplyUiViewport(RectTransform viewportRect, Rect viewport)
        {
            viewportRect.anchorMin = new Vector2(viewport.xMin, viewport.yMin);
            viewportRect.anchorMax = new Vector2(viewport.xMax, viewport.yMax);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
        }

        private void ApplyLetterboxBars(Rect viewport, RectTransform top, RectTransform bottom, RectTransform left, RectTransform right)
        {
            SetBar(top,    new Vector2(0f, viewport.yMax), new Vector2(1f, 1f));
            SetBar(bottom, new Vector2(0f, 0f),             new Vector2(1f, viewport.yMin));
            SetBar(left,   new Vector2(0f, 0f),             new Vector2(viewport.xMin, 1f));
            SetBar(right,  new Vector2(viewport.xMax, 0f),  new Vector2(1f, 1f));
        }

        private void SetBar(RectTransform bar, Vector2 anchorMin, Vector2 anchorMax)
        {
            if(!bar)
                return;

            _tracker.Add(this, bar, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);

            bar.anchorMin = anchorMin;
            bar.anchorMax = anchorMax;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
        }
    }
}
