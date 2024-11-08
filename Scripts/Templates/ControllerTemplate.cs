using System;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Wasp;


public enum NewState
{

}

public enum NewTrigger
{

}

public class NewController : Controller<NewState, NewTrigger>
{
    // Parameters


    // References


    // State
    

    private void Awake()
    {

        // References


        // Machine

        Machine = new Machine<NewState, NewTrigger>(0);
        Machine.OnTransitionCompleted(OnTransitionCompleted);
        

        // Behaviors

        _behaviors = new Dictionary<NewState, Action>()
        {

        };
        
        // Listeners

        _listeners = new Dictionary<NewTrigger, Func<bool>>()
        {

        };
        
        // Debug
        
        InitDebug();
        
        
    }

    private void Update()
    {
        OnUpdate();
    }
    
    
    // BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS
    
    
    
    
    
    // LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS 
    
    
    
    
        
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------

    
}

