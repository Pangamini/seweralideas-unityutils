using System;
using System.Collections.Generic;
using SeweralIdeas.Collections;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
public interface IDetector
{
    IEnumerable<object> GetObjectsInside();
}

public interface IHasDestroyEvent
{
    event Action<object> Destroyed;
}

public class Detector<TObj, TBase> : MonoBehaviour, IDetector
    where TBase : class
    where TObj : class, TBase
{
    private readonly MultiSet<TObj> m_actorsInside = new();
    public ReadonlyMultiSet<TObj> ActorsInside => m_actorsInside.GetReadonly();
    private readonly Action<object> m_onActorKilled;

    public Detector()
    {
        m_onActorKilled = (obj) => m_actorsInside.RemoveAll((TObj)obj);
    }

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

    private void Awake()
    {
        m_actorsInside.Added += OnAdded;
        m_actorsInside.Removed += OnRemoved;
    }
    
    private void OnAdded(TObj obj)
    {
        if(obj is IHasDestroyEvent hasDestroyCallback)
        {
            hasDestroyCallback.Destroyed += m_onActorKilled;
            return;
        }
        
        var addedGo = (obj as Component)!.gameObject;
        addedGo.SubscribeToDestroy(m_onActorKilled, obj);
    }

    private void OnRemoved(TObj obj)
    {
        if(obj is IHasDestroyEvent hasDestroyCallback)
        {
            hasDestroyCallback.Destroyed -= m_onActorKilled;
            return;
        }
        
        // cast so we can use unity's equality operator
        Component component = obj as Component;
        if (component == null)
            return;
        var removedGo = component.gameObject;
        removedGo.UnsubscribeFromDestroy(m_onActorKilled, obj);
    }

    
    protected void OnDestroy()
    {
        m_actorsInside.Clear();
    }

    protected void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentInParent<TBase>() is not TObj actor)
            return;
        
        m_actorsInside.Add(actor);
    }
    
    protected void OnTriggerExit(Collider other)
    {
        if(other.GetComponentInParent<TBase>() is not TObj actor)
            return;
        
        m_actorsInside.Remove(actor);
    }

    IEnumerable<object> IDetector.GetObjectsInside() => m_actorsInside;
}
}