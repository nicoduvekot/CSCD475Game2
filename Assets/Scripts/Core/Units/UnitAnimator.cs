using UnityEngine;

namespace Units
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");

        public void SetAttacking(bool value)
        {
            animator.SetBool(IsAttacking, value);
        }
    }
}