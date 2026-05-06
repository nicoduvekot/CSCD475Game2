using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Selection
{
    public sealed class SelectionInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference selectAdditiveAction;
        [SerializeField] private InputActionReference selectStartAction;
        [SerializeField] private InputActionReference selectEndAction;
        [SerializeField] private InputActionReference commandAction;
        
        [Header("References")]
        [SerializeField] private Camera cam;
        
        [Header("Selection")]
        [SerializeField] private List<GameObject> selectedObjects = new();
        
        [Header("Dragging Settings")]
        [SerializeField] private float holdThreshold = 0.15f;
        [SerializeField] private float dragStartDistance = 5f;
        
        private ISelectable _lastHoveredSelectable;
        
        private Vector2 _startPos;
        private Vector2 _currentPos;
        
        private bool _isHolding;
        private bool _isDragging;
        private float _holdTimer;
        
        private bool _isAdditive;
        private bool _isAdditiveAtStart;
        
        private System.Action<InputAction.CallbackContext> _pointerMovedHandler;
        
        private void OnEnable()
        {
            pointerPositionAction.action.Enable();
            selectAdditiveAction.action.Enable();
            selectStartAction.action.Enable();
            selectEndAction.action.Enable();
            commandAction.action.Enable();

            _pointerMovedHandler = ctx => OnPointerMoved(ctx.ReadValue<Vector2>());
            
            pointerPositionAction.action.performed += _pointerMovedHandler;
            selectAdditiveAction.action.performed += OnAdditiveChanged;
            selectAdditiveAction.action.canceled  += OnAdditiveChanged;
            selectStartAction.action.started += OnSelectStarted;
            selectEndAction.action.canceled += OnSelectEnded;
            commandAction.action.started += OnCommand;
        }

        private void OnDisable()
        {
            pointerPositionAction.action.Disable();
            selectAdditiveAction.action.Disable();
            selectStartAction.action.Disable();
            selectEndAction.action.Disable();
            commandAction.action.Disable();

            pointerPositionAction.action.performed -= _pointerMovedHandler;
            selectAdditiveAction.action.performed -= OnAdditiveChanged;
            selectAdditiveAction.action.canceled  -= OnAdditiveChanged;
            selectStartAction.action.started -= OnSelectStarted;
            selectEndAction.action.canceled -= OnSelectEnded;
            commandAction.action.started -= OnCommand;
        }

        private void Update()
        {
            if (!_isHolding) return;
            
            if (!_isDragging)
            {
                _holdTimer += Time.deltaTime;
                
                bool movedEnough = Vector2.Distance(_startPos, _currentPos) >= dragStartDistance;

                if (_holdTimer >= holdThreshold && movedEnough)
                    _isDragging = true;
            }
        }

        private void OnPointerMoved(Vector2 pos)
        {
            _currentPos = pos;
            
            if (TryGetSelectableUnderCursor(pos, out ISelectable selectable))
            {
                if (_lastHoveredSelectable == selectable)
                    return;

                _lastHoveredSelectable?.OnHoverExit();
                _lastHoveredSelectable = selectable;
                selectable.OnHoverEnter();
            }
            else
            {
                if (_lastHoveredSelectable != null)
                {
                    _lastHoveredSelectable.OnHoverExit();
                    _lastHoveredSelectable = null;
                }
            }
        }

        private void OnAdditiveChanged(InputAction.CallbackContext ctx)
        {
            _isAdditive = ctx.performed;
        }

        private void OnSelectStarted(InputAction.CallbackContext ctx)
        {
            Vector2 pos = ctx.ReadValue<Vector2>();
            
            _startPos = pos;
            
            _isHolding = true;
            _isDragging = false;
            _holdTimer = 0f;
            
            _isAdditiveAtStart = _isAdditive;
        }

        private void OnSelectEnded(InputAction.CallbackContext ctx)
        {
            Vector2 pos = ctx.ReadValue<Vector2>();

            if (_isDragging)
            {
                Debug.Log("[Nico] Drag Detected: Dragging Scenario not implemented");
            }
            else // single click selection
            {
                if (TryGetSelectableUnderCursor(pos, out ISelectable selectable))
                {
                    if (!_isAdditiveAtStart)
                    {
                        ClearSelection();
                    }

                    AddToSelection(selectable);
                }
                else // clicked, no selectable
                {
                    ClearSelection();
                }
            }
            _isHolding = false;
            _isDragging = false;
        }

        private void OnCommand(InputAction.CallbackContext ctx)
        {
            Vector2 pos = ctx.ReadValue<Vector2>();
            
            if (!TryGetWorldPoint(pos, out Vector3 worldPos))
                return;

            // iterate backwards, as we possibly remove from list during iteration
            for (int i = selectedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = selectedObjects[i];

                // NRE - destroyed object or otherwise
                if (obj == null)
                {
                    selectedObjects.RemoveAt(i);
                    continue;
                }

                // Somehow a non-ISelectable is in list
                if (!obj.TryGetComponent(out ISelectable selectable))
                {
                    selectedObjects.RemoveAt(i);
                    continue;
                }

                // valid entry is given command
                selectable.OnCommand(worldPos);
            }
        }
        
        private bool TryGetWorldPoint(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                worldPos = hit.point;
                return true;
            }
            worldPos = default;
            return false;
        }

        private bool TryGetSelectableUnderCursor(Vector2 pos, out ISelectable selectable)
        {
            Ray ray = cam.ScreenPointToRay(pos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                selectable = hit.collider.GetComponentInParent<ISelectable>();
                return selectable != null;
            }

            selectable = null;
            return false;
        }
        
        private void ClearSelection()
        {
            for (int i = selectedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = selectedObjects[i];

                // NRE in list
                if (obj == null)
                {
                    selectedObjects.RemoveAt(i);
                    continue;
                }

                // Non-ISelectable object
                if (!obj.TryGetComponent(out ISelectable selectable))
                {
                    selectedObjects.RemoveAt(i);
                    continue;
                }

                // valid entry is given deselect and removed
                selectable.OnDeselected();
                selectedObjects.RemoveAt(i);
            }
        }
        
        private void AddToSelection(ISelectable selectable)
        {
            GameObject go = selectable.Behaviour.gameObject;

            if (selectedObjects.Contains(go)) return;
            
            selectedObjects.Add(go);
            selectable.OnSelected();
        }
    }
}