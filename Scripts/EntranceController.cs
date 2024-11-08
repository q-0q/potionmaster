using System;
using System.Collections.Generic;
using UnityEngine;
using Wasp;

namespace DefaultNamespace
{
    public enum EntranceState
    {
        Ready,
        Spawning,
        Completed,
    }
    
    public enum EntranceTrigger
    {
        Timeout,
        DayEnd,
    }

    public class EntranceController : Controller<EntranceState, EntranceTrigger>
    {
        // Parameters
        public float readyTime = 2f;
        public float spawnRate = 2f;
        public GameObject customerPrefab;
        
        // State
        private float _timeSinceLastSpawn = 0f;
        
        private void Awake()
        {
            
            // Machine
            
            Machine = new Machine<EntranceState, EntranceTrigger>(EntranceState.Ready);
            Machine.OnTransitionCompleted(OnTransitionCompleted);
            
            Machine.Configure(EntranceState.Ready)
                .Permit(EntranceTrigger.Timeout, EntranceState.Spawning);

            Machine.Configure(EntranceState.Spawning)
                .OnEntry(_ => _timeSinceLastSpawn = spawnRate)
                .Permit(EntranceTrigger.DayEnd, EntranceState.Completed);
            
            // Behaviors

            _behaviors = new Dictionary<EntranceState, Action>()
            {
                { EntranceState.Ready, ReadyBehavior },
                { EntranceState.Spawning, SpawningBehavior }
            };

            _listeners = new Dictionary<EntranceTrigger, Func<bool>>()
            {
                { EntranceTrigger.Timeout , TimeoutListener },
                { EntranceTrigger.DayEnd , DayEndListener }
            };
            
            // Debug
            
            InitDebug();
        }

        private void Update()
        {
            OnUpdate();
        }

        private void ReadyBehavior()
        {
            
        }

        private void SpawningBehavior()
        {
            _timeSinceLastSpawn += Time.deltaTime;
            
            if (_timeSinceLastSpawn > spawnRate)
            {
                Instantiate(customerPrefab, transform.position, Quaternion.identity, null);
                _timeSinceLastSpawn = 0;
            }
            
        }

        private bool TimeoutListener()
        {
            if (Machine.IsInState(EntranceState.Ready))
            {
                return TimeInState > readyTime;
            }

            return false;
        }
        
        private bool DayEndListener()
        {
            return false;
        }
    }

}