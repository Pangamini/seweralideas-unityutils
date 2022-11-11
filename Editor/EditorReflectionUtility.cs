using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SeweralIdeas.UnityUtils.Editor
{
    public static class EditorReflectionUtility
    {
        private static Regex s_arrayDataIndexSelector = new Regex("data\\[(\\d*)\\]");

        public static FieldOrProperty? FindFieldInfo(string[] path, string name, Object[] objs)
        {
            foreach (Object obj in objs)
            {
                FieldOrProperty? fi = null;
                object o = obj;

                for (int i = 0; i < path.Length; i++)
                {
                    fi = GetFieldOrProperty(o.GetType(), path[i]);                     
                    if (fi == null)
                        return null;
                    o = fi.Value.GetValue(o);
                }

                return GetFieldOrProperty(o.GetType(), name);
            }
            return null;
        }

        public static FieldOrProperty? FindFieldInfo(string[] path, Object[] objs)
        {
            if (path.Length > 0)
                return FindFieldInfo(path, path[path.Length - 1], objs);
            return null;
        }

        public static void GetVariable<T>(string path, Object[] objs, List<T> result)
        {
            var pathSplit = path.Split('.');
            foreach (Object obj in objs)
            {
                FieldOrProperty? fi;
                object o = obj;

                for (int i = 0; i < pathSplit.Length; i++)
                {
                    if (pathSplit[i] == "Array")
                    {
                        ++i;
                        var match = s_arrayDataIndexSelector.Match(pathSplit[i]);
                        var matchVal = match.Groups[1].Value;
                        var matchIndex = int.Parse(matchVal);
                        var arr = o as System.Collections.IList;
                        try
                        {
                            o = arr[matchIndex];
                        }
                        catch// (System.IndexOutOfRangeException)
                        {
                            var valType = o.GetType().GetElementType();
                            if (!typeof(Object).IsAssignableFrom(valType))
                                o = System.Activator.CreateInstance(valType);
                            else
                                o = null;
                        }
                    }
                    else
                    {
                        fi = GetFieldOrProperty(o.GetType(), pathSplit[i]);
                        o = fi.Value.GetValue(o);
                    }
                }

                if(o is T)
                    result.Add((T)o);
            }
        }

        public static void SetVariable<T>(string path, Object[] objs, T value)
        {
            var pathSplit = path.Split('.');
            int objIndex = 0;
            var setStack = new Stack<System.Func<object, object>>();

            foreach (Object obj in objs)
            {
                FieldOrProperty? fi;
                object o = obj;

                for (int i = 0; i < pathSplit.Length; i++)
                {
                    if (!o.GetType().IsValueType)
                        setStack.Clear();

                    if (pathSplit[i] == "Array")
                    {
                        ++i;
                        var match = s_arrayDataIndexSelector.Match(pathSplit[i]);
                        var matchVal = match.Groups[1].Value;
                        var matchIndex = int.Parse(matchVal);
                        var arr = o as System.Collections.IList;
                        o = arr[matchIndex];
                        setStack.Push((object ob)=> { arr[matchIndex] = ob; return arr; });
                    }
                    else
                    {
                        fi = GetFieldOrProperty(o.GetType(), pathSplit[i]);
                        var targO = o;  // copy to stack for the closure to handle old value properly
                        var targFi = fi;
                        setStack.Push((object ob) => { targFi.Value.SetValue(targO, ob); return targO; });
                        o = fi.Value.GetValue(o);
                    }
                }
                o = value;
                while (setStack.Count > 0)
                    o = setStack.Pop()(o);
                objIndex++;
            }
        }

        public struct FieldOrProperty
        {
            private FieldInfo m_field;
            private PropertyInfo m_property;

            public FieldOrProperty(FieldInfo field)
            {
                m_field = field;
                m_property = null;
            }

            public FieldOrProperty(PropertyInfo property)
            {
                m_field = null;
                m_property = property;
            }

            public object GetValue(object obj)
            {
                if (m_field != null)
                    return m_field.GetValue(obj);
                if (m_property != null)
                    return m_property.GetValue(obj, null);
                return null;
            }

            public void SetValue(object obj, object value)
            {
                if (m_field != null)
                    m_field.SetValue(obj, value);
                else if (m_property != null)
                    m_property.SetValue(obj, value, null);
            }
        }

        public static FieldOrProperty? GetFieldOrProperty(System.Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return new FieldOrProperty(field);

            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
                return new FieldOrProperty(prop);


            if (type.BaseType != null)
                return GetFieldOrProperty(type.BaseType, fieldName);
            return null;

        }

    }
}