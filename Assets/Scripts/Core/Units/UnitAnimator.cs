using UnityEngine;

namespace Units
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

        public void SetAttacking(bool value)
        {
            animator.SetBool(IsAttacking, value);
        }
        
        public void SetWalking(bool walking)
        {
            animator.SetBool(IsWalkingHash, walking);
        }
    }
}