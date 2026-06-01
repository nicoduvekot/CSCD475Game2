using System;
using System.Collections.Generic;
using Core.UIElements;
using HealthSystem;
using Selection;
using TeamControl;
using UnityEngine;

namespace Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(UnitStats))]
    [RequireComponent(typeof(UnitPathing))]
    public abstract class BaseUnit : MonoBehaviour, ISelectable
    {
        // used by ISelection to retrieve the Mono behavior of this
        public MonoBehaviour Behaviour => this;
        
        // this is the person who can control this unit
        public UnitOwner Owner { get; private set; }
        
        private bool _ownerInitialized;
        
        private Renderer[] _renderers;
        private SpriteRenderer _spriteRenderer;
        private UnitAnimator _unitAnimator;
        private UnitPathing _pathing;
        private UnitPathResolver _pathResolver;

        private Health Health { get; set; }
        private Healthbar Healthbar { get; set; }
        private UnitStats Stats { get; set; }
        private StateDisplayUI StateDisplayUI { get; set; }
        
        // Int Representing the type of unit this is
        // 0 = Soldier
        // 1 = Archer
        // 2 = Horseman
        // NOTE: Set by the derived class
        public int UnitType { get; protected set; }
        
        private Transform _transform;
        private PerspectiveManager _perspectiveManager;

        public UnitAgentGoal CurrentGoal { get; private set; } 
            = UnitAgentGoal.None;
        
        public UnitMotorState MotorState { get; private set; } 
            = UnitMotorState.Standing;
        
        private bool _isStepping;
        private float _currentStepTimer;
        private int _pathIndex;
        private float _currentStepDuration;

        [Header("Tile Pathing")]
        
        // the tile this unit is currently on
        public TileScript CurrentHex { get; private set; }
        public TileScript NextHex { get; private set; }
        
        // the tile this unit is moving to
        public TileScript TargetHex { get; private set; }
        
        private readonly List<TileScript> _previewPath = new(16);

        private BaseUnit _targetUnit;

        public void Initialize(TileScript startingTile, UnitOwner unitOwner)
        {
            CurrentHex = startingTile;
            Owner = unitOwner;

            _ownerInitialized = true;

            startingTile.TrySetUnitOccupant(this);
            
            transform.position = startingTile.transform.position;
        }

        #region Public API Helpers

        public event Action<BaseUnit> OnUnitDeath;
        
        public bool IsAlive => Health != null && Health.IsAlive;
        public BaseUnit CurrentEnemyTarget() => _targetUnit;

        #endregion


        #region Unity Functions

        protected virtual void Awake()
        {
            _transform = transform;
            
            Health = GetComponent<Health>();
            Healthbar = GetComponentInChildren<Healthbar>();
            
            Stats = GetComponent<UnitStats>();
            
            _pathing = GetComponent<UnitPathing>();
            _pathResolver = new UnitPathResolver(_pathing);
            
            StateDisplayUI = GetComponentInChildren<StateDisplayUI>();
            
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            _unitAnimator = GetComponentInChildren<UnitAnimator>();
            
            Health.InitializeHealth(Stats.BaseMaxHealth);
            Health.OnHealthEmpty += TransitionToDying;
        }

        private void Start()
        {
            _perspectiveManager = PerspectiveManager.Instance;
            PerspectiveManager.Instance.OnPerspectiveChanged += UpdateVisibility;
            UpdateVisibility(_perspectiveManager.CurrentPerspective);
            
        }

        #endregion

        #region Update And Motor Core

        // Update uses MotorState to define how it updates
        private void Update()
        {
            switch (MotorState)
            {
                case UnitMotorState.Standing:
                    HandleStanding();
                    break;

                case UnitMotorState.Moving:
                    HandleMoving();
                    break;
                
                case UnitMotorState.Pursuing:
                    HandlePursuing();
                    break;

                case UnitMotorState.Fighting:
                    HandleFighting();
                    break;
                
                case UnitMotorState.Fleeing:
                    HandleFleeing();
                    break;

                case UnitMotorState.Capturing:
                    HandleCapturing(); // facades to Standing right now
                    break;

                case UnitMotorState.Dying:
                    HandleDying();
                    break;
                
                default:
                    Debug.LogError($"[UNIT] Unknown MotorState: {MotorState}, defaulting to standing");
                    TransitionToStanding();
                    return;
            }
            
            // goal counters tick
            if (CurrentGoal != UnitAgentGoal.None) 
                TickGoalCounters();
        }

        private void HandleStanding()
        {
            if (_isGuardStandingPhase)
                TickGuardCore();
            
            if (_isCaptureStandingPhase)
                TickCaptureCore();
            
            if (_isSecureStandingPhase)
                TickSecureCore();
        }

        private float moveTime = 0f;
        private int moveDirection = 0;
        private float timePassed = 0f;
        private void HandleMoving()
        {
            // no path anymore
            if (IsInRange())
            {
                TransitionToStanding();
                return;
            }

            if (CurrentGoal == UnitAgentGoal.Capture && CaptureGoalInterruptedMidMove())
            {
                ComputeCaptureReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                TransitionToStanding();
                return;
            }

            // checked every frame during movement
            if (EnemySightedDuringMovement())
            {
                TransitionToStanding();
                return;
            }
            
            
            // if not currently stepping, begin a step
            if (!_isStepping)
            {
                // set next hex from path
                NextHex = _previewPath[_pathIndex];

                // begin the next step
                BeginStep(NextHex);
                _isStepping = true;

                _isStepping = true;
                moveTime = 1f / _currentStepTimer;
                moveDirection = UnitPathing.getDirection(new int[] {CurrentHex.x,CurrentHex.y,CurrentHex.z},new int[] {NextHex.x,NextHex.y,NextHex.z});


                GameObject Arrow =  transform.Find("Arrow").gameObject;
                
                Arrow.transform.rotation = Quaternion.Euler(ArrowDir.getArrowDirection(moveDirection));
                Arrow.transform.localPosition = ArrowDir.getPosition(moveDirection);
                if(Arrow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("arrow")){
                    Arrow.GetComponent<Animator>().SetFloat("Speed",moveTime);
                    Arrow.GetComponent<Animator>().Play("arrow",0,0f);
                    Arrow.GetComponent<Animator>().SetBool("Start",true);
                    
                }else{
                    
                    Arrow.GetComponent<Animator>().SetFloat("Speed",moveTime);
                    Arrow.GetComponent<Animator>().SetBool("Start",true);
                }

                return;
            }
            
            // increment stepping counter
            _currentStepTimer -= Time.deltaTime;
            
            // bail if step counter not reached
            if (_currentStepTimer > 0f)
                return;
            
            // reset to standing if step could not be taken
            if (!TryTakeStep())
            {
                TransitionToStanding();
                return;
            }
            
            // the step completed the path
            if (IsPathComplete())
            {
                TransitionToStanding();
            }
        }

        private void HandlePursuing()
        {
            // target null or died
            if (_targetUnit == null || !_targetUnit.IsAlive)
            {
                TransitionToStanding();
                return;
            }
            
            // get target's current hex, if no longer visible, bail
            TileScript targetsTile = _targetUnit.CurrentHex;
            if (!IsTileVisible(targetsTile))
            {
                TransitionToStanding();
                return;
            }
            
            TargetHex = targetsTile;
            
            // compute path using range as index trim
            int attackRange = Stats.BaseAttackRange;
            if (!TryComputePath(attackRange))
            {
                TransitionToStanding();
                return;
            }
            
            // if in range, fight
            if (IsInRange())
            {
                TransitionToFighting();
                return;
            }
            
            // step check
            if (!_isStepping)
            {
                NextHex = _previewPath[_pathIndex];
                BeginStep(NextHex);
                _isStepping = true;
                return;
            }
            
            // step counter
            _currentStepTimer -= Time.deltaTime;

            if (_currentStepTimer > 0f)
                return;

            // take the step
            if (!TryTakeStep())
            {
                TransitionToStanding();
            }
        }

        private void HandleFighting()
        {

            // null safety bail + target is dead check
            if (_targetUnit == null || !_targetUnit.IsAlive)
            {
                TransitionToStanding();
                return;
            }
            
            // if target left our range - stop fighting and pursue
            float dist = Vector3.Distance(_targetUnit.transform.position, _transform.position);
            if (dist > Stats.BaseAttackRange)
            {
                // target moved → pursue again
                _unitAnimator.SetAttacking(false);
                TransitionToPursuing();
                return;
            }
            
            // get direction so we ensure facing target
            Vector3 dir = _targetUnit.transform.position - _transform.position;
            HandleSpriteFlip(dir);

            
            
            // animation drives on attack hit
            _unitAnimator.SetAttacking(true);

        }

        private void HandleFleeing()
        {
            // no path - stop
            if (IsInRange())
            {
                TransitionToStanding();
                return;
            }
            
            // NOTE : fleeing does not check new tile status
            // it could though if this was deemed needed
            
            // step check
            if (!_isStepping)
            {
                NextHex = _previewPath[_pathIndex];
                BeginStep(NextHex);
                _isStepping = true;
                return;
            }
            
            // step counter
            _currentStepTimer -= Time.deltaTime;
            
            if (_currentStepTimer > 0f)
                return;
            
            // take step
            if (!TryTakeStep())
            {
                TransitionToStanding();
                return;
            }

            // Path complete = stop
            if (IsPathComplete())
            {
                TransitionToStanding();
            }
        }

        private void HandleCapturing()
        {
            // intentional facade to standing right now
            HandleStanding();
        }

        private void HandleDying()
        {
            // intentionally empty
            // TransitionToDying handles calling anim
        }
        
        /// <summary>
        /// Intended to be called within Unit Anim:
        /// If not the unit anim, please do not call this,
        /// it is public so the unit anim can see it
        /// </summary>
        public void OnDeathAnimationCompleted()
        {
            MarkForDestruction();
        }

        #endregion

        /// <summary>
        /// Used by Player to give the unit an order
        ///
        /// Author: Nico
        ///
        /// Intended to be used by player,
        /// So it's functionality is: This Unit, do this thing
        /// does not care about goal, which is defined by player
        ///
        /// Any <see cref="ISelectable"/> can be a target
        /// Currently Handles
        /// <see cref="TileScript"/> as a target
        /// <see cref="BaseUnit"/> as a target
        /// <see cref="BuildingScript"/> as a target
        /// And reports Error for cases not yet being handled
        /// </summary>
        /// <param name="worldPos">
        /// This is the Vector3 world position of target
        /// </param>
        /// <param name="target">
        /// This is the ISelectable component of the target
        /// </param>
        public void OnCommand(Vector3 worldPos, ISelectable target)
        {
            switch (target)
            {
                case TileScript targetTile:
                    HandleTileCommand(targetTile);
                    break;
                
                case BaseUnit targetUnit:
                    HandleUnitCommand(targetUnit);
                    break;
                
// ReSharper disable once SuspiciousTypeConversion.Global
                // NOTE: Building Script does not currently implement ISelectable
                case BuildingScript targetBuilding:
                    HandleBuildingCommand(targetBuilding);
                    break;
                
                default:
                    Debug.LogError("[UNIT] Unknown Command Target for Unit");
                    break;
            }
        }

        /// <summary>
        /// Logic for handling a Tile destination
        ///
        /// Author: Nico
        /// </summary>
        /// <param name="tile"></param>
        private void HandleTileCommand(TileScript tile)
        {
            // Tile is not a tile we can move to target
            if (!IsWalkable(tile))
            {
                FindAlternativeRoute(tile);
                return;
            }
            
            // Tile is reporting empty
            if (!tile.TryGetOccupant(out MonoBehaviour occupant))
            {
                TryGetPathAndMove(tile);
                return;
            }
            
            // Tile reporting a building occupant
            // NOTE: code can get occupancy regardless of visibility
            if (occupant is BuildingScript targetBuilding)
            {
                HandleBuildingCommand(targetBuilding);
                return;
            }
            
            // Tile reporting a unit occupant = check visibility status
            // NOTE: code can get occupancy regardless of visibility
            if (occupant is BaseUnit targetUnit)
            {
                // check if we should see this unit
                if (!IsTileVisible(tile))
                {
                    // We cannot see the occupant = treat as empty tile
                    TryGetPathAndMove(tile);
                    return;
                }
                
                TryAttackTarget(targetUnit);
                return;
            }
            
            // REPORT : probably added an occupancy type
            // and have not adjusted this logic path yet
            Debug.LogWarning("[UNIT] Unit was given a tile of un-logic-ed occupancy");
        }
        
        private void HandleUnitCommand(BaseUnit targetUnit)
        {
            // friendly unit
            if (IsFriendly(targetUnit))
            {
                HandleFriendlyUnitCommand(targetUnit);
                return;
            }
            // Enemy unit clicked
            HandleEnemyUnitCommand(targetUnit);
        }

        private void HandleFriendlyUnitCommand(BaseUnit friendlyUnit)
        {
            // NOTE: This could become a switch for doing friendly actions
            // For now, fallback to finding an alternative route logic
            FindAlternativeRoute(friendlyUnit.CurrentHex);
        }

        private void HandleEnemyUnitCommand(BaseUnit enemyUnit)
        {
            TryAttackTarget(enemyUnit);
        }

        /// <summary>
        /// Operation for handling a building destination:
        ///
        /// Author: Nico,
        ///
        /// Our Game is designed where unit can not move to building occupied tiles,
        /// <see cref="FindAlternativeRoute"/>
        /// is our helper to reroute
        /// </summary>
        /// <param name="building"></param>
        private void HandleBuildingCommand(BuildingScript building)
        {
            FindAlternativeRoute(building.occupantTile);
        }
        
        private void TryGetPathAndMove(TileScript tile)
        {
            TargetHex = tile;
            
            // if retrieved path is in range
            if (!TryComputePath(0) || IsInRange())
            {
                TransitionToStanding();
                return;
            }

            // move order during combat requires flee movement motor
            if (MotorState == UnitMotorState.Fighting)
            {
                TransitionToFleeing();
                return;
            }

            // standard movement motor
            TransitionToMoving();
        }
        
        private void TryAttackTarget(BaseUnit targetUnit)
        {
            // safety bail
            if (targetUnit == null)
                return;
            
            // set our internal target fields
            _targetUnit = targetUnit;
            TargetHex = targetUnit.CurrentHex;
            
            int attackRange = Stats.BaseAttackRange;
            
            // pathing fails
            if (!TryComputePath(attackRange))
            {
                TransitionToStanding();
                return;
            }

            // pathing succeed, and in range
            if (IsInRange())
            {
                TransitionToFighting();
                return;
            }
            
            // pathing succeed, not in range = move
            TransitionToPursuing();
        }

        private void TryGetPathAndFlee(TileScript tile)
        {
            TargetHex = tile;
            _targetUnit = null;

            if (!TryComputePath(0) || IsInRange())
            {
                TransitionToStanding();
                return;
            }
            
            TransitionToFleeing();
        }

        private void FindAlternativeRoute(TileScript tile)
        {
            // intentionally empty right now
            Debug.LogWarning("[UNIT] No direct path. TODO: implement alternative routing");
        }

        private bool TryComputePath(int range)
        {
            if (!_pathResolver.TryGetPath(CurrentHex, TargetHex, range, out List<TileScript> path))
                return false;

            // clear existing path preview
            _previewPath.Clear();
            
            // if path null or count 0 at this point,
            // we are where we need to be
            if (path == null || path.Count == 0)
                return true;
            
            // update path preview
            _previewPath.AddRange(path);
            return true;
        }

        #region Motor Transition Helpers

        private void TransitionToStanding()
        {
            if (CurrentGoal == UnitAgentGoal.Capture && !_isCaptureStandingPhase)
                ActivateCaptureCore();
            
            if (CurrentGoal == UnitAgentGoal.Secure && !_isSecureStandingPhase)
                ActivateSecureCore();
            
            MotorState = UnitMotorState.Standing;

            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;

            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(false);

            if (CurrentGoal == UnitAgentGoal.Guard && !_isGuardStandingPhase)
                ActivateGuardCore();
        }

        private void TransitionToMoving()
        {
            MotorState = UnitMotorState.Moving;
            
            _pathIndex = 0;
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(true);
            _unitAnimator.SetAttacking(false);
        }

        private void TransitionToPursuing()
        {
            if (CurrentGoal is UnitAgentGoal.Fight or UnitAgentGoal.Defend or UnitAgentGoal.Support)
            {
                _attackTimeSpentPursuing = 0f;
            }
            
            MotorState = UnitMotorState.Pursuing;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            _pathIndex = 0;
            
            _unitAnimator.SetWalking(true);
            _unitAnimator.SetAttacking(false);
        }

        private void TransitionToFighting()
        {
            _unitAnimator.SetAttackSpeed(Stats.BaseAttackSpeed);
            
            print("start fight");
            if (CurrentGoal == UnitAgentGoal.Capture)
            {
                ComputeCaptureReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
            }
            
            if (CurrentGoal is UnitAgentGoal.Fight or UnitAgentGoal.Defend or UnitAgentGoal.Support)
            {
                _attackTimeSpentFighting = 0f;
            }
            
            MotorState = UnitMotorState.Fighting;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(true);
            print("end fight");
        }

        private void TransitionToFleeing()
        {
            MotorState = UnitMotorState.Fleeing;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            _pathIndex = 0;
            
            _unitAnimator.SetWalking(true);
            _unitAnimator.SetAttacking(false);
        }

        private void TransitionToDying()
        {
            if (CurrentGoal != UnitAgentGoal.None)
                EndGoalWithFailure();
            
            MotorState = UnitMotorState.Dying;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(false);
            _unitAnimator.TriggerDeath();
            GlobalSound.unitDead(UnitType);
        }

        private void TransitionToCapturing()
        {
            MotorState = UnitMotorState.Capturing;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(false);
        }

        #endregion

        #region Step Helpers

        private bool EnemySightedDuringMovement()
        {
            // safety bail
            if (TargetHex == null)
                return false;
            
            // target is visible and contains an enemy
            if (IsTileVisible(TargetHex) &&
                TargetHex.TryGetOccupant(out MonoBehaviour targetOcc) &&
                targetOcc is BaseUnit targetUnit &&
                IsEnemy(targetUnit))
            {
                return true;
            }
            
            // safety bail
            if (_previewPath == null || _previewPath.Count == 0)
                return false;
            
            // up to 3 tiles away from target (sight distance)
            int startIndex = Mathf.Max(0, _previewPath.Count - 3);
            for (int i = startIndex; i < _previewPath.Count; i++)
            {
                // get the tile
                TileScript tile = _previewPath[i];
                if (tile == null)
                    continue;

                //visibility check
                if (!IsTileVisible(tile))
                    continue;

                // get its occupant
                if (!tile.TryGetOccupant(out MonoBehaviour occ))
                    continue;

                // if enemy
                if (occ is BaseUnit unit && IsEnemy(unit))
                    return true;
            }
            return false;
        }

        private void BeginStep(TileScript nextHex)
        {
            // bail to standing if next hex somehow null
            if (nextHex == null)
            {
                Debug.LogError("[UNIT] tried to begin step with null NextHex");
                TransitionToStanding();
                return;
            }
            
            // flip sprite if needed
            Vector3 direction = nextHex.transform.position - _transform.position;
            HandleSpriteFlip(direction);
            
            // update counter for movement value
            int tileCost = nextHex.getMovement();
            float moveSpeed = Stats.BaseMoveSpeed;
            
            _currentStepDuration = tileCost / moveSpeed;
            _currentStepTimer = _currentStepDuration;

            // ensure walking anim is on
            _unitAnimator.SetWalking(true);
            _isStepping = true;
        }
        
        private bool TryTakeStep()
        {
            // Decrement timer
            _currentStepTimer -= Time.deltaTime;
            
            // Step counter not finished yet
            if (_currentStepTimer > 0f)
                return true;
            
            // take step, return false if could not occupy the space
            if (!NextHex.TrySetUnitOccupant(this))
            {
                Debug.LogWarning("[UNIT] Failed to claim next tile during movement");
                return false;
            }
            
            // Clear old tile, return false if could not
            if (!CurrentHex.TryClearUnitOccupant(this))
            {
                Debug.LogError("[UNIT] Failed to clear previous tile");
                return false;
            }
            
            // update internals
            CurrentHex = NextHex;
            // move the unit in Unity
            _transform.position = NextHex.transform.position;
            
            // Reset stepping state
            _isStepping = false;
            _pathIndex++;

            return true;
        }

        #endregion

        #region Helper Operations
        
        private void HandleSpriteFlip(Vector3 direction)
        {
            if (Math.Abs(direction.x) < Mathf.Epsilon)
                return;
            
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = direction.x < 0f;
        }
        
        private bool IsWalkable(TileScript tile) => 
            tile != null && tile.getMovement() > 0;
        
        private bool IsTileVisible(TileScript tile) =>
            tile != null && tile.IsVisibleTo(Owner);

        private bool IsFriendly(BaseUnit other) => 
            other != null && other.Owner == this.Owner;
        
        private bool IsEnemy(BaseUnit other) =>
            other != null && other.Owner != this.Owner;

        private bool IsInRange() => 
            _previewPath.Count == 0;

        private bool IsPathComplete() =>
            _pathIndex >= _previewPath.Count;

        private bool TryGetFirstClosestEnemyInArea(TileScript location, int range, out BaseUnit enemy)
        {
            enemy = null;
            float closestDist = float.MaxValue;
            
            List<TileScript> tiles = location.GetTilesInRange(range);
            
            foreach (TileScript tile in tiles)
            {
                // only get visible tile info
                if (!IsTileVisible(tile))
                    continue;

                // skip if no occupant
                if (!tile.TryGetOccupant(out MonoBehaviour occ))
                    continue;

                // get occupant as unit
                if (occ is not BaseUnit unit)
                    continue;

                // skip if friendly
                if (!IsEnemy(unit))
                    continue;

                // Compute distance to enemy and keep closest
                float dist = Vector3.Distance(transform.position, unit.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    enemy = unit;
                }
            }

            return enemy != null;
        }

        #endregion

        #region Game State Support Operations
        
        public void MarkForDestruction()
        {
            // unsubscribe from own health bar's event
            if (Health != null) 
                Health.OnHealthEmpty -= TransitionToDying;
            
            // unsubscribe from perspective manager events
            PerspectiveManager.Instance.OnPerspectiveChanged -= UpdateVisibility;
            
            // if we occupied a hex (should always be true), clear ourselves from it
            if (CurrentHex != null)
            {
                // Only clear if we were actually the occupant
                CurrentHex.TryClearUnitOccupant(this);
            }
            
            // unity only does the destroy AFTER this frame
            Destroy(gameObject);
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

        #endregion

        #region Combat Core

        private void Attack(BaseUnit enemy)
        {
            enemy.TakeDamage(Stats.BaseAttackPower, this);
            GlobalSound.playFight(UnitType);
        }
        
        private void TakeDamage(float amount, BaseUnit attacker)
        {
            Health.ApplyDamage(amount);

            TryRetaliate(attacker);
        }
        
        private void TryRetaliate(BaseUnit attacker)
        {
            // do not retaliate from these motor states
            switch (MotorState)
            {
                case UnitMotorState.Fleeing:
                // Already fighting or chasing a target
                case UnitMotorState.Fighting or UnitMotorState.Pursuing:
                    return;
            }

            // NRE Bail
            if (attacker == null)
                return;

            // cache target values
            _targetUnit = attacker;
            TargetHex = attacker.CurrentHex;

            // compute path with range
            int attackRange = Stats.BaseAttackRange;

            if (!TryComputePath(attackRange))
                return;

            if (IsInRange())
            {
                TransitionToFighting();
                return;
            }

            TransitionToMoving();
        }

        public void OnAttackHit()
        {
            // NRE bail
            if (_targetUnit == null)
                return;
            
            Attack(_targetUnit);
            
            // this attack killed enemy = done fighting
            if (_targetUnit == null || !_targetUnit.IsAlive)
            {
                _unitAnimator.SetAttacking(false);
            
                _isStepping = false;
                _currentStepTimer = 0f;
                NextHex = null;
                _pathIndex = 0;
            
                TransitionToStanding();
            }
        }

        #endregion

        #region Agent Core Actions

        private float _agentReward;
        private float _timeDuringAction;
        
        private float _goalTimeout;
        private const float MaxGoalDuration = 30f;

        /// <summary>
        /// Used By Agent to set goal for action given to unit
        /// </summary>
        /// <param name="goal"></param>
        private void SetGoal(UnitAgentGoal goal)
        {
            // set the goal
            CurrentGoal = goal;
            
            // reset helper tracker
            _timeDuringAction = 0f;

            _goalTimeout = 0f;
        }

        private void EndGoalWithFailure()
        {
            switch (CurrentGoal)
            {
                case UnitAgentGoal.Guard:
                    ComputeGuardReward(GoalResult.Failure);
                    ResolveGoal(GoalResult.Failure);
                    break;

                case UnitAgentGoal.Move:
                case UnitAgentGoal.Capture:
                case UnitAgentGoal.Defend:
                case UnitAgentGoal.Secure:
                case UnitAgentGoal.Fight:
                case UnitAgentGoal.Support:
                case UnitAgentGoal.FlyYouFools:
                    ResolveGoal(GoalResult.Failure);
                    break;

                case UnitAgentGoal.None:
                default:
                    break;
            }
        }

        private void ResolveGoal(GoalResult result)
        {
            if (CurrentGoal != UnitAgentGoal.None)
            {
                OnGoalResolved?.Invoke(this, CurrentGoal, result, _agentReward);
            }
            
            CurrentGoal = UnitAgentGoal.None;
        }

        private void ApplyGoalOutcomeModifier(GoalResult result)
        {
            switch (result)
            {
                case GoalResult.Success:
                    _agentReward += 0.5f;
                    break;

                case GoalResult.Failure:
                    _agentReward -= 0.5f;
                    break;

                case GoalResult.Interrupted:
                    _agentReward -= 0.1f;
                    break;

                case GoalResult.PartialSuccess:
                    _agentReward += 0.2f;
                    break;
                
                case GoalResult.Bugged:
                    _agentReward *= 0f;
                    break;
                
                default:
                    Debug.LogError($"[UNIT] Unhandled goal result {result}");
                    break;
            }
        }

        /// <summary>
        /// The broadcast from the unit on logic that "resolved" a goal
        /// NOTE: this is the unit, the goal that was intended, and the outcome
        /// </summary>
        public event Action<BaseUnit, UnitAgentGoal, GoalResult, float> OnGoalResolved;

        private void TickGoalCounters()
        {
            _timeDuringAction += Time.deltaTime;
            _goalTimeout += Time.deltaTime;
            
            if (CurrentGoal is UnitAgentGoal.Fight or UnitAgentGoal.Defend or UnitAgentGoal.Support)
                TickAttackCore();
            
            if (CurrentGoal == UnitAgentGoal.Move)
                TickMoveCore();
            
            if (CurrentGoal == UnitAgentGoal.FlyYouFools)
                TickFleeCore();

            if (_goalTimeout >= MaxGoalDuration)
            {
                ResolveGoal(GoalResult.Interrupted);
                return;
            }
        }

        #endregion

        #region Move Agent Core

        private TileScript _moveTile;
        private float _moveTimeSpent;
        private bool _moveSuccessTriggered;

        public void RequestMoveTo(TileScript moveTile)
        {
            SetGoal(UnitAgentGoal.Move);

            if (moveTile == null || !IsWalkable(moveTile))
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }

            _moveTile = moveTile;
            _moveTimeSpent = 0f;
            _moveSuccessTriggered = false;

            TryGetPathAndMove(moveTile);
        }

        private void TickMoveCore()
        {
            _moveTimeSpent += Time.deltaTime;

            // SUCCESS: reached the tile
            if (CurrentHex == _moveTile)
            {
                _moveSuccessTriggered = true;
                ComputeMoveReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);
                return;
            }

            // INTERRUPTED: entered combat
            if (MotorState == UnitMotorState.Fighting)
            {
                ComputeMoveReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                return;
            }

            // INTERRUPTED: tile no longer reachable
            if (!TryComputePath(0))
            {
                ComputeMoveReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                return;
            }
        }
        
        private void ComputeMoveReward(GoalResult result)
        {
            _agentReward = 0f;

            // Small reward for reaching the tile
            if (_moveSuccessTriggered)
                _agentReward += 0.1f;

            // Small penalty for long travel
            float travelPenalty = Mathf.Clamp01(_moveTimeSpent / 10f);
            _agentReward -= travelPenalty * 0.05f;

            ApplyGoalOutcomeModifier(result);   
        }

        #endregion

        #region Attack Agent Core

        private BaseUnit _attackTarget;
        private float _attackTimeSpentPursuing;
        private float _attackTimeSpentFighting;
        private bool _attackSuccessTriggered;

        public void RequestAttackUnit(BaseUnit target, UnitAgentGoal goal)
        {
            if (goal != UnitAgentGoal.Fight &&
                goal != UnitAgentGoal.Defend &&
                goal != UnitAgentGoal.Support)
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }
            
            if (target == null || !target.IsAlive || IsFriendly(target))
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }
            
            SetGoal(goal);

            _attackTarget = target;
            _attackTimeSpentPursuing = 0f;
            _attackTimeSpentFighting = 0f;
            _attackSuccessTriggered = false;

            // Use your existing attack logic
            TryAttackTarget(target);
        }

        private void TickAttackCore()
        {
            if (MotorState == UnitMotorState.Pursuing)
                _attackTimeSpentPursuing += Time.deltaTime;
            
            if (MotorState == UnitMotorState.Fighting)
                _attackTimeSpentFighting += Time.deltaTime;
            
            // target died
            if (_attackTarget == null || !_attackTarget.IsAlive)
            {
                _attackSuccessTriggered = true;
                ComputeAttackReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);
                return;
            }
            
            // lost sight
            if (!IsTileVisible(_attackTarget.CurrentHex))
            {
                ComputeAttackReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                return;
            }
        }
        
        private void ComputeAttackReward(GoalResult result)
        {
            _agentReward = 0f;

            // Reward for time spent fighting
            float fightRatio = Mathf.Clamp01(_attackTimeSpentFighting / 5f);
            _agentReward += fightRatio * 0.3f;

            // Penalty for long pursuit
            float pursuePenalty = Mathf.Clamp01(_attackTimeSpentPursuing / 10f);
            _agentReward -= pursuePenalty * 0.1f;

            // Bonus for success
            if (_attackSuccessTriggered)
                _agentReward += 0.4f;

            ApplyGoalOutcomeModifier(result);     
        }
        
        private void ActivateSecureCore()
        {
            _secureTimeSpentMoving = _timeDuringAction;
            _timeDuringAction = 0f;

            _isSecureStandingPhase = true;
            _secureElapsedStandingTime = 0f;
        }
        
        private void TickSecureCore()
        {
            _secureElapsedStandingTime += Time.deltaTime;

            // If we left the tile → interrupted
            if (CurrentHex != _secureTile)
            {
                ComputeSecureReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                _isSecureStandingPhase = false;
                return;
            }

            // If building ownership changes (enemy captured it) → interrupted
            if (_secureBuilding.getOwner() != Owner)
            {
                ComputeSecureReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                _isSecureStandingPhase = false;
                return;
            }

            // If enemy enters capture ring → fight
            if (TryGetFirstClosestEnemyInArea(CurrentHex, 1, out BaseUnit enemy))
            {
                TryAttackTarget(enemy);
                return;
            }

            // SUCCESS: held the tile long enough
            if (_secureElapsedStandingTime >= 5f) // tune this
            {
                _secureSuccessTriggered = true;
                ComputeSecureReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);
                _isSecureStandingPhase = false;
                return;
            }
        }
        
        private void ComputeSecureReward(GoalResult result)
        {
            _agentReward = 0f;

            // Reward for holding the tile
            float holdRatio = Mathf.Clamp01(_secureElapsedStandingTime / 5f);
            _agentReward += holdRatio * 0.2f;

            // Penalty for long travel
            float travelPenalty = Mathf.Clamp01(_secureTimeSpentMoving / 10f);
            _agentReward -= travelPenalty * 0.05f;

            // Bonus for success
            if (_secureSuccessTriggered)
                _agentReward += 0.3f;

            ApplyGoalOutcomeModifier(result);
        }

        #endregion

        #region Secure Agent Core

        private TileScript _secureTile;
        private BuildingScript _secureBuilding;

        private float _secureTimeSpentMoving;
        private float _secureElapsedStandingTime;
        private bool _isSecureStandingPhase;
        private bool _secureSuccessTriggered;
        
        public void RequestSecureLocation(TileScript tile)
        {
            SetGoal(UnitAgentGoal.Secure);

            _secureTile = tile;
            _secureBuilding = null;

            // Find building via neighbors
            foreach (TileScript n in tile.GetNeighbours())
            {
                if (n.AttachedBuilding != null)
                {
                    _secureBuilding = n.AttachedBuilding;
                    break;
                }
            }

            // Invalid secure target
            if (_secureBuilding == null)
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }
            
            // Invalid: we do NOT own this building
            if (_secureBuilding.getOwner() != Owner)
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }

            // Reset tracking
            _secureTimeSpentMoving = 0f;
            _secureElapsedStandingTime = 0f;
            _isSecureStandingPhase = false;
            _secureSuccessTriggered = false;

            TryGetPathAndMove(tile);
        }

        #endregion

        #region Capture Agent Core

        private TileScript _captureTile;
        private BuildingScript _captureBuilding;
        
        private float _captureTimeSpentMoving;
        private float _captureElapsedStandingTime;
        private bool _isCaptureStandingPhase;
        private bool _captureSuccessTriggered;

        public void RequestCaptureLocation(TileScript captureTile)
        {
            SetGoal(UnitAgentGoal.Capture);
            
            // cache tile
            _captureTile = captureTile;
            
            // reset ref
            _captureBuilding = null;
            
            // get neighbors - the unit with building is what we want
            foreach (TileScript tiles in captureTile.GetNeighbours())
            {
                if (tiles.AttachedBuilding != null)
                {
                    _captureBuilding = tiles.AttachedBuilding;
                    break;
                }
            }
            
            // NRE bail, and goal can not be achieved fail
            if (_captureBuilding == null)
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }
            
            // if we own the building - uncapturable - fail
            if (_captureBuilding.getOwner() == this.Owner)
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }
            
            // Reset capture tracking
            _captureTimeSpentMoving = 0f;
            _captureElapsedStandingTime = 0f;
            _isCaptureStandingPhase = false;
            _captureSuccessTriggered = false;

            // Move toward capture tile
            TryGetPathAndMove(captureTile);
        }

        private void ActivateCaptureCore()
        {
            // Record travel time
            _captureTimeSpentMoving = _timeDuringAction;
            _timeDuringAction = 0f;

            // Begin standing phase
            _isCaptureStandingPhase = true;
            _captureElapsedStandingTime = 0f;
        }

        private void TickCaptureCore()
        {
            _captureElapsedStandingTime += Time.deltaTime;
            
            // left capture tile during capture
            if (CurrentHex != _captureTile)
            {
                ComputeCaptureReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                _isCaptureStandingPhase = false;
                return;
            }
            
            // we captured building
            if (_captureBuilding.getOwner() == this.Owner)
            {
                _captureSuccessTriggered = true;
                ComputeCaptureReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);
                _isCaptureStandingPhase = false;
            }
        }

        private void ComputeCaptureReward(GoalResult result)
        {
            _agentReward = 0f;
            
            // stand time shape
            float standingRatio = Mathf.Clamp01(_captureElapsedStandingTime / 5f);
            _agentReward += standingRatio * 0.3f;
            
            // travel time penalty
            float travelPenalty = Mathf.Clamp01(_captureTimeSpentMoving / 10f);
            _agentReward -= travelPenalty * 0.1f;
            
            if (_captureSuccessTriggered)
                _agentReward += 0.4f;

            ApplyGoalOutcomeModifier(result);
        }

        private bool CaptureGoalInterruptedMidMove()
        {
            return _captureBuilding.getOwner() == this.Owner;
        }

        #endregion

        #region FlyYouFools Core

        private TileScript _fleeTile;
        private float _fleeTimeSpent;
        private bool _fleeSuccessTriggered;
        
        public void RequestFlee(TileScript tile)
        {
            SetGoal(UnitAgentGoal.FlyYouFools);

            // if trying to pass gandalf, don't
            if (tile == null || !IsWalkable(tile))
            {
                ResolveGoal(GoalResult.Failure);
                return;
            }

            _fleeTile = tile;
            _fleeTimeSpent = 0f;
            _fleeSuccessTriggered = false;

            // If we are in combat, this forces a disengage
            TryGetPathAndFlee(tile);
        }
        
        private void TickFleeCore()
        {
            _fleeTimeSpent += Time.deltaTime;

            // SUCCESS: reached safety
            if (CurrentHex == _fleeTile)
            {
                _fleeSuccessTriggered = true;
                ComputeFleeReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);
                return;
            }

            // INTERRUPTED: path breaks
            if (!TryComputePath(0))
            {
                ComputeFleeReward(GoalResult.Interrupted);
                ResolveGoal(GoalResult.Interrupted);
                return;
            }
        }
        
        private void ComputeFleeReward(GoalResult result)
        {
            _agentReward = 0f;

            // Reward for reaching safety
            if (_fleeSuccessTriggered)
                _agentReward += 0.1f;

            // Penalty for long travel
            float travelPenalty = Mathf.Clamp01(_fleeTimeSpent / 10f);
            _agentReward -= travelPenalty * 0.05f;

            ApplyGoalOutcomeModifier(result);
        }

        #endregion

        #region Guard Agent Core

        private const int GuardRange = 2;
        private float _guardTimeSpentMoving;
        private float _guardRequiredDuration;
        private float _guardElapsedStandingTime;
        private int _guardEnemiesDefeated;
        private bool _guardHadCombat;
        private bool _isGuardStandingPhase;
        private bool _isGuardCombatPhase;
        //private bool _isGuardReturningToPost;
        private TileScript _guardTile;
        
        public void RequestGuardLocation(TileScript target, float guardDuration)
        {
            // goal is to guard
            SetGoal(UnitAgentGoal.Guard);
            
            // cache the requested tile
            _guardTile = target;
            
            // reset guard duration tracking
            _guardTimeSpentMoving = 0f;
            _guardRequiredDuration = guardDuration;
            _guardElapsedStandingTime = 0f;
            
            // reset guard action kill counter
            _guardEnemiesDefeated = 0;
            _guardHadCombat = false;
            
            // reset state of phase flags
            _isGuardStandingPhase = false;
            _isGuardCombatPhase = false;
            
            // unit must try and move to target
            TryGetPathAndMove(target);
        }

        private void ActivateGuardCore()
        {
            // cache the time spent moving and reset
            _guardTimeSpentMoving = _timeDuringAction;
            _timeDuringAction = 0f;
            
            // guard core init (guard at post)
            _isGuardStandingPhase = true;
            _isGuardCombatPhase = false;
            
            // reset timer for length standing
            _guardElapsedStandingTime = 0f;
        }

        private void TickGuardCore()
        {
            // tick the counter
            _guardElapsedStandingTime += Time.deltaTime;
            
            // counter expired, resolve goal
            if (_guardElapsedStandingTime >= _guardRequiredDuration)
            {
                // compute reward and resolve
                ComputeGuardReward(GoalResult.Success);
                ResolveGoal(GoalResult.Success);

                // release from guard duty
                _isGuardStandingPhase = false;
                _isGuardCombatPhase = false;
                return;
            }
            
            // if fighting do not scan
            if (_isGuardCombatPhase)
                return;
            
            // if not at post, return to it
            if (CurrentHex != _guardTile) 
                TryGetPathAndMove(_guardTile);
            
            // scan for enemies at post
            if (TryGetFirstClosestEnemyInArea(_guardTile, GuardRange, out BaseUnit enemy))
            {
                // flag we are in combat
                _isGuardCombatPhase = true;
                _guardHadCombat = true;
                TryAttackTarget(enemy);
            }
        }

        private void ComputeGuardReward(GoalResult result)
        {
            // reset reward value
            _agentReward = 0f;
            
            // reward for killing enemies
            // as the primary intent of guard, high reward per
            _agentReward += _guardEnemiesDefeated * 0.6f;
            
            // big reward for even having combat to help drive policy
            if (_guardHadCombat)
                _agentReward += 0.15f;
            
            // small bonus for being in combat during end of action time
            if (_isGuardCombatPhase)
                _agentReward += 0.2f;
            
            // how much of the requested time did the guard achieve
            float standingRatio = Mathf.Clamp01(_guardElapsedStandingTime / _guardRequiredDuration);
            _agentReward += standingRatio * 0.3f;
            
            // small penalty for time spent moving to guard location
            float travelPenalty = Mathf.Clamp01(_guardTimeSpentMoving / 10f); 
            _agentReward -= travelPenalty * 0.1f;

            ApplyGoalOutcomeModifier(result);
        }

        #endregion

        #region Preview Path Gizmo

        private readonly Vector3 _pathingGizmoOffset = new(0, 0.5f, 0);
        
        private void OnDrawGizmos()
        {
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

        public void OnSelected(){
            if(Owner == UnitOwner.Player && PerspectiveManager.Instance.CurrentPerspective == Perspective.Player){
                float height = CurrentHex.getHeight() / 1.95f;
                GameObject t = Instantiate(Resources.Load("Prefabs/SelectionHex", typeof(GameObject)) as GameObject,transform.position + new Vector3(0,height,0),Quaternion.Euler(90,0,0),transform);
                t.name = "SelectionHex";
            }
            
        }

        public void OnDeselected(){
            if(Owner == UnitOwner.Player && PerspectiveManager.Instance.CurrentPerspective == Perspective.Player){
                Destroy(transform.Find("SelectionHex").gameObject);
            }
        }

        #endregion
    }
    
    /// <summary>
    /// This is used to define HOW a unit is performing an action,
    /// Drives current Update behavior
    /// </summary>
    public enum UnitMotorState
    {
        Standing,       // Standing Motor
        Moving,         // General Purpose Movement Motor
        Pursuing,       // Pursuing Movement Motor
        Fighting,       // Currently Fighting Motor
        Fleeing,        // Triggered from fighting, specifically does not retaliate
        Capturing,      // FIXME! Facade to Standing right now
        Dying           // Motor for dying
    }
    
    /// <summary>
    /// This is what the unit is trying to achieve, used by Agent
    /// </summary>
    public enum UnitAgentGoal
    {
        None, // This unit can be given an action
        
        // Movement oriented goals:
        Move,           // Move to a space
        Capture,        // Capture a point
        
        // Standing oriented goals:
        Guard,          // Guard a point (any space)
        Secure,         // Secure a point (a capture point)
        FlyYouFools,    // flee from combat (toward capital)
        
        // Attack oriented goals:
        Defend,         // Defend a point (by attacking)
        Fight,          // Fight a unit
        Support,        // Support another unit
    }
    
    /// <summary>
    /// Used by unit to tell agent outcome of action requested
    /// </summary>
    public enum GoalResult
    {
        Success,
        Failure,
        Interrupted,
        PartialSuccess,
        OverriddenByAgent,
        Bugged
    }

    
}