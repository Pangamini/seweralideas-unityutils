using System.Runtime.CompilerServices;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// A null coalescing that respects Unity's null: the object, or the fallback if it is null or destroyed.
        /// The <c>??</c> operator only sees a real null and lets a destroyed object through.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T>(this T obj, T fallback) where T : Object => obj ? obj : (fallback ? fallback : null);

        /// <summary>
        /// The object, or a real null if it is null or destroyed, so that <c>?.</c> and <c>??</c> can be used after it:
        /// <c>target.OrNull()?.Foo()</c>. Those operators alone only see a real null and let a destroyed object through.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;
    }
}
