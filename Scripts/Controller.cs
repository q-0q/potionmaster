using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Wasp;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class Controller<TState, TTrigger> : MonoBehaviour where TState : new()
{
    
    // State
    protected float TimeInState;
    
    // Control
    public Machine<TState, TTrigger> Machine;
    protected Dictionary<TState, Action> _behaviors;
    protected Dictionary<TTrigger, Func<bool>> _listeners;
    
    // Debug
    private GameObject _controllerDebugPrefab;
    private ControllerDebug _controllerDebug;

    public Controller()
    {
        TimeInState = 0;
    }

    protected void InitDebug()
    {
        _controllerDebugPrefab = Resources.Load("Prefabs/ControllerDebug") as GameObject;
        _controllerDebug = Instantiate(_controllerDebugPrefab, transform).GetComponent<ControllerDebug>();
    }
    
    protected void OnUpdate()
    {
        InvokeFires();
        InvokeBehaviors();
        IncrementTimeInState();
        UpdateDebug();
    }
    
    private void InvokeFires()
    {
        foreach (TTrigger trigger in Enum.GetValues(typeof(TTrigger)))
        {
            if (InvokeListener(trigger))
            {
                Machine.Fire(trigger);
            }
        }
    }
    
    
    protected void ManualFire(TTrigger trigger, TriggerParams? triggerParams)
    {
        Machine.Fire(trigger, triggerParams);
    }

    private bool InvokeListener(TTrigger trigger)
    {
        if (_listeners.ContainsKey(trigger))
        {
            return _listeners[trigger]();
        }

        return false;
    }
    
    private void InvokeBehaviors()
    {
        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            if (!Machine.IsInState(state)) continue;
            if (!_behaviors.ContainsKey(state)) continue;
            _behaviors[state]?.Invoke();
        }
    }


    protected void OnTransitionCompleted(TriggerParams? triggerParams)
    {
        TimeInState = 0;
    }
    
    public void IncrementTimeInState()
    {
        TimeInState += Time.deltaTime;
    }

    private void UpdateDebug()
    {
        if (_controllerDebug is null) return; 
        _controllerDebug.SetText(Machine.State().ToString());
    }
}
