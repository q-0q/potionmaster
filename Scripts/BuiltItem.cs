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

public class BuiltItem : MonoBehaviour
{
    
    // State
    private float _timeInState;
    
    // Parameters
    public float buildTime = 1f;
    public float destroyTime = 1f;
    public Material buildMaterial;
    public string interest;
    
    // Control
    public Machine<State, Trigger> Machine;
    private Dictionary<State, Action> _behaviors;
    
    // References
    private Canvas _canvas;
    private TextMeshProUGUI _stateTmp;
    private TextMeshProUGUI _interestTmp;
    private Material _baseMaterial;
    private Transform _model;
    

    private void Awake()
    {
        // References
        {
            _canvas = GetComponentInChildren<Canvas>();
            _stateTmp = _canvas.transform.Find("State").GetComponent<TextMeshProUGUI>();
            _interestTmp = _canvas.transform.Find("Interest").GetComponent<TextMeshProUGUI>();
            _model = transform.Find("Model");
            _baseMaterial = _model.GetComponentInChildren<Renderer>().material;
        }


        // Machine
        {
            Machine = new Machine<State, Trigger>(State.Built);
            Machine.OnTransitionCompleted(OnTransitionCompleted);

            Machine.Configure(State.Planning)
                .OnEntry(SetToBuildMaterial)
                .Permit(Trigger.Build, State.Building);

            Machine.Configure(State.Building)
                .Permit(Trigger.Timeout, State.Built);
            
            Machine.Configure(State.Built)
                .OnEntry(SetToBaseMaterial)
                .Permit(Trigger.Destroy, State.Destroying)
                .Permit(Trigger.CreateFromPlayer, State.Planning);

            Machine.Configure(State.Destroying)
                .Permit(Trigger.Timeout, State.Destroyed)
                .OnExit(_ => Destroy(gameObject));
        }

        // Behaviors
        {
            _behaviors = new Dictionary<State, Action>()
            {
                { State.Building , BuildingBehavior}
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
            _stateTmp.text = Machine.State().ToString();
            _interestTmp.text = interest;
            _canvas.transform.LookAt(Camera.main.transform);
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
            {
                // Build
                {
                    // Comes from signal
                }
                
                // Timeout
                {
                    if (Machine.IsInState(State.Building) && _timeInState > buildTime)
                    {
                        Machine.Fire(Trigger.Timeout);
                    }
                    if (Machine.IsInState(State.Destroying) && _timeInState > destroyTime)
                    {
                        Machine.Fire(Trigger.Timeout);
                    }
                }
            }
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
    
    public void SetToBuildMaterial(TriggerParams? triggerParams)
    {
        foreach (var r in _model.GetComponentsInChildren<Renderer>())
        {
            r.material = buildMaterial;
        }
    }
    
    public void SetToBaseMaterial(TriggerParams? triggerParams)
    {
        foreach (var r in _model.GetComponentsInChildren<Renderer>())
        {
            r.material = _baseMaterial;
        }
    }

    private void ReceiveBuildSignal()
    {
        Machine.Fire(Trigger.Build);
    }

    private void BuildingBehavior()
    {
        Material material = _model.GetComponentInChildren<Renderer>().material;
        float f = Mathf.Lerp(material.GetFloat("_Intensity"), 0.1f, Time.deltaTime * 5f);
        Color c = Color.Lerp(material.GetColor("_Emission"), Color.white, Time.deltaTime * 1f);
        material.SetColor("_Emission", c);
        material.SetFloat("_Intensity", f);
    }
    
    
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------

    public enum State
    {
        Planning,
        Building,
        Built,
        Destroying,
        Destroyed,
    }

    public enum Trigger
    {
        Timeout,
        CreateFromPlayer,
        Build,
        Destroy,
    }
    
    private void OnEnable()
    {
        PlayerControllerNew.OnBuildItem += ReceiveBuildSignal;
    }
    
    private void OnDisable()
    {
        PlayerControllerNew.OnBuildItem -= ReceiveBuildSignal;
    }
}
