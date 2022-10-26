using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace SeweralIdeas.Utils
{
    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
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