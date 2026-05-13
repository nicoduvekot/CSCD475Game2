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
            if (targetSelectable is BaseUnit enemy)
            {
                _targetEnemy = enemy;
                SetState(UnitState.Engaging);
                return;
            }
            
            // handle movement from command here
        }
        
        protected virtual void Update()
        {
            switch (_state)
            {
                case UnitState.Engaging:
                    HandleEngaging();
                    break;

                case UnitState.Engaged:
                    HandleEngaged();
                    break;
                
                case UnitState.Dying:
                    
                    break;
            }
        }

        protected virtual void HandleEngaging()
        {
            if (_targetEnemy == null)
            {
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
                return;
            }

            // if out of range => move toward target
            TryMoveTowards(_targetEnemy.transform.position);
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
            //Debug.LogWarning("Unit is trying to move : This is not implemented though!");
        }
        
        protected void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > Mathf.Epsilon)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        
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