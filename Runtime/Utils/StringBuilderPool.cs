using System.Text;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    [UnityEngine.Scripting.Preserve]
    public class StringBuilderPool : ObjectPool<StringBuilder>
    {
        private static readonly StringBuilderPool s_pool = new (1, 1000);
        
        public new static PooledObject<StringBuilder> Get(out StringBuilder value) => ((ObjectPool<StringBuilder>)s_pool).Get(out value);
        
        public StringBuilderPool(int defaultCapacity, int maxSize) : base(
            ()=>new StringBuilder(), 
            obj=>obj.Clear(), 
            obj=>obj.Clear(), 
            null, 
            false, 
            defaultCapacity, 
            maxSize)
        { }
    }
}