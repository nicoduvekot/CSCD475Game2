using UnityEngine;

namespace Units
{
    /// <summary>
    /// Pure data values here
    ///
    /// Intended to be base values for any prefab unit to use
    /// Modifiers to this will be applied elsewhere
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitStats : MonoBehaviour
    {
        [Header("Base Stats (before modifiers)")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseMoveSpeed = 4f;
        
        [Tooltip("Base attack damage value")]
        [SerializeField] private float baseAttackPower = 10f;
        [Tooltip("Base attack per second value")]
        [SerializeField] private float baseAttackSpeed = 1f;
        [Tooltip("Base attack range")]
        [SerializeField] private float baseAttackRange = 1.5f;
        

        public float BaseMaxHealth => baseMaxHealth;
        public float BaseMoveSpeed => baseMoveSpeed;
        
        public float BaseAttackPower => baseAttackPower;
        public float BaseAttackSpeed => baseAttackSpeed;
        public float BaseAttackRange => baseAttackRange;
    }
}