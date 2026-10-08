using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>Added to every pooled instance: lets it find its pool and give itself back.</summary>
    [DisallowMultipleComponent]
    public class PoolReference : MonoBehaviour
    {
        private ObjectPool _pool;
        private Component _referred;

        /// <summary>Whether the instance is sitting in the pool right now. Kept by the pool.</summary>
        public bool InPool { get; internal set; }

        /// <summary>
        /// Gives the instance back to its pool. Returns false if it has no (living) pool, so that the caller can
        /// destroy it instead. Returning one that is already in the pool is an error, but is still "handled".
        /// </summary>
        public bool ReturnToPool()
        {
            if(!_pool || !_referred)
            {
                return false;
            }

            _pool.ReturnInstance(_referred);
            return true;
        }

        internal void OnTakenFromPool(ObjectPool pool, Component referredObject)
        {
            _pool = pool;
            _referred = referredObject;
            InPool = false;
        }
    }
}
