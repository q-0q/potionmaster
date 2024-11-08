using System;
using System.Collections;
using System.Collections.Generic;
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

public class BuiltItemOld : MonoBehaviour
{
    
    // State
    private float _timeInState;
    
    // Parameters
    public float buildTime = 1f;
    public float destroyTime = 1f;
    public Material buildMaterial;
    public string interest;
    public float vibrateStrength;
    public float vibrateDuration;
    public int vibrateVibrato;
    
    // Control
    public Machine<State, Trigger> Machine;
    private Dictionary<State, Action> _behaviors;
    
    // References
    private Canvas _canvas;
    private TextMeshProUGUI _stateTmp;
    private TextMeshProUGUI _interestTmp;
    private Material _baseMaterial;
    private Transform _model;
    private ProgressBar _progressBar;
    

    private void Awake()
    {
        // References
        {
            _canvas = GetComponentInChildren<Canvas>();
            _stateTmp = _canvas.transform.Find("State").GetComponent<TextMeshProUGUI>();
            _interestTmp = _canvas.transform.Find("Interest").GetComponent<TextMeshProUGUI>();
            _model = transform.Find("Model");
            _baseMaterial = _model.GetComponentInChildren<Renderer>().material;
            _progressBar = GetComponentInChildren<ProgressBar>();
        }


        // Machine
        {
            Machine = new Machine<State, Trigger>(State.Built);
            Machine.OnTransitionCompleted(OnTransitionCompleted);

            Machine.Configure(State.Planning)
                .OnEntry(SetToBuildMaterial)
                .Permit(Trigger.Interact, State.Building);

            Machine.Configure(State.Building)
                .OnEntry(Vibrate)
                .OnEntry(ShowProgressBar)
                .OnExit(HideProgressBar)
                .Permit(Trigger.Timeout, State.Built);
            
            Machine.Configure(State.Built)
                .OnEntry(Vibrate)
                .OnEntry(SetToBaseMaterial)
                .Permit(Trigger.Destroy, State.Destroying)
                .Permit(Trigger.Interact, State.Planning)
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
            HideProgressBar(null);
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
        
        Machine.Fire(Trigger.Interact);
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

        var rawT = _timeInState / buildTime;
        _progressBar.t = rawT;
        
        GameController.SnapToGrid(transform.position, transform, transform.forward);
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
        Interact,
        Destroy,
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
