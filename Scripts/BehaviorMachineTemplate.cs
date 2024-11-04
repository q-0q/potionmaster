using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Wasp;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BehaviorMachineTemplate : MonoBehaviour
{
    
    // State
    private float _timeInState;
    
    // Parameters

    
    // Control
    public Machine<State, Trigger> Machine;
    private Dictionary<State, Action> _behaviors;
    
    // References

    
    

    private void Awake()
    {
        // References
        {

        }


        // Machine
        {
            Machine = new Machine<State, Trigger>(0);
            Machine.OnTransitionCompleted(OnTransitionCompleted);
        }

        // Behaviors
        {
            _behaviors = new Dictionary<State, Action>()
            {
                
            };
        }
        
        // State 
        {

        }
    }
    

    // Update is called once per frame
    void Update()
    {
        // Update debug
        {

        }
        
        // Execute behaviors
        {
            IncrementTimeInState(Time.deltaTime);
            foreach (State state in Enum.GetValues(typeof(State)))
            {
                if (!Machine.IsInState(state)) continue;
                if (!_behaviors.ContainsKey(state)) continue;
                _behaviors[state]?.Invoke();
            }
        }
        
        // Fire
        {

        }
    }
    
    
    private void OnTransitionCompleted(TriggerParams? triggerParams)
    {
        _timeInState = 0;
    }
    
    public void IncrementTimeInState(float deltaTime)
    {
        _timeInState += deltaTime;
    }
    
    
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------

    public enum State
    {

    }

    public enum Trigger
    {

    }
}
