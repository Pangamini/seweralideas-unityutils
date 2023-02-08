using UnityEngine;

namespace SeweralIdeas.Utils.MathUtils
{
    public struct Parabola
    {
        public float horizontalSpeed;
        public float verticalSpeed;
        public float duration;
        public float gravity;

        public static Vector2 CalculatePlanarOffset(Vector3 origin, Vector3 destination, out Vector3 forward, out Vector3 up)
        {
            Vector3 offset = destination - origin;

            up = Vector3.up;
            forward = offset;
            forward.y = 0;
            forward = Vector3.Normalize(forward);

            Plane fwPlane = new Plane(forward, 0);
            Plane upPlane = new Plane(up, 0);

            Vector2 planarOffset;
            planarOffset.x = fwPlane.GetDistanceToPoint(offset);
            planarOffset.y = upPlane.GetDistanceToPoint(offset);

            return planarOffset;
        }

        public static Parabola FromTargetOffset(Vector2 targOffset, float gravity, float speedFactor = 2)
        {
            Parabola parabola;
            parabola.gravity = gravity;
            parabola.horizontalSpeed = speedFactor * Mathf.Sqrt(targOffset.x * targOffset.normalized.x);
            parabola.duration = targOffset.x / parabola.horizontalSpeed;
            parabola.verticalSpeed = (targOffset.y - 0.5f * parabola.duration * parabola.duration * gravity) / parabola.duration;
            return parabola;
        }

        public Vector2 Evaluate(float time)
        {
            float posX = time * horizontalSpeed;
            float posY = ((0.5f * gravity) * time * time) + time * verticalSpeed;
            Vector2 pos = new Vector2(posX, posY);
            return pos;
        }

        public Vector3 Evaluate(float time, Vector3 origin, Vector3 forward, Vector3 up)
        {
            var parabolaPos = Evaluate(time);
            return origin + forward * parabolaPos.x + up * parabolaPos.y;
        }
        
        // public void DebugDraw(Quaternion orietnation, Vector3 origin)
        // {
        //     Debug.DrawLine(Evaluate(0, origin, orietnation), Evaluate(duration, origin, orietnation), Color.blue, duration);
        //
        //     var count = 128;
        //
        //     Vector3 prev = Evaluate(0, origin, orietnation);
        //
        //     for (int i = 1; i <= count; ++i)
        //     {
        //         float progress = (float)i / count;
        //         float time = progress * duration;
        //         var pos = Evaluate(time, origin, orietnation);
        //         Debug.DrawLine(prev, pos, Color.yellow, duration);
        //         prev = pos;
        //     }
        //
        // }

    }
}