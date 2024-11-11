using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    [UnityEngine.Scripting.Preserve]
    public class MaterialPropertyPool : ObjectPool<MaterialPropertyBlock>
    {
        private static readonly MaterialPropertyPool s_pool = new (1, 1000);
        
        public new static PooledObject<MaterialPropertyBlock> Get(out MaterialPropertyBlock value) => ((ObjectPool<MaterialPropertyBlock>)s_pool).Get(out value);
        
        public MaterialPropertyPool(int defaultCapacity, int maxSize) : base(
            ()=>new MaterialPropertyBlock(), 
            obj=>obj.Clear(), 
            obj=>obj.Clear(), 
            null, 
            false, 
            defaultCapacity, 
            maxSize)
        { }
    }
}