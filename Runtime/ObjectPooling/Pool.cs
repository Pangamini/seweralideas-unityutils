using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// A pool seen as handing out <typeparamref name="T"/>: the prefab's type as the caller knows it. What
    /// <see cref="ObjectPoolManager.GetPool{T}"/> returns. Costs nothing: it is only a reference to the pool.
    /// </summary>
    public readonly struct Pool<T> where T : Component
    {
        private readonly ComponentPool _pool;

        internal Pool(ComponentPool pool) => _pool = pool;

        /// <summary>The pool, with its instances as plain components.</summary>
        public ComponentPool Underlying => _pool;

        public T Prefab => (T)_pool.Prefab;

        /// <inheritdoc cref="ObjectPool{T}.Take"/>
        public T Take(bool active = true, Transform parent = null) => (T)_pool.Take(active, parent);

        /// <inheritdoc cref="ObjectPool{T}.Take(Vector3, Quaternion, bool, Transform)"/>
        public T Take(Vector3 position, Quaternion rotation, bool active = true, Transform parent = null) =>
            (T)_pool.Take(position, rotation, active, parent);

        public void Return(T obj) => _pool.Return(obj);

        /// <inheritdoc cref="ObjectPool{T}.Prewarm"/>
        public void Prewarm(int count) => _pool.Prewarm(count);
    }
}
