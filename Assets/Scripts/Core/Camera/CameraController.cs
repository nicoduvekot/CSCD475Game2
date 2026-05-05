using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform cam;
    [SerializeField] private Transform cameraTarget;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference zoomAction;
    
    [Tooltip("Minimum input magnitude required before movement is registered.")]
    [SerializeField] private float inputDeadZone = 0.1f;

    // Movement Settings
    [Header("Movement Settings")]
    
    [Tooltip("Base movement speed of the camera. \nHigher = faster panning.")]
    [SerializeField] private float moveSpeed = 20f;
    
    [Tooltip("How quickly the camera accelerates to full speed. \nHigher = snappier start.")]
    [SerializeField] private float acceleration = 10f;
    
    [Tooltip("How long (in seconds) it takes to fully stop after releasing input. \nLower = faster stop.")]
    [SerializeField] private float decelerationTime = 0.5f;
    
    [Tooltip("How quickly the cameraTarget moves toward its desired position. \nHigher = tighter, snappier movement.")]
    [SerializeField] private float positionDamping = 15f;
    
    private Vector3 _currentVelocity;
    private bool _hasMoveInput;
    private Vector3 _moveInput3D;
    private Vector3 _decelerationStartVelocity;
    private float _decelerationTimer;
    
    // Zoom Settings
    [Header("Zoom Settings")]
    
    [Tooltip("How quickly the camera zooms in/out. \nHigher = faster zoom.")]
    [SerializeField] private float zoomSpeed = 10f;
    
    [Tooltip("Closest allowed zoom distance. \nMust be lower than Max Zoom")]
    [SerializeField] private float minZoom = 5f;

    [Tooltip("Farthest allowed zoom distance.\nMust be higher than Min Zoom")]
    [SerializeField] private float maxZoom = 10f;
    
    private float _targetDistance;
    
    [Header("State Machine")]
    private EState _state = EState.Normal;
    private Vector3 _trackedPosition;

    private void OnValidate()
    {
        if (cam == null)
        {
            Camera foundCam = GetComponentInChildren<Camera>();
            if (foundCam != null)
                cam = foundCam.transform;
            else
                Debug.LogError("[CameraController] No Camera found in children.");
        }
        
        if (cameraTarget == null)
            cameraTarget = transform;
        
        // see region: ValidationHelpers
        ValidateZoomRange();
        ValidateInputReferences();
    }

    private void Start()
    {
        _targetDistance = -cam.localPosition.z;
    }
    
    private void OnEnable()
    {
        moveAction.action.Enable();
        zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        zoomAction.action.Disable();
    }

    private void Update()
    {
        HandleInput();
        //TODO: HandleZoom();

        switch (_state)
        {
            case EState.Normal:
                UpdateMovement();
                break;
            
            case EState.TrackingPosition:
                //TODO: UpdateTrackingPosition();
                break;
                
            case EState.TrackingObject:
                //TODO: UpdateTrackingObject();
                break;
                
            default:
                Debug.LogWarning($"[CameraController] Unhandled state: {_state}. Resetting to Normal.");
                ChangeState(EState.Normal);
                break;
        }
    }

    private void HandleInput()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        _hasMoveInput = moveInput.sqrMagnitude > inputDeadZone * inputDeadZone;
            
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();
            
        Vector3 right = cam.transform.right;
        right.y = 0f;
        right.Normalize();
            
        _moveInput3D = forward * moveInput.y + right * moveInput.x;
        
        if (_hasMoveInput && _state != EState.Normal)
            ChangeState(EState.Normal);
    }

    private void UpdateMovement()
    {
        if (_hasMoveInput) // "accelerating"
        {
            Vector3 targetVelocity = _moveInput3D * moveSpeed;
            
            float maxDelta = acceleration * Time.unscaledDeltaTime;
            
            _currentVelocity = Vector3.MoveTowards(
                _currentVelocity,
                targetVelocity,
                maxDelta
            );
            
            _decelerationTimer = 0f;
            _decelerationStartVelocity = _currentVelocity;
        }
        else // "decelerating"
        {
            float normalizedTime = decelerationTime > Mathf.Epsilon
                ? _decelerationTimer / decelerationTime
                : 1f;
            
            _currentVelocity = Vector3.Lerp(
                _decelerationStartVelocity,
                Vector3.zero,
                normalizedTime
            );

            _decelerationTimer = Mathf.Min(_decelerationTimer + Time.unscaledDeltaTime, decelerationTime);
        }
        // apply movement
        Vector3 targetPos = cameraTarget.position + _currentVelocity * Time.unscaledDeltaTime;
        
        float damping = 1f - Mathf.Exp(-positionDamping * Time.unscaledDeltaTime);
        
        cameraTarget.position = Vector3.Lerp(
            cameraTarget.position,
            targetPos,
            damping
        );
    }

    #region StateMachine

    private enum EState
    {
        Normal,
        TrackingPosition,
        TrackingObject
    }

    private void ChangeState(EState newState)
    {
        _state = newState;
    }

    #endregion

    #region Validation Helpers

    // validation helpers
    private bool _zoomRangeWarningLogged;
    private bool _moveActionWarningLogged;
    private bool _zoomActionWarningLogged;
    private bool _moveActionTypeWarningLogged;
    private bool _zoomActionTypeWarningLogged;
    
    private void ValidateZoomRange()
    {
        // minimum zoom can not be negative
        if (minZoom < 0f)
            minZoom = 0f;
        
        bool invalidRange = maxZoom < minZoom;
        
        if (invalidRange)
        {
            if (!_zoomRangeWarningLogged)
            {
                Debug.LogError("[CameraController] maxZoom must be greater than or equal to minZoom.");
                _zoomRangeWarningLogged = true;
            }
        }
        else
        {
            // Range is valid again → clear the warning flag
            _zoomRangeWarningLogged = false;
        }
    }
    
    private void ValidateInputReferences()
    {
        // Move Action is missing or invalid
        bool moveInvalid = moveAction == null || moveAction.action == null;

        if (moveInvalid)
        {
            if (!_moveActionWarningLogged)
            {
                Debug.LogError("[CameraController] Move ActionReference is missing or invalid.");
                _moveActionWarningLogged = true;
            }
        }
        else
        {
            _moveActionWarningLogged = false;
        }

        // Zoom Action is missing or invalid
        bool zoomInvalid = zoomAction == null || zoomAction.action == null;

        if (zoomInvalid)
        {
            if (!_zoomActionWarningLogged)
            {
                Debug.LogError("[CameraController] Zoom ActionReference is missing or invalid.");
                _zoomActionWarningLogged = true;
            }
        }
        else
        {
            _zoomActionWarningLogged = false;
        }

        // Move Action has wrong control type (must be Vector2)
        if (!moveInvalid)
        {
            bool wrongType = moveAction.action.expectedControlType != "Vector2";

            if (wrongType)
            {
                if (!_moveActionTypeWarningLogged)
                {
                    Debug.LogError("[CameraController] Move action must be a Vector2 action.");
                    _moveActionTypeWarningLogged = true;
                }
            }
            else
            {
                _moveActionTypeWarningLogged = false;
            }
        }

        // Zoom Action has wrong control type (must be Axis - float)
        if (!zoomInvalid)
        {
            bool wrongType = zoomAction.action.expectedControlType != "Axis";

            if (wrongType)
            {
                if (!_zoomActionTypeWarningLogged)
                {
                    Debug.LogError("[CameraController] Zoom action must be an Axis (float) action.");
                    _zoomActionTypeWarningLogged = true;
                }
            }
            else
            {
                _zoomActionTypeWarningLogged = false;
            }
        }
    }

    #endregion
}
