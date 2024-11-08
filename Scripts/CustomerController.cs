using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using Wasp;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class CustomerController : MonoBehaviour
{
    
    // State
    public List<string> interests;
    public List<string> inventory;
    private Vector3 _destination;
    private float _timeInState;
    private Vector3 stuckLastPosition;
    private float stuckTimeSinceLastUpdate;
    private ConstructController _currentConstructController;
    
    // Parameters
    public float maxIdleTime = 1f;
    public float arrivalDistance = 0.1f;
    public float stuckTimeInterval = 2f;
    public float stuckDistance = 0.1f;
    public float sightDistance = 5f;
    public float acquireTime = 1f;
    
    // Control
    public Machine<State, Trigger> Machine;
    private Dictionary<State, Action> _behaviors;
    
    // References
    private NavMeshAgent _navMeshAgent;
    private NavMeshSurface _navMeshSurface;
    private Canvas _canvas;
    private TextMeshProUGUI _stateTmp;
    private TextMeshProUGUI _interestTmp;
    private TextMeshProUGUI _inventoryTmp;
    private Transform _destinationDebug;
    private LineRenderer _lineRenderer;
    

    private void Awake()
    {
        // References
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshSurface = FindObjectOfType<NavMeshSurface>();
            _canvas = GetComponentInChildren<Canvas>();
            _stateTmp = _canvas.transform.Find("State").GetComponent<TextMeshProUGUI>();
            _interestTmp = _canvas.transform.Find("Interest").GetComponent<TextMeshProUGUI>();
            _inventoryTmp = _canvas.transform.Find("Inventory").GetComponent<TextMeshProUGUI>();
            _destinationDebug = transform.Find("Destination");
            _destinationDebug.SetParent(null, true);
            _lineRenderer = GetComponentInChildren<LineRenderer>();
        }


        // Machine
        {
            Machine = new Machine<State, Trigger>(State.Idle);
            Machine.OnTransitionCompleted(OnTransitionCompleted);

            Machine.Configure(State.Searching)
                .Permit(Trigger.Locate, State.Interested)
                .Permit(Trigger.ArriveAtItem, State.Idle)
                .PermitReentry(Trigger.Stuck)
                .OnEntry(ChooseRandomNearbyDestination)
                .SubstateOf(State.Walking);

            Machine.Configure(State.Idle)
                .Permit(Trigger.Locate, State.Interested)
                .Permit(Trigger.Timeout, State.Searching);

            Machine.Configure(State.Interested)
                .Permit(Trigger.ArriveAtItem, State.AcquiringItem)
                .Permit(Trigger.Timeout, State.Searching)
                .Permit(Trigger.Stuck, State.Searching)
                .OnEntry(UpdateDestination)
                .SubstateOf(State.Walking);

            Machine.Configure(State.AcquiringItem)
                .Permit(Trigger.Acquire, State.Searching)
                .Permit(Trigger.Timeout, State.Searching)
                .OnExitFrom(Trigger.Acquire, OnFinishInteracting);
        }

        // Behaviors
        {
            _behaviors = new Dictionary<State, Action>()
            {
                { State.Walking, UpdateAgentDestinationBehavior },
                { State.AcquiringItem, UpdateAgentDestinationBehavior }
            };
        }
        
        // State 
        {
            interests = new List<string>() { "A", "B", "C" };
            inventory = new List<string>() {};
        }
    }

    private void Start()
    {
        ChooseRandomNearbyDestination(null);
    }


    // Update is called once per frame
    void Update()
    {
        // Update debug
        {
            _stateTmp.text = "STATE: " + Machine.State().ToString();
            
            var interestTxt = "INTERESTS: ";
            foreach (var interest in interests)
            {
                interestTxt += interest + ", ";
            }
            _interestTmp.text = interestTxt;
            
            var inventoryTxt = "INVENTORY: ";
            foreach (var item in inventory)
            {
                inventoryTxt += item + ", ";
            }
            _inventoryTmp.text = inventoryTxt;
            
            _canvas.transform.LookAt(Camera.main.transform);
            _destinationDebug.position = _destination;
            
            Debug.DrawLine(transform.position + Vector3.up, _destination, Color.red);

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
            // Timeout
            {
                if (Machine.IsInState(State.Idle) && _timeInState > maxIdleTime)
                {
                    Machine.Fire(Trigger.Timeout);
                }
            }

            // Arrive
            {
                var distance = Vector3.Distance(transform.position, _destination);
                if (distance < arrivalDistance)
                {
                    Machine.Fire(Trigger.ArriveAtItem);
                }
            }

            // Stuck
            {
                float distanceChange = Vector3.Distance(stuckLastPosition, transform.position);
                stuckTimeSinceLastUpdate += Time.deltaTime;
                if (stuckTimeSinceLastUpdate >= stuckTimeInterval)
                {
                    
                    if (distanceChange < stuckDistance) Machine.Fire(Trigger.Stuck);
                    stuckLastPosition = transform.position;
                    stuckTimeSinceLastUpdate = 0f;
                }
            }
            
            // Locate
            {
                foreach (var target in FindObjectsOfType<ConstructController>())
                {
                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    if (distance < sightDistance && interests.Contains(target.interest) && target.Machine.IsInState(ConstructState.Built))
                    {
                        Machine.Fire(Trigger.Locate, new Vector3TriggerParam() { vector3 = target.transform.position });
                        _currentConstructController = target;
                        break;
                    }
                }
            }
            
            // Acquire
            {
                if (Machine.IsInState(State.AcquiringItem) && _timeInState > acquireTime)
                {
                    Machine.Fire(Trigger.Acquire, new AcquireTriggerParam() { Item = _currentConstructController.interest });
                }
            }
        }
    }
    
    void UpdateAgentDestinationBehavior()
    {
        _navMeshAgent.SetDestination(_destination + Vector3.forward * 0.5f);
    }

    void UpdateDestination(TriggerParams? triggerParams)
    {
        Assert.IsNotNull(triggerParams);
        var vector3TriggerParam = (Vector3TriggerParam)triggerParams;
        _destination = vector3TriggerParam.vector3;
    }
    
    void ChooseRandomNearbyDestination(TriggerParams? triggerParams)
    {
        
        Vector3 size = _navMeshSurface.size;
        float x = Random.Range(-size.x, size.x);
        float z = Random.Range(-size.z, size.z);

        var newDestination = new Vector3(x, transform.position.y, z);
        
        _destination = newDestination;
    }
    
    private void OnTransitionCompleted(TriggerParams? triggerParams)
    {
        _timeInState = 0;
    }
    
    // private void OnInteracting(TriggerParams? triggerParams)
    // {
    //     Assert.IsNotNull(triggerParams);
    //     var customerTargetTriggerParam = (CustomerTargetTriggerParam)triggerParams;
    //     _currentCustomerTarget = customerTargetTriggerParam.CustomerTarget;
    // }

    private void OnFinishInteracting(TriggerParams? triggerParams)
    {
        Assert.IsNotNull(triggerParams);
        var acquireTriggerParam = (AcquireTriggerParam)triggerParams;
        var item = acquireTriggerParam.Item;
        interests.Remove(item);

        if (item == "Checkout")
        {
            interests.Add("Exit");
        }
        else if (item == "Exit")
        {
            Destroy(gameObject);
        }
        else
        {
            inventory.Add(item);
            if (interests.Count == 0)
            {
                interests.Add("Checkout");
            }
        }
    }

    public void IncrementTimeInState(float deltaTime)
    {
        _timeInState += deltaTime;
    }
    
    
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------

    public class Vector3TriggerParam : TriggerParams
    {
        public Vector3 vector3 = Vector3.zero;
    }
    
    public class CustomerTargetTriggerParam : TriggerParams
    {
        public ConstructController ConstructController = null;
    }
    
    public class AcquireTriggerParam : TriggerParams
    {
        public string Item = "";
    }
    
    public enum State
    {
        Walking,
        
        Searching,
        Idle,
        Interested,
        AcquiringItem,
        CheckingOut,
        Talking
    }

    public enum Trigger
    {
        Locate,
        Timeout,
        ArriveAtItem,
        ArriveAtCheckout,
        ArriveAtExit,
        Acquire,
        Stuck,
        Checkout
    }

    private void OnDestroy()
    {
        if (_destinationDebug != null) Destroy(_destinationDebug.gameObject);
    }
}
