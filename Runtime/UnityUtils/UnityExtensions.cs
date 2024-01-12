using System.Collections.Generic;
using JetBrains.Annotations;
using SeweralIdeas.Pooling;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class UnityExtensions
    {
        public static bool ClampScreenPointToRectTransform(this RectTransform rectTransform, ref Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out var localPoint);
            Rect boundsRect = rectTransform.rect;
            var normalized = Rect.PointToNormalized(boundsRect, localPoint);
            var boundLocalPoint = Rect.NormalizedToPoint(boundsRect, normalized);
            var boundWorldPoint = rectTransform.TransformPoint(boundLocalPoint);
            var boundScreenPoint = Vector2Int.FloorToInt(RectTransformUtility.WorldToScreenPoint(null, boundWorldPoint));

            if(boundScreenPoint != screenPoint)
            {
                screenPoint = boundScreenPoint;
                return true;
            }
            return false;
        }

        public static T FindObjectOfType<T>(this UnityEngine.SceneManagement.Scene scene) where T:class
        {
            using (ListPool<GameObject>.Get(out var list))
            {
                scene.GetRootGameObjects(list);
                foreach (var go in list)
                {
                    var comp = go.GetComponentInChildren<T>();
                    if (comp != null) 
                        return comp;
                }
            }
            return null;
        }

        public static void FindObjectsOfType<T>(this UnityEngine.SceneManagement.Scene scene, List<T> result) where T:class
        {
            result.Clear();
            var compList = new List<T>();   // IL2CPP AOT would fail with StackAlloc
            //using (ListPool<T>.Get(out var compList))
            using (ListPool<GameObject>.Get(out var goList))
            {
                scene.GetRootGameObjects(goList);
                foreach (var go in goList)
                {
                    compList.Clear();
                    go.GetComponentsInChildren(compList);
                    result.AddList(compList);
                }
            }
        }
        public static void PlayRandomSound(this AudioSource source, AudioClip[] sounds)
        {
            if (sounds.Length == 0)
                return;

            var index = Random.Range(0, sounds.Length);
            var clip = sounds[index];
            source.PlayOneShot(clip);
        }

        public static float Squared(this float value)
        {
            return value * value;
        }

        public static IEnumerator<YieldInstruction> WaitRoutine(YieldInstruction waitInstruction, System.Action action)
        {
            yield return waitInstruction;
            action();
        }

        public static void Wait(this MonoBehaviour component, YieldInstruction waitInstruction, System.Action action)
        {
            component.StartCoroutine(WaitRoutine(waitInstruction, action));
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var r = go.GetComponent<T>();
            return r ? r : go.AddComponent<T>();
        }

        public static bool IsPlaying(this GameObject go)
        {
            #if UNITY_EDITOR
            if(!Application.isPlaying)
                return false;
            #endif
            return go.scene.IsValid();
        }
        
        public static float ExplosionImpulseFalloff(float distance, float radius, ExplosionFalloff falloffMode)
        {
            switch (falloffMode)
            {
                case ExplosionFalloff.Linear:
                    return Mathf.Clamp01(1 - distance / radius);
                case ExplosionFalloff.None:
                    return 1;
                default:
                    throw new System.ArgumentException();
            }
        }

        public enum ExplosionFalloff
        {
            None,
            Linear
        }

        private static readonly Collider[] m_buffer = new Collider[256];

        public static void Explosion(this PhysicsScene phys3D, Vector3 position, float radius, float impulse, ExplosionFalloff falloffMode = ExplosionFalloff.None)
        {
            float radiusSqr = radius * radius;

            using (HashSetPool<Rigidbody>.Get(out var bodies))
            {
                int colliderCount = phys3D.OverlapSphere(position, radius, m_buffer, -1, QueryTriggerInteraction.Ignore);
                if (colliderCount >= m_buffer.Length)
                    Debug.LogError($"{nameof(Explosion)} collider buffer too small!");
                for (int i = 0; i < colliderCount; ++i)
                {
                    var collider = m_buffer[i];
                    m_buffer[i] = null;

                    var body = collider.attachedRigidbody;
                    if (body == null || body.isKinematic)
                        continue;

                    if (bodies.Add(body))
                    {
                        // apply impulse
                        var offset = body.position - position;
                        var distanceSqr = offset.sqrMagnitude;
                        if (distanceSqr > radiusSqr)
                            continue;

                        float distance = Mathf.Sqrt(distanceSqr);
                        var direction = distance > 0 ? offset / distance : Vector3.forward;
                        var falloff = ExplosionImpulseFalloff(distance, radius, falloffMode);
                        body.AddForce(direction * falloff * impulse, ForceMode.Impulse);
                    }
                }
            }
        }

        public static void Explosion(this PhysicsScene2D phys2D, Vector2 position, float radius, float impulse, ExplosionFalloff falloffMode = ExplosionFalloff.None)
        {
            float radiusSqr = radius * radius;

            ContactFilter2D filter = new ContactFilter2D()
            {
                layerMask = -1,
                useTriggers = false,
            };

            using (ListPool<Collider2D>.Get(out var colliders))
            using (HashSetPool<Rigidbody2D>.Get(out var bodies))
            {
                phys2D.OverlapCircle(position, radius, filter, colliders);
                foreach (var collider in colliders)
                {
                    var body = collider.attachedRigidbody;
                    if (body == null || body.isKinematic)
                        continue;
                    if (bodies.Add(body))
                    {
                        // apply impulse
                        var offset = body.position - position;
                        var distanceSqr = offset.sqrMagnitude;
                        if (distanceSqr > radiusSqr)
                            continue;

                        float distance = Mathf.Sqrt(distanceSqr);
                        var direction = distance > 0 ? offset / distance : Vector2.up;
                        var falloff = ExplosionImpulseFalloff(distance, radius, falloffMode);
                        body.AddForce(direction * falloff * impulse, ForceMode2D.Impulse);

                    }
                }
            }
        }
        
        /// <summary>
        /// Similar to GetComponentsInChildren, but stops entering children with a TBlock component.
        /// NOTE that the target collection is NOT cleared.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="result"></param>
        /// <typeparam name="TComp"></typeparam>
        /// <typeparam name="TBlock"></typeparam>
        public static void GetComponentsInChildrenRecursively<TComp, TBlock>(this GameObject gameObject, ICollection<TComp> result)
            where TComp : Component
            where TBlock : Component
        {
            using (ListPool<TComp>.Get(out var thisGoComps))
            {
                gameObject.GetComponents(thisGoComps);
                foreach (TComp thisGoComp in thisGoComps)
                {
                    result.Add(thisGoComp);
                }
            }

            var transform = gameObject.transform;
            int childCount = transform.childCount;
            for( int i = 0; i < childCount; ++i)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if(child.GetComponent<TBlock>() != null)
                    continue;   // skip child that has TBlock

                GetComponentsInChildrenRecursively<TComp, TBlock>(child, result);
            }
        }
    }
}