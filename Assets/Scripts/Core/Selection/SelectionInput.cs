using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Units;

namespace Selection
{
    public sealed class SelectionInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference selectAction;
        [SerializeField] private InputActionReference commandAction;
        
        [Header("References")]
        private Camera _mainCamera;
        private GraphicRaycaster _uiRaycaster;
        private EventSystem _eventSystem;
        
        [Header("Layer Mask")]
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask tileLayerMask;
        
        private LayerMask _allSelectableLayerMask;
        
        [Header("Selection")]
        
        [Tooltip("This is for Debug purposes only!\nDragging something in here will not work")]
        [SerializeField] [UsedImplicitly] private GameObject selectedObjectDebug;
        
        private ISelectable _selectedObject;
        private ISelectable _lastHoveredSelectable;

        [Header("Command System Fields")] 
        private bool _isCommandPreviewActive;
        private Vector3 _previewWorldPos;
        private ISelectable _previewTarget;
        
        private Action<InputAction.CallbackContext> _pointerMovedHandler;

        private void OnValidate()
        {
            ValidateInputActionReferences();
        }
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                Debug.LogError("SelectionInput: No MainCamera found in scene.");
            
            _eventSystem = EventSystem.current;
            if (_eventSystem == null)
                Debug.LogError("SelectionInput: No EventSystem found in scene.");
            
            _uiRaycaster = FindFirstObjectByType<GraphicRaycaster>();
            if (_uiRaycaster == null)
                Debug.LogWarning("SelectionInput: No GraphicRaycaster found. UI blocking will not work.");
        }

        private bool InputActionsValid =>
            pointerPositionAction != null && pointerPositionAction.action != null &&
            selectAction != null && selectAction.action != null &&
            commandAction != null && commandAction.action != null;
        
        private void OnEnable()
        {
            // safety bail
            if (!InputActionsValid)
            {
                Debug.LogError("SelectionInput: InputActionReferences are not assigned. Aborting setup.");
                enabled = false;
                return;
            }
            
            pointerPositionAction.action.Enable();
            selectAction.action.Enable();
            commandAction.action.Enable();

            _pointerMovedHandler = ctx => OnPointerMoved(ctx.ReadValue<Vector2>());
            
            pointerPositionAction.action.performed += _pointerMovedHandler;
            selectAction.action.performed += OnSelectAction;
            commandAction.action.started  += OnCommandStarted;
            commandAction.action.canceled += OnCommandReleased;
        }

        private void OnDisable()
        {
            if (!InputActionsValid)
                return;
            
            pointerPositionAction.action.Disable();
            selectAction.action.Disable();
            commandAction.action.Disable();

            pointerPositionAction.action.performed -= _pointerMovedHandler;
            selectAction.action.performed -= OnSelectAction;
            commandAction.action.started  -= OnCommandStarted;
            commandAction.action.canceled -= OnCommandReleased;
        }

        private void Start()
        {
            _allSelectableLayerMask = unitLayerMask | tileLayerMask;
        }

        private void OnPointerMoved(Vector2 pos)
        {
            if (TryGetSelectableUnderCursor(pos, out ISelectable selectable))
            {
                // only update the hovered ISelectable when it is a new ISelectable
                if (!ReferenceEquals(_lastHoveredSelectable, selectable))
                {
                    _lastHoveredSelectable?.OnHoverExit();
                    _lastHoveredSelectable = selectable;
                    selectable.OnHoverEnter();
                }
            }
            else
            {
                if (_lastHoveredSelectable != null)
                {
                    _lastHoveredSelectable.OnHoverExit();
                    _lastHoveredSelectable = null;
                }
            }
            
            // command is held + pointer is being moved = ask for preview update logic
            if (_isCommandPreviewActive && _selectedObject != null)
            {
                if (!ReferenceEquals(_previewTarget, _lastHoveredSelectable))
                    _previewTarget = _lastHoveredSelectable;
                
                if (TryGetWorldPoint(pos, out Vector3 newWorldPos))
                {
                    // Only update preview if world position changed
                    if ((newWorldPos - _previewWorldPos).sqrMagnitude > 0.0001f)
                    {
                        _previewWorldPos = newWorldPos;
                        _selectedObject.OnPreviewCommand(_previewWorldPos, _previewTarget);
                    }
                }
            }
        }

        private void OnSelectAction(InputAction.CallbackContext ctx)
        {
            // NRE safety bail
            if (pointerPositionAction == null)
                return;
            
            Vector2 pos = pointerPositionAction.action.ReadValue<Vector2>();
            
            if (IsPointerOverUI(pos))
                return;
            
            if (TryGetSelectableUnderCursor(pos, out ISelectable selectable))
            {
                ClearSelected();
                SetSelected(selectable);
            }
            else
            {
                ClearSelected();
            }
        }
        
        private void OnCommandStarted(InputAction.CallbackContext ctx)
        {
            // if nothing is selected, nothing can be commanded
            // and NRE safety bail
            if (_selectedObject == null || pointerPositionAction == null) 
                return;
            
            Vector2 pos = pointerPositionAction.action.ReadValue<Vector2>();
            
            if (IsPointerOverUI(pos)) 
                return;
            
            // flag for OnPointerMoved to do preview update logic
            _isCommandPreviewActive = true;
            
            // do the first preview - OnPointerMoved handles updating from here
            if (TryGetWorldPoint(pos, out Vector3 startWorldPos))
                _previewWorldPos = startWorldPos;
            
            _previewTarget = _lastHoveredSelectable;
            
            _selectedObject.OnPreviewCommand(_previewWorldPos, _previewTarget);
        }
        
        private void OnCommandReleased(InputAction.CallbackContext ctx)
        {
            // bail if Command System is not actually active
            if (!_isCommandPreviewActive) 
                return;
            
            // flag for OnPointerMoved to stop doing preview updates
            _isCommandPreviewActive = false;
            
            if (_selectedObject != null)
            {
                _selectedObject.OnCommand(_previewWorldPos, _previewTarget);
                _selectedObject.OnPreviewCancel();
            }
            
            _previewTarget = null;
        }
        
        private bool TryGetWorldPoint(Vector2 screenPos, out Vector3 worldPos)
        {
            // set default out
            worldPos = default;
            
            // safety bail
            if (_mainCamera == null)
                return false;
            
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return false;
            
            worldPos = hit.point;
            return true;
        }

        private bool TryGetSelectableUnderCursor(Vector2 pos, out ISelectable selectable)
        {
            // set default out
            selectable = null;
            
            // safety bail
            if (_mainCamera == null)
                return false;
            
            Ray ray = _mainCamera.ScreenPointToRay(pos);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _allSelectableLayerMask)) return false;
            
            selectable = hit.collider.GetComponentInParent<ISelectable>();
            return selectable != null;
        }
        
        private void ClearSelected()
        {
            if(_selectedObject is UnityEngine.Object obj && obj != null){
                _selectedObject.OnDeselected();
            }
            _selectedObject = null;
            selectedObjectDebug = null;
            
            
        }
        
        private void SetSelected(ISelectable selectable)
        {
            _selectedObject = selectable;
            selectedObjectDebug = selectable?.Behaviour != null 
                ? selectable.Behaviour.gameObject 
                : null;
            _selectedObject.OnSelected();
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            // safety bails
            if (_uiRaycaster == null || _eventSystem == null)
                return false;

            PointerEventData eventData = new(_eventSystem)
            {
                position = screenPos
            };

            List<RaycastResult> results = new();
            
            _uiRaycaster.Raycast(eventData, results);
            
            // for debug purposes
            // foreach (RaycastResult r in results)
            //     Debug.Log("UI Hit: " + r.gameObject.name);

            return results.Count > 0;
        }

        private void ValidateInputActionReferences()
        {
            ValidateAction(pointerPositionAction, "Pointer Position");
            ValidateAction(selectAction, "Select");
            ValidateAction(commandAction, "Command");
        }

        private void ValidateAction(InputActionReference actionReference, string actionName)
        {
            if (actionReference == null)
            {
                Debug.LogWarning($"[SelectionInput] {actionName} ActionReference is not assigned.");
                return;
            }

            if (actionReference.action == null)
                Debug.LogWarning($"[SelectionInput] {actionName} ActionReference has no action bound");
        }
    }
}