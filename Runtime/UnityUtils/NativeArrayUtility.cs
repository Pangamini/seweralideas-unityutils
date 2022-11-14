using Unity.Collections;
using Unity.Jobs;
#if UNITY_BURST
using Unity.Burst;
#endif

namespace SeweralIdeas.Utils
{
    #if UNITY_BURST
    [BurstCompile(CompileSynchronously = true)]
    #endif
    public struct MemsetNativeArray<T> : IJobParallelFor where T : unmanaged
    {
        private NativeArray<T> array;
        private T value;

        public int Length => array.Length;

        public MemsetNativeArray(NativeArray<T> array, T value)
        {
            this.array = array;
            this.value = value;
        }

        public void Execute(int index)
        {
            array[index] = value;
        }
    }
}