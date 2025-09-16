#nullable enable
using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    public static class PhysicsUtil
    {
        private static          RaycastHit[] _hitBuffer              = new RaycastHit[256];
        private static          Collider?[]  _colliderBuffer         = new Collider[256];
        private static          float[]      _hitDistancesBuffer     = new float[256];
        private static readonly int[]        LayerCollisionMaskCache = new int[32];
        private static          bool         _colliderBufferLock;
        private static          bool         _layerMaskCacheInitialized;

        public delegate bool ColliderFilter(Collider collider);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            _layerMaskCacheInitialized  = false;
        }


        public static bool CheckGameObject(
            GameObject gameObject,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            ColliderFilter filter,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            using (ListPool<Collider>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);
                
                return CheckGameObject(colliders, relativeMatrices, position, rotation, scale, filter, layerMask, qti);
            }
        }

        public static bool CheckGameObject(
            GameObject gameObject,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            using (ListPool<Collider>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);
                
                return CheckGameObject(colliders, relativeMatrices, position, rotation, scale, layerMask, qti);
            }
        }

        public static bool CheckGameObject(
            IList<Collider> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            for (var index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];
                
                GetChildWorldTransformRelativeTo(relativeMatrix, position, rotation,
                    scale, out var colliderPos, out var colliderRot, out var colliderScale);

                var mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CheckCollider(collider, colliderPos, colliderRot, colliderScale, mask, qti))
                    return true;
            }

            return false;
        }

        public static bool CheckGameObject(
            IList<Collider> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            ColliderFilter filter,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            for (var index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];
                
                GetChildWorldTransformRelativeTo(relativeMatrix, position, rotation,
                    scale, out var colliderPos, out var colliderRot, out var colliderScale);

                var mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CheckCollider(collider, colliderPos, colliderRot, colliderScale, mask, qti, filter))
                    return true;
            }

            return false;
        }

        public static void OverlapGameObject(
            GameObject gameObject,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            ICollection<Collider> result,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            using (ListPool<Collider>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);
                
                OverlapGameObject(colliders, relativeMatrices, position , rotation, scale, result, layerMask, qti);
            }
        }

        public static void OverlapGameObject(
            IList<Collider> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            ICollection<Collider> result,
            int layerMask = Physics.AllLayers,
            QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
        {
            for (var index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];
                
                GetChildWorldTransformRelativeTo(relativeMatrix, position, rotation,
                    scale, out var colliderPos, out var colliderRot, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                OverlapCollider(collider, colliderPos, colliderRot, colliderScale, mask, result, qti);
            }
        }

        public static bool CheckBoxCollider(BoxCollider box, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti)
        {
            var (center, halfExtents) = GetBoxColliderCenterAndHalfExtents(box, position, rotation, scale);
            return Physics.CheckBox(center, halfExtents, rotation, layerMask, qti);
        }
        
        public static bool CheckBoxCollider(BoxCollider box, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti, ColliderFilter filter)
        {
            var (center, halfExtents) = GetBoxColliderCenterAndHalfExtents(box, position, rotation, scale);
            
            using (ListPool<Collider>.Get(out var colliders))
            {
                OverlapBox(center, halfExtents, rotation, layerMask, colliders, qti);
                return CheckColliders(colliders, filter);
            }
        }

        public static int OverlapBoxCollider(BoxCollider box, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, ICollection<Collider> result, QueryTriggerInteraction qti)
        {
            var (center, halfExtents) = GetBoxColliderCenterAndHalfExtents(box, position, rotation, scale);
            return OverlapBox(center, halfExtents, rotation, layerMask, result, qti);
        }


        public static int OverlapSphereCollider(SphereCollider sphere, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, ICollection<Collider> result, QueryTriggerInteraction qti)
        {
            var (center, radius) = GetSphereColliderCenterAndRadius(sphere, position, rotation, scale);
            return OverlapSphere(center, radius, layerMask, result, qti);
        }

        public static int OverlapCapsuleCollider(CapsuleCollider capsule, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, ICollection<Collider> result,
            QueryTriggerInteraction qti)
        {
            var (point1, point2, radius) = GetCapsuleColliderParams(capsule, position, rotation, scale);
            return OverlapCapsule(point1, point2, radius, layerMask, result, qti);
        }

        public static (Vector3 center, Vector3 halfExtents) GetBoxColliderCenterAndHalfExtents(BoxCollider box, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Vector3 worldCenter = position + rotation * Vector3.Scale(box.center, scale);
            Vector3 worldHalfExtents = Vector3.Scale(box.size * 0.5f, scale);
            return (worldCenter, worldHalfExtents);
        }

        public static bool CheckSphereCollider(SphereCollider sphere, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti)
        {
            var (center, radius) = GetSphereColliderCenterAndRadius(sphere, position, rotation, scale);
            return Physics.CheckSphere(center, radius, layerMask, qti);
        }

        public static bool CheckSphereCollider(SphereCollider sphere, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti, ColliderFilter filter)
        {
            var (center, radius) = GetSphereColliderCenterAndRadius(sphere, position, rotation, scale);
            using (ListPool<Collider>.Get(out var colliders))
            {
                OverlapSphere(center, radius, layerMask, colliders, qti);
                return CheckColliders(colliders, filter);
            }
        }

        public static (Vector3 center, float radius) GetSphereColliderCenterAndRadius(SphereCollider sphere, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Vector3 worldCenter = position + rotation * Vector3.Scale(sphere.center, scale);
            float maxScale = Mathf.Max(Mathf.Max(scale.x, scale.y), scale.z); // conservative approach
            float worldRadius = sphere.radius * maxScale;
            return (worldCenter, worldRadius);
        }

        public static bool CheckCapsuleCollider(CapsuleCollider capsule, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti)
        {
            var (point1, point2, radius) = GetCapsuleColliderParams(capsule, position, rotation, scale);
            return Physics.CheckCapsule(point1, point2, radius, layerMask, qti);
        }

        public static bool CheckCapsuleCollider(CapsuleCollider capsule, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, QueryTriggerInteraction qti, ColliderFilter filter)
        {
            var (point1, point2, radius) = GetCapsuleColliderParams(capsule, position, rotation, scale);

            using (ListPool<Collider>.Get(out var colliders))
            {
                OverlapCapsule(point1, point2, radius, layerMask, colliders, qti);
                return CheckColliders(colliders, filter);
            }
        }

        public static (Vector3 point1, Vector3 point2, float radius) GetCapsuleColliderParams(CapsuleCollider capsule, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;

            var up = capsule.direction switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.forward,
                _ => Vector3.up,
            };
            Vector3 scaledCenter = Vector3.Scale(center, scale);
            Vector3 worldCenter = position + (rotation * scaledCenter);

            float scaledRadius = radius * Mathf.Max(scale[(capsule.direction + 1) % 3], scale[(capsule.direction + 2) % 3]); // max. of other two axes
            float scaledHeight = height * scale[capsule.direction];

            float segment = Mathf.Max(0f, (scaledHeight * 0.5f) - scaledRadius);
            Vector3 offset = up * segment;

            Vector3 point1 = worldCenter + (rotation * offset);
            Vector3 point2 = worldCenter - (rotation * offset);
            return (point1, point2, scaledRadius);
        }

        public static LayerMask GetLayerCollisionMask(int layer)
        {
            if (!_layerMaskCacheInitialized)
            {
                for (int i = 0; i < 32; i++)
                    LayerCollisionMaskCache[i] = ComputeLayerCollisionMask(i);
                _layerMaskCacheInitialized = true;
            }

            return LayerCollisionMaskCache[layer];
        }

        private static int ComputeLayerCollisionMask(int layer)
        {
            int mask = 0;
            for (int other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, other))
                    mask |= 1 << other;
            }

            return mask;
        }

        // Recursively computes the local matrix from root to child
        public static Matrix4x4 GetRelativeMatrix(Transform root, Transform target)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            Transform current = target;
            while (current != null && current != root)
            {
                matrix = Matrix4x4.TRS(current.localPosition, current.localRotation, current.localScale) * matrix;
                current = current.parent;
            }

            if (current != root)
                throw new ArgumentException("The target transform is not a child of the root.");

            return matrix;
        }
        
        public static void GetRelativeMatrices(Transform root, List<Collider> colliders, List<Matrix4x4> result)
        {
            foreach (var coll in colliders)
                result.Add(GetRelativeMatrix(root, coll.transform));
        }

        public static void GetChildWorldTransformRelativeTo(
            Transform prefabRoot,
            Transform child,
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            Vector3 desiredScale,
            out Vector3 worldPosition,
            out Quaternion worldRotation,
            out Vector3 worldScale)
        {
            // Step 1: Get the local transform matrix from the root to the child
            Matrix4x4 relativeMatrix = GetRelativeMatrix(prefabRoot, child);
            GetChildWorldTransformRelativeTo(relativeMatrix, desiredPosition, desiredRotation, desiredScale, out worldPosition, out worldRotation, out worldScale);
        }
        
        public static void GetChildWorldTransformRelativeTo(
            Matrix4x4 relativeMatrix,
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            Vector3 desiredScale,
            out Vector3 worldPosition,
            out Quaternion worldRotation,
            out Vector3 worldScale)
        {
            // Step 2: Create the hypothetical root transform matrix
            Matrix4x4 rootMatrix = Matrix4x4.TRS(desiredPosition, desiredRotation, desiredScale);

            // Step 3: Compute the world matrix for the child
            Matrix4x4 childWorldMatrix = rootMatrix * relativeMatrix;

            // Step 4: Decompose matrix
            worldPosition = childWorldMatrix.MultiplyPoint3x4(Vector3.zero);
            worldRotation = childWorldMatrix.rotation;
            worldScale = childWorldMatrix.lossyScale;
        }

        private static bool CheckColliders(Collider[] colliders, int count, ColliderFilter? filter)
        {
            if (filter == null)
                return count > 0;
            for (int i = 0; i < count; i++)
            {
                var collider = colliders[i];
                if (filter(collider))
                    return true; // at least one collider found that's not filtered out
            }

            return false;
        }

        private static bool CheckColliders(List<Collider> colliders, ColliderFilter filter)
        {
            int count = colliders.Count;
            for (int i = 0; i < count; i++)
            {
                var collider = colliders[i];
                if (filter(collider))
                    return true; // at least one collider found that's not filtered out
            }

            return false;
        }

        public static bool CheckCollider(
            Collider collider,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            QueryTriggerInteraction qti,
            ColliderFilter filter)
        {
            var layerMask = GetLayerCollisionMask(collider.gameObject.layer);
            return CheckCollider(collider, position, rotation, scale, layerMask, qti, filter);
        }

        public static void OverlapCollider(
            Collider collider,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int layerMask,
            ICollection<Collider> result,
            QueryTriggerInteraction qti)
        {
            if(collider == null)
                throw new ArgumentNullException(nameof(collider));
            
            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider box:
                    OverlapBoxCollider(box, position, rotation, scale, layerMask, result, qti);
                    return;
                case SphereCollider sphere:
                    OverlapSphereCollider(sphere, position, rotation, scale, layerMask, result, qti);
                    return;
                case CapsuleCollider capsule:
                    OverlapCapsuleCollider(capsule, position, rotation, scale, layerMask, result, qti);
                    return;
                default:
                    Debug.LogWarning($"Collider type {collider.GetType().Name} not supported.");
                    return;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        public static bool CheckCollider(
            Collider collider,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int layerMask,
            QueryTriggerInteraction qti)
        {
            if(collider == null)
                throw new ArgumentNullException(nameof(collider));
            
            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider box:
                    return CheckBoxCollider(box, position, rotation, scale, layerMask, qti);
                case SphereCollider sphere:
                    return CheckSphereCollider(sphere, position, rotation, scale, layerMask, qti);
                case CapsuleCollider capsule:
                    return CheckCapsuleCollider(capsule, position, rotation, scale, layerMask, qti);
                default:
                    Debug.LogWarning($"Collider type {collider.GetType().Name} not supported.");
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }
        
        public static bool CheckCollider(
            Collider collider,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int layerMask,
            QueryTriggerInteraction qti,
            ColliderFilter filter)
        {
            if(collider == null)
                throw new ArgumentNullException(nameof(collider));
            
            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider box:
                    return CheckBoxCollider(box, position, rotation, scale, layerMask, qti, filter);
                case SphereCollider sphere:
                    return CheckSphereCollider(sphere, position, rotation, scale, layerMask, qti, filter);
                case CapsuleCollider capsule:
                    return CheckCapsuleCollider(capsule, position, rotation, scale, layerMask, qti, filter);
                default:
                    Debug.LogWarning($"Collider type {collider.GetType().Name} not supported.");
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

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

        private static void AddColliderBufferToList(ICollection<Collider> result, int hitCount)
        {
            for (int i = 0; i < hitCount; ++i)
            {
                Debug.Assert(_colliderBuffer[i] != null);
                result.Add(_colliderBuffer[i]!);
                _colliderBuffer[i] = null;
            }
        }

        public static int OverlapSphere(Vector3 center, float radius, LayerMask layerMask, ICollection<Collider> result, QueryTriggerInteraction triggerInteraction)
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

        public static int OverlapSphere(this PhysicsScene scene, Vector3 center, float radius, LayerMask layerMask, ICollection<Collider> result, QueryTriggerInteraction triggerInteraction)
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

        public static int OverlapBox(Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, ICollection<Collider> result, QueryTriggerInteraction triggerInteraction)
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

        public static int OverlapBox(this PhysicsScene scene, Vector3 center, Vector3 extents, Quaternion rotation, LayerMask layerMask, ICollection<Collider> result,
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

        public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, ICollection<Collider> result, QueryTriggerInteraction triggerInteraction)
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

        public static int OverlapCapsule(this PhysicsScene scene, Vector3 point0, Vector3 point1, float radius, LayerMask layerMask, ICollection<Collider> result,
            QueryTriggerInteraction triggerInteraction)
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

        private static void ClearBuffer<T>(T?[] buffer, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                buffer[i] = default;
            }
        }

        private static bool VisitHits(Vector3 origin, int hitCount, out RaycastHit hitResult, Visitor<RaycastHit> visitor)
        {
            if (_hitDistancesBuffer.Length != _hitBuffer.Length)
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

        private static bool VisitColliders(int hitCount, Collider[] colliderBuffer, out Collider? hitResult, Visitor<Collider> visitor)
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

        public static bool FindClosest<T>(Vector3 position, float searchRadius, int layerMask, QueryTriggerInteraction qti, out T? closest)
            where T : Component
        {
            using (ListPool<Collider>.Get(out var colliders))
            {
                OverlapSphere(position, searchRadius, layerMask, colliders, qti);
                closest = null;
                float closestDistSqr = float.PositiveInfinity;

                foreach (Collider collider in colliders)
                {
                    var component = collider.GetComponentInParent<T>();
                    if (!component)
                        continue;

                    Vector3 closestPoint = collider.ClosestPoint(position);
                    float distSqr = (closestPoint - position).sqrMagnitude;
                    if (distSqr >= closestDistSqr)
                        continue;

                    closestDistSqr = distSqr;
                    closest = component;
                }

                return closest != null;
            }
        }
    }
}