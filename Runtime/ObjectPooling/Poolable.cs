using System;
using SeweralIdeas.UnityUtils;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// The root of an object that is meant to be pooled: it can end its own life, whether that means going back to
    /// its pool or, if it has none, being destroyed. See <see cref="Release"/>.
    ///
    /// Not to be confused with <see cref="Spawnable.OnDespawn"/>, which runs whenever the object is disabled. A life
    /// ends once: it is released, or destroyed.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class Poolable : Spawnable, IDelayedRelease
    {
        private bool _releaseQueued;

        /// <summary>
        /// Raised once when the object's life is over: it was destroyed, or returned to its pool.
        /// </summary>
        public event Action<Poolable> Released;

        /// <summary>Whether it is waiting in its pool right now (and so already released).</summary>
        public bool IsInPool => TryGetComponent(out PoolReference poolRef) && poolRef.InPool;

        /// <summary>Whether <see cref="Release"/> has been called and the end of the frame, when it takes effect, is yet to come.</summary>
        public bool IsReleasing => _releaseQueued;

        protected override void OnDestroyed()
        {
            InvokeReleasedEvent();
            base.OnDestroyed();
        }

        private void InvokeReleasedEvent()
        {
            while(Released != null)
            {
                Action<Poolable> released = Released;
                Released = null;
                released.Invoke(this);
            }
        }

        /// <summary>
        /// Ends the object's life at the end of the frame, as <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/>
        /// does, so that whoever is still working with it this frame finds it intact, instead of having it deactivated
        /// under their hands. Returns it to its pool if it has one, destroys it otherwise. Calling it again before the
        /// end of the frame does nothing.
        /// </summary>
        public void Release()
        {
            if(!TryGetComponent(out PoolReference poolRef))
            {
                Destroy(gameObject); // not pooled: destroying it is delayed already
                return;
            }

            // Already released, or about to be. (Queueing one that is in the pool would never be undone: the queued
            // release would find it in the pool and skip it, leaving the flag set for its next life.)
            if(poolRef.InPool || _releaseQueued)
                return;

            _releaseQueued = true;
            ObjectPoolManager.GetInstance(gameObject.scene).ReleaseAtEndOfFrame(this);
        }

        /// <summary>
        /// Ends the object's life on the spot: returns it to its pool, which deactivates it, or destroys it if it has none.
        /// Use with care, see <see cref="Release"/>.
        /// </summary>
        public void ReleaseImmediate()
        {
            _releaseQueued = false;

            var poolRef = GetComponent<PoolReference>();
            if(poolRef && poolRef.ReturnToPool())
            {
                InvokeReleasedEvent();
                return;
            }

            DestroyImmediate(gameObject);
        }

        void IDelayedRelease.ReleaseNow()
        {
            _releaseQueued = false; // whatever happens next, this request is over

            if(!IsInPool) // not if it was released some other way in the meantime
                ReleaseImmediate();
        }

        #region Editor
        #if UNITY_EDITOR
        private const string ReleaseContextName = "Release";

        [ContextMenu(ReleaseContextName)]
        private void Context_Release()
        {
            if(!Context_ReleaseValidate())
                return;
            Release();
        }

        [ContextMenu(ReleaseContextName, true)]
        private bool Context_ReleaseValidate()
        {
            if(!Application.isPlaying)
                return false;
            if(!gameObject.scene.IsValid())
                return false;
            if(IsInPool) // already released
                return false;
            return true;
        }
        #endif
        #endregion
    }
}
