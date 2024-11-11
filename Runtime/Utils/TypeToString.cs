using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Pool;

namespace SeweralIdeas.Utils
{
    public static class TypeToString
    {
        private static Dictionary<string, System.Type> s_nameToTypeCache = new Dictionary<string, System.Type>();
        private static Dictionary<System.Type, string> s_typeToNameCache = new Dictionary<System.Type, string>();

        private static System.Threading.ReaderWriterLockSlim s_nameCacheLock = new System.Threading.ReaderWriterLockSlim();
        private static System.Threading.ReaderWriterLockSlim s_typeCacheLock = new System.Threading.ReaderWriterLockSlim();

        /// <summary>
        /// Group 1 = type name with excluded assembly information
        /// Group 2 = type name with excluded generic arguments
        /// Group 3 = generic arguments (if present)
        /// Group 4 = array brackets at the end
        /// </summary>
        private static Regex s_genericArgsRegex = new Regex("^(([^<>\\[\\],]*)(?:<(.*)>)?)(\\[.*\\])?[,]?");

        public static void ClearCaches()
        {
            s_nameToTypeCache.Clear();
            s_typeToNameCache.Clear();
        }

        public static void SplitNestedTypes(string typeName, List<string> output)
        {
            output.Clear();
            int brDepth = 0;
            int subStart = 0;
            for (int i = 0; i < typeName.Length; ++i)
            {
                var ch = typeName[i];
                if (ch == '<')
                    brDepth++;
                else if (ch == '>')
                    brDepth--;
                else if (ch == '+' && brDepth == 0)
                {
                    var sub = typeName.Substring(subStart, i - subStart);
                    output.Add(sub);
                    subStart = i + 1;
                }
            }
            var subEnd = typeName.Substring(subStart);
            output.Add(subEnd);
            return;
        }
        
        private static string GetTypeName_Internal(System.Type type, System.Type[] genericArgs, ref int genericIndex)
        {
            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[]";
            }

            var name = type.Name;

            if (type.DeclaringType == null || type.IsGenericParameter)
            {
                if (!string.IsNullOrEmpty(type.Namespace))
                    name = type.Namespace + "." + name;
            }
            else
                name = GetTypeName_Internal(type.DeclaringType, genericArgs, ref genericIndex) + "+" + name;



            if (type.IsGenericType)
            {
                var split = name.Split('`');
                if (split.Length > 1)
                {
                    name = split[0];
                    name += "<";
                    
                    bool first = true;
                    var genArgs = type.GetGenericArguments().Length;
                    for (int i = genericIndex; i < genArgs; ++i)
                    {
                        if (first) first = false;
                        else
                            name += ", ";
                        var argName = GetTypeName(genericArgs[i]);
                        name += argName;
                        ++genericIndex;
                    }
                    name += ">";
                }
            }

            return name;
        }

        public static string GetTypeName(System.Type type)
        {
            try
            {
                string name;
                if (type == null) return null;

                s_typeCacheLock.EnterReadLock();
                var hasValue = s_typeToNameCache.TryGetValue(type, out name);
                s_typeCacheLock.ExitReadLock();

                if (!hasValue)
                {
                    int genIndex = 0;
                    name = GetTypeName_Internal(type, type.GetGenericArguments(), ref genIndex);

                    s_typeCacheLock.EnterWriteLock();
                    try
                    {
                        s_typeToNameCache.Add(type, name);
                    }
                    catch (System.ArgumentException)
                    { /*ignore, some other thread filled this already*/  }
                    finally
                    {
                        s_typeCacheLock.ExitWriteLock();
                    }
                }
                return name;
            }
            catch (System.Exception e)
            {
                throw new System.Exception(string.Format("TypeToString.GetTypeName(\"{0}\") failed with an error", type), e);
            }
        }


        public static System.Type ParseType(string name)
        {
            try
            {
                System.Type type;
                if (string.IsNullOrEmpty(name))
                    return null;

                s_nameCacheLock.EnterReadLock();
                var hasValue = s_nameToTypeCache.TryGetValue(name, out type);
                s_nameCacheLock.ExitReadLock();

                if (!hasValue)
                {
                    using (ListPool<System.Type>.Get(out var genericArgs))
                    {
                        type = ParseType_Internal(name, genericArgs);
                    }
                    
                    s_nameCacheLock.EnterWriteLock();
                    try
                    {
                        s_nameToTypeCache.Add(name, type);
                    }
                    catch (System.ArgumentException)
                    { /*ignore, some other thread filled this already*/  }
                    finally
                    {
                        s_nameCacheLock.ExitWriteLock();
                    }
                }
                return type;
            }
            catch (System.Exception e)
            {
                throw new System.Exception(string.Format("TypeToString.ParseType(\"{0}\") failed with an error", name), e);
            }
        }

        private static System.Type ParseType_Internal(string name, List<System.Type> genericArgs, System.Type declaringType = null, int depth = 0)
        {
            if (depth >= 50)
                throw new System.InvalidOperationException("Infinite loop or something");
            ++depth;

            System.Type type = null;

            // try the system format
            if (declaringType != null)
                type = declaringType.GetNestedType(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            else
                type = System.Type.GetType(name);
            if (type != null)
            {
                if (type.IsGenericTypeDefinition)
                    type = type.MakeGenericType(genericArgs.ToArray());
                return type;
            }

            // split nested types
            using (ListPool<string>.Get(out var splitNested))
            {
                SplitNestedTypes(name, splitNested);
                if (splitNested.Count > 1)
                {
                    type = ParseType_Internal(splitNested[0], genericArgs, null, depth);
                    for (int i = 1; i < splitNested.Count; ++i)
                    {
                        type = ParseType_Internal(splitNested[i], genericArgs, type, depth);
                    }
                    return type;
                }
            }


            var genMatch = s_genericArgsRegex.Match(name);
            if (genMatch.Success)
            {
                var typeNameStr = genMatch.Groups[1].Value;
                var typeBaseNameStr = genMatch.Groups[2].Value;
                var genericArgsStr = genMatch.Groups[3].Value;
                var arrayBrackets = genMatch.Groups[4].Value;
                int newArgCount = 0;

                if (!string.IsNullOrEmpty(genericArgsStr))
                {
                    var genArgNames = genericArgsStr.Split(',');
                    newArgCount = genArgNames.Length;
                    for (int i = 0; i < genArgNames.Length; ++i)
                    {
                        var genArg = ParseType(genArgNames[i].Trim());
                        genericArgs.Add(genArg);
                    }
                }

                string baseName;
                if (newArgCount > 0)
                    baseName = typeBaseNameStr + '`' + newArgCount;
                else
                    baseName = typeBaseNameStr;

                System.Type baseType;
                if (declaringType != null)
                    baseType = declaringType.GetNestedType(baseName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                else
                    baseType = FindTypeInAssemblies(baseName);

                if (baseType != null && baseType.IsGenericTypeDefinition)
                {
                    type = baseType.MakeGenericType(genericArgs.ToArray());
                }
                else
                    type = baseType;
                

                if (!string.IsNullOrEmpty(arrayBrackets))
                {
                    type = type.MakeArrayType();
                }
            }

            return type;
        }
        
        private static System.Type FindTypeInAssemblies(string fullTypeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var aType in assembly.GetTypes())
                {
                    if (aType.FullName == fullTypeName)
                    {
                        return aType;
                    }
                }
            }
            return null;
        }

        public static System.Type GetCommonAncestor(IList objects)
        {
            if (objects.Count == 0) return null;
            System.Type ancestor = null;
            for (int i = 0; i < objects.Count; ++i)
            {
                var obj = objects[i];
                // skip null objects
                if (obj == null) continue;

                var objType = obj.GetType();
                if (ancestor == null)
                {
                    // first valid object
                    ancestor = objType;
                    continue;
                }

                ancestor = GetCommonAncestor(ancestor, objType);
            }

            return ancestor;
        }

        public static System.Type GetCommonAncestor(System.Type lhs, System.Type rhs)
        {
            if (lhs == rhs) return lhs;
            if (lhs.IsAssignableFrom(rhs))
                return lhs;
            return GetCommonAncestor(lhs.BaseType, rhs);
        }
    }
}