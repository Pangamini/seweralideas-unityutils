using System;
using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    public class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> m_comparison;
        public ComparisonComparer(Comparison<T> comparison) => m_comparison = comparison;
        public override int Compare(T lhs, T rhs) => this.m_comparison(lhs, rhs);
    }
}
