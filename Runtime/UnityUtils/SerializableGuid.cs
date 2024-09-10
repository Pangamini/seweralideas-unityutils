using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [Serializable]
    public struct SerializableGuid : IEquatable<SerializableGuid>, IComparable<SerializableGuid>
    {
        [SerializeField] private int m_a;
        [SerializeField] private short m_b;
        [SerializeField] private short m_c;
        [SerializeField] private byte m_d;
        [SerializeField] private byte m_e;
        [SerializeField] private byte m_f;
        [SerializeField] private byte m_g;
        [SerializeField] private byte m_h;
        [SerializeField] private byte m_i;
        [SerializeField] private byte m_j;
        [SerializeField] private byte m_k;
        
        public SerializableGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
        {
            m_a = a;
            m_b = b;
            m_c = c;
            m_d = d;
            m_e = e;
            m_f = f;
            m_g = g;
            m_h = h;
            m_i = i;
            m_j = j;
            m_k = k;
        }
        
        public int A
        {
            get => m_a;
            set => m_a = value;
        }
        public short B
        {
            get => m_b;
            set => m_b = value;
        }
        public short C
        {
            get => m_c;
            set => m_c = value;
        }
        public byte D
        {
            get => m_d;
            set => m_d = value;
        }
        public byte E
        {
            get => m_e;
            set => m_e = value;
        }
        public byte F
        {
            get => m_f;
            set => m_f = value;
        }
        public byte G
        {
            get => m_g;
            set => m_g = value;
        }
        public byte H
        {
            get => m_h;
            set => m_h = value;
        }
        public byte I
        {
            get => m_i;
            set => m_i = value;
        }
        public byte J
        {
            get => m_j;
            set => m_j = value;
        }
        public byte K
        {
            get => m_k;
            set => m_k = value;
        }

        public bool Equals(SerializableGuid other) => ((Guid)this).Equals((Guid)other);
        public override bool Equals(object obj) => obj is SerializableGuid other && Equals(other);
        public override int GetHashCode() => ((Guid)this).GetHashCode();
        public static bool operator ==(SerializableGuid left, SerializableGuid right) => left.Equals(right);
        public static bool operator !=(SerializableGuid left, SerializableGuid right) => !left.Equals(right);
        
        public static implicit operator Guid(SerializableGuid serializable)
        {
            return new Guid(
                serializable.m_a,
                serializable.m_b, 
                serializable.m_c, 
                serializable.m_d,
                serializable.m_e,
                serializable.m_f, 
                serializable.m_g,
                serializable.m_h, 
                serializable.m_i, 
                serializable.m_j, 
                serializable.m_k);
        }
        
        public static implicit operator SerializableGuid(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);

            SerializableGuid serializable;
            
            serializable.m_a = BitConverter.ToInt32(bytes.Slice(0));
            serializable.m_b = BitConverter.ToInt16(bytes.Slice(4));
            serializable.m_c = BitConverter.ToInt16(bytes.Slice(6));
            serializable.m_d = bytes[8];
            serializable.m_e = bytes[9];
            serializable.m_f = bytes[10];
            serializable.m_g = bytes[11];
            serializable.m_h = bytes[12];
            serializable.m_i = bytes[13];
            serializable.m_j = bytes[14];
            serializable.m_k = bytes[15];
            return serializable;
        }

        public static SerializableGuid Parse(string input) => Guid.Parse(input);
        public static SerializableGuid Parse(ReadOnlySpan<char> input) => Guid.Parse(input);
        public override string ToString() => ((Guid)this).ToString();
        public int CompareTo(SerializableGuid other) => ((Guid)this).CompareTo((Guid)other);
        
        public static bool TryParse(string input, out SerializableGuid guid)
        {
            if(!Guid.TryParse(input, out Guid stdGuid))
            {
                guid = default;
                return false;
            }
            guid = stdGuid;
            return true;
        }
        
        public static bool TryParse(ReadOnlySpan<char> input, out SerializableGuid guid)
        {
            if(!Guid.TryParse(input, out Guid stdGuid))
            {
                guid = default;
                return false;
            }
            guid = stdGuid;
            return true;
        }
    }
}
