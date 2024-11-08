using System;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Wasp;


public enum GameState
{

}

public enum GameTrigger
{

}

public class GameController : Controller<GameState, GameTrigger>
{
    
    // Parameters
    public static readonly float gridSize = 3f;
    public static readonly float gridOffset = 0f;
    public static readonly float gridSnapStrength = 50f;
    
    // State
    public float _playerMoney;
    
    // References
    public static GameController Singleton;
    

    private void Awake()
    {

        // References


        // Machine

        Machine = new Machine<GameState, GameTrigger>(0);
        Machine.OnTransitionCompleted(OnTransitionCompleted);
        

        // Behaviors

        _behaviors = new Dictionary<GameState, Action>()
        {

        };
        
        // Listeners

        _listeners = new Dictionary<GameTrigger, Func<bool>>()
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

    
    public static void SnapToGrid(Vector3 originalPosition, Transform transform, Vector3 desiredForwardDirection)
    {
        var offsetVector = new Vector3(gridOffset, 0, gridOffset);
            
        Vector3 adjustedPosition = originalPosition - offsetVector;

        float snappedX = Mathf.Round(adjustedPosition.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(adjustedPosition.z / gridSize) * gridSize;

        Vector3 v = new Vector3(snappedX, originalPosition.y, snappedZ) + offsetVector;
        transform.position = Vector3.Lerp(transform.position, v, Time.deltaTime * gridSnapStrength);
        
        // Ensure the forward direction is snapped to 90-degree increments (cardinal directions)
        Vector3 snappedForward = GetSnappedDirection(desiredForwardDirection);

        // Set the rotation to match the snapped forward direction
        transform.forward = Vector3.Lerp(transform.forward, snappedForward, Time.deltaTime * gridSnapStrength);
    }
    
    // Helper function to snap the forward direction to the nearest 90-degree increment
    private static Vector3 GetSnappedDirection(Vector3 direction)
    {
        // Ensure the direction is normalized
        direction.Normalize();

        // Get the forward direction vector in the XZ plane
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;

        // Snap to the nearest cardinal direction (90 degree increments)
        if (Vector3.Angle(flatDirection, Vector3.forward) <= 45)
            return Vector3.forward;   // Snap to "forward"
        else if (Vector3.Angle(flatDirection, Vector3.back) <= 45)
            return Vector3.back;      // Snap to "back"
        else if (Vector3.Angle(flatDirection, Vector3.right) <= 45)
            return Vector3.right;     // Snap to "right"
        else
            return Vector3.left;      // Snap to "left"
    }
    
}

