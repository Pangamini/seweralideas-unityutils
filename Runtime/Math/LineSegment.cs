using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Utils.MathUtils
{
    public struct LineSegment2
    {
        public readonly Vector2 point1;
        public readonly Vector2 point2;
        public readonly float length;

        public LineSegment2(Vector2 point1, Vector2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
            length = (point2 - point1).magnitude;
        }

        /// <summary>
        /// User of this constructor is responsible for passing in the correct length value. Used for performance / optimization reasons, when the length is already known
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="length"></param>
        internal LineSegment2(Vector2 point1, Vector2 point2, float length)
        {
            this.point1 = point1;
            this.point2 = point2;
            this.length = length;
        }

        /// <summary>
        /// Creates a segment that's translated relative to the original one
        /// </summary>
        /// <param name="translation"></param>
        /// <returns>The translated segment</returns>
        public LineSegment2 Translated(Vector2 translation)
        {
            return new LineSegment2(point1 + translation, point2 + translation, length);
        }

        public override string ToString()
        {
            return $"{nameof(LineSegment2)}({point1} - {point2})";
        }
    }

    public struct LineSegment3
    {
        public readonly Vector3 point1;
        public readonly Vector3 point2;
        public readonly float length;

        public LineSegment3(Vector3 point1, Vector3 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
            length = (point2 - point1).magnitude;
        }

        /// <summary>
        /// User of this constructor is responsible for passing in the correct length value. Used for performance / optimization reasons, when the length is already known
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="length"></param>
        internal LineSegment3(Vector3 point1, Vector3 point2, float length)
        {
            this.point1 = point1;
            this.point2 = point2;
            this.length = length;
        }

        /// <summary>
        /// Creates a segment that's translated relative to the original one
        /// </summary>
        /// <param name="translation"></param>
        /// <returns>The translated segment</returns>
        public LineSegment3 Translated(Vector3 translation)
        {
            return new LineSegment3(point1 + translation, point2 + translation, length);
        }

        public override string ToString()
        {
            return $"{nameof(LineSegment3)}({point1} - {point2})";
        }
    }
}