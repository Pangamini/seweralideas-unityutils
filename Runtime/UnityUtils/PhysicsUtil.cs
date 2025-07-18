using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class PhysicsUtil
    {
        const int c_hitBufferSize = 32;
        const int c_colliderBufferSize = 32;
        private static readonly RaycastHit[] m_hitBuffer = new RaycastHit[c_hitBufferSize];
        private static readonly Collider[] m_colliderBuffer = new Collider[c_colliderBufferSize];

        private static readonly float[] m_hitDistancesBuffer = new float[c_hitBufferSize];
        private static bool m_colliderBufferLock;

        public static bool VisitSphereCast(Ray ray, float radius, out RaycastHit hitResult, float maxDistance, LayerMask layerMask, Visitor<RaycastHit> visitor)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = Physics.SphereCastNonAlloc(ray, radius, m_hitBuffer, maxDistance, layerMask);
                var ret = VisitHits(ray.origin, hitCount, m_hitBuffer, out hitResult, visitor);
                ClearBuffer(m_hitBuffer, hitCount); // to avoid memory leaks, hits refer to components
                return ret;
            }
            catch
            {
                ClearBuffer(m_hitBuffer, m_hitBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }

        public static bool VisitSphereCast(this PhysicsScene scene, Ray ray, float radius, out RaycastHit hitResult, float maxDistance, LayerMask layerMask, Visitor<RaycastHit> visitor, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = scene.SphereCast(ray.origin, radius, ray.direction, m_hitBuffer, maxDistance, layerMask, triggerInteraction);
                var ret = VisitHits(ray.origin, hitCount, m_hitBuffer, out hitResult, visitor);
                ClearBuffer(m_hitBuffer, hitCount); // to avoid memory leaks, hits refer to components
                return ret;
            }
            catch
            {
                ClearBuffer(m_hitBuffer, m_hitBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }

        private static void EnsureCapacity(List<Collider> list, int elemCountToAdd)
        {
            var newCap = list.Count + elemCountToAdd;
            if (list.Capacity < newCap)
                list.Capacity = newCap;
        }

        private static void AddColliderBufferToList(List<Collider> result, int hitCount)
        {
            EnsureCapacity(result, hitCount);

            for (int i = 0; i < hitCount; ++i)
            {
                result.Add(m_colliderBuffer[i]);
                m_colliderBuffer[i] = null;
            }
        }

        public static int OverlapSphere(Vector3 center, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = Physics.OverlapSphereNonAlloc(center, radius, m_colliderBuffer, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }

        public static int OverlapBox(Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = Physics.OverlapBoxNonAlloc(center, extents, m_colliderBuffer, rotation, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }
        
        public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, m_colliderBuffer, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }
        
        public static int OverlapSphere(this PhysicsScene scene, Vector3 center, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = scene.OverlapSphere(center, radius, m_colliderBuffer, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }

        public static int OverlapBox(this PhysicsScene scene, Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = scene.OverlapBox(center, extents, m_colliderBuffer, rotation, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }
        
        public static int OverlapCapsule(this PhysicsScene scene, Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (m_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                m_colliderBufferLock = true;
                var hitCount = scene.OverlapCapsule(point0, point1, radius, m_colliderBuffer, layerMask, triggerInteraction);
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(m_colliderBuffer, m_colliderBuffer.Length);
                throw;
            }
            finally
            {
                m_colliderBufferLock = false;
            }
        }

        private static void ClearBuffer<T>(T[] buffer, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                buffer[i] = default;
            }
        }

        private static bool VisitHits(Vector3 origin, int hitCount, RaycastHit[] hitBuffer, out RaycastHit hitResult, Visitor<RaycastHit> visitor)
        {
            for (int i = 0; i < hitCount; ++i)
            {
                var hit = hitBuffer[i];
                if(hit.point == default && hit.distance == 0f)
                {
                    hit.point = origin;
                    hitBuffer[i] = hit;
                }
                
                m_hitDistancesBuffer[i] = (hit.point - origin).sqrMagnitude;
            }

            Array.Sort(m_hitDistancesBuffer, hitBuffer, 0, hitCount);

            for (int i = 0; i < hitCount; ++i)
                if (visitor(hitBuffer[i]))
                {
                    hitResult = hitBuffer[i];
                    return true;
                }

            hitResult = default;
            return false;
        }

        private static bool VisitColliders(int hitCount, Collider[] colliderBuffer, out Collider hitResult, Visitor<Collider> visitor)
        {
            for (int i = 0; i < hitCount; ++i)
                if (visitor(colliderBuffer[i]))
                {
                    hitResult = colliderBuffer[i];
                    return true;
                }

            hitResult = default;
            return false;
        }
    }
}