using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public static class CollectionExtensions
    {
        public static bool ContentEquals<T>(List<T> lhs, List<T> rhs)
        {
            //first, compare for equality
            if (lhs == rhs)
                return true;

            // if one of lists is null, return false (since we already compared them for equality, both can't be null here)
            if (lhs == null || rhs == null)
                return false;

            // if count is different, return false
            if (lhs.Count != rhs.Count)
                return false;

            // compare elements
            for (int i = 0; i < lhs.Count; ++i)
            {
                if (!EqualityComparer<T>.Default.Equals(lhs[i], rhs[i]))
                    return false;
            }

            return true;
        }
    }
}