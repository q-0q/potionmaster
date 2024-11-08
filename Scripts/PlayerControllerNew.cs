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

public class PlayerControllerNew : MonoBehaviour
{
    
    // State
    private float _timeInState;
    private Vector3 _moveVector; 
    private Vector3 _lookRayCastPosition;
    
    // Parameters
    public float moveSpeed = 4f;
    public float lookSpeed = 350f;
    public float flySpeed = 4f;
    public float panTiltAmount = 2f;
    public float panTiltRecoveryStrength = 9f;
    private float _panTiltTarget = 0;
    public float bobAmount = 0.035f;
    public float bobSpeed = 15f;
    private float _bobWeight = 0f;
    private float _bobTargetWeight = 0f;
    public float bobRampSpeed = 15f;
    public float inventoryLookAngle = 52f;
    public float inventoryLookAngleSpeed = 18f;
    public float inventoryTransitionDuration = 0.25f;
    public float inventoryStartRotation = 30f;
    public float buildDistance = 6f;
    public GameObject BuiltItemPrefab;
    
    // Control
    public Machine<State, Trigger> Machine;
    private Dictionary<State, Action> _behaviors;
    
    // References
    public static PlayerControllerNew Singleton;
    private PlayerInput _playerInput;
    private Transform _orientation;
    private Transform _cameraHolder;
    private Transform _cameraLockTransform;
    private Transform _lookAt;
    public GameObject obstacle;
    private TextMeshProUGUI _stateTmp;
    private GameObject _inventoryModel;
    private GameObject _ghostBuildItem;
    public static event Action OnBuildItem;
    private Transform _debugBall;
    


    private void OnEnable()
    {
        InventoryButton.OnPlayerSelectGhostItem += ReceiveButtonSignal;
    }
    
    private void OnDisable()
    {
        InventoryButton.OnPlayerSelectGhostItem -= ReceiveButtonSignal;
    }

    
    private void Awake()
    {
        // Misc
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        // References
        {
            if (Singleton != this && Singleton != null)
            {
                Destroy(gameObject);
            }
            Singleton = this;
        
            _playerInput = GetComponent<PlayerInput>();
            _orientation = transform.Find("Orientation");
            _cameraHolder = _orientation.Find("CameraHolder");
            _cameraLockTransform = _cameraHolder.Find("CameraTransformLock");
            _lookAt = _cameraHolder.Find("LookAt");
            _stateTmp = transform.Find("Canvas").GetComponentInChildren<TextMeshProUGUI>();
            _inventoryModel = GetComponentInChildren<PlayerInventoryModel>().gameObject;
            _debugBall = transform.Find("DebugBall");
            _debugBall.SetParent(null);
        }


        // Machine
        {
            Machine = new Machine<State, Trigger>(0);
            Machine.OnTransitionCompleted(OnTransitionCompleted);

            Machine.Configure(State.InWorld)
                .Permit(Trigger.PressInteractKey, State.InventoryOpening);
            
            Machine.Configure(State.InventoryOpening)
                .OnEntry(OnStartInventoryOpening)
                .SubstateOf(State.InventoryTransition)
                .Permit(Trigger.Timeout, State.InventoryOpen);
            
            Machine.Configure(State.InventoryOpen)
                .OnEntry(OnInventoryFinishOpening)
                .OnExit(OnInventoryStartClosing)
                .Permit(Trigger.PressInteractKey, State.InventoryClosingToWorld)
                .Permit(Trigger.SelectItem, State.InventoryClosingToGhostItem);

            Machine.Configure(State.InventoryClosing)
                .OnExit(OnInventoryClose)
                .SubstateOf(State.InventoryTransition);
            
            Machine.Configure(State.InventoryClosingToWorld)
                .SubstateOf(State.InventoryClosing)
                .Permit(Trigger.Timeout, State.InWorld);
            
            Machine.Configure(State.InventoryClosingToGhostItem)
                .SubstateOf(State.InventoryClosing)
                .OnEntry(SetGhostItem)
                .Permit(Trigger.Timeout, State.PlanningBuildCannotPlace);
            
            Machine.Configure(State.PlanningBuildCanPlace)
                .OnExitFrom(Trigger.PressInteractKey, BuildGhostItem)
                .Permit(Trigger.PressInteractKey, State.InWorld)
                .Permit(Trigger.LookAtAir, State.PlanningBuildCannotPlace)
                .SubstateOf(State.PlanningBuild);
            
            Machine.Configure(State.PlanningBuildCannotPlace)
                .Permit(Trigger.LookAtGround, State.PlanningBuildCanPlace)
                .SubstateOf(State.PlanningBuild);

        }

        // Behaviors
        {
            _behaviors = new Dictionary<State, Action>()
            {
                { State.InWorld , InWorldBehavior},
                { State.InventoryOpen , InventoryOpenBehavior},
                { State.InventoryClosing , InventoryClosingBehavior },
                { State.InventoryOpening , InventoryOpeningBehavior },
                { State.PlanningBuild , PlanningBuildBehavior },
                { State.PlanningBuildCanPlace , CanBuildBehavior} ,
                { State.PlanningBuildCannotPlace , CannotBuildBehavior}
            };
        }
        
        // State 
        {
            _moveVector = Vector3.zero;
            _inventoryModel.SetActive(false);
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Update debug
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Instantiate(obstacle, transform.position, _orientation.rotation);
            }
            _stateTmp.text = Machine.State().ToString();
            _debugBall.position = _lookRayCastPosition;
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
            // ToggleInventory
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Machine.Fire(Trigger.PressInteractKey);
                }
            }
            
            // Timeout
            {
                if (Machine.IsInState(State.InventoryTransition) && _timeInState > inventoryTransitionDuration)
                {
                    Machine.Fire(Trigger.Timeout);
                }
            }
            
            // LookAtGround, LookAtAir
            {
                if (_lookRayCastPosition.y < 0.1f)
                {
                    Machine.Fire(Trigger.LookAtGround);
                    
                }
                else
                {
                    Machine.Fire(Trigger.LookAtAir);
                }
            }
        }
    }

    private void SetGhostItem(TriggerParams? triggerParams)
    {
        if (triggerParams is null) return;
        string interest = ((StringTriggerParam)triggerParams).str;
        _ghostBuildItem = Instantiate(BuiltItemPrefab);
        _ghostBuildItem.GetComponent<ConstructController>().interest = interest;
        _ghostBuildItem.GetComponent<ConstructController>().Machine.Fire(ConstructTrigger.CreateFromPlayer);
    }
    
    private void BuildGhostItem(TriggerParams? triggerParams)
    {
        _ghostBuildItem.transform.SetParent(null);
        _ghostBuildItem = null;
        OnBuildItem?.Invoke();
    }
    
    private void ClearGhostItem(TriggerParams? triggerParams)
    {
        if (_ghostBuildItem != null) Destroy(_ghostBuildItem);
        _ghostBuildItem = null;
    }

    private void OnStartInventoryOpening(TriggerParams? triggerParams)
    {
        _inventoryModel.SetActive(true);
        if (_cameraHolder.localRotation.eulerAngles.x > 100f)
        {
            _cameraHolder.localRotation = Quaternion.Euler(5f, 0, 0f);
        }

    }
    
    private void OnInventoryClose(TriggerParams? triggerParams)
    {
        _inventoryModel.SetActive(false);

    }

    private void OnInventoryFinishOpening(TriggerParams? triggerParams)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void OnInventoryStartClosing(TriggerParams? triggerParams)
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    
    private void OnTransitionCompleted(TriggerParams? triggerParams)
    {
        _timeInState = 0;
    }
    
    public void IncrementTimeInState(float deltaTime)
    {
        _timeInState += deltaTime;
    }

    void InWorldBehavior()
    {
        LookBehavior();
        MoveBehavior();
        FlyBehavior();
    }

    void InventoryOpenBehavior()
    {
        MoveBehavior(0.5f, 0.5f);
    }
    
    void InventoryClosingBehavior()
    {
        MoveBehavior(0.3f, 0.3f);
        LerpVerticalLookAngle(5f);
        LerpInventoryRotation(inventoryStartRotation);
    }

    private void LerpVerticalLookAngle(float angle)
    {
        Vector3 currentHolderEulers = _cameraHolder.localRotation.eulerAngles;
        var verticalRotation = currentHolderEulers.x;

        verticalRotation =
            Mathf.Lerp(verticalRotation, angle, Time.deltaTime * inventoryLookAngleSpeed);
        
        _cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    
    private void LerpInventoryRotation(float angle)
    {
        Vector3 currentEulers = _inventoryModel.transform.localRotation.eulerAngles;
        var verticalRotation = currentEulers.x;

        verticalRotation =
            Mathf.Lerp(verticalRotation, angle, Time.deltaTime * inventoryLookAngleSpeed);
        
        _inventoryModel.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void InventoryOpeningBehavior()
    {
        LerpInventoryRotation(0);
        MoveBehavior(0.3f, 0.3f);
        LerpVerticalLookAngle(inventoryLookAngle);
    }

    void PlanningBuildBehavior()
    {
        LookBehavior();
        MoveBehavior();
        FlyBehavior();
    }
    
    void CannotBuildBehavior()
    {
        _ghostBuildItem.transform.position = _lookRayCastPosition;
    }
    
    void CanBuildBehavior()
    {
        GameController.SnapToGrid(_lookRayCastPosition, _ghostBuildItem.transform, _orientation.forward * -1);
    }


    void LookBehavior()
    {
        Vector2 lookDelta2 = _playerInput.actions["Look"].ReadValue<Vector2>();
        Vector3 currentOrientationEulers = _orientation.localRotation.eulerAngles;
        Vector3 currentHolderEulers = _cameraHolder.localRotation.eulerAngles;

        float tiltAmt = lookDelta2.x * panTiltAmount;
        tiltAmt = Mathf.Clamp(tiltAmt, -20f, 20f);
        _panTiltTarget = Mathf.Lerp(_panTiltTarget, tiltAmt, Time.deltaTime * panTiltRecoveryStrength);

        var horizontalRotation = currentOrientationEulers.y + (lookDelta2.x * Time.deltaTime * lookSpeed);

        var verticalRotation = currentHolderEulers.x + (lookDelta2.y * Time.deltaTime * lookSpeed) * -1f;
        
        _orientation.localRotation = Quaternion.Euler(0, horizontalRotation, 0);
        _cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0, _panTiltTarget);
        
        if (Physics.Raycast(_cameraHolder.position, _cameraHolder.forward, out var hit, buildDistance,
                ~LayerMask.NameToLayer("Floor"), QueryTriggerInteraction.Collide))
        {
            _lookRayCastPosition = hit.point;
        }
        else
        {
            _lookRayCastPosition = _cameraHolder.position + _cameraHolder.forward * buildDistance;
        }
    }

    void MoveBehavior(float speedModifier = 1f, float bobSpeedModifier = 1f)
    {
        Vector2 moveVector2 = _playerInput.actions["Move"].ReadValue<Vector2>();
        _moveVector = new Vector3(moveVector2.x, 0, moveVector2.y);
        _moveVector = _orientation.rotation * _moveVector;
        transform.position += _moveVector * (moveSpeed * Time.deltaTime * speedModifier);
        
        _bobTargetWeight = _moveVector.magnitude * moveSpeed;
        _bobTargetWeight = Mathf.Clamp(_bobTargetWeight, 0, 1);
        _bobWeight = Mathf.Lerp(_bobWeight, _bobTargetWeight, Time.deltaTime * bobRampSpeed);
        _cameraLockTransform.localPosition = new Vector3(0, Mathf.Sin(Time.time * bobSpeed * bobSpeedModifier) * bobAmount * _bobWeight, 0);
        _cameraLockTransform.LookAt(_lookAt, _cameraHolder.up);
    }

    private void FlyBehavior()
    {
        Vector2 flyVector2 = _playerInput.actions["Fly"].ReadValue<Vector2>();
        Vector3 flyVector = new Vector3(0, flyVector2.y, 0);
        flyVector = _orientation.rotation * flyVector;
        transform.position += flyVector * (flySpeed * Time.deltaTime);
    }

    private void ReceiveButtonSignal(string item)
    {
        Machine.Fire(Trigger.SelectItem, new StringTriggerParam() { str = item});
    }


    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------


    private class StringTriggerParam : TriggerParams
    {
        public string str = null;
    }
    
    public enum State
    {
        InWorld,
        InventoryOpen,
        InventoryOpening,
        InventoryClosing,
        InventoryClosingToWorld,
        InventoryClosingToGhostItem,
        InventoryTransition,
        PlanningBuild,
        PlanningBuildCanPlace,
        PlanningBuildCannotPlace,
    }

    public enum Trigger
    {
        PressInteractKey,
        Timeout,
        SelectItem,
        LookAtGround,
        LookAtAir,
    }
}
