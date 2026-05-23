using UnityEngine;

namespace Units
{
    public class UnitAnimator : MonoBehaviour
    {
        private Animator _animator;
        private BaseUnit _unit;
        
        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _unit = GetComponentInParent<BaseUnit>();
        }
        
        public void SetAttacking(bool value)
        {
            _animator.SetBool(IsAttacking, value);
        }

        public void SetWalking(bool value)
        {
            _animator.SetBool(IsWalking, value);
        }

        public void TriggerDeath()
        {
            _animator.SetTrigger(DeathTrigger);
        }
        
        public void DeathAnimationCompleted()
        {
            _unit.OnDeathAnimationCompleted();
        }
    }
}