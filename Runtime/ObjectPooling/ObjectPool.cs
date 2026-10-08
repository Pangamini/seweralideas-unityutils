using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// What <see cref="PoolReference"/> knows about a pool, whatever it holds. The pool itself is <see cref="ObjectPool{T}"/>,
    /// and <see cref="ComponentPool"/> is the one that <see cref="ObjectPoolManager"/> makes.
    /// </summary>
    public abstract class ObjectPool : MonoBehaviour
    {
        // Only ObjectPool<T> implements it, for the instances it handed out.
        internal abstract void ReturnInstance(Component instance);
    }
}
