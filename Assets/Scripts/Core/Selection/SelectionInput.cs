using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Selection
{
    public sealed class SelectionInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private InputActionReference selectAdditiveAction;
        [SerializeField] private InputActionReference selectAction;
        [SerializeField] private InputActionReference commandAction;
        
        [Header("References")]
        [SerializeField] private Camera cam;
        [SerializeField] private GraphicRaycaster uiRaycaster;
        [SerializeField] private EventSystem eventSystem;
        
        [Header("Layer Masks")]
        [SerializeField] private LayerMask unitMask;
        [SerializeField] private LayerMask tileMask;
        
        private LayerMask _selectableMask;
        
        [Header("Selection")]
        [SerializeField] private List<GameObject> selectedObjects = new();
        
        private ISelectable _lastHoveredSelectable;
        
        // TODO: Possible bring this back for command issuing system
        /*
        //[Header("Dragging Settings")]
        //[SerializeField] private float holdThreshold = 0.15f;
        //[SerializeField] private float dragStartDistance = 5f;
        
        //private Vector2 _startPos;
        //private Vector2 _currentPos;
        
        //private bool _isHolding;
        //private bool _isDragging;
        //private float _holdTimer;
        */
        
        private bool _isAdditive;
        private bool _isAdditiveAtStart;
        
        private System.Action<InputAction.CallbackContext> _pointerMovedHandler;
        
        private void OnEnable()
        {
            pointerPositionAction.action.Enable();
            selectAdditiveAction.action.Enable();
            selectAction.action.Enable();
            commandAction.action.Enable();

            _pointerMovedHandler = ctx => OnPointerMoved(ctx.ReadValue<Vector2>());
            
            pointerPositionAction.action.performed += _pointerMovedHandler;
            selectAdditiveAction.action.performed += OnAdditiveChanged;
            selectAdditiveAction.action.canceled  += OnAdditiveChanged;
            selectAction.action.performed += OnSelectAction;
            commandAction.action.started += OnCommand;
        }

        private void OnDisable()
        {
            pointerPositionAction.action.Disable();
            selectAdditiveAction.action.Disable();
            selectAction.action.Disable();
            commandAction.action.Disable();

            pointerPositionAction.action.performed -= _pointerMovedHandler;
            selectAdditiveAction.action.performed -= OnAdditiveChanged;
            selectAdditiveAction.action.canceled  -= OnAdditiveChanged;
            selectAction.action.performed += OnSelectAction;
            commandAction.action.started -= OnCommand;
        }

        private void Start()
        {
            _selectableMask = unitMask | tileMask;
        }

        private void OnPointerMoved(Vector2 pos)
        {
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

        private void OnSelectAction(InputAction.CallbackContext ctx)
        {
            Vector2 pos = pointerPositionAction.action.ReadValue<Vector2>();
            
            if (IsPointerOverUI(pos))
                return;
            
            if (TryGetSelectableUnderCursor(pos, out ISelectable selectable))
            {
                if (!_isAdditive)
                    ClearSelection();

                AddToSelection(selectable);
            }
            else
            {
                ClearSelection();
            }
        }

        private void OnCommand(InputAction.CallbackContext ctx)
        {
            Vector2 pos = pointerPositionAction.action.ReadValue<Vector2>();

            if (!TryGetWorldPoint(pos, out Vector3 worldPos))
                return;

            if (!TryGetSelectableUnderCursor(pos, out ISelectable targetSelectable))
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
                selectable.OnCommand(worldPos, targetSelectable);
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

            if (Physics.Raycast(ray, out RaycastHit layerHit, 1000f, _selectableMask))
            {
                selectable = layerHit.collider.GetComponentInParent<ISelectable>();
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
        
        private bool IsPointerOverUI(Vector2 screenPos)
        {
            PointerEventData eventData = new(eventSystem)
            {
                position = screenPos
            };

            List<RaycastResult> results = new();
            uiRaycaster.Raycast(eventData, results);

            return results.Count > 0;
        }
    }
}