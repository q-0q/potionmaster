using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Wasp;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public enum ConstructState
{
    Planning,
    Building,
    Built,
    Moving,
    Destroying,
    Destroyed,
    
    PlayerInteracting,
    
    Placeable,
    NotPlaceable,
    
    PlanningPlaceable,
    PlanningNotPlaceable,
    
    MovingPlaceable,
    MovingNotPlaceable,
}

public enum ConstructTrigger
{
    Timeout,
    CreateFromPlayer,
    Interact,
    Destroy,
    
    BecomePlaceable,
    BecomeNotPlaceable,

}


public class ConstructController : Controller<ConstructState, ConstructTrigger>
{
    
    // State

    
    // Parameters
    public float buildTime = 1f;
    public float destroyTime = 1f;
    public Material planningMaterial;
    public string interest;
    public float vibrateStrength;
    public float vibrateDuration;
    public int vibrateVibrato;
    
    // References
    
    private Material _baseMaterial;
    private Transform _model;
    private ProgressBar _progressBar;
    public static event Action<ConstructController> PlayerInteractionStarted;
    public static event Action<ConstructController> PlayerInteractionEnded;
    

    private void Awake()
    {
        // References
        _model = transform.Find("Model");
        _baseMaterial = _model.GetComponentInChildren<Renderer>().material;
        _progressBar = GetComponentInChildren<ProgressBar>();
        
        
        // Machine
        
        Machine = new Machine<ConstructState, ConstructTrigger>(ConstructState.Built);
        Machine.OnTransitionCompleted(OnTransitionCompleted);

        Machine.Configure(ConstructState.PlayerInteracting)
            .OnEntry(_ => PlayerInteractionStarted?.Invoke(this));
            
        Machine.Configure(ConstructState.Planning)
            .SubstateOf(ConstructState.PlayerInteracting)
            .OnEntry(SetToPlanningMaterial);
        
        Machine.Configure(ConstructState.PlanningPlaceable)
            .Permit(ConstructTrigger.Interact, ConstructState.Building)
            .Permit(ConstructTrigger.BecomeNotPlaceable, ConstructState.PlanningNotPlaceable)
            .SubstateOf(ConstructState.Planning)
            .SubstateOf(ConstructState.Placeable);

        Machine.Configure(ConstructState.PlanningNotPlaceable)
            .Permit(ConstructTrigger.BecomePlaceable, ConstructState.PlanningPlaceable)
            .SubstateOf(ConstructState.Planning)
            .SubstateOf(ConstructState.NotPlaceable);

        Machine.Configure(ConstructState.Building)
            .OnEntry(_ => PlayerInteractionEnded?.Invoke(this))
            .OnEntry(Vibrate)
            .OnEntry(ShowProgressBar)
            .OnExit(HideProgressBar)
            .Permit(ConstructTrigger.Timeout, ConstructState.Built);
        
        Machine.Configure(ConstructState.Built)
            .OnEntry(_ => PlayerInteractionEnded?.Invoke(this))
            .OnEntry(Vibrate)
            .OnEntry(SetToBaseMaterial)
            .Permit(ConstructTrigger.Destroy, ConstructState.Destroying)
            .Permit(ConstructTrigger.Interact, ConstructState.MovingNotPlaceable)
            .Permit(ConstructTrigger.CreateFromPlayer, ConstructState.PlanningNotPlaceable);

        Machine.Configure(ConstructState.Moving)
            .SubstateOf(ConstructState.PlayerInteracting)
            .OnEntry(SetToPlanningMaterial);

        Machine.Configure(ConstructState.MovingPlaceable)
            .Permit(ConstructTrigger.Interact, ConstructState.Built)
            .Permit(ConstructTrigger.BecomeNotPlaceable, ConstructState.MovingNotPlaceable)
            .SubstateOf(ConstructState.Moving)
            .SubstateOf(ConstructState.Placeable);

        Machine.Configure(ConstructState.MovingNotPlaceable)
            .Permit(ConstructTrigger.BecomePlaceable, ConstructState.MovingPlaceable)
            .SubstateOf(ConstructState.Moving)
            .SubstateOf(ConstructState.NotPlaceable);

        Machine.Configure(ConstructState.Destroying)
            .Permit(ConstructTrigger.Timeout, ConstructState.Destroyed)
            .OnExit(_ => Destroy(gameObject));

        // Behaviors
        
        _behaviors = new Dictionary<ConstructState, Action>()
        {
            { ConstructState.Building , BuildingBehavior},
            { ConstructState.Placeable, PlaceableBehavior },
            { ConstructState.NotPlaceable , NotPlaceableBehavior }
        };
        
        // Listeners

        _listeners = new Dictionary<ConstructTrigger, Func<bool>>()
        {
            { ConstructTrigger.Timeout, TimeoutListener },
            { ConstructTrigger.BecomePlaceable, BecomePlaceableListener },
            { ConstructTrigger.BecomeNotPlaceable, BecomeNotPlaceableListener },
        };
        
        // State 
        
        HideProgressBar(null);
        
        // Debug
        
        InitDebug();
        
    }
    

    // Update is called once per frame
    void Update()
    {
        OnUpdate();
    }
    
        
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------

    
    // BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS BEHAVIORS

    private void PlaceableBehavior()
    {
        GameController.SnapToGrid(PlayerController.Singleton.lookRayCastPosition, transform, 
            -PlayerController.Singleton.orientation.forward);
    }
    
    private void NotPlaceableBehavior()
    {
        transform.position = PlayerController.Singleton.lookRayCastPosition;
        transform.LookAt(PlayerController.Singleton.transform);
    }

    private bool TimeoutListener()
    {
        if (Machine.IsInState(ConstructState.Building) && base.TimeInState > buildTime)
        {
            return true;
        }
        if (Machine.IsInState(ConstructState.Destroying) && base.TimeInState > destroyTime)
        {
            return true;
        }

        return false;
    }
    
    private void BuildingBehavior()
    {
        Material material = _model.GetComponentInChildren<Renderer>().material;
        float f = Mathf.Lerp(material.GetFloat("_Intensity"), 0.1f, Time.deltaTime * 5f / buildTime);
        float f2 = Mathf.Lerp(material.GetFloat("_Intensity_2"), 0.1f, Time.deltaTime * 5f / buildTime);
        Color c = Color.Lerp(material.GetColor("_Color"), Color.white, Time.deltaTime * 2f / buildTime);
        material.SetColor("_Color", c);
        material.SetFloat("_Intensity", f);
        material.SetFloat("_Intensity_2", f2);

        var rawT = TimeInState / buildTime;
        _progressBar.t = rawT;
        
        GameController.SnapToGrid(transform.position, transform, transform.forward);
    }
    
    
    // LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS LISTENERS 


    private bool BecomePlaceableListener()
    {
        return transform.position.y <= 0.05f;
    }
    
    private bool BecomeNotPlaceableListener()
    {
        return transform.position.y > 0.05f;
    }
    

    
    
    
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    
    
    public void SetToPlanningMaterial(TriggerParams? triggerParams)
    {
        foreach (var r in _model.GetComponentsInChildren<Renderer>())
        {
            r.material = planningMaterial;
        }
    }
    
    public void SetToBaseMaterial(TriggerParams? triggerParams)
    {
        foreach (var r in _model.GetComponentsInChildren<Renderer>())
        {
            r.material = _baseMaterial;
        }
    }
    
    public void Vibrate(TriggerParams? triggerParams)
    {
        transform.DOShakePosition(vibrateDuration, vibrateStrength, vibrateVibrato, 90F, false, true,
            ShakeRandomnessMode.Full);
    }
    
    public void ShowProgressBar(TriggerParams? triggerParams)
    {
        _progressBar.gameObject.SetActive(true);
    }

    public void HideProgressBar(TriggerParams? triggerParams)
    {
        _progressBar.gameObject.SetActive(false);
    }

    private void ReceiveInteractWithSignal(GameObject obj)
    {
        Debug.Log(obj.name);
        if (obj != gameObject) return;
        
        Machine.Fire(ConstructTrigger.Interact);
    }


    


    
    private void OnEnable()
    {
        PlayerController.OnInteractWithItem += ReceiveInteractWithSignal;
    }
    
    private void OnDisable()
    {
        PlayerController.OnInteractWithItem -= ReceiveInteractWithSignal;
    }
}
