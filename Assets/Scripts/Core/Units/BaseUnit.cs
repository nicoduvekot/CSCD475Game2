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

        public GameObject GetGameObject(){return gameObject;}
        
        private bool _ownerInitialized;
        
        private Renderer[] _renderers;
        private SpriteRenderer _spriteRenderer;
        private UnitAnimator _unitAnimator;
        private UnitPathing _pathing;
        private UnitPathResolver _pathResolver;

        private Health Health { get; set; }
        private Healthbar Healthbar { get; set; }
        private UnitStats Stats;
        private StateDisplayUI _stateDisplayUI;
        
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
            
            UpdateStateUI();
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
            
            _stateDisplayUI = GetComponentInChildren<StateDisplayUI>();
            
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            _unitAnimator = GetComponentInChildren<UnitAnimator>();
            
            
            Health.OnHealthEmpty += TransitionToDying;
            //print("currentUnitType is " + UnitType);
            //print("upgrades for soldier is " + TechController.Instance.getUnitsModifier(UnitOwner.Player,0));
            //print("upgrades for archer is " + TechController.Instance.getUnitsModifier(UnitOwner.Player,1));
            //print("upgrades for horse is " + TechController.Instance.getUnitsModifier(UnitOwner.Player,2));

        }

        private void Start()
        {
            Health.InitializeHealth(Stats.BaseMaxHealth + (50 *(float)TechController.Instance.getUnitsModifier(Owner,UnitType)) - 50);
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
        }

        private void HandleStanding()
        {
            
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

            // checked every frame during movement
            if (EnemySightedDuringMovement())
            {
                TransitionToStanding();
                return;
            }
            
            
            // if not currently stepping, begin a step
            if (!_isStepping)
            {
                if (!RefreshPath(0))
                {
                    TransitionToStanding();
                    return;
                }
                
                // set next hex from path
                NextHex = _previewPath[_pathIndex];

                // begin the next step
                BeginStep(NextHex);
                _isStepping = true;

                _isStepping = true;
                moveTime = 1f / _currentStepTimer;
                moveDirection = UnitPathing.getDirection(new int[] {CurrentHex.x,CurrentHex.y,CurrentHex.z},new int[] {NextHex.x,NextHex.y,NextHex.z});


                // BUG: ? handle moving is getting arrow info? what?
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
            
            // step check
            if (!_isStepping)
            {
                if (!RefreshPath(attackRange))
                {
                    if (IsInRange())
                        TransitionToFighting();
                    else 
                        TransitionToStanding();
                    return;
                }
                
                if (IsInRange())
                {
                    TransitionToFighting();
                    return;
                }

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
            // float dist = Vector3.Distance(_targetUnit.transform.position, _transform.position);
            // if (TryComputePath(Stats.BaseAttackRange))
            // {
            //     // target moved → pursue again
            //     _unitAnimator.SetAttacking(false);
            //     TransitionToPursuing();
            //     return;
            // }
            
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
                if (!RefreshPath(0))
                {
                    TransitionToStanding();
                    return;
                }
                
                if (IsInRange())
                {
                    TransitionToStanding();
                    return;
                }

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
            OnUnitDeath?.Invoke(this);
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
                
                case BuildingScript targetBuilding:
                    HandleBuildingCommand(targetBuilding);
                    break;
                
                default:
                    Debug.LogError("[UNIT] Unknown Command Target for Unit");
                    break;
            }
        }

        /// <summary>
        /// AUTOMATED MOVING USE ONLY!!
        /// </summary>
        /// <param name="targetTile">the target tile</param>
        public void AutomateMoveTo(TileScript targetTile)
        {
            HandleTileCommand(targetTile);
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
            MotorState = UnitMotorState.Standing;

            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;

            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(false);
            
            UpdateStateUI();
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
            
            if(Owner == UnitOwner.Player){
                GlobalSound.playMovement(UnitType);
            }
            
            UpdateStateUI();
        }

        private void TransitionToPursuing()
        {
            MotorState = UnitMotorState.Pursuing;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            _pathIndex = 0;
            
            _unitAnimator.SetWalking(true);
            _unitAnimator.SetAttacking(false);
            
            UpdateStateUI();
        }

        private void TransitionToFighting()
        {
            _unitAnimator.SetAttackSpeed(Stats.BaseAttackSpeed);
            
            MotorState = UnitMotorState.Fighting;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(true);
            
            UpdateStateUI();
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
            
            UpdateStateUI();
        }

        private void TransitionToDying()
        {
            MotorState = UnitMotorState.Dying;
            
            _isStepping = false;
            _currentStepTimer = 0f;
            NextHex = null;
            
            _unitAnimator.SetWalking(false);
            _unitAnimator.SetAttacking(false);
            _unitAnimator.TriggerDeath();
            GlobalSound.unitDead(UnitType);
            
            UpdateStateUI();
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
                if (NextHex.TryGetOccupant(out MonoBehaviour occ) && occ is BaseUnit otherUnit)
                {
                    if (IsFriendly(otherUnit))
                    {
                        // reset step flag for re-draw of path
                        _isStepping = false;
                        _pathIndex = 0;

                        // next frame, Handle (movement) will RefreshPath and try again
                        return true;
                    }
                    else
                    {
                        // Enemy blocked = engage them
                        _targetUnit = otherUnit;
                        TransitionToFighting();
                        return false;
                    }
                }
                
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

        private bool RefreshPath(int range)
        {
            if (!TryComputePath(range))
                return false;

            if (_previewPath.Count == 0)
            {
                _pathIndex = 0;
                return false;
            }

            _pathIndex = 0;
            
            return true;
        }

        #endregion

        #region Helper Operations
        
        // helper to change the text on state display UI
        private void UpdateStateUI()
        {
            if (_stateDisplayUI != null)
                _stateDisplayUI.SetText(MotorState.ToString());
        }
        
        // helper to flip the sprite
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

            // if (StateDisplayUI != null)
            //     StateDisplayUI.SetVisible(isOwnerPerspective);    
        }

        #endregion

        #region Combat Core

        private void Attack(BaseUnit enemy)
        {
            enemy.TakeDamage(Stats.BaseAttackPower + (50 * (float)TechController.Instance.getUnitsModifier(Owner,UnitType)) - 50, this);
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
            if(gameObject != null && Owner == UnitOwner.Player && PerspectiveManager.Instance.CurrentPerspective == Perspective.Player){
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