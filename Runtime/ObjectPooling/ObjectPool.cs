using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// What a <see cref="Spawnable"/> knows about its pool, whatever it holds. The pool itself is <see cref="ObjectPool{T}"/>,
    /// and <see cref="ComponentPool"/> is the one that <see cref="ObjectPoolManager"/> makes.
    /// </summary>
    public abstract class ObjectPool : MonoBehaviour
    {
        // Only ObjectPool<T> implements it: takes in a spawnable whose life is over (see Spawnable.Despawn).
        internal abstract void ReturnInstance(Spawnable instance);
    }
}
