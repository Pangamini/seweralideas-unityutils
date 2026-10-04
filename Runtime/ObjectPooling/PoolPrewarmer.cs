using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// Fills the scene's pool of a prefab with a number of instances at Start, each one with its Awake already done
    /// (see <see cref="ObjectPool.Prewarm"/>). Add one per prefab to prewarm.
    /// </summary>
    public class PoolPrewarmer : MonoBehaviour
    {
        [SerializeField, ComponentPicker] private Component _prefab;
        [SerializeField, Min(0)] private int _count = 16;

        // Not in Awake: the Awakes of the instances can use what the rest of the scene sets up in its own.
        protected void Start()
        {
            if(_prefab == null)
            {
                Debug.LogError($"{name}: no prefab to prewarm.", this);
                return;
            }

            ObjectPoolManager.GetOrCreate(gameObject.scene).GetPool(_prefab).Prewarm(_count);
        }
    }
}
