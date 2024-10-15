using System.Collections.Generic;
using SeweralIdeas.Collections;
using UnityEngine;
using UnityEngine.Pool;
namespace SeweralIdeas.UnityUtils
{
    public interface IDetector
    {
        IEnumerable<object> GetObjectsInside();
    }

    public abstract class Detector : MonoBehaviour, IDetector
    {
        public abstract IEnumerable<object> GetObjectsInside();
    }
    
    public class Detector<TObj, TBase> : Detector
        where TBase : class
        where TObj : class, TBase
    {
        private readonly MultiSet<TObj> m_actorsInside = new();
        
        private readonly Dictionary<Collider, TObj> m_collidersInside  = new();
        private readonly HashSet<Collider> m_collidersStaying = new();
        
        public ReadonlyMultiSet<TObj> ActorsInside => m_actorsInside.GetReadonly();

        protected virtual bool Filter(TObj obj) => true;

        public bool GetAny(out TObj obj)
        {
            foreach (var inside in m_actorsInside)
            {
                if(!Filter(inside))
                    continue;
                obj = inside;
                return true;
            }
            obj = default;
            return false;
        }

        protected void OnDestroy()
        {
            m_actorsInside.Clear();
        }

        protected void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<TBase>() is not TObj actor)
                return;

            m_collidersInside[other] = actor;
            m_collidersStaying.Add(other);
            m_actorsInside.Add(actor);
        }

        protected void OnTriggerStay(Collider other)
        {
            m_collidersStaying.Add(other);
        }

        protected void FixedUpdate()
        {
            // process m_collidersStaying
            using (ListPool<KeyValuePair<Collider, TObj>>.Get(out var toRemove))
            {
                foreach (var pair in m_collidersInside)
                {
                    if(m_collidersStaying.Contains(pair.Key))
                        continue;
                    
                    toRemove.Add(pair);
                }
                
                m_collidersStaying.Clear();

                foreach (var pair in toRemove)
                    m_collidersInside.Remove(pair.Key);

                foreach (var pair in toRemove)
                    MyTriggerExit(pair);
            }
        }

        private void MyTriggerExit(KeyValuePair<Collider, TObj> other)
        {
            m_actorsInside.Remove(other.Value);
        }

        public override sealed IEnumerable<object> GetObjectsInside() => m_actorsInside;
    }

    public class Detector<T> : Detector<T, T>
        where T : class
    {
    }
}