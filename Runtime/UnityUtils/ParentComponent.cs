using SeweralIdeas.Collections;
using UnityEngine;
using UnityEngine.Pool;
using SeweralIdeas.Utils;
    
namespace SeweralIdeas.UnityUtils
{
    public class ParentComponent : MonoBehaviour
    {}
    
    public class ChildComponent : MonoBehaviour
    {}
    
    public class ParentComponent<TParent, TChild> : ParentComponent
        where TParent : ParentComponent<TParent, TChild>
        where TChild : ChildComponent<TParent, TChild>
    {
        private readonly ObservableSet<TChild> m_children = new();
        internal bool ParentDestroyed => m_children == null;

        public ReadonlyObservableSet<TChild> Children => m_children.GetReadonly();

        internal void RegisterChild(TChild childComponent)
        {
            m_children.Add(childComponent);
        }
        
        internal void UnregisterChild(TChild childComponent)
        {
            if(m_children != null)
                m_children.Remove(childComponent);
        }
        
        protected virtual void Awake()
        {
            TParent thisAsParent = (TParent)this;
            using (ListPool<TChild>.Get(out var children))
            {
                gameObject.GetComponentsInChildrenRecursively<TChild, TParent>(children);
                foreach (var child in children)
                {
                    child.SetParent(thisAsParent);
                }
            }
        }
        
        protected virtual void OnDestroy()
        {
            using (ListPool<ChildComponent<TParent, TChild>>.Get(out var children))
            {
                foreach(var child in Children)
                    children.Add(child);
                
                m_children.Clear();
                
                foreach (var child in children)
                {
                    child.FindNewParent();
                }
            }
        }
    }

    public class ChildComponent<TParent, TChild> : ChildComponent
        where TParent : ParentComponent<TParent, TChild>
        where TChild : ChildComponent<TParent, TChild>
    {
        private readonly Observable<TParent> m_parent = new();
        public Observable<TParent>.Readonly Parent => m_parent.ReadOnly;

        protected internal void SetParent(TParent newParent)
        {
            if(m_parent.Value == newParent)
                return;
            
            if(m_parent.Value)
                m_parent.Value.UnregisterChild((TChild)this);
            
            m_parent.Value = newParent;
                
            if(m_parent.Value)
                m_parent.Value.RegisterChild((TChild)this);
            
        }
        
        protected virtual void Awake()
        {
            if(m_parent.Value == null)
                FindNewParent();
        }

        protected virtual void OnDestroy()
        {
            SetParent( null );
        }
        
        protected void OnTransformParentChanged()
        {
            FindNewParent();
        }
        
        internal void FindNewParent()
        {
            using (ListPool<TParent>.Get(out var parents))
            {
                var transf = transform;

                while(transf != null)
                {
                    transf.GetComponents(parents);
                    foreach (var parent in parents)
                    {
                        if(parent.ParentDestroyed)
                            continue;

                        SetParent(parent);
                        return;
                    }

                    transf = transf.parent;
                }
            }
            SetParent( null );
        }
    }
}