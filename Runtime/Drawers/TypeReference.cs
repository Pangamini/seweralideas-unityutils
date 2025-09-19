using System;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [Serializable]
    public struct TypeReference : ISerializationCallbackReceiver
    {
        [SerializeField] private string _typeName;
        private                  Type   _type;

        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                _typeName = _type?.AssemblyQualifiedName;
            }
        }

        public static implicit operator TypeReference(Type type) => new(){Type = type};
        public static implicit operator Type(TypeReference type) => type.Type;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize() => _type = Type.GetType(_typeName);
    }

    public class TypeReferenceAttribute : PropertyAttribute
    {
        private TypeUtility.TypeQuery _query;
        public TypeUtility.TypeQuery Query => _query;
        public TypeReferenceAttribute(Type baseType, bool includeAbstract, bool includeSelf)
        {
            _query = new TypeUtility.TypeQuery(baseType, includeAbstract, includeSelf);
        }
    }
}
