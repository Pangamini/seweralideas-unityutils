using System.Collections.Generic;
using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    public static class BoundsExtensions
    {
        public static void GetCorners(this in Bounds bounds, List<Vector3> result)
        {
            var min = bounds.min;
            var max = bounds.max;

            result.Add(min);
            result.Add(new Vector3(min.x, min.y, max.z));
            result.Add(new Vector3(min.x, max.y, min.z));
            result.Add(new Vector3(max.x, min.y, min.z));
            result.Add(new Vector3(min.x, max.y, max.z));
            result.Add(new Vector3(max.x, min.y, max.z));
            result.Add(new Vector3(max.x, max.y, min.z));
            result.Add(max);
        }
    }
}
