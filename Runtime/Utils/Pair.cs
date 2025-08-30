#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
namespace SeweralIdeas.Utils
{
    /// <summary>
    /// A generic, serializable pair of values.
    /// Can be implicitly converted from and to a <c>Tuple</c> and a <c>KeyValuePair</c>
    /// </summary>
    [Serializable]
    public struct Pair<TKey, TVal> : IEquatable<Pair<TKey, TVal>>
    {
        [SerializeField] public TKey Key;
        [SerializeField] public TVal Value;
        
        public static implicit operator Pair<TKey, TVal>((TKey, TVal) obj) => new Pair<TKey, TVal> { Key = obj.Item1, Value = obj.Item2 };
        public static implicit operator Pair<TKey, TVal>(KeyValuePair<TKey, TVal> obj) => new Pair<TKey, TVal> { Key = obj.Key, Value = obj.Value };
        public static implicit operator (TKey, TVal)(Pair<TKey, TVal> obj) => (obj.Key, obj.Value);
        public static implicit operator KeyValuePair<TKey, TVal>(Pair<TKey, TVal> obj) => new (obj.Key, obj.Value);
        
        public bool Equals(Pair<TKey, TVal> other) => EqualityComparer<TKey>.Default.Equals(Key, other.Key) && EqualityComparer<TVal>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Pair<TKey, TVal> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Value);
        public static bool operator ==(Pair<TKey, TVal> left, Pair<TKey, TVal> right) => left.Equals(right);
        public static bool operator !=(Pair<TKey, TVal> left, Pair<TKey, TVal> right) => !left.Equals(right);
    }

    [Serializable]
    public struct UnorderedPair<TKey> : IEquatable<UnorderedPair<TKey>>
    {
        [SerializeField] public TKey Item1;
        [SerializeField] public TKey Item2;
        
        public static implicit operator UnorderedPair<TKey>((TKey, TKey) obj) => new UnorderedPair<TKey> { Item1 = obj.Item1, Item2 = obj.Item2 };
        public static implicit operator UnorderedPair<TKey>(KeyValuePair<TKey, TKey> obj) => new UnorderedPair<TKey> { Item1 = obj.Key, Item2 = obj.Value };
        public static implicit operator (TKey, TKey)(UnorderedPair<TKey> obj) => (obj.Item1, obj.Item2);
        public static implicit operator KeyValuePair<TKey, TKey>(UnorderedPair<TKey> obj) => new (obj.Item1, obj.Item2);
        
        public bool Equals(UnorderedPair<TKey> other)
        {
            return (EqualityComparer<TKey>.Default.Equals(Item1, other.Item1) && EqualityComparer<TKey>.Default.Equals(Item2, other.Item2))
                || (EqualityComparer<TKey>.Default.Equals(Item1, other.Item2) && EqualityComparer<TKey>.Default.Equals(Item2, other.Item1));
        }
        
        public override int GetHashCode()
        {
            int hashCode1 = Item1 != null ? Item1.GetHashCode() : 0;
            int hashCode2 = Item2 != null ? Item2.GetHashCode() : 0;
            unchecked
            {
                int max = Mathf.Max(hashCode1, hashCode2);
                return (hashCode1 + hashCode2) ^ max;
            }
        }
        
        public static bool Equals(UnorderedPair<TKey> lhs, UnorderedPair<TKey> rhs) => lhs.Equals(rhs);
        public override bool Equals(object? obj) => obj is UnorderedPair<TKey> other && Equals(other);
        public static bool operator ==(UnorderedPair<TKey> left, UnorderedPair<TKey> right) => left.Equals(right);
        public static bool operator !=(UnorderedPair<TKey> left, UnorderedPair<TKey> right) => !left.Equals(right);
    }

}
