using System;
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
        
        private UnitAnimator _unitAnimator;
        private SpriteRenderer _spriteRenderer;
        
        private UnitPathing Pathing { get; set; }

        private Health Health { get; set; }
        private Healthbar Healthbar { get; set; }
        private UnitStats Stats { get; set; }
        private StateDisplayUI StateDisplayUI { get; set; }
        private Renderer[] _renderers;
        
        [Header("Tile Pathing")]
        // TEMP SOLUTION - changes to this unit to unit will reflect a change in prefab
        // SerializeField is not ideal solution - expect this to change if I have time
        [SerializeField] private TileScript startingHex;
        [HideInInspector] public TileScript CurrentHex;


        private TileScript TargetHex { get; set; }
        private TileScript NextHex { get; set; }
        private GameObject PathingGameObject { get; set; }
        private readonly List<TileScript> _previewPath = new(16);
        private readonly Vector3 _pathingGizmoOffset = new(0, 0.5f, 0);
        private int _pathIndex;
        
        private int _pathRetryCount = 0;
        private const int MaxPathRetries = 2;

        [field: ReadOnly]
        public UnitOwner Owner;

        [HideInInspector] public bool _ownerInitialized;

        private UnitState _state = UnitState.Idle;
        private BaseUnit _targetEnemy;
        
        //private float _attackCooldownTimer;
        
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
            
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            
            _unitAnimator = GetComponentInChildren<UnitAnimator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            print("_ownerInitialized in awake is " + _ownerInitialized);
            print("current hex in awake is " + CurrentHex);
            print("starting hex in awake is " + startingHex);
            

        }
        
        public virtual void Start()
        {
            
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
            
            PerspectiveManager.Instance.OnPerspectiveChanged += UpdateVisibility;

            print("_ownerInitialized in start is " + _ownerInitialized);
            print("current hex in start is " + CurrentHex);
            print("starting hex in start is " + startingHex);

            UpdateVisibility(PerspectiveManager.Instance.CurrentPerspective);

            

            if (!_ownerInitialized)
                Debug.LogWarning($"CAUTION: {name} was spawned with default ownership of {Owner}");
        }
        
        protected virtual void OnDestroy()
        {
            if (Health != null) Health.OnHealthEmpty -= HandleDeath;
            
            PerspectiveManager.Instance.OnPerspectiveChanged -= UpdateVisibility;
        }
        
        // public API
        // called before unit is instantiated
        public void initializeUnit(TileScript startingTile, UnitOwner newOwner){

            startingHex = startingTile;
            CurrentHex = startingTile;

            Owner = newOwner;
            _ownerInitialized = true;
            print("_ownerInitialized in init is " + _ownerInitialized);
        }

        public void debugInitializeOwner(UnitOwner newOwner){
            Owner = newOwner;
            _ownerInitialized = true;
            if (CurrentHex != null)
                CurrentHex.TryUpdateUnitOwnership(this);
            
            UpdateVisibility(PerspectiveManager.Instance.CurrentPerspective);
        }
        
        private void TakeDamage(float amount, BaseUnit attacker)
        {
            Health.ApplyDamage(amount);
            
            Debug.Log($"{attacker.name} damaged {this.name} with {amount} damage");
            
            TryRetaliate(attacker);
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

                    if (!TryGeneratePath(0))
                    {
                        TargetHex = null;
                        SetState(UnitState.Idle);
                        return;
                    }

                    _pathIndex = 0;
                    
                    _isStepping = false;
                    _currentStepTimer = 0f;
                    NextHex = null;
                    
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
                _unitAnimator.SetWalking(false);
                TargetHex = null;
                SetState(UnitState.Idle);
                return;
            }

            // if we are not currently stepping, begin a new step
            if (!_isStepping)
            {
                NextHex = _previewPath[_pathIndex];
                
                // bail if NextHex was found to be null
                if (NextHex == null)
                {
                    Debug.LogError($"{name} encountered null tile at index {_pathIndex}");
                    _unitAnimator.SetWalking(false);
                    TargetHex = null;
                    SetState(UnitState.Idle);
                    return;
                }
                
                BeginStep(NextHex);
                _isStepping = true;
                return;
            }

            // if we are currently stepping - increment timer
            _currentStepTimer -= Time.deltaTime;
            
            // timer not completed yet
            if (_currentStepTimer > 0f)
                return;
            
            // step completed
            
            // attempt to claim the step hex
            if (!NextHex.TrySetUnitOccupant(this))
            {
                if (_pathRetryCount < MaxPathRetries)
                {
                    // increment path retry count
                    _pathRetryCount++;

                    // Try to rebuild the path to the same target
                    if (TryGeneratePath(0))
                    {
                        _pathIndex = 0;
                        _isStepping = false;
                        return;
                    }
                }
                
                Debug.LogWarning("[BaseUnit]-[HandleMoving] retry pathing failure tries expired.");
                _pathRetryCount = 0;
                _unitAnimator.SetWalking(false);
                SetState(UnitState.Idle);
                return;
            }

            // clear from current
            if (!CurrentHex.TryClearUnitOccupant(this))
            {
                Debug.LogError("Unit failed to clear the tile it came from");
            }
            
            // update current
            CurrentHex = NextHex;
            
            // immediate snap to nextHex location ??
            _transform.position = NextHex.transform.position;
            
            _pathRetryCount = 0;
            
            // Optimization remarks - this will generate a new path every step
            if (!TryGeneratePath(0))
            {
                _unitAnimator.SetWalking(false);
                TargetHex = null;
                SetState(UnitState.Idle);
                return;
            }

            // reset stepping flag = next frame start next step calculations
            _pathIndex = 0;
            _isStepping = false;
        }

        protected virtual void HandleEngaging()
        {
            // target null mid-tracking - bail and idle
            if (_targetEnemy == null)
            {
                _unitAnimator.SetWalking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            // set target as enemy location
            TargetHex = _targetEnemy.CurrentHex;
            
            // recalculate path to target
            if (!TryGeneratePath(Stats.BaseAttackRange))
            {
                // No path → stop engaging
                _unitAnimator.SetWalking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            // path is empty means we are already in range
            if (_previewPath.Count == 0)
            {
                EnterEngagedState();
                return;
            }

            _pathIndex = 0;
            
            if (!_isStepping)
            {
                NextHex = _previewPath[_pathIndex];

                if (NextHex == null)
                {
                    Debug.LogError($"{name} encountered a null tile at index {_pathIndex}. Aborting movement");
                    _unitAnimator.SetWalking(false);
                    SetState(UnitState.Idle);
                    return;
                }
                
                BeginStep(NextHex);
                _isStepping = true;
                return;
            }
            
            _currentStepTimer -= Time.deltaTime;
            
            if (_currentStepTimer > 0f)
                return;
            
            // attempt to claim the step hex
            if (!NextHex.TrySetUnitOccupant(this))
            {
                Debug.LogWarning("[BaseUnit]-[HandleEngaging] Unit could not set a next hex, currently aborting logic");
                
                if (_pathRetryCount < MaxPathRetries)
                {
                    _pathRetryCount++;

                    if (TryGeneratePath(Stats.BaseAttackRange))
                    {
                        _pathIndex = 0;
                        _isStepping = false;
                        return;
                    }
                }
                
                Debug.LogWarning("[BaseUnit]-[HandleEngaging] retry attempts exhausted.");
                _pathRetryCount = 0;
                _unitAnimator.SetWalking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            // clear from current
            if (!CurrentHex.TryClearUnitOccupant(this))
            {
                Debug.LogError("Unit failed to clear the tile it came from");
            }
            
            // update current
            CurrentHex = NextHex;
            
            _transform.position = NextHex.transform.position;
            
            _pathRetryCount = 0;
            
            if (!TryGeneratePath(Stats.BaseAttackRange))
            {
                _unitAnimator.SetWalking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            if (_previewPath.Count == 0)
            {
                EnterEngagedState();
            }
            
            _pathIndex = 0;
            _isStepping = false;
        }

        private void EnterEngagedState()
        {
            // stop walking if we were
            _unitAnimator.SetWalking(false);
            
            // if target no longer exists. bail
            if (_targetEnemy == null)
            {
                SetState(UnitState.Idle);
                return;
            }
            
            SetState(UnitState.Engaged);
            
            _unitAnimator.SetAttackSpeed(Stats.BaseAttackSpeed);
            
            _unitAnimator.SetAttacking(true);
        }

        protected virtual void HandleEngaged()
        {
            // the unit has become null, bail
            if (_targetEnemy == null)
            {
                _unitAnimator.SetAttacking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            // update target to where enemy is
            TargetHex = _targetEnemy.CurrentHex;
            
            // if no path, bail and idle
            if (!TryGeneratePath(Stats.BaseAttackRange))
            {
                _unitAnimator.SetAttacking(false);
                SetState(UnitState.Idle);
                return;
            }
            
            // if we are now out of range, go back to engaging logic
            if (_previewPath.Count > 0)
            {
                // stop the attack anim
                _unitAnimator.SetAttacking(false);

                // reset stepping
                _isStepping = false;
                _currentStepTimer = 0f;
                NextHex = null;
                _pathRetryCount = 0;
                _pathIndex = 0;

                SetState(UnitState.Engaging);
                return;
            }
        }
        
        protected virtual void Attack(BaseUnit enemy)
        {
            enemy.TakeDamage(Stats.BaseAttackPower, this);
        }

        protected virtual void TryMoveTowards(Vector3 targetPos)
        {
            Vector3 direction = targetPos - transform.position;
            
            HandleSpriteFlip(direction);
            
            float step = Stats.BaseMoveSpeed * Time.deltaTime;
            
            if (direction.sqrMagnitude > Mathf.Epsilon)
                _unitAnimator.SetWalking(true);

            _transform.position = Vector3.MoveTowards(_transform.position, targetPos, step);
        }

        private bool TryHandleUnitTarget(BaseUnit other)
        {
            if (other ==null) return false;

            // other is same faction
            // Note, use of this. is redundant, but used to be explicit 
            if (other.Owner == this.Owner)
            {
                Debug.Log($"{name} targeted a friendly unit ({other.name}). No logic set");
                return true;
            }
            
            // else other is a target
            _targetEnemy = other;
            TargetHex = other.CurrentHex;

            if (!TryGeneratePath(Stats.BaseAttackRange))
            {
                SetState(UnitState.Idle);
                return true;
            }

            _pathIndex = 0;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            _pathRetryCount = 0;
            
            SetState(UnitState.Engaging);
            return true;
        }

        public virtual void OnAttackHit()
        {
            // bail if target is null
            if (_targetEnemy == null)
                return;
            
            // do damage
            Attack(_targetEnemy);
            
            // target is null or dead after hit
            if (_targetEnemy == null || !_targetEnemy.Health.IsAlive)
            {
                _unitAnimator.SetAttacking(false);

                // reset step safety
                _isStepping = false;
                _currentStepTimer = 0f;
                NextHex = null;
                _pathRetryCount = 0;
                _pathIndex = 0;

                SetState(UnitState.Idle);
            }
        }

        #region DeathStateLogic

        private void HandleDeath()
        {
            if (_state == UnitState.Dying) return;

            SetState(UnitState.Dying);

            _unitAnimator.TriggerDeath();
        }
        
        public void OnDeathAnimationCompleted()
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
        }

        #endregion // state machine

        private bool TryGeneratePath(int rangeIndex)
        {
            _previewPath.Clear();

            // bail out if current or target is null
            if (CurrentHex == null || TargetHex == null)
            {
                Debug.LogError("[BaseUnit] Tried to get path, but current or target was null");
                return false;
            }
            
            Pathing.setPosition(CurrentHex.x, CurrentHex.y, CurrentHex.z);
            Pathing.setTarget(TargetHex.x, TargetHex.y, TargetHex.z);
            
            List<int[]> pathList = Pathing.findPath();
            if (pathList == null || pathList.Count == 0)
            {
                Debug.LogError("Pathing list was null or empty after algorithm");
                _previewPath.Clear();
                TargetHex = null;
                return false;
            }

            for (int i = 0; i < pathList.Count; i++)
            {
                int[] coords = pathList[i];
                PathingGameObject = MapGenerateScript.getHex(coords[0], coords[1], coords[2]);
                

                if (PathingGameObject != null && PathingGameObject.TryGetComponent(out TileScript tile))
                {
                    // if the tile is the one we are on, skip it
                    if (tile.x == CurrentHex.x &&
                        tile.y == CurrentHex.y &&
                        tile.z == CurrentHex.z)
                    {
                        continue;
                    }

                    _previewPath.Add(tile);
                }
            }

            if (_previewPath.Count == 0)
                return false;
            
            // get full path length to enemy
            int fullDistance = _previewPath.Count;
            
            // already in range
            if (fullDistance <= rangeIndex)
            {
                _previewPath.Clear();
                return true;
            }
            
            int stopIndex = fullDistance - 1 - rangeIndex;
            
            if (stopIndex < _previewPath.Count - 1)
                _previewPath.RemoveRange(stopIndex + 1, _previewPath.Count - (stopIndex + 1));
            
            return true;
        }

        private void TryRetaliate(BaseUnit attacker)
        {
            // already engaged, or a target is set, bail retaliation
            if (_state == UnitState.Engaging || _state == UnitState.Engaged)
                return;
            
            // attacker is null, bail
            if (attacker == null)
                return;
            
            // set enemy and update target hex
            _targetEnemy = attacker;
            TargetHex = attacker.CurrentHex;
            
            // no path to target, bail
            if (!TryGeneratePath(Stats.BaseAttackRange))
            {
                return;
            }
            
            // already in range - retaliate
            if (_previewPath.Count == 0)
            {
                EnterEngagedState();
                return;
            }
            
            // reset step and path to target
            _pathIndex = 0;
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            _pathRetryCount = 0;

            SetState(UnitState.Engaging);
        }

        protected virtual void HandleSpriteFlip(Vector3 direction)
        {
            if (Math.Abs(direction.x) < Mathf.Epsilon)
                return;
            
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = direction.x < 0f;
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

        private bool _isStepping;
        private float _currentStepTimer;
        private float _currentStepDuration;
        
        private void BeginStep(TileScript nextHex)
        {
            // if nextHex somehow null at this point, safety bail
            if (nextHex == null)
            {
                Debug.LogError($"[BaseUnit] {name} tried to begin step with null NextHex.");
                SetState(UnitState.Idle);
                return;
            }
            
            // sprite flip logic
            Vector3 direction = nextHex.transform.position - _transform.position;
            HandleSpriteFlip(direction);
            
            int tileCost = nextHex.getMovement();
            float moveSpeed = Stats.BaseMoveSpeed;
            
            _currentStepDuration = tileCost / moveSpeed;
            _currentStepTimer = _currentStepDuration;
            
            _unitAnimator.SetWalking(true);
        }
    }
}