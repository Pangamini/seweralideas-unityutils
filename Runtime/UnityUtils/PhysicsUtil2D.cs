#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    public static class PhysicsUtil2D
    {
        private static readonly int[] LayerCollisionMaskCache = new int[32];
        private static          bool  _layerMaskCacheInitialized;

        public delegate bool Collider2DFilter(Collider2D collider);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            _layerMaskCacheInitialized = false;
        }

        // ----- CheckGameObject -----

        public static bool CheckGameObject(
            GameObject gameObject,
            Vector2 position,
            float angle,
            Vector2 scale,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            using (ListPool<Collider2D>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);

                return CheckGameObject(colliders, relativeMatrices, position, angle, scale, layerMask, useTriggers);
            }
        }

        public static bool CheckGameObject(
            GameObject gameObject,
            Vector2 position,
            float angle,
            Vector2 scale,
            Collider2DFilter filter,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            using (ListPool<Collider2D>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);

                return CheckGameObject(colliders, relativeMatrices, position, angle, scale, filter, layerMask, useTriggers);
            }
        }

        public static bool CheckGameObject(
            IList<Collider2D> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector2 position,
            float angle,
            Vector2 scale,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];

                GetChildWorldTransformRelativeTo(relativeMatrix, position, angle, scale,
                    out var colliderPos, out var colliderAngle, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CheckCollider(collider, colliderPos, colliderAngle, colliderScale, mask, useTriggers))
                    return true;
            }

            return false;
        }

        public static bool CheckGameObject(
            IList<Collider2D> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector2 position,
            float angle,
            Vector2 scale,
            Collider2DFilter filter,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];

                GetChildWorldTransformRelativeTo(relativeMatrix, position, angle, scale,
                    out var colliderPos, out var colliderAngle, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CheckCollider(collider, colliderPos, colliderAngle, colliderScale, mask, useTriggers, filter))
                    return true;
            }

            return false;
        }

        // ----- OverlapGameObject -----

        public static void OverlapGameObject(
            GameObject gameObject,
            Vector2 position,
            float angle,
            Vector2 scale,
            ICollection<Collider2D> result,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            using (ListPool<Collider2D>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);

                OverlapGameObject(colliders, relativeMatrices, position, angle, scale, result, layerMask, useTriggers);
            }
        }

        public static void OverlapGameObject(
            IList<Collider2D> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector2 position,
            float angle,
            Vector2 scale,
            ICollection<Collider2D> result,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];

                GetChildWorldTransformRelativeTo(relativeMatrix, position, angle, scale,
                    out var colliderPos, out var colliderAngle, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                OverlapCollider(collider, colliderPos, colliderAngle, colliderScale, mask, result, useTriggers);
            }
        }

        // ----- Per-type world-param extraction -----

        public static (Vector2 center, Vector2 size, float angle) GetBoxColliderParams(BoxCollider2D box, Vector2 position, float angle, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(box.offset, scale);
            Vector2 worldCenter  = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * scaledOffset);
            Vector2 worldSize    = Vector2.Scale(box.size, scale);
            return (worldCenter, worldSize, angle);
        }

        public static (Vector2 center, float radius) GetCircleColliderParams(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(circle.offset, scale);
            Vector2 worldCenter  = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * scaledOffset);
            float   maxScale     = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)); // conservative
            float   worldRadius  = circle.radius * maxScale;
            return (worldCenter, worldRadius);
        }

        public static (Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle) GetCapsuleColliderParams(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(capsule.offset, scale);
            Vector2 worldCenter  = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * scaledOffset);
            Vector2 worldSize    = Vector2.Scale(capsule.size, scale);
            return (worldCenter, worldSize, capsule.direction, angle);
        }

        // ----- Per-type Check / Overlap -----

        public static bool CheckBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapBox(center, size, worldAngle, layerMask, hits, useTriggers);
                return hits.Count > 0;
            }
        }

        public static bool CheckBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers, Collider2DFilter filter)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapBox(center, size, worldAngle, layerMask, hits, useTriggers);
                return CheckColliders(hits, filter);
            }
        }

        public static int OverlapBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            return OverlapBox(center, size, worldAngle, layerMask, result, useTriggers);
        }

        public static bool CheckCircleCollider(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers)
        {
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapCircle(center, radius, layerMask, hits, useTriggers);
                return hits.Count > 0;
            }
        }

        public static bool CheckCircleCollider(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers, Collider2DFilter filter)
        {
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapCircle(center, radius, layerMask, hits, useTriggers);
                return CheckColliders(hits, filter);
            }
        }

        public static int OverlapCircleCollider(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            return OverlapCircle(center, radius, layerMask, result, useTriggers);
        }

        public static bool CheckCapsuleCollider(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers)
        {
            var (center, size, direction, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapCapsule(center, size, direction, worldAngle, layerMask, hits, useTriggers);
                return hits.Count > 0;
            }
        }

        public static bool CheckCapsuleCollider(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers, Collider2DFilter filter)
        {
            var (center, size, direction, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapCapsule(center, size, direction, worldAngle, layerMask, hits, useTriggers);
                return CheckColliders(hits, filter);
            }
        }

        public static int OverlapCapsuleCollider(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var (center, size, direction, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);
            return OverlapCapsule(center, size, direction, worldAngle, layerMask, result, useTriggers);
        }

        // ----- Generic dispatchers -----

        public static bool CheckCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            int layerMask,
            bool useTriggers)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:         return CheckBoxCollider(box, position, angle, scale, layerMask, useTriggers);
                case CircleCollider2D circle:   return CheckCircleCollider(circle, position, angle, scale, layerMask, useTriggers);
                case CapsuleCollider2D capsule: return CheckCapsuleCollider(capsule, position, angle, scale, layerMask, useTriggers);
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        public static bool CheckCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            int layerMask,
            bool useTriggers,
            Collider2DFilter filter)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:         return CheckBoxCollider(box, position, angle, scale, layerMask, useTriggers, filter);
                case CircleCollider2D circle:   return CheckCircleCollider(circle, position, angle, scale, layerMask, useTriggers, filter);
                case CapsuleCollider2D capsule: return CheckCapsuleCollider(capsule, position, angle, scale, layerMask, useTriggers, filter);
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        public static bool CheckCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            bool useTriggers,
            Collider2DFilter filter)
        {
            int layerMask = GetLayerCollisionMask(collider.gameObject.layer);
            return CheckCollider(collider, position, angle, scale, layerMask, useTriggers, filter);
        }

        public static void OverlapCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            int layerMask,
            ICollection<Collider2D> result,
            bool useTriggers)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:
                    OverlapBoxCollider(box, position, angle, scale, layerMask, result, useTriggers);
                    return;
                case CircleCollider2D circle:
                    OverlapCircleCollider(circle, position, angle, scale, layerMask, result, useTriggers);
                    return;
                case CapsuleCollider2D capsule:
                    OverlapCapsuleCollider(capsule, position, angle, scale, layerMask, result, useTriggers);
                    return;
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    return;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        // ----- Shape-level overlap wrappers (no allocations beyond ListPool) -----

        public static int OverlapBox(Vector2 center, Vector2 size, float angle, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                Physics2D.OverlapBox(center, size, angle, filter, hits);
                AddRange(hits, result);
                return hits.Count;
            }
        }

        public static int OverlapCircle(Vector2 center, float radius, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                Physics2D.OverlapCircle(center, radius, filter, hits);
                AddRange(hits, result);
                return hits.Count;
            }
        }

        public static int OverlapCapsule(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                Physics2D.OverlapCapsule(center, size, direction, angle, filter, hits);
                AddRange(hits, result);
                return hits.Count;
            }
        }

        // ----- CastGameObject -----

        public static bool CastGameObject(
            GameObject gameObject,
            Vector2 position,
            float angle,
            Vector2 scale,
            Vector2 direction,
            float distance,
            out RaycastHit2D hit,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            using (ListPool<Collider2D>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);

                return CastGameObject(colliders, relativeMatrices, position, angle, scale, direction, distance, out hit, layerMask, useTriggers);
            }
        }

        public static bool CastGameObject(
            IList<Collider2D> colliders,
            IList<Matrix4x4> relativeMatrices,
            Vector2 position,
            float angle,
            Vector2 scale,
            Vector2 direction,
            float distance,
            out RaycastHit2D hit,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            hit = default;
            bool any = false;
            float bestDistance = float.PositiveInfinity;

            for (int index = 0; index < colliders.Count; index++)
            {
                var collider = colliders[index];
                var relativeMatrix = relativeMatrices[index];

                GetChildWorldTransformRelativeTo(relativeMatrix, position, angle, scale,
                    out var colliderPos, out var colliderAngle, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CastCollider(collider, colliderPos, colliderAngle, colliderScale, direction, distance, mask, useTriggers, out var localHit))
                {
                    if (localHit.distance < bestDistance)
                    {
                        bestDistance = localHit.distance;
                        hit          = localHit;
                        any          = true;
                    }
                }
            }

            return any;
        }

        // ----- Per-type Cast -----

        public static bool CastBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            return BoxCast(center, size, worldAngle, direction, distance, layerMask, useTriggers, out hit);
        }

        public static bool CastCircleCollider(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            return CircleCast(center, radius, direction, distance, layerMask, useTriggers, out hit);
        }

        public static bool CastCapsuleCollider(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var (center, size, capsuleDirection, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);
            return CapsuleCast(center, size, capsuleDirection, worldAngle, direction, distance, layerMask, useTriggers, out hit);
        }

        // ----- Generic Cast dispatcher -----

        public static bool CastCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            Vector2 direction,
            float distance,
            int layerMask,
            bool useTriggers,
            out RaycastHit2D hit)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:         return CastBoxCollider(box, position, angle, scale, direction, distance, layerMask, useTriggers, out hit);
                case CircleCollider2D circle:   return CastCircleCollider(circle, position, angle, scale, direction, distance, layerMask, useTriggers, out hit);
                case CapsuleCollider2D capsule: return CastCapsuleCollider(capsule, position, angle, scale, direction, distance, layerMask, useTriggers, out hit);
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    hit = default;
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        // ----- Shape-level Cast wrappers (closest hit) -----

        public static bool BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.BoxCast(origin, size, angle, direction, filter, hits, distance);
                return FirstHit(hits, count, out hit);
            }
        }

        public static bool CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.CircleCast(origin, radius, direction, filter, hits, distance);
                return FirstHit(hits, count, out hit);
            }
        }

        public static bool CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.CapsuleCast(origin, size, capsuleDirection, angle, direction, filter, hits, distance);
                return FirstHit(hits, count, out hit);
            }
        }

        // ----- Layer mask cache (mirrors 3D version) -----

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
                if (!Physics2D.GetIgnoreLayerCollision(layer, other))
                    mask |= 1 << other;
            }
            return mask;
        }

        // ----- Transform helpers (reuse the 3D math) -----

        public static void GetRelativeMatrices(Transform root, List<Collider2D> colliders, List<Matrix4x4> result)
        {
            foreach (var coll in colliders)
                result.Add(PhysicsUtil.GetRelativeMatrix(root, coll.transform));
        }

        public static void GetChildWorldTransformRelativeTo(
            Transform prefabRoot,
            Transform child,
            Vector2 desiredPosition,
            float desiredAngle,
            Vector2 desiredScale,
            out Vector2 worldPosition,
            out float worldAngle,
            out Vector2 worldScale)
        {
            Matrix4x4 relativeMatrix = PhysicsUtil.GetRelativeMatrix(prefabRoot, child);
            GetChildWorldTransformRelativeTo(relativeMatrix, desiredPosition, desiredAngle, desiredScale,
                out worldPosition, out worldAngle, out worldScale);
        }

        public static void GetChildWorldTransformRelativeTo(
            Matrix4x4 relativeMatrix,
            Vector2 desiredPosition,
            float desiredAngle,
            Vector2 desiredScale,
            out Vector2 worldPosition,
            out float worldAngle,
            out Vector2 worldScale)
        {
            PhysicsUtil.GetChildWorldTransformRelativeTo(
                relativeMatrix,
                new Vector3(desiredPosition.x, desiredPosition.y, 0f),
                Quaternion.Euler(0f, 0f, desiredAngle),
                new Vector3(desiredScale.x, desiredScale.y, 1f),
                out var worldPosition3, out var worldRotation3, out var worldScale3);

            worldPosition = worldPosition3;
            worldAngle    = worldRotation3.eulerAngles.z;
            worldScale    = new Vector2(worldScale3.x, worldScale3.y);
        }

        // ----- Private helpers -----

        private static ContactFilter2D MakeFilter(int layerMask, bool useTriggers)
        {
            var filter = new ContactFilter2D
            {
                useTriggers = useTriggers,
            };
            filter.SetLayerMask(layerMask);
            return filter;
        }

        private static void AddRange(List<Collider2D> source, ICollection<Collider2D> destination)
        {
            int count = source.Count;
            for (int i = 0; i < count; i++)
                destination.Add(source[i]);
        }

        private static bool CheckColliders(List<Collider2D> colliders, Collider2DFilter filter)
        {
            int count = colliders.Count;
            for (int i = 0; i < count; i++)
            {
                if (filter(colliders[i]))
                    return true;
            }
            return false;
        }

        private static bool FirstHit(List<RaycastHit2D> hits, int count, out RaycastHit2D hit)
        {
            // Physics2D cast results are sorted by distance ascending — hits[0] is closest.
            if (count > 0)
            {
                hit = hits[0];
                return true;
            }
            hit = default;
            return false;
        }
    }
}
