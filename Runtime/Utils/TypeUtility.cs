using System.Collections.Generic;
using System;
using UnityEngine.Pool;

namespace SeweralIdeas.Utils
{
    public class TypeUtility
    {
        private TypeUtility() { }

        public class TypeList
        {
            public TypeList(IEnumerable<Type> _types)
            {
                types = new List<Type>(_types).ToArray();

                names = new string[types.Length];
                for (int i = 0; i < types.Length; ++i)
                    names[i] = types[i].ToString();
            }

            public readonly Type[] types;
            public readonly string[] names;
        }

        [Serializable]
        public struct TypeQuery
        {
            public Type type;
            public bool includeAbstract;
            public bool includeSelf;

            public TypeQuery(Type type, bool includeAbstract, bool includeSelf)
            {
                this.type = type;
                this.includeAbstract = includeAbstract;
                this.includeSelf = includeSelf;
            }

        }

        private static Dictionary<TypeQuery, TypeList> m_cache = new Dictionary<TypeQuery, TypeList>();


        public static Type GetCommonBaseType(Type lhs, Type rhs)
        {
            while (lhs != null)
            {
                if (lhs.IsAssignableFrom(rhs))
                    return lhs;
                lhs = lhs.BaseType;
            }
            return null;
        }

        private static TypeList FindDerivedTypes(TypeQuery query)
        {
            using (ListPool<Type>.Get(out var result))
            {
                if (query.includeSelf && (query.includeAbstract || !query.type.IsAbstract))
                    result.Add(query.type);

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch(System.Reflection.ReflectionTypeLoadException e)
                    {
                        Console.Error.WriteLine(e.Message);
                        continue;
                    }
                    foreach (var type in types)
                    {
                        if (type == query.type)
                            continue;   // base type included above, to support generics
                        if ((!query.includeAbstract) && type.IsAbstract)
                            continue;

                        if (query.type.IsAssignableFrom(type))
                        {
                            result.Add(type);
                        }
                    }
                }
                var typeList = new TypeList(result);
                return typeList;
            }
        }

        public static TypeList GetDerivedTypes(TypeQuery query)
        {
            TypeList typeList;
            if (!m_cache.TryGetValue(query, out typeList))
            {
                typeList = FindDerivedTypes(query);
                m_cache.Add(query, typeList);
            }
            return typeList;
        }
        
    }
}