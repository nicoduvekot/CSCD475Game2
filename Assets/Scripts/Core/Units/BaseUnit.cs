using System.Collections;
using System.Collections.Generic;
using Core.UIElements;
using Selection;
using UnityEngine;
using HealthSystem;
using EditorTools.Attributes;
using TeamControl;

namespace Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(UnitStats))]
    [RequireComponent(typeof(UnitPathing))]
    public abstract class BaseUnit : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;
        
        public UnitPathing Pathing { get; set; }

        protected Health Health { get; private set; }
        private Healthbar Healthbar { get; set; }
        protected UnitStats Stats { get; private set; }
        protected StateDisplayUI StateDisplayUI { get; private set; }
        private Renderer[] _renderers;
        
        [Header("Tile Pathing")]
        // TEMP SOLUTION - changes to this unit to unit will reflect a change in prefab
        // SerializeField is not ideal solution - expect this to change if I have time
        [SerializeField] private TileScript startingHex;
        protected TileScript CurrentHex { get; set; }
        protected TileScript TargetHex { get; set; }
        protected TileScript NextHex { get; set; }
        protected GameObject PathingGameObject { get; set; }
        private readonly List<TileScript> _previewPath = new(16);
        private readonly Vector3 _pathingGizmoOffset = new(0, 0.5f, 0);
        private int _pathIndex;

        [field: ReadOnly]
        public UnitOwner Owner { get; private set; }

        private bool _ownerInitialized;

        protected UnitState _state = UnitState.Idle;
        protected BaseUnit _targetEnemy;
        
        protected float _attackCooldownTimer;

        private const float FakeDeathAnimTime = 1.5f;
        private Transform _transform;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Healthbar = GetComponentInChildren<Healthbar>();
            Stats = GetComponent<UnitStats>();
            Pathing = GetComponent<UnitPathing>();
            
            _transform = transform;
            
            Health.InitializeHealth(Stats.BaseMaxHealth);
            Health.OnHealthEmpty += HandleDeath;
            
            StateDisplayUI = GetComponentInChildren<StateDisplayUI>();
            if (StateDisplayUI != null) 
                StateDisplayUI.Initialize(_transform);
            
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

            PerspectiveManager.Instance.OnPerspectiveChanged += UpdateVisibility;
        }
        
        protected virtual void Start()
        {
            if (!_ownerInitialized)
                Debug.LogWarning($"CAUTION: {name} was spawned with default ownership of {Owner}");
            
            if (StateDisplayUI != null)
                StateDisplayUI.SetText(_state.ToString());

            if (startingHex == null)
            {
                Debug.LogError($"[BaseUnit] no starting hex assigned for {name}, disabling unit");
                enabled = false;
                return;
            }

            CurrentHex = startingHex;
            // TEMP SOLUTION for getting unit to start at the hex and be set as occupant
            // Expect a more rigid solution in the future

            CurrentHex.TrySetUnitOccupant(this);
            _transform.position = CurrentHex.transform.position;
            
        }
        
        protected virtual void OnDestroy()
        {
            if (Health != null) Health.OnHealthEmpty -= HandleDeath;
            
            PerspectiveManager.Instance.OnPerspectiveChanged -= UpdateVisibility;
        }
        
        // public API
        
        public void InitializeOwner(UnitOwner newOwner)
        {
            Owner = newOwner;
            _ownerInitialized = true;
            
            if (CurrentHex != null)
                CurrentHex.TryUpdateUnitOwnership(this);
            
            UpdateVisibility(PerspectiveManager.Instance.CurrentPerspective);
        }
        
        public void TakeDamage(float amount)
        {
            Health.ApplyDamage(amount);
        }
        
        public virtual void OnCommand(Vector3 worldPos, ISelectable targetSelectable)
        {
            // 1. If clicked a hex
            if (targetSelectable is TileScript hexTile)
            {
                // Check occupancy
                if (!hexTile.TryGetOccupant(out MonoBehaviour occupant))
                {
                    // TargetHex is empty -> generate path and move to it
                    TargetHex = hexTile;

                    GeneratePreviewPath();

                    _pathIndex = 0;
                    
                    // Safety: skip current hex if first node is current
                    if (_previewPath.Count > 0 && _previewPath[0] == CurrentHex)
                        _pathIndex = 1;
                    
                    // only set to moving if there was a path retrieved?
                    if (_previewPath.Count > _pathIndex)
                        SetState(UnitState.Moving);
                    return;
                }
                
                // Hex is occupied -> check if it's a unit
                if (occupant is BaseUnit targetUnit)
                {
                    // Inquire with Nico if unintended action occured
                    Debug.Log("TargetHex was occupied by unit - TryHandleUnitTarget is handling movement");
                    if (TryHandleUnitTarget(targetUnit))
                        return;
                }
                
                // Hex has a building -> cannot move there
                // Inquire with Nico if unintended action occured
                Debug.Log($"Hex {hexTile.name} was occupied (not unit) " +
                          $"(ideally there is a building). Cannot move here logic was triggered.");
            }
            // 2. If clicked a unit directly
            else if (targetSelectable is BaseUnit targetUnit)
            {
                // Inquire with Nico if unintended action occured
                Debug.Log("Target was a unit directly - TryHandleUnitTarget is handling movement");
                if (TryHandleUnitTarget(targetUnit)) return;
            }
            
            // 3. If clicked a building directly
            // if (targetSelectable is Building building)
            // path to nearest tile adj to building
        }
        
        protected virtual void Update()
        {
            switch (_state)
            {
                case UnitState.Idle:
                    break;
                
                case UnitState.Moving:
                    HandleMoving();
                    break;
                
                case UnitState.Engaging:
                    HandleEngaging();
                    break;

                case UnitState.Engaged:
                    HandleEngaged();
                    break;
                
                case UnitState.Dying:
                    break;
                
                default:
                    Debug.LogWarning($"[BaseUnit] Unhandled state: {_state}. Resetting to Idle.");
                    SetState(UnitState.Idle);
                    break;
            }
        }

        protected virtual void HandleMoving()
        {
            // bail out if no path
            if (_previewPath == null || _previewPath.Count == 0 || _pathIndex >= _previewPath.Count)
            {
                TargetHex = null;
                SetState(UnitState.Idle);
                return;
            }
            
            NextHex = _previewPath[_pathIndex];
            // bail out if NextHex somehow is null
            if (NextHex == null)
            {
                Debug.LogError($"[BaseUnit] {name} path reached a null tile at index {_pathIndex}. Resetting to Idle.");
                TargetHex = null;
                SetState(UnitState.Idle);
                return;
            }
            
            Vector3 targetPos = NextHex.transform.position;
            TryMoveTowards(targetPos);

            // if significantly close enough to target -> reached
            if ((_transform.position - targetPos).sqrMagnitude < 0.01f)
            {
                // Clear hex we came from occupant
                if (CurrentHex != null)
                    CurrentHex.TryClearUnitOccupant(this);
                
                // set us as occupant of new hex
                if (NextHex.TrySetUnitOccupant(this))
                    CurrentHex = NextHex;
                else
                    Debug.LogError($"Unit {name} reached hex but could not set self as occupant." +
                                   $"TargetHex was {(TargetHex ? TargetHex.name : "NULL")}");

                // Dequeue the step we just took
                _pathIndex++;

                // reached Target
                if (_pathIndex >= _previewPath.Count)
                {
                    TargetHex = null;
                    SetState(UnitState.Idle);
                }
            }
        }

        protected virtual void HandleEngaging()
        {
            if (_targetEnemy == null)
            {
                SetState(UnitState.Idle);
                return;
            }
            
            TileScript enemyHex = _targetEnemy.CurrentHex;
            
            if (enemyHex == null)
            {
                Debug.LogError(
                    $"[BaseUnit] {name} is trying to Engage logic {_targetEnemy.name}, " +
                    $"but the enemy unit has no CurrentHex assigned. BAIL and Reset self to Idle."
                );

                SetState(UnitState.Idle);
                return;
            }
            
            // get distance to enemy
            float distance = Vector3.Distance(_transform.position, _targetEnemy._transform.position);
            
            // if in range => engage
            if (distance <= Stats.BaseAttackRange)
            {
                SetState(UnitState.Engaged);
                _attackCooldownTimer = 0f;
            }
            
            Vector3 targetPos = enemyHex.transform.position;
            TryMoveTowards(targetPos);
        }

        protected virtual void HandleEngaged()
        {
            if (_targetEnemy == null)
            {
                SetState(UnitState.Idle);
                return;
            }
            
            FaceTarget(_targetEnemy._transform.position);
            
            // get distance to enemy
            float distance = Vector3.Distance(_transform.position, _targetEnemy._transform.position);
            
            // if unit moved out of range logic
            if (distance > Stats.BaseAttackRange)
            {
                SetState(UnitState.Engaging);
                return;
            }
            
            // else attack logic
            _attackCooldownTimer -= Time.deltaTime;

            if (_attackCooldownTimer <= 0f)
            {
                _attackCooldownTimer = 1f / Stats.BaseAttackSpeed;
                Attack(_targetEnemy);
            }
        }
        
        protected virtual void Attack(BaseUnit enemy)
        {
            enemy.TakeDamage(Stats.BaseAttackPower);
        }

        protected virtual void TryMoveTowards(Vector3 targetPos)
        {
            float step = Stats.BaseMoveSpeed * Time.deltaTime;

            _transform.position = Vector3.MoveTowards(_transform.position, targetPos, step);

            FaceTarget(targetPos);
        }
        
        protected void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = targetPos - _transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRot, 720f * Time.deltaTime);
            }
        }

        protected bool TryHandleUnitTarget(BaseUnit other)
        {
            if (other ==null) return false;

            // other is same faction
            if (other.Owner == this.Owner)
            {
                Debug.Log($"{name} targeted a friendly unit ({other.name}). No logic set");
                return true;
            }
            
            // else other is a target
            _targetEnemy = other;
            SetState(UnitState.Engaging);
            return true;
        }

        #region DeathStateLogic

        private void HandleDeath()
        {
            if (_state == UnitState.Dying) return;

            SetState(UnitState.Dying);

            OnDeathAnimationStarted();
        }
        
        protected virtual void OnDeathAnimationStarted()
        {
            StartCoroutine(FakeDeathAnimationRoutine());
        }
        
        private IEnumerator FakeDeathAnimationRoutine()
        {
            Debug.LogWarning("Base Unit is faking death animation");
            yield return new WaitForSeconds(FakeDeathAnimTime);
            OnDeathAnimationCompleted();
        }
        
        protected virtual void OnDeathAnimationCompleted()
        {
            CurrentHex.TryClearUnitOccupant(this);
            Destroy(gameObject);
        }

        #endregion

        #region State Machine

        protected enum UnitState
        {
            Idle,
            Moving,
            Engaging,
            Engaged,
            Dying
        }

        protected void SetState(UnitState newState)
        {
            _state = newState;
            
            if (StateDisplayUI != null)
                StateDisplayUI.SetText(_state.ToString());
            // if we do anims, the transitions can happen here?
        }

        #endregion // state machine

        private void GeneratePreviewPath()
        {
            _previewPath.Clear();

            // bail out if current or target is null
            if (CurrentHex == null || TargetHex == null)
            {
                Debug.LogError("[BaseUnit] Tried to get path, but current or target was null");
                return;
            }
            
            Pathing.setPosition(CurrentHex.x, CurrentHex.y, CurrentHex.z);
            Pathing.setTarget(TargetHex.x, TargetHex.y, TargetHex.z);
            
            List<int[]> pathList = Pathing.findPath();
            if (pathList == null || pathList.Count == 0)
            {
                Debug.LogError("Pathing list was null or empty after algorithm");
                _previewPath.Clear();
                TargetHex = null;
                return;
            }

            for (int i = 0; i < pathList.Count; i++)
            {
                int[] coords = pathList[i];
                PathingGameObject = MapGenerateScript.getHex(coords[0], coords[1], coords[2]);
                

                if (PathingGameObject != null && PathingGameObject.TryGetComponent(out TileScript tile))
                    _previewPath.Add(tile);
            }
        }


        protected virtual void OnDrawGizmos()
        {
            // Draw a cyan line showing the forward direction
            Gizmos.color = Color.cyan;

            Vector3 start = transform.position + Vector3.up * 0.1f;
            Vector3 end = start + transform.forward * 1.5f;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.05f);
            
            // Draw the preview path
            if (_previewPath == null || _previewPath.Count == 0)
                return;
            
            Gizmos.color = Color.yellow;
            
            for (int i = 0; i < _previewPath.Count; i++)
            {
                TileScript hex = _previewPath[i];
                if (hex == null) continue;
        
                Vector3 pos = hex.transform.position + _pathingGizmoOffset;
        
                // Draw node
                Gizmos.DrawSphere(pos + Vector3.up * 0.2f, 0.2f);
        
                // Draw line to next
                if (i < _previewPath.Count - 1)
                {
                    Vector3 nextPos = _previewPath[i + 1].transform.position + _pathingGizmoOffset;
                    Gizmos.DrawLine(pos + Vector3.up * 0.2f, nextPos + Vector3.up * 0.2f);
                }
            }
        }
        
        public void UpdateVisibility(Perspective p)
        {
            bool isOwnerPerspective = p switch
            {
                Perspective.Player => Owner == UnitOwner.Player,
                Perspective.Enemy  => Owner == UnitOwner.Enemy,
                Perspective.World  => Owner == UnitOwner.World,
                Perspective.Admin  => true,
                _ => false
            };
            
            bool tileVisibleToPerspective = p switch
            {
                Perspective.Admin => true,
                Perspective.Player => !CurrentHex.fogForPlayer,
                Perspective.Enemy  => !CurrentHex.fogForEnemy,
                Perspective.World  => !CurrentHex.fogForWorld,
                _ => false
            };
            
            bool unitVisible = isOwnerPerspective || tileVisibleToPerspective;

            foreach (Renderer r in _renderers)
                r.enabled = unitVisible;
            
            if (Healthbar != null)
                Healthbar.SetVisible(unitVisible);

            if (StateDisplayUI != null)
                StateDisplayUI.SetVisible(isOwnerPerspective);
        }
    }
}