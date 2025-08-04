using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class PhysicsUtil
    {
        private static RaycastHit[] _hitBuffer          = new RaycastHit[256];
        private static Collider[]   _colliderBuffer     = new Collider[256];
        private static float[]      _hitDistancesBuffer = new float[256];
        private static bool         _colliderBufferLock;

        private static bool CheckBufferSize<T>(ref T[] buffer, int hitCount)
        {
            if (hitCount < buffer.Length)
                return false;

            var newSize = buffer.Length * 2;
            buffer = new T[newSize];
            return true;
        }


        public static bool VisitSphereCast(Ray ray, float radius, out RaycastHit hitResult, float maxDistance, LayerMask layerMask, Visitor<RaycastHit> visitor)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;

                int hitCount;
                do
                {
                    hitCount = Physics.SphereCastNonAlloc(ray, radius, _hitBuffer, maxDistance, layerMask);
                } while (CheckBufferSize(ref _hitBuffer, hitCount));

                var ret = VisitHits(ray.origin, hitCount, out hitResult, visitor);
                ClearBuffer(_hitBuffer, hitCount); // to avoid memory leaks, hits refer to components
                return ret;
            }
            catch
            {
                ClearBuffer(_hitBuffer, _hitBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static bool VisitSphereCast(this PhysicsScene scene, Ray ray, float radius, out RaycastHit hitResult, float maxDistance, LayerMask layerMask, Visitor<RaycastHit> visitor,
            QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = Physics.SphereCastNonAlloc(ray, radius, _hitBuffer, maxDistance, layerMask);
                } while (CheckBufferSize(ref _hitBuffer, hitCount));
                
                var ret = VisitHits(ray.origin, hitCount, out hitResult, visitor);
                ClearBuffer(_hitBuffer, hitCount); // to avoid memory leaks, hits refer to components
                return ret;
            }
            catch
            {
                ClearBuffer(_hitBuffer, _hitBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
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
                result.Add(_colliderBuffer[i]);
                _colliderBuffer[i] = null;
            }
        }

        public static int OverlapSphere(Vector3 center, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                int hitCount;
                do
                {
                    hitCount = Physics.OverlapSphereNonAlloc(center, radius, _colliderBuffer, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));
                
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static int OverlapBox(Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = Physics.OverlapBoxNonAlloc(center, extents, _colliderBuffer, rotation, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));
                
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, _colliderBuffer, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));
                
                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static int OverlapSphere(this PhysicsScene scene, Vector3 center, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = scene.OverlapSphere(center, radius, _colliderBuffer, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));

                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static int OverlapBox(this PhysicsScene scene, Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, List<Collider> result,
            QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = scene.OverlapBox(center, extents, _colliderBuffer, rotation, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));

                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        public static int OverlapCapsule(this PhysicsScene scene, Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, List<Collider> result, QueryTriggerInteraction triggerInteraction)
        {
            if (_colliderBufferLock) throw new Exception("PhysicsUtil methods cannot be used recursively, as they reuse a buffer");
            try
            {
                _colliderBufferLock = true;
                
                int hitCount;
                do
                {
                    hitCount = scene.OverlapCapsule(point0, point1, radius, _colliderBuffer, layerMask, triggerInteraction);
                } while (CheckBufferSize(ref _colliderBuffer, hitCount));

                AddColliderBufferToList(result, hitCount);
                return hitCount;
            }
            catch
            {
                ClearBuffer(_colliderBuffer, _colliderBuffer.Length);
                throw;
            }
            finally
            {
                _colliderBufferLock = false;
            }
        }

        private static void ClearBuffer<T>(T[] buffer, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                buffer[i] = default;
            }
        }

        private static bool VisitHits(Vector3 origin, int hitCount, out RaycastHit hitResult, Visitor<RaycastHit> visitor)
        {
            if(_hitDistancesBuffer.Length != _hitBuffer.Length)
                _hitDistancesBuffer = new float[_hitBuffer.Length];
            
            for (int i = 0; i < hitCount; ++i)
            {
                var hit = _hitBuffer[i];
                if (hit.point == default && hit.distance == 0f)
                {
                    hit.point = origin;
                    _hitBuffer[i] = hit;
                }

                _hitDistancesBuffer[i] = (hit.point - origin).sqrMagnitude;
            }

            Array.Sort(_hitDistancesBuffer, _hitBuffer, 0, hitCount);

            for (int i = 0; i < hitCount; ++i)
                if (visitor(_hitBuffer[i]))
                {
                    hitResult = _hitBuffer[i];
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