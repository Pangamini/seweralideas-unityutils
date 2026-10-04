#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
#if UNITY_TILEMAP
using UnityEngine.Tilemaps;
#endif

namespace SeweralIdeas.UnityUtils
{
    public static class PhysicsUtil2D
    {
        private static readonly int[] LayerCollisionMaskCache = new int[32];
        private static          bool  _layerMaskCacheInitialized;

        public delegate bool Collider2DFilter(Collider2D collider);
        public delegate bool RaycastHit2DFilter(RaycastHit2D hit);
        // Convention (matches Collider2DFilter): returns true if the hit/collider is kept,
        // false to skip — for casts, the next hit in distance order is tried.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            _layerMaskCacheInitialized = false;

            // HideAndDontSave objects outlive the play session when domain reload is off.
            if (_polygonScratch != null)
                UnityEngine.Object.DestroyImmediate(_polygonScratch.gameObject);
            _polygonScratch = null;
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

        // BoxCollider2D.edgeRadius dilates the box outward by that amount, in every direction (rounded corners).
        public static float GetBoxEdgeRadius(BoxCollider2D box, Vector2 scale)
            => box.edgeRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)); // same convention as the circle radius

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
                OverlapRoundedBox(center, size, worldAngle, GetBoxEdgeRadius(box, scale), layerMask, hits, useTriggers);
                return hits.Count > 0;
            }
        }

        public static bool CheckBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, int layerMask, bool useTriggers, Collider2DFilter filter)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                OverlapRoundedBox(center, size, worldAngle, GetBoxEdgeRadius(box, scale), layerMask, hits, useTriggers);
                return CheckColliders(hits, filter);
            }
        }

        public static int OverlapBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            return OverlapRoundedBox(center, size, worldAngle, GetBoxEdgeRadius(box, scale), layerMask, result, useTriggers);
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

        // Physics2D has no polygon shape query, so the polygon is copied onto a hidden scratch collider, placed at the
        // requested pose, and queried with Physics2D.OverlapCollider. The scratch collider is only enabled for the
        // duration of the query, so nothing else can ever see (or collide with) it.
        private static PolygonCollider2D? _polygonScratch;

        public static int OverlapPolygonCollider(PolygonCollider2D polygon, Vector2 position, float angle, Vector2 scale, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            PolygonCollider2D scratch = GetPolygonScratch();
            Transform scratchTransform = scratch.transform;

            // Set everything up while disabled, so the collider enters the physics world already in place.
            scratchTransform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            scratchTransform.localScale = new Vector3(scale.x, scale.y, 1f);
            scratch.gameObject.layer = polygon.gameObject.layer;
            scratch.offset = polygon.offset;

            int pathCount = polygon.pathCount;
            scratch.pathCount = pathCount;
            using (ListPool<Vector2>.Get(out var path))
            {
                for (int i = 0; i < pathCount; i++)
                {
                    polygon.GetPath(i, path);
                    scratch.SetPath(i, path);
                }
            }

            var filter = MakeFilter(layerMask, useTriggers);
            using (ListPool<Collider2D>.Get(out var hits))
            {
                scratch.enabled = true;
                try
                {
                    Physics2D.OverlapCollider(scratch, filter, hits);
                }
                finally
                {
                    scratch.enabled = false;
                }

                AddRange(hits, result);
                return hits.Count;
            }
        }

        private static PolygonCollider2D GetPolygonScratch()
        {
            if (_polygonScratch != null)
                return _polygonScratch;

            var go = new GameObject("PhysicsUtil2D polygon scratch") { hideFlags = HideFlags.HideAndDontSave };
            _polygonScratch = go.AddComponent<PolygonCollider2D>();
            _polygonScratch.isTrigger = true;
            // Interact with everything, whatever the layer collision matrix says.
            _polygonScratch.includeLayers = ~0;
            _polygonScratch.excludeLayers = 0;
            _polygonScratch.enabled = false;
            return _polygonScratch;
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
                case PolygonCollider2D polygon:
                    OverlapPolygonCollider(polygon, position, angle, scale, layerMask, result, useTriggers);
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

        // A box with rounded corners (BoxCollider2D with an edge radius): the box dilated by edgeRadius. Physics2D has no
        // such query, but the shape is exactly the union of two boxes - each grown along one axis - and a circle on every
        // corner, so it's queried as those and the hits are merged (without duplicates).
        public static int OverlapRoundedBox(Vector2 center, Vector2 size, float angle, float edgeRadius, int layerMask, ICollection<Collider2D> result, bool useTriggers)
        {
            if (edgeRadius <= 0f)
                return OverlapBox(center, size, angle, layerMask, result, useTriggers);

            using (HashSetPool<Collider2D>.Get(out var unique))
            {
                OverlapBox(center, new Vector2(size.x + 2f * edgeRadius, size.y), angle, layerMask, unique, useTriggers);
                OverlapBox(center, new Vector2(size.x, size.y + 2f * edgeRadius), angle, layerMask, unique, useTriggers);

                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                for (int corner = 0; corner < 4; corner++)
                    OverlapCircle(BoxCorner(center, size, rotation, corner), edgeRadius, layerMask, unique, useTriggers);

                foreach (Collider2D collider in unique)
                    result.Add(collider);
                return unique.Count;
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
            return RoundedBoxCast(center, size, worldAngle, GetBoxEdgeRadius(box, scale), direction, distance, layerMask, useTriggers, out hit);
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

        // A box with rounded corners (BoxCollider2D with an edge radius) sweeps out exactly the union of what its parts
        // sweep out - two boxes, each grown along one axis, and a circle on every corner (see OverlapRoundedBox) - so the
        // first thing it hits is the nearest of what the parts hit.
        public static bool RoundedBoxCast(Vector2 origin, Vector2 size, float angle, float edgeRadius, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit)
        {
            if (edgeRadius <= 0f)
                return BoxCast(origin, size, angle, direction, distance, layerMask, useTriggers, out hit);

            bool any = false;
            RaycastHit2D best = default;
            RaycastHit2D candidate;

            KeepCloser(BoxCast(origin, new Vector2(size.x + 2f * edgeRadius, size.y), angle, direction, distance, layerMask, useTriggers, out candidate), candidate, ref any, ref best);
            KeepCloser(BoxCast(origin, new Vector2(size.x, size.y + 2f * edgeRadius), angle, direction, distance, layerMask, useTriggers, out candidate), candidate, ref any, ref best);

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 cornerPoint = BoxCorner(origin, size, rotation, corner);
                KeepCloser(CircleCast(cornerPoint, edgeRadius, direction, distance, layerMask, useTriggers, out candidate), candidate, ref any, ref best);
            }

            hit = best;
            return any;
        }

        public static bool RoundedBoxCast(Vector2 origin, Vector2 size, float angle, float edgeRadius, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            if (edgeRadius <= 0f)
                return BoxCast(origin, size, angle, direction, distance, layerMask, useTriggers, out hit, filter);

            bool any = false;
            RaycastHit2D best = default;
            RaycastHit2D candidate;

            KeepCloser(BoxCast(origin, new Vector2(size.x + 2f * edgeRadius, size.y), angle, direction, distance, layerMask, useTriggers, out candidate, filter), candidate, ref any, ref best);
            KeepCloser(BoxCast(origin, new Vector2(size.x, size.y + 2f * edgeRadius), angle, direction, distance, layerMask, useTriggers, out candidate, filter), candidate, ref any, ref best);

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 cornerPoint = BoxCorner(origin, size, rotation, corner);
                KeepCloser(CircleCast(cornerPoint, edgeRadius, direction, distance, layerMask, useTriggers, out candidate, filter), candidate, ref any, ref best);
            }

            hit = best;
            return any;
        }

        private static void KeepCloser(bool found, RaycastHit2D candidate, ref bool any, ref RaycastHit2D best)
        {
            if (!found || (any && candidate.distance >= best.distance))
                return;

            best = candidate;
            any = true;
        }

        // The four corners of a box (0..3), given its center, size and rotation.
        private static Vector2 BoxCorner(Vector2 center, Vector2 size, Quaternion rotation, int corner)
        {
            float sx = (corner & 1) == 0 ? -1f : 1f;
            float sy = (corner & 2) == 0 ? -1f : 1f;
            return center + (Vector2)(rotation * new Vector3(sx * Mathf.Abs(size.x) * 0.5f, sy * Mathf.Abs(size.y) * 0.5f, 0f));
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

        // ----- Filter-aware Cast overloads (skip hits the filter rejects) -----

        public static bool CastGameObject(
            GameObject gameObject,
            Vector2 position,
            float angle,
            Vector2 scale,
            Vector2 direction,
            float distance,
            out RaycastHit2D hit,
            RaycastHit2DFilter filter,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            using (ListPool<Collider2D>.Get(out var colliders))
            using (ListPool<Matrix4x4>.Get(out var relativeMatrices))
            {
                gameObject.GetComponentsInChildren(colliders);
                GetRelativeMatrices(gameObject.transform, colliders, relativeMatrices);

                return CastGameObject(colliders, relativeMatrices, position, angle, scale, direction, distance, out hit, filter, layerMask, useTriggers);
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
            RaycastHit2DFilter filter,
            int layerMask = Physics2D.AllLayers,
            bool useTriggers = false)
        {
            hit = default;
            bool  any          = false;
            float bestDistance = float.PositiveInfinity;

            for (int index = 0; index < colliders.Count; index++)
            {
                var collider       = colliders[index];
                var relativeMatrix = relativeMatrices[index];

                GetChildWorldTransformRelativeTo(relativeMatrix, position, angle, scale,
                    out var colliderPos, out var colliderAngle, out var colliderScale);

                int mask = GetLayerCollisionMask(collider.gameObject.layer) & layerMask;
                if (CastCollider(collider, colliderPos, colliderAngle, colliderScale, direction, distance, mask, useTriggers, out var localHit, filter))
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

        public static bool CastBoxCollider(BoxCollider2D box, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            return RoundedBoxCast(center, size, worldAngle, GetBoxEdgeRadius(box, scale), direction, distance, layerMask, useTriggers, out hit, filter);
        }

        public static bool CastCircleCollider(CircleCollider2D circle, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            return CircleCast(center, radius, direction, distance, layerMask, useTriggers, out hit, filter);
        }

        public static bool CastCapsuleCollider(CapsuleCollider2D capsule, Vector2 position, float angle, Vector2 scale, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var (center, size, capsuleDirection, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);
            return CapsuleCast(center, size, capsuleDirection, worldAngle, direction, distance, layerMask, useTriggers, out hit, filter);
        }

        public static bool CastCollider(
            Collider2D collider,
            Vector2 position,
            float angle,
            Vector2 scale,
            Vector2 direction,
            float distance,
            int layerMask,
            bool useTriggers,
            out RaycastHit2D hit,
            RaycastHit2DFilter filter)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:         return CastBoxCollider(box, position, angle, scale, direction, distance, layerMask, useTriggers, out hit, filter);
                case CircleCollider2D circle:   return CastCircleCollider(circle, position, angle, scale, direction, distance, layerMask, useTriggers, out hit, filter);
                case CapsuleCollider2D capsule: return CastCapsuleCollider(capsule, position, angle, scale, direction, distance, layerMask, useTriggers, out hit, filter);
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    hit = default;
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        public static bool BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var contactFilter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.BoxCast(origin, size, angle, direction, contactFilter, hits, distance);
                return FirstPassingHit(hits, count, filter, out hit);
            }
        }

        public static bool CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var contactFilter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.CircleCast(origin, radius, direction, contactFilter, hits, distance);
                return FirstPassingHit(hits, count, filter, out hit);
            }
        }

        public static bool CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, bool useTriggers, out RaycastHit2D hit, RaycastHit2DFilter filter)
        {
            var contactFilter = MakeFilter(layerMask, useTriggers);
            using (ListPool<RaycastHit2D>.Get(out var hits))
            {
                int count = Physics2D.CapsuleCast(origin, size, capsuleDirection, angle, direction, contactFilter, hits, distance);
                return FirstPassingHit(hits, count, filter, out hit);
            }
        }

        // ----- Random point on collider -----
        //
        // `skin` grows (positive) or shrinks (negative) the shape before sampling uniformly inside
        // the result — think "add skin to the radius" generalized to every collider type:
        //   skin == 0 : anywhere inside the shape (or anywhere along it, for line-only shapes).
        //   skin >  0 : anywhere inside the shape grown by `skin` — i.e. a circle of radius `skin`
        //               centered on the returned point is guaranteed to at least touch the shape
        //               (the point can be deep inside the original shape just as easily as just
        //               outside it — there's no ring/shell, no exclusion of the interior).
        //   skin <  0 : inside the shape, shrunk by |skin| — i.e. a circle of radius |skin| centered
        //               on the returned point fits entirely inside the shape.
        // Returns false (instead of an approximate point) whenever the request is infeasible:
        // negative skin on a line-only shape (no interior to speak of), an erosion deeper than the
        // shape allows, or an unsupported/unknown collider type.

        private const int RandomPointMaxAttempts = 64;

        public static bool TryGetRandomPoint(this BoxCollider2D box, out Vector2 point) => TryGetRandomPoint(box, 0f, out point);

        public static bool TryGetRandomPoint(this BoxCollider2D box, float skin, out Vector2 point)
        {
            Vector2 position = box.transform.position;
            float   angle    = box.transform.eulerAngles.z;
            Vector2 scale    = box.transform.lossyScale;
            var (center, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);
            Vector2 halfExtents = size * 0.5f;
            var     rotation    = Quaternion.Euler(0f, 0f, worldAngle);

            if (skin <= 0f)
            {
                Vector2 erodedHalf = new Vector2(halfExtents.x + skin, halfExtents.y + skin);
                if (erodedHalf.x <= 0f || erodedHalf.y <= 0f)
                {
                    point = default;
                    return false;
                }

                Vector2 local = new Vector2(
                    UnityEngine.Random.Range(-erodedHalf.x, erodedHalf.x),
                    UnityEngine.Random.Range(-erodedHalf.y, erodedHalf.y));
                point = center + (Vector2)(rotation * local);
                return true;
            }

            Vector2 outerHalf = halfExtents + Vector2.one * skin;
            for (int attempt = 0; attempt < RandomPointMaxAttempts; attempt++)
            {
                Vector2 local = new Vector2(
                    UnityEngine.Random.Range(-outerHalf.x, outerHalf.x),
                    UnityEngine.Random.Range(-outerHalf.y, outerHalf.y));

                float sdf = BoxSdf(local, halfExtents);
                if (sdf <= skin)
                {
                    point = center + (Vector2)(rotation * local);
                    return true;
                }
            }

            point = default;
            return false;
        }

        public static bool TryGetRandomPoint(this CircleCollider2D circle, out Vector2 point) => TryGetRandomPoint(circle, 0f, out point);

        public static bool TryGetRandomPoint(this CircleCollider2D circle, float skin, out Vector2 point)
        {
            Vector2 position = circle.transform.position;
            float   angle    = circle.transform.eulerAngles.z;
            Vector2 scale    = circle.transform.lossyScale;
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);

            float scaledRadius = radius + skin;
            if (scaledRadius <= 0f)
            {
                point = default;
                return false;
            }

            float r     = scaledRadius * Mathf.Sqrt(UnityEngine.Random.value);
            float theta = UnityEngine.Random.value * Mathf.PI * 2f;
            point = center + new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            return true;
        }

        public static bool TryGetRandomPoint(this CapsuleCollider2D capsule, out Vector2 point) => TryGetRandomPoint(capsule, 0f, out point);

        public static bool TryGetRandomPoint(this CapsuleCollider2D capsule, float skin, out Vector2 point)
        {
            Vector2 position = capsule.transform.position;
            float   angle    = capsule.transform.eulerAngles.z;
            Vector2 scale    = capsule.transform.lossyScale;
            var (center, size, direction, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);

            bool  vertical   = direction == CapsuleDirection2D.Vertical;
            float baseRadius = 0.5f * (vertical ? size.x : size.y);
            float longSize   = vertical ? size.y : size.x;
            float halfLen    = Mathf.Max(0f, longSize * 0.5f - baseRadius);
            var   rotation   = Quaternion.Euler(0f, 0f, worldAngle);

            if (skin <= 0f)
            {
                float erodedRadius = baseRadius + skin;
                if (erodedRadius <= 0f)
                {
                    point = default;
                    return false;
                }

                Vector2 local = RandomPointInCapsuleLocal(erodedRadius, halfLen, vertical);
                point = center + (Vector2)(rotation * local);
                return true;
            }

            float   outerRadius   = baseRadius + skin;
            Vector2 sampleExtents = vertical
                ? new Vector2(outerRadius, halfLen + outerRadius)
                : new Vector2(halfLen + outerRadius, outerRadius);

            for (int attempt = 0; attempt < RandomPointMaxAttempts; attempt++)
            {
                Vector2 local = new Vector2(
                    UnityEngine.Random.Range(-sampleExtents.x, sampleExtents.x),
                    UnityEngine.Random.Range(-sampleExtents.y, sampleExtents.y));

                float sdf = CapsuleSdf(local, halfLen, baseRadius, vertical);
                if (sdf <= skin)
                {
                    point = center + (Vector2)(rotation * local);
                    return true;
                }
            }

            point = default;
            return false;
        }

        public static bool TryGetRandomPoint(this PolygonCollider2D polygon, out Vector2 point) => TryGetRandomPoint(polygon, 0f, out point);

        public static bool TryGetRandomPoint(this PolygonCollider2D polygon, float skin, out Vector2 point)
        {
            Vector2 position = polygon.transform.position;
            float   angle    = polygon.transform.eulerAngles.z;
            Vector2 scale    = polygon.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            {
                for (int i = 0; i < polygon.pathCount; i++)
                    worldPaths.Add(ToWorldPath(polygon.GetPath(i), polygon.offset, position, angle, scale));

                if (skin == 0f)
                    return TryRandomPointInPolygons(worldPaths, out point);

                return TryGetRandomPointWithSkin(worldPaths, polygon.bounds, skin, hasInterior: true, closed: true, out point);
            }
        }

        public static bool TryGetRandomPoint(this EdgeCollider2D edge, out Vector2 point) => TryGetRandomPoint(edge, 0f, out point);

        public static bool TryGetRandomPoint(this EdgeCollider2D edge, float skin, out Vector2 point)
        {
            if (skin < 0f)
            {
                point = default;
                return false;
            }

            Vector2 position = edge.transform.position;
            float   angle    = edge.transform.eulerAngles.z;
            Vector2 scale    = edge.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            {
                worldPaths.Add(ToWorldPath(edge.points, edge.offset, position, angle, scale));

                if (skin == 0f)
                {
                    point = RandomPointOnPolylines(worldPaths, closed: false);
                    return true;
                }

                return TryGetRandomPointWithSkin(worldPaths, edge.bounds, skin, hasInterior: false, closed: false, out point);
            }
        }

        public static bool TryGetRandomPoint(this CompositeCollider2D composite, out Vector2 point) => TryGetRandomPoint(composite, 0f, out point);

        public static bool TryGetRandomPoint(this CompositeCollider2D composite, float skin, out Vector2 point)
        {
            bool isOutline = composite.geometryType == CompositeCollider2D.GeometryType.Outlines;
            if (isOutline && skin < 0f)
            {
                point = default;
                return false;
            }

            Vector2 position = composite.transform.position;
            float   angle    = composite.transform.eulerAngles.z;
            Vector2 scale    = composite.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            using (ListPool<Vector2>.Get(out var buffer))
            {
                for (int i = 0; i < composite.pathCount; i++)
                {
                    buffer.Clear();
                    composite.GetPath(i, buffer);
                    worldPaths.Add(ToWorldPath(buffer, composite.offset, position, angle, scale));
                }

                if (isOutline)
                {
                    if (skin == 0f)
                    {
                        point = RandomPointOnPolylines(worldPaths, closed: true);
                        return true;
                    }

                    return TryGetRandomPointWithSkin(worldPaths, composite.bounds, skin, hasInterior: false, closed: true, out point);
                }

                if (skin == 0f)
                    return TryRandomPointInPolygons(worldPaths, out point);

                return TryGetRandomPointWithSkin(worldPaths, composite.bounds, skin, hasInterior: true, closed: true, out point);
            }
        }

#if UNITY_TILEMAP
        // TilemapCollider2D exposes no path/geometry query API of its own — the only reliable
        // way to sample it is via a sibling CompositeCollider2D that actually merges its shapes.
        public static bool TryGetRandomPoint(this TilemapCollider2D tilemap, out Vector2 point) => TryGetRandomPoint(tilemap, 0f, out point);

        public static bool TryGetRandomPoint(this TilemapCollider2D tilemap, float skin, out Vector2 point)
        {
            var composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite != null && tilemap.usedByComposite && composite.pathCount > 0)
                return composite.TryGetRandomPoint(skin, out point);

            Debug.LogWarning($"Collider2D type {tilemap.GetType().Name} not supported without an attached, used CompositeCollider2D.");
            point = default;
            return false;
        }
#endif

        public static bool TryGetRandomPoint(this Collider2D collider, out Vector2 point) => TryGetRandomPoint(collider, 0f, out point);

        public static bool TryGetRandomPoint(this Collider2D collider, float skin, out Vector2 point)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:             return box.TryGetRandomPoint(skin, out point);
                case CircleCollider2D circle:       return circle.TryGetRandomPoint(skin, out point);
                case CapsuleCollider2D capsule:     return capsule.TryGetRandomPoint(skin, out point);
                case PolygonCollider2D polygon:     return polygon.TryGetRandomPoint(skin, out point);
                case EdgeCollider2D edge:           return edge.TryGetRandomPoint(skin, out point);
                case CompositeCollider2D composite: return composite.TryGetRandomPoint(skin, out point);
#if UNITY_TILEMAP
                case TilemapCollider2D tilemap:     return tilemap.TryGetRandomPoint(skin, out point);
#endif
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    point = default;
                    return false;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        // ----- Circle overlap test (reuses the same signed-distance geometry as TryGetRandomPoint) -----

        public static bool TouchesCircle(this BoxCollider2D box, Vector2 center, float radius)
        {
            Vector2 position = box.transform.position;
            float   angle    = box.transform.eulerAngles.z;
            Vector2 scale    = box.transform.lossyScale;
            var (boxCenter, size, worldAngle) = GetBoxColliderParams(box, position, angle, scale);

            Vector2 local = WorldToLocalUnrotated(center, boxCenter, worldAngle);
            return BoxSdf(local, size * 0.5f) <= radius;
        }

        public static bool TouchesCircle(this CircleCollider2D circle, Vector2 center, float radius)
        {
            Vector2 position = circle.transform.position;
            float   angle    = circle.transform.eulerAngles.z;
            Vector2 scale    = circle.transform.lossyScale;
            var (circleCenter, circleRadius) = GetCircleColliderParams(circle, position, angle, scale);

            return Vector2.Distance(center, circleCenter) <= circleRadius + radius;
        }

        public static bool TouchesCircle(this CapsuleCollider2D capsule, Vector2 center, float radius)
        {
            Vector2 position = capsule.transform.position;
            float   angle    = capsule.transform.eulerAngles.z;
            Vector2 scale    = capsule.transform.lossyScale;
            var (capsuleCenter, size, direction, worldAngle) = GetCapsuleColliderParams(capsule, position, angle, scale);

            bool  vertical   = direction == CapsuleDirection2D.Vertical;
            float baseRadius = 0.5f * (vertical ? size.x : size.y);
            float longSize   = vertical ? size.y : size.x;
            float halfLen    = Mathf.Max(0f, longSize * 0.5f - baseRadius);

            Vector2 local = WorldToLocalUnrotated(center, capsuleCenter, worldAngle);
            return CapsuleSdf(local, halfLen, baseRadius, vertical) <= radius;
        }

        public static bool TouchesCircle(this PolygonCollider2D polygon, Vector2 center, float radius)
        {
            Vector2 position = polygon.transform.position;
            float   angle    = polygon.transform.eulerAngles.z;
            Vector2 scale    = polygon.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            {
                for (int i = 0; i < polygon.pathCount; i++)
                    worldPaths.Add(ToWorldPath(polygon.GetPath(i), polygon.offset, position, angle, scale));

                return PolygonSdf(center, worldPaths) <= radius;
            }
        }

        public static bool TouchesCircle(this EdgeCollider2D edge, Vector2 center, float radius)
        {
            Vector2 position = edge.transform.position;
            float   angle    = edge.transform.eulerAngles.z;
            Vector2 scale    = edge.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            {
                worldPaths.Add(ToWorldPath(edge.points, edge.offset, position, angle, scale));
                return DistanceToNearestEdge(center, worldPaths, closed: false) <= radius;
            }
        }

        public static bool TouchesCircle(this CompositeCollider2D composite, Vector2 center, float radius)
        {
            bool isOutline = composite.geometryType == CompositeCollider2D.GeometryType.Outlines;

            Vector2 position = composite.transform.position;
            float   angle    = composite.transform.eulerAngles.z;
            Vector2 scale    = composite.transform.lossyScale;

            using (ListPool<Vector2[]>.Get(out var worldPaths))
            using (ListPool<Vector2>.Get(out var buffer))
            {
                for (int i = 0; i < composite.pathCount; i++)
                {
                    buffer.Clear();
                    composite.GetPath(i, buffer);
                    worldPaths.Add(ToWorldPath(buffer, composite.offset, position, angle, scale));
                }

                return isOutline
                    ? DistanceToNearestEdge(center, worldPaths, closed: true) <= radius
                    : PolygonSdf(center, worldPaths) <= radius;
            }
        }

#if UNITY_TILEMAP
        // TilemapCollider2D exposes no path/geometry query API of its own — delegate to a sibling
        // CompositeCollider2D if available, same as TryGetRandomPoint. An indeterminate area shape
        // should never cause callers to wrongly conclude "no overlap", so this defaults to true.
        public static bool TouchesCircle(this TilemapCollider2D tilemap, Vector2 center, float radius)
        {
            var composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite != null && tilemap.usedByComposite && composite.pathCount > 0)
                return composite.TouchesCircle(center, radius);

            Debug.LogWarning($"Collider2D type {tilemap.GetType().Name} not supported without an attached, used CompositeCollider2D.");
            return true;
        }
#endif

        public static bool TouchesCircle(this Collider2D collider, Vector2 center, float radius)
        {
            if (collider == null)
                throw new ArgumentNullException(nameof(collider));

            // ReSharper disable Unity.NoNullPatternMatching
            switch (collider)
            {
                case BoxCollider2D box:             return box.TouchesCircle(center, radius);
                case CircleCollider2D circle:       return circle.TouchesCircle(center, radius);
                case CapsuleCollider2D capsule:     return capsule.TouchesCircle(center, radius);
                case PolygonCollider2D polygon:     return polygon.TouchesCircle(center, radius);
                case EdgeCollider2D edge:           return edge.TouchesCircle(center, radius);
                case CompositeCollider2D composite: return composite.TouchesCircle(center, radius);
#if UNITY_TILEMAP
                case TilemapCollider2D tilemap:     return tilemap.TouchesCircle(center, radius);
#endif
                default:
                    Debug.LogWarning($"Collider2D type {collider.GetType().Name} not supported.");
                    return true;
            }
            // ReSharper restore Unity.NoNullPatternMatching
        }

        // Convenience overload for the common case of testing against another collider's own
        // CircleCollider2D (e.g. "does the play area still touch this actor's collider").
        public static bool TouchesCircle(this Collider2D collider, CircleCollider2D circle)
        {
            Vector2 position = circle.transform.position;
            float   angle    = circle.transform.eulerAngles.z;
            Vector2 scale    = circle.transform.lossyScale;
            var (center, radius) = GetCircleColliderParams(circle, position, angle, scale);
            return collider.TouchesCircle(center, radius);
        }

        // Inverse of the rotation applied when placing local shape points into world space —
        // brings a world point into a shape's own de-rotated, world-scaled local frame.
        private static Vector2 WorldToLocalUnrotated(Vector2 worldPoint, Vector2 worldCenter, float worldAngle)
        {
            return (Vector2)(Quaternion.Euler(0f, 0f, -worldAngle) * (Vector3)(worldPoint - worldCenter));
        }

        // Samples uniformly by area: a rectangle for the straight section plus a full circle
        // (the two end caps combined) split back into top/bottom (or left/right) halves.
        private static Vector2 RandomPointInCapsuleLocal(float radius, float halfLen, bool vertical)
        {
            float rectLength = halfLen * 2f;
            float rectArea   = 2f * radius * rectLength;
            float circleArea = Mathf.PI * radius * radius;
            float totalArea  = rectArea + circleArea;

            if (totalArea <= 0f)
                return Vector2.zero;

            if (UnityEngine.Random.value < rectArea / totalArea)
            {
                float along  = UnityEngine.Random.Range(-halfLen, halfLen);
                float across = UnityEngine.Random.Range(-radius, radius);
                return vertical ? new Vector2(across, along) : new Vector2(along, across);
            }
            else
            {
                float r     = radius * Mathf.Sqrt(UnityEngine.Random.value);
                float theta = UnityEngine.Random.value * Mathf.PI * 2f;
                float cx    = r * Mathf.Cos(theta);
                float cy    = r * Mathf.Sin(theta);
                float along = (cy >= 0f ? halfLen : -halfLen) + cy;
                return vertical ? new Vector2(cx, along) : new Vector2(along, cx);
            }
        }

        // True Euclidean signed distance to an axis-aligned box (negative inside, positive
        // outside), correct in the corner regions too — needed so the outward "skin" shell has
        // properly rounded corners instead of a naively expanded rectangle.
        private static float BoxSdf(Vector2 p, Vector2 halfExtents)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfExtents;
            float outsideDist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            float insideDist  = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
            return outsideDist + insideDist;
        }

        // Signed distance to a capsule's core segment minus its radius (negative inside).
        private static float CapsuleSdf(Vector2 p, float halfLen, float radius, bool vertical)
        {
            Vector2 a = vertical ? new Vector2(0f, -halfLen) : new Vector2(-halfLen, 0f);
            Vector2 b = vertical ? new Vector2(0f, halfLen) : new Vector2(halfLen, 0f);
            return DistancePointToSegment(p, a, b) - radius;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab        = b - a;
            float   lengthSqr = ab.sqrMagnitude;
            float   t         = lengthSqr > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSqr) : 0f;
            Vector2 closest   = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        // Transforms a raw local collider point (offset not yet applied) into world space.
        private static Vector2 LocalToWorld(Vector2 localPoint, Vector2 offset, Vector2 position, float angle, Vector2 scale)
        {
            Vector2 scaled = Vector2.Scale(scale, localPoint + offset);
            return position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * scaled);
        }

        private static Vector2[] ToWorldPath(IReadOnlyList<Vector2> localPath, Vector2 offset, Vector2 position, float angle, Vector2 scale)
        {
            var worldPath = new Vector2[localPath.Count];
            for (int i = 0; i < localPath.Count; i++)
                worldPath[i] = LocalToWorld(localPath[i], offset, position, angle, scale);
            return worldPath;
        }

        // Even-odd (crossing number) point-in-polygon test, accumulated across every path as one
        // edge soup. This makes holes (an inner path wound either direction) and disjoint islands
        // work without needing to know or track winding order.
        private static bool PointInPolygons(Vector2 point, List<Vector2[]> paths)
        {
            bool inside = false;
            foreach (var path in paths)
            {
                int count = path.Length;
                for (int i = 0, j = count - 1; i < count; j = i++)
                {
                    Vector2 a = path[i];
                    Vector2 b = path[j];
                    if ((a.y > point.y) != (b.y > point.y))
                    {
                        float xAtY = a.x + (point.y - a.y) / (b.y - a.y) * (b.x - a.x);
                        if (point.x < xAtY)
                            inside = !inside;
                    }
                }
            }
            return inside;
        }

        // closed:false skips the wrap-around edge from each path's last point back to its first —
        // required for genuinely open polylines (EdgeCollider2D) so no phantom closing edge is measured against.
        private static float DistanceToNearestEdge(Vector2 point, List<Vector2[]> paths, bool closed)
        {
            float best = float.PositiveInfinity;
            foreach (var path in paths)
            {
                int count = path.Length;
                int segmentCount = closed ? count : count - 1;
                for (int i = 0; i < segmentCount; i++)
                    best = Mathf.Min(best, DistancePointToSegment(point, path[i], path[(i + 1) % count]));
            }
            return best;
        }

        // Signed distance to one or more (possibly holed / disjoint) closed polygon paths:
        // negative inside, positive outside, magnitude always the true distance to the nearest edge.
        private static float PolygonSdf(Vector2 point, List<Vector2[]> paths)
        {
            float dist = DistanceToNearestEdge(point, paths, closed: true);
            return PointInPolygons(point, paths) ? -dist : dist;
        }

        // Rejection-samples a point inside one or more (possibly holed / disjoint) closed
        // polygon paths, given in world space.
        private static bool TryRandomPointInPolygons(List<Vector2[]> paths, out Vector2 point)
        {
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var path in paths)
            {
                foreach (var p in path)
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            }

            for (int attempt = 0; attempt < RandomPointMaxAttempts; attempt++)
            {
                Vector2 candidate = new Vector2(
                    UnityEngine.Random.Range(min.x, max.x),
                    UnityEngine.Random.Range(min.y, max.y));

                if (PointInPolygons(candidate, paths))
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        // Rejection-samples a point satisfying a skin offset against one or more world-space
        // paths: the shape effectively grown (skin > 0) or shrunk (skin <= 0) by `skin`, sampled
        // uniformly as a solid region — for skin > 0 this includes the entire original interior,
        // not just a thin outer band. hasInterior:false treats the paths as open or closed
        // polylines (per `closed`) with no inside (Edge / Composite Outlines, skin > 0 only —
        // their sdf is already an unsigned distance, so the same sdf<=skin test applies).
        private static bool TryGetRandomPointWithSkin(List<Vector2[]> worldPaths, Bounds worldBounds, float skin, bool hasInterior, bool closed, out Vector2 point)
        {
            Vector2 min = worldBounds.min;
            Vector2 max = worldBounds.max;

            if (skin > 0f)
            {
                min -= Vector2.one * skin;
                max += Vector2.one * skin;
            }

            for (int attempt = 0; attempt < RandomPointMaxAttempts; attempt++)
            {
                Vector2 candidate = new Vector2(
                    UnityEngine.Random.Range(min.x, max.x),
                    UnityEngine.Random.Range(min.y, max.y));

                float sdf = hasInterior ? PolygonSdf(candidate, worldPaths) : DistanceToNearestEdge(candidate, worldPaths, closed);

                if (sdf <= skin)
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        // Length-weighted sample across one or more polylines, given in world space.
        // Pass closed:true when each path's last point should connect back to its first.
        private static Vector2 RandomPointOnPolylines(List<Vector2[]> paths, bool closed)
        {
            float totalLength = 0f;
            foreach (var path in paths)
                totalLength += PolylineLength(path, closed);

            if (totalLength <= 0f)
                return paths.Count > 0 && paths[0].Length > 0 ? paths[0][0] : Vector2.zero;

            float target = UnityEngine.Random.value * totalLength;

            foreach (var path in paths)
            {
                int segmentCount = closed ? path.Length : path.Length - 1;
                for (int i = 0; i < segmentCount; i++)
                {
                    Vector2 a = path[i];
                    Vector2 b = path[(i + 1) % path.Length];
                    float segLength = Vector2.Distance(a, b);

                    if (target <= segLength)
                        return Vector2.Lerp(a, b, segLength > 0f ? target / segLength : 0f);

                    target -= segLength;
                }
            }

            // Fallback for the floating-point edge case of landing exactly at the end of the last segment.
            var lastPath = paths[paths.Count - 1];
            return lastPath[lastPath.Length - 1];
        }

        private static float PolylineLength(Vector2[] path, bool closed)
        {
            float length = 0f;
            int segmentCount = closed ? path.Length : path.Length - 1;
            for (int i = 0; i < segmentCount; i++)
                length += Vector2.Distance(path[i], path[(i + 1) % path.Length]);
            return length;
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

        private static bool FirstPassingHit(List<RaycastHit2D> hits, int count, RaycastHit2DFilter filter, out RaycastHit2D hit)
        {
            // Walk hits in distance order; return the first one the filter accepts.
            for (int i = 0; i < count; i++)
            {
                if (filter == null || filter(hits[i]))
                {
                    hit = hits[i];
                    return true;
                }
            }
            hit = default;
            return false;
        }
    }
}
