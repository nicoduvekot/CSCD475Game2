using UnityEngine;

namespace Units
{
    public class UnitAnimator : MonoBehaviour
    {
        private Animator _animator;
        private BaseUnit _unit;
        
        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed"); 
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _unit = GetComponentInParent<BaseUnit>();
        }

        #region Walking Logic Operations

        public void SetWalking(bool value)
        {
            _animator.SetBool(IsWalking, value);
        }

        #endregion

        #region Attack Logic Operations

        public void SetAttacking(bool value)
        {
            _animator.SetBool(IsAttacking, value);
        }

        public void SetAttackSpeed(float value)
        {
            _animator.SetFloat(AttackSpeed, value);
        }

        public void OnAttackAnimationHit()
        {
            _unit.OnAttackHit();
        }

        #endregion
        
        #region Death Logic Operations
        
        public void TriggerDeath()
        {
            _animator.SetTrigger(DeathTrigger);
        }
        
        public void DeathAnimationCompleted()
        {
            _unit.OnDeathAnimationCompleted();
        }
        
        #endregion
    }
}