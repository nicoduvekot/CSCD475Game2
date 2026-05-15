using System.Collections;
using Core.UIElements;
using Selection;
using UnityEngine;
using HealthSystem;
using EditorTools.Attributes;

namespace Units
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(UnitStats))]
    public abstract class BaseUnit : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;

        protected Health Health { get; private set; }
        protected UnitStats Stats { get; private set; }
        protected StateDisplayUI StateDisplayUI { get; private set; }
        
        protected TestSelectableHex CurrentHex { get; set; }
        protected TestSelectableHex TargetHex { get; set; }

        [field: ReadOnly]
        public UnitOwner Owner { get; private set; }

        private bool _ownerInitialized;

        protected UnitState _state = UnitState.Idle;
        protected BaseUnit _targetEnemy;
        
        protected float _attackCooldownTimer;

        private const float FakeDeathAnimTime = 1.5f;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Stats = GetComponent<UnitStats>();
            
            Health.InitializeHealth(Stats.BaseMaxHealth);
            Health.OnHealthEmpty += HandleDeath;
            
            StateDisplayUI = GetComponentInChildren<StateDisplayUI>();
            if (StateDisplayUI != null) 
                StateDisplayUI.Initialize(transform);
        }
        
        protected virtual void Start()
        {
            if (!_ownerInitialized)
                Debug.LogWarning($"{name} was spawned without an owner! This must be set at runtime.");
            
            if (StateDisplayUI != null)
                StateDisplayUI.SetText(_state.ToString());
        }
        
        protected virtual void OnDestroy()
        {
            if (Health != null) Health.OnHealthEmpty -= HandleDeath;
        }
        
        // public API
        
        public void InitializeOwner(UnitOwner newOwner)
        {
            Owner = newOwner;
            _ownerInitialized = true;
        }
        
        public void TakeDamage(float amount)
        {
            Health.ApplyDamage(amount);
        }
        
        public virtual void OnCommand(Vector3 worldPos, ISelectable targetSelectable)
        {
            // 1. If clicked a hex
            if (targetSelectable is TestSelectableHex hexTile)
            {
                // Check occupancy
                if (!hexTile.TryGetOccupant(out MonoBehaviour occupant))
                {
                    // Hex is empty -> move there
                    TargetHex = hexTile;
                    SetState(UnitState.Moving);
                    return;
                }
                
                // Hex is occupied -> check if it's a unit
                if (occupant is BaseUnit targetUnit)
                {
                    if (TryHandleUnitTarget(targetUnit))
                        return;
                }
                
                // Hex has a building -> cannot move there
                Debug.Log($"Hex {hexTile.name} was occupied (not unit) " +
                          $"(ideally there is a building). Cannot move here.");
            }
            // 2. If clicked a unit directly
            else if (targetSelectable is BaseUnit targetUnit)
            {
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
            if (TargetHex == null)
            {
                SetState(UnitState.Idle);
                return;
            }
            
            Vector3 targetPos = TargetHex.transform.position;
            
            TryMoveTowards(targetPos);

            // if significantly close enough to target -> reached
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                // Clear hex we came from occupant
                if (CurrentHex != null)
                    CurrentHex.TryClearUnitOccupant(this);
                
                // set us as occupant of new hex
                if (TargetHex.TrySetUnitOccupant(this))
                    CurrentHex = TargetHex;
                else
                    Debug.LogError($"Unit {name} reached hex {TargetHex.name} " +
                                   $"but could not set self as occupant.");
                
                // clear target and set to idle
                TargetHex = null;
                SetState(UnitState.Idle);
            }
        }

        protected virtual void HandleEngaging()
        {
            if (_targetEnemy == null)
            {
                SetState(UnitState.Idle);
                return;
            }
            
            TestSelectableHex enemyHex = _targetEnemy.CurrentHex;
            
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
            float distance = Vector3.Distance(transform.position, _targetEnemy.transform.position);
            
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
            
            FaceTarget(_targetEnemy.transform.position);
            
            // get distance to enemy
            float distance = Vector3.Distance(transform.position, _targetEnemy.transform.position);
            
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

            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            FaceTarget(targetPos);
        }
        
        protected void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > Mathf.Epsilon)
                transform.rotation = Quaternion.LookRotation(dir);
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
        

        protected virtual void OnDrawGizmos()
        {
            // Draw a cyan line showing the forward direction
            Gizmos.color = Color.cyan;

            Vector3 start = transform.position + Vector3.up * 0.1f;
            Vector3 end = start + transform.forward * 1.5f;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.05f);
        }
    }
}