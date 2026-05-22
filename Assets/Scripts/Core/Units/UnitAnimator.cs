using System;
using UnityEngine;

namespace Units
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private BaseUnit _owner;

        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int HurtTrigger =  Animator.StringToHash("Hurt");
        private static readonly int DeathTrigger =  Animator.StringToHash("Death");

        private void Start()
        {
            _owner = GetComponentInParent<BaseUnit>();
        }

        public void SetAttacking(bool value)
        {
            animator.SetBool(IsAttacking, value);
        }
        
        public void SetWalking(bool walking)
        {
            animator.SetBool(IsWalkingHash, walking);
        }

        public void PlayHurt()
        {
            animator.SetTrigger(HurtTrigger);
        }

        public void PlayDeath()
        {
            animator.SetTrigger(DeathTrigger);
        }

        public void DeathAnimationCompleted()
        {
            _owner?.OnDeathAnimationCompleted();
        }
    }
}