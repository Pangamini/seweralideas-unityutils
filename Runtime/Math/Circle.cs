using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Utils.MathUtils
{
    [System.Serializable]
    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public bool Contains(Vector2 point)
        {
            float distanceSqr = (point - center).sqrMagnitude;
            return distanceSqr <= radius * radius;
        }

        /// <summary>
        ///  Emits up to two segments, after the circle is subtracted from the original segment
        /// </summary>
        /// <param name="segment">Original segment to be cut</param>
        /// <param name="outSegment1">If exists, is set to a new segment whose point2 is at intersection. Set to original segment, if there's no intersection</param>
        /// <param name="outSegment2">If exists, is set to a new segment whose point1 is at intersection. Set to null if there's no intersection</param>

        public void CutLineSegment(LineSegment2 segment, out LineSegment2? outSegment1, out LineSegment2? outSegment2)
        {
            outSegment1 = null;
            outSegment2 = null;

            bool containsA = Contains(segment.point1);
            bool containsB = Contains(segment.point2);

            if (containsA && containsB)
            {
                // return no segment
                return;
            }

            var intersections = Math.CircleLineIntersection(this, segment.point1, segment.point2, out Vector2 intersection0, out Vector2 intersection1);
            Vector2 normalizedLineDirection = (segment.point2 - segment.point1) / segment.length;
            
            if (intersections == 2)
            {
                float lerp0 = Vector2.Dot(normalizedLineDirection, intersection0 - segment.point1) / segment.length;
                float lerp1 = Vector2.Dot(normalizedLineDirection, intersection1 - segment.point1) / segment.length;

                Math.Sort(ref lerp0, ref lerp1, ref intersection0, ref intersection1);

                if (lerp0 > 0 && lerp0 < 1)
                    outSegment1 = new LineSegment2(segment.point1, intersection0);
                if (lerp1 > 0 && lerp1 < 1)
                    outSegment2 = new LineSegment2(intersection1, segment.point2);

                // if the line segment does not touch the circle, return the original
                if (!outSegment1.HasValue && !outSegment2.HasValue)
                {
                    outSegment1 = segment;
                }
            }

            else
            {
                // if the segment touches or misses the circle, return the original segment
                outSegment1 = segment;
            }
        }
    }
}