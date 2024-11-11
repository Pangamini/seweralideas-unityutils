#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace SeweralIdeas.Utils
{
    public static class ArrayUtility
    {

        public static bool Contains<T>(T[] array, T value)
        {
            foreach (T v in array)
            {
                if (v.Equals(value))
                    return true;
            }
            return false;
        }

        public static T Find<T>(T[] array, Predicate<T> predicate)
        {
            for (int i = 0; i < array.Length; ++i)
                if (predicate(array[i]))
                    return array[i];
            return default(T);
        }

        public static int FindIndex<T>(T[] array, Predicate<T> predicate)
        {
            for (int i = 0; i < array.Length; ++i)
                if (predicate(array[i]))
                    return i;
            return -1;
        }

        
        public static int FindIndex<T>(T[] array, T value) where T:IEquatable<T>
        {
            for (int i = 0; i < array.Length; ++i)
                if (value.Equals(array[i]))
                    return i;
            return -1;
        }


        public static void Add<T>(ref T[] array, T value)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = value;
        }

        public static void Add<T>(ref T[] array, T value, int count)
        {
            Array.Resize(ref array, array.Length + count);
            for (int i = 0; i < count; ++i)
                array[array.Length - 1 - i] = value;
        }

        public static bool Remove<T>(ref T[] array, T value)
        {
            int index = IndexOf(array, value);
            if (index > -1)
            {
                array[index] = array[array.Length - 1];
                Array.Resize(ref array, array.Length - 1);
                return true;
            }
            return false;
        }


        public static int Remove<T>(ref T[] array, T value, int count)
        {
            int removedCount = 0;
            using (ListPool<T>.Get(out var list))
            {
                list.Capacity = array.Length;
                for (int i = 0; i < array.Length; ++i)
                {
                    var val = array[i];
                    if (value.Equals(val))
                    {
                        removedCount++;
                        continue;
                    }

                    list.Add(val);
                }
                if (removedCount > 0)
                    array = list.ToArray();
            }
            return removedCount;
        }

        public static void RemoveAt<T>(ref T[] array, int index)
        {
            if (index < 0 || index >= array.Length) throw new IndexOutOfRangeException();
            var outArr = new T[array.Length - 1];
            {
                int j = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (i != index)
                    {
                        outArr[j] = array[i];
                        ++j;
                    }
                }
                array = outArr;
            }
        }

        public static int IndexOf<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

#if UNITY
        public static T PickRandom<T>(this T[] data)
        {
            var index = UnityEngine.Random.Range(0, data.Length);
            return data[index];
        }
        
        public static void GetRandomOrder(int count, List<(int index, int randomValue)> shuffle)
        {
            shuffle.Clear();
            shuffle.Capacity = count;
            for (int i = 0; i < count; ++i)
            {
                shuffle.Add((i, UnityEngine.Random.Range(int.MinValue, int.MaxValue)));
            }
        
            Comparison<(int i, int r)> comparison = ((int i, int r) lhs, (int i, int r) rhs) => lhs.r.CompareTo(rhs.r);
            shuffle.Sort(comparison);
        }
#endif
    }
}