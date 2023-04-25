using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class VectorExtensions
    {
        public static Vector3 x0y(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public static Vector3 xRy(this Vector2 v, float r)
        {
            return new Vector3(v.x, r, v.y);
        }

        public static Vector3 xyR(this Vector2 v, float r)
        {
            return new Vector3(v.x, v.y, r);
        }

        public static Vector2 xz(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector3 xzy(this Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

        public static Vector2 xy(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 yz(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }

        
        
        public static Vector3Int x0y(this Vector2Int v)
        {
            return new Vector3Int(v.x, 0, v.y);
        }

        public static Vector3Int xRy(this Vector2Int v, int r)
        {
            return new Vector3Int(v.x, r, v.y);
        }

        public static Vector2Int xz(this Vector3Int v)
        {
            return new Vector2Int(v.x, v.z);
        }

        public static Vector3Int xzy(this Vector3Int v)
        {
            return new Vector3Int(v.x, v.z, v.y);
        }

        public static Vector2Int xy(this Vector3Int v)
        {
            return new Vector2Int(v.x, v.y);
        }

        public static Vector2Int yz(this Vector3Int v)
        {
            return new Vector2Int(v.y, v.z);
        }

        public static Vector3Int ToVector3Int(this Vector3 v)
        {
            return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }

        public static Vector2Int ToVector2Int(this Vector2 v)
        {
            return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

    }
}
