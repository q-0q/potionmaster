using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Wasp;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Singleton;
    
    private PlayerInput _playerInput;
    private Transform _orientation;
    private Transform _cameraHolder;
    private Transform _cameraLockTransform;
    private Transform _lookAt;

    public float moveSpeed = 20f;
    public float lookSpeed = 20f;
    
    public float panTiltAmount = 20f;
    public float panTiltRecoveryStrength = 20f;
    private float _panTiltTarget = 0;

    public float bobAmount = 20f;
    public float bobSpeed = 20f;
    private float _bobWeight = 0f;
    private float _bobTargetWeight = 0f;
    public float bobRampSpeed = 5f;

    public GameObject Obstacle;
    

    private void Awake()
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
    }

    private void Start()
    {
        // Application.targetFrameRate = 140; // Set to your desired frame rate
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Vector2 moveVector2 = _playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 moveVector = new Vector3(moveVector2.x, 0, moveVector2.y);
        moveVector = _orientation.rotation * moveVector;
        transform.position += moveVector * (moveSpeed * Time.deltaTime);

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
        
        _bobTargetWeight = moveVector.magnitude * moveSpeed;
        _bobTargetWeight = Mathf.Clamp(_bobTargetWeight, 0, 1);
        _bobWeight = Mathf.Lerp(_bobWeight, _bobTargetWeight, Time.deltaTime * bobRampSpeed);
        _cameraLockTransform.localPosition = new Vector3(0, Mathf.Sin(Time.time * bobSpeed) * bobAmount * _bobWeight, 0);
        _cameraLockTransform.LookAt(_lookAt, _cameraHolder.up);

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Instantiate(Obstacle, transform.position, _orientation.rotation);
        }
    }
}
