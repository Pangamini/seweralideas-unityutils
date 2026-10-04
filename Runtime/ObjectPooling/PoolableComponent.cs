using SeweralIdeas.UnityUtils;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>A part of a <see cref="Poolable"/> object, with the same spawn and despawn lifecycle.</summary>
    [RequireComponent(typeof(Poolable))]
    public abstract class PoolableComponent : Spawnable
    {
        public Poolable Poolable { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            Poolable = GetComponent<Poolable>();
        }
    }
}
