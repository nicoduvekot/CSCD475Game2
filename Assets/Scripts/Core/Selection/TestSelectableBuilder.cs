using System;
using DataDefinitions;
using UnityEngine;
using Core.UIElements;

namespace Selection
{
    [Obsolete("BaseUnit should be every unit", true)]
    public class TestSelectableBuilder : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;
        
        [SerializeField] private StateDisplayUI stateDisplayUI;
        
        private UnitState _state = UnitState.Idle;
        
        private Vector3 _targetPos;
        [SerializeField] private float baseMoveSpeed = 4f;
        
        private BuildingData _buildingToConstruct;
        private bool _isDemolishing;
        private float _buildTimer;
        private float _buildDuration;
        
        private bool _isSelected;
        
        private TestSelectableHex CurrentHex { get; set; }
        private TestSelectableHex TargetHex { get; set; }
        
        private void Awake()
        {
            SetState(UnitState.Idle);
        }

        private void Update()
        {
            HandleMovement();
            HandleBuilding();
        }
        
        private void HandleMovement()
        {
            if (_state != UnitState.Moving)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                _targetPos,
                baseMoveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
            {
                if (TargetHex != null)
                {
                    CurrentHex = TargetHex;
                    TargetHex = null;
                    
                    if (_isSelected)
                        BuilderUI.Instance.ShowOptionsFor(CurrentHex, this);
                }

                SetState(UnitState.Idle);
            }
        }
        
        private void HandleBuilding()
        {
            if (_state != UnitState.Building)
                return;

            _buildTimer += Time.deltaTime;

            if (_buildTimer >= _buildDuration)
            {
                if (CurrentHex != null && _buildingToConstruct != null)
                {
                    if (_isDemolishing)
                        CurrentHex.DemolishBuilding();
                    else
                        CurrentHex.AttachBuilding(_buildingToConstruct);
                }
                
                _buildingToConstruct = null;
                _isDemolishing = false;
                
                if (_isSelected && CurrentHex != null)
                    BuilderUI.Instance.ShowOptionsFor(CurrentHex, this);
                
                SetState(UnitState.Idle);
            }
        }
        
        private static string HoverText => "Builder Unit";
        
        public void OnHoverEnter()
        {
            HoverUI.Instance.Show(HoverText);
        }

        public void OnHoverExit()
        {
            HoverUI.Instance.Hide();
        }
        
        public void OnSelected()
        {
            //Debug.Log($"{name} selected");
            
            _isSelected = true;
            stateDisplayUI.gameObject.SetActive(true);
            
            if (CurrentHex != null)
                BuilderUI.Instance.ShowOptionsFor(CurrentHex, this);
            else
                BuilderUI.Instance.Hide();
        }
        
        public void OnDeselected()
        {
            //Debug.Log($"{name} deselected");
            
            _isSelected = false;
            BuilderUI.Instance.Hide();
        }
        
        public void OnCommand(Vector3 worldPos, ISelectable targetSelectable)
        {
            // If the clicked target is a hex tile, assign it
            if (targetSelectable is TestSelectableHex hex)
            {
                TargetHex = hex;
                CurrentHex = null; // leaving current hex
                StartMoving(hex.transform.position);
                return;
            }
            
            TargetHex = null;
            CurrentHex = null;
            StartMoving(worldPos);
        }
        
        private void StartMoving(Vector3 pos)
        {
            _targetPos = pos;
            _buildTimer = 0f;
            _buildDuration = 0f;

            SetState(UnitState.Moving);
        }

        public void StartBuilding(BuildingData data, bool isDemolishing)
        {
            if (CurrentHex == null)
                return;
            
            _isDemolishing = isDemolishing;
            _buildingToConstruct = data;
            _buildDuration = data.buildTime;
            _buildTimer = 0f;

            SetState(UnitState.Building);
        }

        #region State Machine

        private enum UnitState
        {
            Idle,
            Moving,
            Building
        }
        
        private void SetState(UnitState newState)
        {
            _state = newState;

            switch (_state)
            {
                case UnitState.Idle:
                    stateDisplayUI.SetText("idle");
                    break;

                case UnitState.Moving:
                    stateDisplayUI.SetText("moving");
                    break;

                case UnitState.Building:
                    stateDisplayUI.SetText("building");
                    break;
            }
        }

        #endregion // state machine
        
        
    }
}