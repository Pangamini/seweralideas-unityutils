using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// Lays its children out along an ellipse that fits in its RectTransform (a circle when the rect is square).
    /// Angles are in turns, like the RadialProgressBar's: 0 = up, increasing clockwise, 0.25 = right.
    /// The children are spread evenly from <see cref="StartAngle"/> to <see cref="EndAngle"/>; a span of a full turn
    /// (or more) closes the circle, so the last child does not land on the first.
    /// Only the children's positions are driven (not their size or rotation), so it works in the editor without
    /// changing what is saved in the scene.
    /// </summary>
    [AddComponentMenu("Layout/Circular Layout Group")]
    public class CircularLayoutGroup : LayoutGroup
    {
        private const int GizmoSteps = 64;

        [Tooltip("Angle of the first child, in turns. 0 = up, increasing clockwise, 0.25 = right.")]
        [SerializeField] private float _startAngle = 0f;

        [Tooltip("Angle of the last child, in turns. A span of a full turn closes the circle. Smaller than the start goes counter-clockwise.")]
        [SerializeField] private float _endAngle = 1f;

        [Tooltip("Shifts all children along the arc by this fraction of the angle between two children. 0.5 puts each in the middle of its slice of a closed circle.")]
        [SerializeField] private float _sliceOffset = 0f;

        [Tooltip("Turn each child to follow the ellipse: upright at the top (angle 0), its top pointing away from the center. Drives the children's rotation.")]
        [SerializeField] private bool _rotateChildren = false;

        [Tooltip("Extra rotation of every child, in degrees, counter-clockwise like the Z rotation. 180 makes the tops point to the center.")]
        [SerializeField] private float _rotationOffset = 0f;

        [Tooltip("Keep the children inside the rect: the ellipse the child centers lie on shrinks by half of the largest child. Off: the ellipse touches the rect (inside the padding), and the children stick out by half their size.")]
        [SerializeField] private bool _keepChildrenInside = true;

        public float StartAngle
        {
            get => _startAngle;
            set
            {
                if(_startAngle == value)
                    return;
                _startAngle = value;
                SetDirty();
            }
        }

        public float EndAngle
        {
            get => _endAngle;
            set
            {
                if(_endAngle == value)
                    return;
                _endAngle = value;
                SetDirty();
            }
        }

        /// <summary> Shift of the children along the arc, as a fraction of the angle between two children. </summary>
        public float SliceOffset
        {
            get => _sliceOffset;
            set
            {
                if(_sliceOffset == value)
                    return;
                _sliceOffset = value;
                SetDirty();
            }
        }

        public bool RotateChildren
        {
            get => _rotateChildren;
            set
            {
                if(_rotateChildren == value)
                    return;
                _rotateChildren = value;
                SetDirty();
            }
        }

        public float RotationOffset
        {
            get => _rotationOffset;
            set
            {
                if(_rotationOffset == value)
                    return;
                _rotationOffset = value;
                SetDirty();
            }
        }

        public bool KeepChildrenInside
        {
            get => _keepChildrenInside;
            set
            {
                if(_keepChildrenInside == value)
                    return;
                _keepChildrenInside = value;
                SetDirty();
            }
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal() => SetChildren(0);

        public override void SetLayoutVertical() => SetChildren(1);

        private void SetChildren(int axis)
        {
            int count = rectChildren.Count;
            if(count == 0)
                return;

            GetEllipse(out Vector2 center, out Vector2 radii);

            // center is in layout space (from the top left, y down); the ellipse's y is up, so it is flipped.
            for(int i = 0; i < count; i++)
            {
                RectTransform child = rectChildren[i];
                float turns = AngleOf(i, count);
                Vector2 offset = EllipsePoint(turns, radii);

                // Once, with the first axis: the tracker was cleared before it.
                if(axis == 0 && _rotateChildren)
                {
                    m_Tracker.Add(this, child, DrivenTransformProperties.Rotation);
                    // Turns go clockwise, a Z rotation counter-clockwise.
                    child.localRotation = Quaternion.Euler(0f, 0f, _rotationOffset - turns * 360f);
                }

                float size = child.rect.size[axis];
                float pos = axis == 0
                    ? center.x + offset.x - size * 0.5f
                    : center.y - offset.y - size * 0.5f;
                SetChildAlongAxis(child, axis, pos);
            }
        }

        // Turns of the i-th child out of count.
        private float AngleOf(int index, int count)
        {
            float span = _endAngle - _startAngle;
            if(count == 1)
            {
                if(Mathf.Abs(span) < 1f)
                    return _startAngle + span * 0.5f;
                return _startAngle + span * _sliceOffset;
            }

            // A closed circle has no end to put the last child on; an arc does.
            int steps = Mathf.Abs(span) >= 1f ? count : count - 1;
            return _startAngle + span * (index + _sliceOffset) / steps;
        }

        // Point of the ellipse at the angle in turns, relative to its center, y up.
        private static Vector2 EllipsePoint(float turns, Vector2 radii)
        {
            float radians = turns * (2f * Mathf.PI);
            return new Vector2(Mathf.Sin(radians) * radii.x, Mathf.Cos(radians) * radii.y);
        }

        // The ellipse the child centers lie on: center in layout space (from the top left, y down) and radii.
        private void GetEllipse(out Vector2 center, out Vector2 radii)
        {
            Vector2 size = rectTransform.rect.size;
            float innerWidth = Mathf.Max(0f, size.x - padding.horizontal);
            float innerHeight = Mathf.Max(0f, size.y - padding.vertical);
            center = new Vector2(padding.left + innerWidth * 0.5f, padding.top + innerHeight * 0.5f);

            Vector2 inset = Vector2.zero;
            if(_keepChildrenInside)
            {
                foreach(RectTransform child in rectChildren)
                {
                    Vector2 childSize = child.rect.size;
                    inset = Vector2.Max(inset, childSize * 0.5f);
                }
            }

            radii = new Vector2(Mathf.Max(0f, innerWidth * 0.5f - inset.x), Mathf.Max(0f, innerHeight * 0.5f - inset.y));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            GetEllipse(out Vector2 layoutCenter, out Vector2 radii);

            Rect rect = rectTransform.rect;
            // Layout space (top left, y down) to the rect's local space (y up).
            Vector3 center = new Vector3(rect.xMin + layoutCenter.x, rect.yMax - layoutCenter.y, 0f);

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Color oldColor = Gizmos.color;
            Gizmos.matrix = rectTransform.localToWorldMatrix;

            // The whole ellipse faint, the part the children are spread over bright.
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            DrawArc(center, radii, 0f, 1f, GizmoSteps);

            Gizmos.color = Color.green;
            float span = _endAngle - _startAngle;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Min(Mathf.Abs(span), 1f) * GizmoSteps));
            DrawArc(center, radii, _startAngle, Mathf.Min(Mathf.Abs(span), 1f) * Mathf.Sign(span), steps);

            // Where the children go
            int count = rectChildren.Count;
            for(int i = 0; i < count; i++)
            {
                Vector2 offset = EllipsePoint(AngleOf(i, count), radii);
                Gizmos.DrawWireSphere(center + (Vector3)offset, Mathf.Max(rect.size.x, rect.size.y) * 0.01f);
            }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }

        // Draws the ellipse from turns `from`, over `turns` turns (signed), in `steps` segments.
        private static void DrawArc(Vector3 center, Vector2 radii, float from, float turns, int steps)
        {
            Vector3 previous = center + (Vector3)EllipsePoint(from, radii);
            for(int i = 1; i <= steps; i++)
            {
                Vector3 next = center + (Vector3)EllipsePoint(from + turns * i / steps, radii);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
