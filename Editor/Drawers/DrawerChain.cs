using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    /// <summary>
    /// Implemented by drawers that wrap the rest of the field's drawing (hide it, redirect it to another property, ...) and so need
    /// to pass control on to the field's other attribute drawers. See <see cref="DrawerChain"/>.
    /// </summary>
    public interface IChainedDrawer
    {
        void  OnGUIChained(Rect position, SerializedProperty property, GUIContent label, DrawerChain.Next next);
        float GetHeightChained(SerializedProperty property, GUIContent label, DrawerChain.Next next);
    }

    /// <summary>
    /// Unity runs only one <see cref="PropertyDrawer"/> per field, even when it has several <see cref="PropertyAttribute"/>s.
    /// A drawer that implements <see cref="IChainedDrawer"/> calls <see cref="Start"/> from its OnGUI and GetPropertyHeight instead:
    /// every drawer on the field then runs in <see cref="PropertyAttribute.order"/> order, whichever of them Unity picked, and each
    /// chained drawer decides how to call <see cref="Next"/>. A drawer that isn't chained is drawn as the end of the chain (it
    /// draws through <c>EditorGUI.PropertyField</c>, so the attributes after it are lost). After the last one, the property is drawn
    /// by its own default drawer.
    /// </summary>
    public static class DrawerChain
    {
        public readonly struct Next
        {
            private readonly List<PropertyDrawer> _chain;
            private readonly int                  _index;

            internal Next(List<PropertyDrawer> chain, int index)
            {
                _chain = chain;
                _index = index;
            }

            public void Draw(Rect position, SerializedProperty property, GUIContent label)
            {
                if(_index >= _chain.Count)
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    return;
                }

                PropertyDrawer drawer = _chain[_index];
                if(drawer is IChainedDrawer chained)
                    chained.OnGUIChained(position, property, label, new Next(_chain, _index + 1));
                else
                    drawer.OnGUI(position, property, label);
            }

            public float GetHeight(SerializedProperty property, GUIContent label)
            {
                if(_index >= _chain.Count)
                    return EditorGUI.GetPropertyHeight(property, label, true);

                PropertyDrawer drawer = _chain[_index];
                return drawer is IChainedDrawer chained
                    ? chained.GetHeightChained(property, label, new Next(_chain, _index + 1))
                    : drawer.GetPropertyHeight(property, label);
            }
        }

        /// <summary>The whole chain of the field <paramref name="entry"/> draws, from the first drawer.</summary>
        public static Next Start(PropertyDrawer entry) => new(GetChain(entry), 0);

        private static readonly ConditionalWeakTable<PropertyDrawer, List<PropertyDrawer>> s_chains = new();

        private static readonly FieldInfo s_attributeField = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo s_fieldInfoField = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        private static List<PropertyDrawer> GetChain(PropertyDrawer entry)
        {
            if(s_chains.TryGetValue(entry, out List<PropertyDrawer> chain))
                return chain;

            chain = Build(entry);
            s_chains.Add(entry, chain);
            return chain;
        }

        private static List<PropertyDrawer> Build(PropertyDrawer entry)
        {
            var chain = new List<PropertyDrawer>();
            FieldInfo field = entry.fieldInfo;
            if(field == null || entry.attribute == null)
            {
                chain.Add(entry);
                return chain;
            }

            // OrderBy is stable, so attributes of equal order keep the order the runtime reports them in.
            IEnumerable<PropertyAttribute> attributes = field.GetCustomAttributes(typeof(PropertyAttribute), true)
                                                             .Cast<PropertyAttribute>()
                                                             .OrderBy(a => a.order);

            foreach (PropertyAttribute attribute in attributes)
            {
                if(attribute.GetType() == entry.attribute.GetType())
                {
                    chain.Add(entry);
                    continue;
                }

                PropertyDrawer drawer = CreateDrawer(attribute, field);
                if(drawer != null)
                    chain.Add(drawer);
            }

            if(!chain.Contains(entry))
                chain.Add(entry);
            return chain;
        }

        private static PropertyDrawer CreateDrawer(PropertyAttribute attribute, FieldInfo field)
        {
            Type drawerType = FindDrawerType(attribute.GetType());
            if(drawerType == null)
                return null; // a decorator, or an attribute with no drawer

            if(s_attributeField == null || s_fieldInfoField == null)
            {
                Debug.LogWarning("[DrawerChain] PropertyDrawer's internals changed; the attributes after this one are not drawn.");
                return null;
            }

            var drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
            s_attributeField.SetValue(drawer, attribute);
            s_fieldInfoField.SetValue(drawer, field);
            return drawer;
        }

        // attribute type -> drawer type, with the useForChildren flag
        private static Dictionary<Type, (Type drawer, bool children)> s_drawerTypes;

        private static Type FindDrawerType(Type attributeType)
        {
            s_drawerTypes ??= BuildDrawerTypes();

            for (Type t = attributeType; t != null && t != typeof(PropertyAttribute); t = t.BaseType)
            {
                if(s_drawerTypes.TryGetValue(t, out var entry) && (t == attributeType || entry.children))
                    return entry.drawer;
            }

            return null;
        }

        private static Dictionary<Type, (Type, bool)> BuildDrawerTypes()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo typeField     = typeof(CustomPropertyDrawer).GetField("m_Type", flags);
            FieldInfo childrenField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", flags);

            var result = new Dictionary<Type, (Type, bool)>();
            if(typeField == null || childrenField == null)
                return result;

            foreach (Type drawerType in TypeCache.GetTypesDerivedFrom<PropertyDrawer>())
            {
                foreach (CustomPropertyDrawer custom in drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), false))
                {
                    var target = (Type)typeField.GetValue(custom);
                    if(target != null && typeof(PropertyAttribute).IsAssignableFrom(target))
                        result[target] = (drawerType, (bool)childrenField.GetValue(custom));
                }
            }

            return result;
        }
    }
}
