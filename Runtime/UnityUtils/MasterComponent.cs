using System.Collections.Generic;
using SeweralIdeas.Collections;
using UnityEngine;
using UnityEngine.Pool;
namespace SeweralIdeas.UnityUtils
{
    public class MasterComponent<TMaster, TSlave> : MonoBehaviour
        where TMaster : MasterComponent<TMaster, TSlave>
        where TSlave : SlaveComponent<TMaster, TSlave>
    {
        private HashSet<TSlave> m_slaves = new();
        internal bool MasterDestroyed => m_slaves == null;

        public ReadonlySetView<TSlave> Slaves => new(m_slaves);

        internal void RegisterSlave(TSlave slaveComponent)
        {
            m_slaves.Add(slaveComponent);
        }
        
        internal void UnregisterSlave(TSlave slaveComponent)
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
                foreach(var slave in Slaves)
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
        public TMaster Master
        {
            get => m_master;
            protected internal set
            {
                if(m_master == value)
                    return;

                if(m_master)
                    m_master.UnregisterSlave((TSlave)this);

                m_master = value;
                    
                if(m_master)
                    m_master.RegisterSlave((TSlave)this);

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