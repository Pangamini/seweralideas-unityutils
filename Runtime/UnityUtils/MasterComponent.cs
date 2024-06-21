using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
namespace SeweralIdeas.UnityUtils
{
    public class MasterComponent<TMaster, TSlave> : MonoBehaviour
        where TMaster : MasterComponent<TMaster, TSlave>
        where TSlave : SlaveComponent<TMaster, TSlave>
    {
        private HashSet<SlaveComponent<TMaster, TSlave>> m_slaves = new();
        internal bool MasterDestroyed => m_slaves == null;
        
        internal void RegisterSlave(SlaveComponent<TMaster, TSlave> slaveComponent)
        {
            m_slaves.Add(slaveComponent);
        }
        
        internal void UnregisterSlave(SlaveComponent<TMaster, TSlave> slaveComponent)
        {
            if(m_slaves != null)
                m_slaves.Remove(slaveComponent);
        }
        
        protected virtual void Awake()
        {
            TMaster thisAsMaster = (TMaster)this;
            using (ListPool<TSlave>.Get(out var children))
            {
                gameObject.GetComponentsInChildrenRecursively<TSlave, TMaster>(children);
                foreach (var child in children)
                {
                    child.Master = thisAsMaster;
                }
            }
        }
        
        protected virtual void OnDestroy()
        {
            using (ListPool<SlaveComponent<TMaster, TSlave>>.Get(out var slaves))
            {
                foreach(var slave in m_slaves)
                    slaves.Add(slave);

                m_slaves = null;
                
                foreach (var slave in slaves)
                {
                    slave.FindNewMaster();
                }
            }
        }
    }

    public class SlaveComponent<TMaster, TSlave> : MonoBehaviour
        where TMaster : MasterComponent<TMaster, TSlave>
        where TSlave : SlaveComponent<TMaster, TSlave>
    {
        private TMaster m_master;
        protected internal TMaster Master
        {
            protected get => m_master;
            set
            {
                if(m_master == value)
                    return;

                if(m_master)
                    m_master.UnregisterSlave(this);

                m_master = value;
                    
                if(m_master)
                    m_master.RegisterSlave(this);

            }
        }

        protected virtual void Awake()
        {
            if(Master == null)
                FindNewMaster();
        }

        protected virtual void OnDestroy()
        {
            Master = null;
        }
        
        protected void OnTransformParentChanged()
        {
            FindNewMaster();
        }
        
        internal void FindNewMaster()
        {
            using (ListPool<TMaster>.Get(out var masters))
            {
                var transf = transform;

                while(transf != null)
                {
                    transf.GetComponents(masters);
                    foreach (var master in masters)
                    {
                        if(master.MasterDestroyed)
                            continue;

                        Master = master;
                        return;
                    }

                    transf = transf.parent;
                }
            }
            Master = null;
        }
    }
}