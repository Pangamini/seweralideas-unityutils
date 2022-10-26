using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    /// <returns>True to continue visiting, False to break the visit loop</returns>
    public delegate bool Visitor<T>(T element);

    public static class VisitorExtensions
    {
        public static void Visit<TElem>(this IList<TElem> list, Visitor<TElem> visitor)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (!visitor(list[i]))
                    break;
            }
        }
        
        public static void VisitReversed<TElem>(this IList<TElem> list, Visitor<TElem> visitor)
        {
            for (int i = list.Count-1; i >= 0; --i)
            {
                if (!visitor(list[i]))
                    break;
            }
        }
    }
}