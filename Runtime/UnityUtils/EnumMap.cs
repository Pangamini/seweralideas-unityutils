#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// A serialized map with a value for every option of an enum, drawn as one row per option, so the values can be assigned
    /// in the Inspector. Like Unity does for an enum field, it stores the enum's numeric value (not its name or position):
    /// renaming or reordering the options changes nothing, and a stored value follows its number. The enum is stored as an
    /// <see cref="int"/>, so enums with a 64 bit underlying type are not supported.
    ///
    /// An option without an entry (a newly added one, before the Inspector has shown it) reads as <c>default</c>. Entries
    /// of options that were removed from the enum stay in the data, listed in the Inspector with a button to remove them.
    /// </summary>
    [Serializable]
    public class EnumMap<TEnum, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TEnum, TValue>>
        where TEnum : struct, Enum
    {
        [Serializable]
        private struct Entry
        {
            public int    key;
            public TValue value;
        }

        [SerializeField] private List<Entry> _entries = new();

        // The entries as a lookup; the list is what is serialized
        [NonSerialized] private Dictionary<int, TValue>? _lookup;
        [NonSerialized] private bool                    _entriesStale;

        private static int KeyOf(TEnum option) => unchecked((int)Convert.ToInt64(option));

        private Dictionary<int, TValue> Lookup
        {
            get
            {
                if(_lookup == null)
                {
                    _lookup = new Dictionary<int, TValue>(_entries.Count);
                    foreach(Entry entry in _entries)
                        _lookup[entry.key] = entry.value;
                }
                return _lookup;
            }
        }

        /// <summary>The value of an option; <c>default</c> if it has none stored.</summary>
        public TValue this[TEnum option]
        {
            get => Lookup.TryGetValue(KeyOf(option), out TValue value) ? value : default!;
            set
            {
                Lookup[KeyOf(option)] = value;
                _entriesStale = true;
            }
        }

        /// <summary>Gets the value of an option, false if it has none stored.</summary>
        public bool TryGetValue(TEnum option, out TValue value) => Lookup.TryGetValue(KeyOf(option), out value);

        // The defined options by number, so that enumerating doesn't box to turn a stored number back into the enum
        private static readonly Dictionary<int, TEnum> DefinedOptions = BuildDefinedOptions();

        private static Dictionary<int, TEnum> BuildDefinedOptions()
        {
            var options = new Dictionary<int, TEnum>();
            foreach(TEnum option in Enum.GetValues(typeof(TEnum)))
                options[KeyOf(option)] = option;
            return options;
        }

        private static TEnum ToEnum(int key)
            => DefinedOptions.TryGetValue(key, out TEnum option) ? option : (TEnum)Enum.ToObject(typeof(TEnum), key);

        /// <summary>Enumerates the stored (option, value) pairs; a struct, so that <c>foreach</c> doesn't allocate.</summary>
        public Enumerator GetEnumerator() => new(Lookup);

        IEnumerator<KeyValuePair<TEnum, TValue>> IEnumerable<KeyValuePair<TEnum, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<TEnum, TValue>>
        {
            private Dictionary<int, TValue>.Enumerator _inner;

            internal Enumerator(Dictionary<int, TValue> lookup) => _inner = lookup.GetEnumerator();

            public KeyValuePair<TEnum, TValue> Current
            {
                get
                {
                    KeyValuePair<int, TValue> pair = _inner.Current;
                    return new KeyValuePair<TEnum, TValue>(ToEnum(pair.Key), pair.Value);
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext() => _inner.MoveNext();

            public void Dispose() => _inner.Dispose();

            void IEnumerator.Reset() => throw new NotSupportedException();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Only after a change from code; otherwise the list is what the Inspector edited
            if(!_entriesStale || _lookup == null)
                return;

            _entriesStale = false;
            _entries.Clear();
            foreach(KeyValuePair<int, TValue> pair in _lookup)
                _entries.Add(new Entry { key = pair.Key, value = pair.Value });
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _lookup       = null;
            _entriesStale = false;
        }
    }
}
