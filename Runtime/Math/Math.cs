using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Utils.MathUtils
{
    public static class Math
    {

        /// <summary>
        /// Calculates a 2D line intersection.
        /// </summary>
        /// <param name="a1">First line point 1</param>
        /// <param name="a2">First line point 2</param>
        /// <param name="b1">Second line point 1</param>
        /// <param name="b2">Second line point 2</param>
        /// <param name="intersection">Will contain the intersection point if lines are not parallel</param>
        /// <returns>True if intersection exists</returns>
        public static bool LineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection, float minDeterminant = 0.001f)
        {
            Vector2 s1 = a2 - a1;
            Vector2 s2 = b2 - b1;

            Vector2 u = a1 - b1;
            var det = (-s2.x * s1.y + s1.x * s2.y);
            if (Mathf.Abs(det) <= minDeterminant)    // to avoid nearly parallel lines to produce intersection somewhere far away
            {
                intersection = Vector2.zero;
                return false;
            }

            float ip = 1f / det;

            // float s = (-s1.y * u.x + s1.x * u.y) * ip;
            float t = (s2.x * u.y - s2.y * u.x) * ip;

            intersection = a1 + (s1 * t);
            return true;

        }
        /// <summary>
        /// Makes sure that max is larger than min, by swapping values if they are not
        /// </summary>
        public static void Sort(ref float min, ref float max)
        {
            if (min <= max) return;
            var r = min;
            min = max;
            max = r;
        }


        /// <summary>
        /// Makes sure that max is larger than min, by swapping values if they are not.
        /// Sorts associated objects along with the values
        /// </summary>
        public static void Sort<T>(ref float min, ref float max, ref T minData, ref T maxData)
        {
            if (min <= max) return;
            var r = min;
            min = max;
            max = r;
            var t = minData;
            minData = maxData;
            maxData = t;
        }


        public static void Sort(ref Vector2 min, ref Vector2 max)
        {
            Sort(ref min.x, ref max.x);
            Sort(ref min.y, ref max.y);
        }

        public static void Sort(ref Vector3 min, ref Vector3 max)
        {
            Sort(ref min.x, ref max.x);
            Sort(ref min.y, ref max.y);
            Sort(ref min.z, ref max.z);
        }

#if DEBUG
        [System.Diagnostics.Conditional("DEBUG")]
        public static void AssertSorted(float min, float max)
        {
            Debug.Assert(min <= max, "values not sorted");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void AssertSorted(Vector2 min, Vector2 max)
        {
            Debug.Assert(min.x <= max.x && min.y <= max.y, "values not sorted");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void AssertSorted(Vector3 min, Vector3 max)
        {
            Debug.Assert(min.x <= max.x && min.y <= max.y && min.z <= max.z, "values not sorted");
        }
#endif

        public static bool LineSegmentIntersection(Vector2 lineA, Vector2 lineB, Vector2 segmentA, Vector2 segmentB, out Vector2 intersection)
        {
            //TODO Optimize: this can be optimized a lot, since LineIntersection and LineSegmentIntersection do almost the same math
            if (LineSegmentIntersection(lineA, lineB, segmentA, segmentB))
            {
                return LineIntersection(lineA, lineB, segmentA, segmentB, out intersection);
            }
            intersection = default;
            return false;
        }

        public static bool LineSegmentIntersection(Vector2 lineA, Vector2 lineB, Vector2 segmentA, Vector2 segmentB)
        {
            
            var offset = lineB - lineA;
            var normal = new Vector2(offset.y, -offset.x);
            // ax + by + c = 0
            float c = -(normal.x * lineA.x + normal.y * lineA.y);
            sbyte Side(Vector2 point) { return (sbyte)Mathf.Sign(normal.x * point.x + normal.y * point.y + c); }

            var sideA = Side(segmentA);
            var sideB = Side(segmentB);
            return sideA * sideB < 0;
        }


        /// <summary>
        /// Detects if two line segments intersect
        /// max values must be larger than min values!
        /// </summary>
        public static bool SegmentSegmentIntersection(LineSegment2 segmentA, LineSegment2 segmentB)
        {        
            return LineSegmentIntersection(segmentA.point1, segmentA.point2, segmentB.point1, segmentB.point2) && LineSegmentIntersection(segmentB.point1, segmentB.point2, segmentA.point1, segmentA.point2);
        }

        /// <summary>
        /// Detects if two line segments intersect
        /// max values must be larger than min values!
        /// </summary>
        public static bool SegmentSegmentIntersection(LineSegment2 segmentA, LineSegment2 segmentB, out Vector2 intersection)
        {
            //TODO Optimize: this can be optimized a lot, since LineIntersection and LineSegmentIntersection do almost the same math
            if (LineSegmentIntersection(segmentA.point1, segmentA.point2, segmentB.point1, segmentB.point2) && LineSegmentIntersection(segmentB.point1, segmentB.point2, segmentA.point1, segmentA.point2))
            {
                return LineIntersection(segmentA.point1, segmentA.point2, segmentB.point1, segmentB.point2, out intersection);
            }

            intersection = Vector2.positiveInfinity;
            return false;
        }

        /// <summary>
        /// Detects if a line segment intersects the rectangle
        /// Does not support inverted rectangles
        /// </summary>
        public static bool SegmentRectIntersection(Vector2 point1, Vector2 point2, Rect rect)
        {
#if DEBUG
            AssertSorted(rect.min, rect.max);
#endif
            {
                // check projections
                var pointMin = point1;
                var pointMax = point2;
                Sort(ref pointMin, ref pointMax);
                if (pointMax.x < rect.xMin) return false;
                if (pointMax.y < rect.yMin) return false;
                if (pointMin.x > rect.xMax) return false;
                if (pointMin.y > rect.yMax) return false;
            }

            // check if all rect's corner points are on the same side of the line
            var offset = point2 - point1;
            var normal = new Vector2(offset.y, -offset.x);
            // ax + by + c = 0
            float c = -(normal.x * point1.x + normal.y * point1.y);
            sbyte Side(Vector2 point) { return (sbyte)Mathf.Sign(normal.x * point.x + normal.y * point.y + c); }

            var signMin = Side(new Vector2(rect.xMin, rect.yMin));
            var sameSide = signMin == Side(new Vector2(rect.xMin, rect.yMax)) &&
            signMin == Side(new Vector2(rect.xMax, rect.yMax)) &&
            signMin == Side(new Vector2(rect.xMax, rect.yMin));
            if (sameSide) return false;

            return true;
        }

        /// <summary>
        /// Detects if a line segment intersects the rectangle
        /// Does not support inverted rectangles
        /// </summary>
        public static bool CapsuleRectIntersection(Rect rect, Vector2 point1, Vector2 point2, float width)
        {
#if DEBUG
            AssertSorted(rect.min, rect.max);
#endif
            {
                // check projections
                var pointMin = point1;
                var pointMax = point2;
                Sort(ref pointMin, ref pointMax);
                if (pointMax.x < rect.xMin) return false;
                if (pointMax.y < rect.yMin) return false;
                if (pointMin.x > rect.xMax) return false;
                if (pointMin.y > rect.yMax) return false;
            }

            // check if all rect's corner points are on the same side of the line
            var offset = point2 - point1;
            var normal = new Vector2(offset.y, -offset.x);
            // ax + by + c = 0
            float c = -(normal.x * point1.x + normal.y * point1.y);

            sbyte Side(Vector2 point)
            {
                return (sbyte)Mathf.Sign(normal.x * point.x + normal.y * point.y + c);
            }

            var signMin = Side(new Vector2(rect.xMin, rect.yMin));
            bool sameSide = signMin == Side(new Vector2(rect.xMin, rect.yMax)) &&
            signMin == Side(new Vector2(rect.xMax, rect.yMax)) &&
            signMin == Side(new Vector2(rect.xMax, rect.yMin));
            if (sameSide) return false;

            return true;
        }

        public static Vector3 ClosestPointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            Vector3 relativePoint = point - lineStart;
            Vector3 lineDirection = lineEnd - lineStart;
            return Vector3.Project(relativePoint, lineDirection);
        }
        
        public static Vector2 ClosestPointToLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            Vector2 relativePoint = point - lineStart;
            Vector2 lineDirection = (lineEnd - lineStart).normalized;
            
            float dot = Vector2.Dot(lineDirection, relativePoint);
            
            var ret = lineStart + lineDirection * dot;
            return ret;
        }

        /// <summary>
        /// Returns a squared distance between two segments
        /// </summary>
        /// <param name="segAlength">Precomputed length of the segment A</param>
        /// <param name="segBlength">Precomputed length of the segment B</param>
        /// <returns>the squared distance</returns>
        public static float SegmentSegmentDistanceSqr(LineSegment2 segmentA, LineSegment2 segmentB)
        {
            if (SegmentSegmentIntersection(segmentA, segmentB))
            {
                return 0f;
            }

            //TODO potentially 'inline' the calls for optimization, a lot of computations are duplicated this way
            float lerp;

            Vector2 closePointOnA_bStart = ClosestPointOnSegment(segmentA, segmentB.point1, out lerp);
            Vector2 closePointOnA_bEnd = ClosestPointOnSegment(segmentA, segmentB.point2, out lerp);
            Vector2 closePointOnB_aStart = ClosestPointOnSegment(segmentB, segmentA.point1, out lerp);
            Vector2 closePointOnB_aEnd = ClosestPointOnSegment(segmentB, segmentA.point2, out lerp);

            var lengthSqrA0 = (segmentB.point1 - closePointOnA_bStart).sqrMagnitude;
            var lengthSqrA1 = (segmentB.point2 - closePointOnA_bEnd).sqrMagnitude;
            var lengthSqrB0 = (segmentA.point1 - closePointOnB_aStart).sqrMagnitude;
            var lengthSqrB1 = (segmentA.point2 - closePointOnB_aEnd).sqrMagnitude;

            return Mathf.Min(Mathf.Min(lengthSqrA0, lengthSqrA1), Mathf.Min(lengthSqrB0, lengthSqrB1));
        }

        /// <summary>
        /// Returns a squared distance between two segments
        /// </summary>
        /// <param name="segAlength">Precomputed length of the segment A</param>
        /// <param name="segBlength">Precomputed length of the segment B</param>
        /// <returns>the squared distance</returns>
        public static float SegmentSegmentDistanceSqr(LineSegment2 segmentA, LineSegment2 segmentB, out Vector2 closePointOnA, out Vector2 closePointOnB)
        {
            if (SegmentSegmentIntersection(segmentA, segmentB, out Vector2 intersection))
            {
                closePointOnA = intersection;
                closePointOnB = intersection;
                return 0f;
            }

            //TODO potentially 'inline' the calls for optimization, a lot of computations are duplicated this way
            float lerp;

            Vector2 closePointOnA_bStart = ClosestPointOnSegment(segmentA, segmentB.point1, out lerp);
            Vector2 closePointOnA_bEnd = ClosestPointOnSegment(segmentA, segmentB.point2, out lerp);
            Vector2 closePointOnB_aStart = ClosestPointOnSegment(segmentB, segmentA.point1, out lerp);
            Vector2 closePointOnB_aEnd = ClosestPointOnSegment(segmentB, segmentA.point2, out lerp);

            var lengthSqrA0 = (segmentB.point1 - closePointOnA_bStart).sqrMagnitude;
            var lengthSqrA1 = (segmentB.point2 - closePointOnA_bEnd).sqrMagnitude;
            var lengthSqrB0 = (segmentA.point1 - closePointOnB_aStart).sqrMagnitude;
            var lengthSqrB1 = (segmentA.point2 - closePointOnB_aEnd).sqrMagnitude;

            float smallestLength = Mathf.Min(Mathf.Min(lengthSqrA0, lengthSqrA1), Mathf.Min(lengthSqrB0, lengthSqrB1));

            if (smallestLength == lengthSqrA0)
            {
                closePointOnA = closePointOnA_bStart;
                closePointOnB = segmentB.point1;
            }
            else if (smallestLength == lengthSqrA1)
            {
                closePointOnA = closePointOnA_bEnd;
                closePointOnB = segmentB.point2;
            }

            else if (smallestLength == lengthSqrB0)
            {
                closePointOnB = closePointOnB_aStart;
                closePointOnA = segmentA.point1;
            }
            else
            {
                closePointOnB = closePointOnB_aEnd;
                closePointOnA = segmentA.point2;
            }

            return smallestLength;


        }

        public static float SegmentRectDistanceSqr(LineSegment2 segment, Rect box)
        {
            if (box.Contains(segment.point1) || box.Contains(segment.point2)) return 0;
            //TODO potentially 'inline' the calls for optimization, a lot of computations are duplicated this way
            var distSqr1 = SegmentSegmentDistanceSqr(segment, new LineSegment2(new Vector2(box.xMin, box.yMin), new Vector2(box.xMax, box.yMin), box.width));
            var distSqr2 = SegmentSegmentDistanceSqr(segment, new LineSegment2(new Vector2(box.xMin, box.yMin), new Vector2(box.xMin, box.yMax), box.height));
            var distSqr3 = SegmentSegmentDistanceSqr(segment, new LineSegment2(new Vector2(box.xMax, box.yMax), new Vector2(box.xMax, box.yMin), box.height));
            var distSqr4 = SegmentSegmentDistanceSqr(segment, new LineSegment2(new Vector2(box.xMax, box.yMax), new Vector2(box.xMin, box.yMax), box.width));
            return Mathf.Min(Mathf.Min(distSqr1, distSqr2), Mathf.Min(distSqr3, distSqr4));

        }

        public static Vector3 ClosestPointOnSegment(LineSegment3 segment, Vector3 point, out float lerp)
        {
            Vector3 relativePoint = point - segment.point1;
            Vector3 normalizedLineDirection = (segment.point2 - segment.point1) / segment.length;

            float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp(dot, 0.0F, segment.length);

            lerp = dot / segment.length;
            var ret = segment.point1 + normalizedLineDirection * dot;
            return ret;
        }

        public static Vector2 ClosestPointOnSegment(LineSegment2 segment, Vector2 point, out float lerp)
        {
            Vector2 relativePoint = point - segment.point1;
            Vector2 lineDirection = segment.point2 - segment.point1;
            Vector2 normalizedLineDirection = lineDirection;
            normalizedLineDirection /= segment.length;

            float dot = Vector2.Dot(normalizedLineDirection, relativePoint);
            lerp = dot / segment.length;
            dot = Mathf.Clamp(dot, 0.0F, segment.length);

            var ret = segment.point1 + normalizedLineDirection * dot;
            return ret;
        }

        public static float InverseUnclampedLerp(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }

        public static byte CircleLineIntersection(Circle circle, Vector2 pointA, Vector2 pointB, out Vector2 intersectionA, out Vector2 intersectionB)
        {
            // offset the points to the circle space to simplify circle equation
            pointA -= circle.center;
            pointB -= circle.center;

            Vector2 diff = pointB - pointA;
            float lengthSqr = diff.sqrMagnitude;
            float det = pointA.x * pointB.y - pointB.x * pointA.y;              // determinant
            float dscr = circle.radius * circle.radius * lengthSqr - det * det;   // discriminant

            if (dscr < 0)
            {
                intersectionA = default;
                intersectionB = default;
                return 0;
            }

            float z = Mathf.Sqrt(dscr);
            int sgn = diff.y < 0 ? -1 : 1;

            float x0 = det * diff.y;
            float y0 = -det * diff.x;

            if (dscr == 0)
            {
                intersectionA.x = x0 / lengthSqr;
                intersectionA.y = y0 / lengthSqr;
                intersectionA += circle.center; // offset the points back to the world space

                intersectionB = intersectionA;
                return 1;
            }

            else
            {
                float x1 = sgn * diff.x * z;
                float y1 = Mathf.Abs(diff.y) * z;

                intersectionA.x = (x0 + x1) / lengthSqr;
                intersectionA.y = (y0 + y1) / lengthSqr;

                intersectionB.x = (x0 - x1) / lengthSqr;
                intersectionB.y = (y0 - y1) / lengthSqr;

                // offset the points back to the world space
                intersectionA += circle.center;
                intersectionB += circle.center;
                return 2;
            }

        }

        // ref for matrices is there for performance reasons
        public static bool BoxBoxIntersection(Rect boxA, Rect boxB, ref Matrix3x3 bToA, ref Matrix3x3 aToB)
        {
            // find points relative to boxA's local bounding box
            var point00 = bToA * new Vector2(boxB.xMin, boxB.yMin);
            var point10 = bToA * new Vector2(boxB.xMax, boxB.yMin);
            var point01 = bToA * new Vector2(boxB.xMin, boxB.yMax);
            var point11 = bToA * new Vector2(boxB.xMax, boxB.yMax);

            // if any of the boxB's points lies inside parcel, return true
            if (boxA.Contains(point00)) return true;
            if (boxA.Contains(point01)) return true;
            if (boxA.Contains(point10)) return true;
            if (boxA.Contains(point11)) return true;

            // if any of the boxB's edges intersects parcel, return true
            if (SegmentRectIntersection(point00, point01, boxA)) return true;
            if (SegmentRectIntersection(point01, point11, boxA)) return true;
            if (SegmentRectIntersection(point11, point10, boxA)) return true;
            if (SegmentRectIntersection(point10, point00, boxA)) return true;

            // if any of boxA's points lies inside other, return true

            point00 = aToB * new Vector2(boxA.xMin, boxA.yMin);
            point10 = aToB * new Vector2(boxA.xMax, boxA.yMin);
            point01 = aToB * new Vector2(boxA.xMin, boxA.yMax);
            point11 = aToB * new Vector2(boxA.xMax, boxA.yMax);

            if (boxB.Contains(point00)) return true;
            if (boxB.Contains(point01)) return true;
            if (boxB.Contains(point10)) return true;
            if (boxB.Contains(point11)) return true;

            return false;
        }

        public static long Clamp(long value, long min, long max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static bool GetHalfPlaneSide(Vector2 halfPlanePoint, Vector2 halfPlaneNormal, Vector2 testPoint)
        {
            float c = -(halfPlaneNormal.x * halfPlanePoint.x + halfPlaneNormal.y * halfPlanePoint.y);
            return (sbyte)Mathf.Sign(halfPlaneNormal.x * testPoint.x + halfPlaneNormal.y * testPoint.y + c) > 0;
        }


        struct Slerp
        {
            public readonly Vector2 start;
            public readonly Vector2 end;
            public readonly float dot;
            public readonly float acos;

            public Slerp(Vector2 start, Vector2 end)
            {
                this.start = start;
                this.end = end;
                this.dot = Mathf.Clamp(Vector2.Dot(start, end), -1, 1);
                this.acos = Mathf.Acos(dot);
            }

            public Vector2 Evaluate(float percent)
            {
                float theta = acos * percent;
                Vector2 RelativeVec = end - start * dot;
                RelativeVec.Normalize();
                return ((start * Mathf.Cos(theta)) + (RelativeVec * Mathf.Sin(theta)));
            }
        }

    }
}