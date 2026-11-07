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
        
        private readonly Dictionary<Component, TObj> m_collidersInside  = new();
        private readonly HashSet<Component> m_collidersStaying = new();
        
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

        // Nothing is detected while disabled. Everything is found again from the first OnTriggerStay after it is enabled, including
        // what was inside the whole time.
        protected void OnDisable() => ClearAll();

        protected void OnDestroy() => ClearAll();

        private void ClearAll()
        {
            m_collidersInside.Clear();
            m_collidersStaying.Clear();
            m_actorsInside.Clear();
        }

        // Only Stay is used, not Enter or Exit. Unity sends Stay every physics step to an enabled detector for every collider that
        // overlaps it, however long it has been there - so also after the detector is enabled, for one that was inside the whole time.
        // A collider that is disabled or destroyed while inside gets no Exit, but its Stay stops. So what is inside is what has stayed
        // since the last FixedUpdate (which comes before the physics step, whose Stays follow).
        protected void OnTriggerStay(Collider other) => Detect(other);

        // Enter does the same, so a new collider is seen right away whether or not Unity sends Stay for the step it entered in.
        // Detecting is idempotent: a collider is counted once, however many times it is reported.
        protected void OnTriggerEnter(Collider other) => Detect(other);

        // The same for 2D colliders: a detector works with whichever kind it is on (a collider is a Component either way).
        protected void OnTriggerStay2D(Collider2D other) => Detect(other);

        protected void OnTriggerEnter2D(Collider2D other) => Detect(other);

        private void Detect(Component other)
        {
            m_collidersStaying.Add(other);
            if(m_collidersInside.ContainsKey(other))
                return;

            // New. A collider that is not an actor is remembered too (as null), so it is not looked up in the hierarchy every step.
            TObj actor = other.GetComponentInParent<TBase>() as TObj;
            m_collidersInside[other] = actor;
            if(actor != null)
                m_actorsInside.Add(actor);
        }

        protected void FixedUpdate()
        {
            // process m_collidersStaying
            using (ListPool<KeyValuePair<Component, TObj>>.Get(out var toRemove))
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
            OnFixedUpdate();
        }
        
        protected virtual void OnFixedUpdate()
        {
        }

        private void MyTriggerExit(KeyValuePair<Component, TObj> other)
        {
            if(other.Value != null) // null: a collider that is not an actor
                m_actorsInside.Remove(other.Value);
        }

        public override sealed IEnumerable<object> GetObjectsInside() => m_actorsInside;
    }

    public class Detector<T> : Detector<T, T>
        where T : class
    {
    }
}
