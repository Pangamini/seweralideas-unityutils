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
        private readonly ObservableSet<TChild> _children = new();
        internal bool ParentDestroyed => _children == null;

        public ReadonlyObservableSet<TChild> Children => _children.GetReadonly();

        internal void RegisterChild(TChild childComponent)
        {
            _children.Add(childComponent);
        }
        
        internal void UnregisterChild(TChild childComponent)
        {
            if(_children != null)
                _children.Remove(childComponent);
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
                
                _children.Clear();
                
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
        private readonly Observable<TParent> _parent = new();
        public Observable<TParent>.Readonly Parent => _parent.ReadOnly;

        protected internal void SetParent(TParent newParent)
        {
            if(_parent.Value == newParent)
                return;
            
            if(_parent.Value)
                _parent.Value.UnregisterChild((TChild)this);
            
            _parent.Value = newParent;
                
            if(_parent.Value)
                _parent.Value.RegisterChild((TChild)this);
        }
        
        protected virtual void Awake()
        {
            if(_parent.Value == null)
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
    
    
    public abstract class ParentChildComponent<T> : MonoBehaviour where T:ParentChildComponent<T>
    {
        private readonly Observable<T> _parent = new();
        public Observable<T>.Readonly Parent => _parent.ReadOnly;

        private void SetParent(T newParent)
        {
            if(_parent.Value == newParent)
                return;
            
            if(_parent.Value)
                _parent.Value.UnregisterChild((T)this);
            
            _parent.Value = newParent;
                
            if(_parent.Value)
                _parent.Value.RegisterChild((T)this);
        }
        
        protected virtual void Awake()
        {
            if(_parent.Value == null)
                FindNewParent();
            
            T thisAsParent = (T)this;
            using (ListPool<T>.Get(out var children))
            {
                gameObject.GetComponentsInChildrenRecursively<T, T>(children);
                foreach (var child in children)
                {
                    if (ReferenceEquals(child, this))
                        continue;
                    
                    child.SetParent(thisAsParent);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            SetParent( null );
            
            using (ListPool<T>.Get(out var children))
            {
                foreach(var child in Children)
                    children.Add(child);
                
                _children.Clear();
                
                foreach (var child in children)
                {
                    child.FindNewParent();
                }
            }
        }
        
        protected void OnTransformParentChanged()
        {
            FindNewParent();
        }

        private void FindNewParent()
        {
            using (ListPool<T>.Get(out var parents))
            {
                var transf = transform.parent;

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
        
        /// Parent behaviour
        
        private readonly ObservableSet<T> _children = new();

        private bool ParentDestroyed => _children == null;

        public ReadonlyObservableSet<T> Children => _children.GetReadonly();

        private void RegisterChild(T childComponent)
        {
            _children.Add(childComponent);
        }

        private void UnregisterChild(T childComponent)
        {
            if(_children != null)
                _children.Remove(childComponent);
        }
    }

}