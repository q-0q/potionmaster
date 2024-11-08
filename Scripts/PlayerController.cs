using System;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Wasp;


public enum PlayerState
{
    InWorld,
    
    LookingAtNothing,
    LookingAtConstruct,
    InventoryOpen,
    InventoryOpening,
    InventoryClosing,
    InventoryClosingToWorld,
    InventoryClosingToConstructInteracting,
    InventoryTransition,
    
    ConstructInteracting,
}

public enum PlayerTrigger
{
    PressInteractKey,
    Timeout,
    SelectItem,
    LookAtConstruct,
    LookAwayFromConstruct,
    
    ConstructStartedInteraction,
    ConstructEndedInteraction,
}

public class PlayerController : Controller<PlayerState, PlayerTrigger>
{
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
    public GameObject constructPrefab;

    // References
    public static PlayerController Singleton;
    private PlayerInput _playerInput;
    public Transform orientation;
    private Transform _cameraHolder;
    private Transform _cameraLockTransform;
    private Transform _lookAt;
    private TextMeshProUGUI _stateTmp;
    private GameObject _inventoryModel;
    public static event Action<GameObject> OnInteractWithItem;
    private Transform _debugBall;

    // PlayerState
    private Vector3 _moveVector;
    public Vector3 lookRayCastPosition;
    private ConstructController _currentConstructController;
    
    private void OnEnable()
    {
        InventoryButton.OnPlayerSelectGhostItem += ReceiveButtonSignal;
        ConstructController.PlayerInteractionStarted += ReceiveConstructPlayerInteractionStarted;
        ConstructController.PlayerInteractionEnded += ReceiveConstructPlayerInteractionEnded;
    }
    
    private void OnDisable()
    {
        InventoryButton.OnPlayerSelectGhostItem -= ReceiveButtonSignal;
        ConstructController.PlayerInteractionStarted -= ReceiveConstructPlayerInteractionStarted;
        ConstructController.PlayerInteractionEnded -= ReceiveConstructPlayerInteractionEnded;
    }

    private void Awake()
    {

        // References

        if (Singleton != this && Singleton != null)
        {
            Destroy(gameObject);
        }

        Singleton = this;

        _playerInput = GetComponent<PlayerInput>();
        orientation = transform.Find("Orientation");
        _cameraHolder = orientation.Find("CameraHolder");
        _cameraLockTransform = _cameraHolder.Find("CameraTransformLock");
        _lookAt = _cameraHolder.Find("LookAt");
        _stateTmp = transform.Find("Canvas").GetComponentInChildren<TextMeshProUGUI>();
        _inventoryModel = GetComponentInChildren<PlayerInventoryModel>().gameObject;
        _debugBall = transform.Find("DebugBall");
        _debugBall.SetParent(null);

        // Machine

        Machine = new Machine<PlayerState, PlayerTrigger>(PlayerState.LookingAtNothing);
        Machine.OnTransitionCompleted(OnTransitionCompleted);

        Machine.Configure(PlayerState.InWorld)
            .Permit(PlayerTrigger.ConstructStartedInteraction, PlayerState.ConstructInteracting);
        
        Machine.Configure(PlayerState.LookingAtNothing)
            .Permit(PlayerTrigger.PressInteractKey, PlayerState.InventoryOpening)
            .Permit(PlayerTrigger.LookAtConstruct, PlayerState.LookingAtConstruct)
            .SubstateOf(PlayerState.InWorld);

        Machine.Configure(PlayerState.LookingAtConstruct)
            .OnEntry(SetCurrentConstructController)
            .OnExit(ClearCurrentConstructController)
            .Permit(PlayerTrigger.LookAwayFromConstruct, PlayerState.LookingAtNothing)
            .SubstateOf(PlayerState.InWorld);

        Machine.Configure(PlayerState.InventoryOpening)
            .OnEntry(OnStartInventoryOpening)
            .SubstateOf(PlayerState.InventoryTransition)
            .Permit(PlayerTrigger.Timeout, PlayerState.InventoryOpen);

        Machine.Configure(PlayerState.InventoryOpen)
            .OnEntry(OnInventoryFinishOpening)
            .OnExit(OnInventoryStartClosing)
            .Permit(PlayerTrigger.PressInteractKey, PlayerState.InventoryClosingToWorld)
            .Permit(PlayerTrigger.SelectItem, PlayerState.InventoryClosingToConstructInteracting);

        Machine.Configure(PlayerState.InventoryClosing)
            .OnExit(OnInventoryClose)
            .SubstateOf(PlayerState.InventoryTransition);

        Machine.Configure(PlayerState.InventoryClosingToWorld)
            .SubstateOf(PlayerState.InventoryClosing)
            .Permit(PlayerTrigger.Timeout, PlayerState.LookingAtNothing);

        Machine.Configure(PlayerState.InventoryClosingToConstructInteracting)
            .SubstateOf(PlayerState.InventoryClosing)
            .Permit(PlayerTrigger.Timeout, PlayerState.ConstructInteracting)
            .OnEntry(InstantiatePlannedItem);

        Machine.Configure(PlayerState.ConstructInteracting)
            .Permit(PlayerTrigger.ConstructEndedInteraction, PlayerState.LookingAtNothing);
        
        // Behaviors

        _behaviors = new Dictionary<PlayerState, Action>()
        {
            { PlayerState.InWorld, InWorldBehavior },
            { PlayerState.InventoryOpen, InventoryOpenBehavior },
            { PlayerState.InventoryClosing, InventoryClosingBehavior },
            { PlayerState.InventoryOpening, InventoryOpeningBehavior },
            { PlayerState.ConstructInteracting, ConstructInteractingBehavior },
            { PlayerState.LookingAtConstruct , LookingAtConstructBehavior }
        };
        
        // Listeners

        _listeners = new Dictionary<PlayerTrigger, Func<bool>>()
        {
            { PlayerTrigger.Timeout, TimeoutListener },
            { PlayerTrigger.PressInteractKey, ToggleInventoryListener },
            { PlayerTrigger.LookAtConstruct , LookAtConstructListener },
            { PlayerTrigger.LookAwayFromConstruct , LookAwayFromConstructListener }
        };

        // PlayerState

        _moveVector = Vector3.zero;
        _inventoryModel.SetActive(false);
        

        // Misc
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        OnUpdate();
        _stateTmp.text = Machine.State().ToString();
    }


    private bool ToggleInventoryListener()
    {
        return Input.GetKeyDown(KeyCode.E);
    }

    private bool TimeoutListener()
    {
        return Machine.IsInState(PlayerState.InventoryTransition) && TimeInState > inventoryTransitionDuration;
    }
    
    private bool LookAtConstructListener()
    {
        if (Physics.Raycast(_cameraHolder.position, _cameraHolder.forward, out var hit, buildDistance,
                LayerMask.GetMask("BuiltItem"), QueryTriggerInteraction.Collide))
        {
            GameObject obj = hit.transform.gameObject.GetClosestParentWithComponent<ConstructController>();
            ManualFire(PlayerTrigger.LookAtConstruct, new GameObjectTriggerParam() { GameObject = obj });
            return true;
        }

        return false;
    }

    private bool LookAwayFromConstructListener()
    {
        return !Physics.Raycast(_cameraHolder.position, _cameraHolder.forward, out var hit, buildDistance,
            LayerMask.GetMask("BuiltItem"), QueryTriggerInteraction.Collide);
    }


    private void InstantiatePlannedItem(TriggerParams? triggerParams)
    {
        if (triggerParams is null) return;
        string interest = ((StringTriggerParam)triggerParams).str;
        _currentConstructController = Instantiate(constructPrefab).GetComponent<ConstructController>();
        _currentConstructController.interest = interest;
        _currentConstructController.Machine.Fire(ConstructTrigger.CreateFromPlayer);
    }
    
    private void SetCurrentConstructController(TriggerParams? triggerParams)
    {
        if (triggerParams is null) return;
        GameObject obj = ((GameObjectTriggerParam)triggerParams).GameObject;
        _currentConstructController = obj.GetComponent<ConstructController>();
    }
    
    private void ClearCurrentConstructController(TriggerParams? triggerParams)
    {
        _currentConstructController = null;
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

    void ConstructInteractingBehavior()
    {
        LookBehavior();
        MoveBehavior();
        FlyBehavior();
        FireInteractOnCurrentConstructController();
    }

    void LookingAtConstructBehavior()
    {
        FireInteractOnCurrentConstructController();
    }
    
    void LookBehavior()
    {
        Vector2 lookDelta2 = _playerInput.actions["Look"].ReadValue<Vector2>();
        Vector3 currentOrientationEulers = orientation.localRotation.eulerAngles;
        Vector3 currentHolderEulers = _cameraHolder.localRotation.eulerAngles;

        float tiltAmt = lookDelta2.x * panTiltAmount;
        tiltAmt = Mathf.Clamp(tiltAmt, -20f, 20f);
        _panTiltTarget = Mathf.Lerp(_panTiltTarget, tiltAmt, Time.deltaTime * panTiltRecoveryStrength);

        var horizontalRotation = currentOrientationEulers.y + (lookDelta2.x * Time.deltaTime * lookSpeed);

        var verticalRotation = currentHolderEulers.x + (lookDelta2.y * Time.deltaTime * lookSpeed) * -1f;

        orientation.localRotation = Quaternion.Euler(0, horizontalRotation, 0);
        _cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0, _panTiltTarget);

        if (Physics.Raycast(_cameraHolder.position, _cameraHolder.forward, out var hit, buildDistance,
                LayerMask.GetMask("Floor"), QueryTriggerInteraction.Collide))
        {
            lookRayCastPosition = hit.point;
        }
        else
        {
            lookRayCastPosition = _cameraHolder.position + _cameraHolder.forward * buildDistance;
        }
    }

    void MoveBehavior(float speedModifier = 1f, float bobSpeedModifier = 1f)
    {
        Vector2 moveVector2 = _playerInput.actions["Move"].ReadValue<Vector2>();
        _moveVector = new Vector3(moveVector2.x, 0, moveVector2.y);
        _moveVector = orientation.rotation * _moveVector;
        transform.position += _moveVector * (moveSpeed * Time.deltaTime * speedModifier);

        _bobTargetWeight = _moveVector.magnitude * moveSpeed;
        _bobTargetWeight = Mathf.Clamp(_bobTargetWeight, 0, 1);
        _bobWeight = Mathf.Lerp(_bobWeight, _bobTargetWeight, Time.deltaTime * bobRampSpeed);
        _cameraLockTransform.localPosition = new Vector3(0,
            Mathf.Sin(Time.time * bobSpeed * bobSpeedModifier) * bobAmount * _bobWeight, 0);
        _cameraLockTransform.LookAt(_lookAt, _cameraHolder.up);
    }

    private void FlyBehavior()
    {
        Vector2 flyVector2 = _playerInput.actions["Fly"].ReadValue<Vector2>();
        Vector3 flyVector = new Vector3(0, flyVector2.y, 0);
        flyVector = orientation.rotation * flyVector;
        transform.position += flyVector * (flySpeed * Time.deltaTime);
    }

    private void ReceiveButtonSignal(string item)
    {
        Machine.Fire(PlayerTrigger.SelectItem, new StringTriggerParam() { str = item });
    }
    
    private void ReceiveConstructPlayerInteractionStarted(ConstructController constructController)
    {
        _currentConstructController = constructController;
        Machine.Fire(PlayerTrigger.ConstructStartedInteraction);
    }
    
    private void ReceiveConstructPlayerInteractionEnded(ConstructController constructController)
    {
        Machine.Fire(PlayerTrigger.ConstructEndedInteraction);
    }

    private void FireInteractOnCurrentConstructController()
    {
        if (Input.GetKeyDown(KeyCode.E) && _currentConstructController != null)
        {
            Debug.Log("fireee!");
            _currentConstructController.Machine.Fire(ConstructTrigger.Interact);
        }
    }

    private class StringTriggerParam : TriggerParams
    {
        public string str = null;
    }
    
    private class GameObjectTriggerParam : TriggerParams
    {
        public GameObject GameObject = null;
    }
}

