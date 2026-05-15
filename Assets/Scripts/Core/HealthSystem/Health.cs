using UnityEngine;
using UnityEngine.Events;

namespace HealthSystem
{
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("The max amount of health this unit has")]
        [SerializeField] private float maxHealth = 100f;
        
        public UnityAction<float> OnHealthChanged { get; set; }
        public UnityAction OnHealthEmpty { get; set; }
        
        /// <summary>
        /// Gets or sets the max amount of health
        /// </summary>
        public float MaxHealth
        {
            get => maxHealth;
            // max health can not be negative
            set => maxHealth = Mathf.Max(1f, value);
        }
        
        /// <summary>
        /// Gets the current amount of health
        /// </summary>
        public float CurrentHealth { get; private set; }
        
        /// <summary>
        /// Returns true if the current amount of health is greater than zero
        /// </summary>
        public bool IsAlive => CurrentHealth > 0f;
        
        private void Awake()
        {
            SetHealth(maxHealth);
        }
        
        private void SetHealth(float value)
        {
            float previous = CurrentHealth;

            CurrentHealth = Mathf.Clamp(value, 0f, MaxHealth);

            float delta = CurrentHealth - previous;

            if (Mathf.Abs(delta) > 0f)
                OnHealthChanged?.Invoke(delta);

            if (CurrentHealth <= 0f)
                OnHealthEmpty?.Invoke();
        }
        
        /// <summary>
        /// Sets max and current health without firing events.
        /// Used for initialization only.
        /// </summary>
        public void InitializeHealth(float max)
        {
            maxHealth = Mathf.Max(1f, max);
            CurrentHealth = maxHealth;
        }
        
        /// <summary>
        /// Applies the given amount of damage
        /// </summary>
        /// <param name="amount"></param>
        public void ApplyDamage(float amount)
        {
            if (!IsAlive) return;

            SetHealth(CurrentHealth - amount);
        }
        
        /// <summary>
        /// Adds the given amount of health
        /// </summary>
        /// <param name="amount"></param>
        public void AddHealth(float amount)
        {
            if (!IsAlive) return;

            SetHealth(CurrentHealth + amount);
        }

        /// <summary>
        /// Changes the max health, only adjusting current if current > new max
        /// </summary>
        /// <param name="amount"></param>
        public void ChangeMaxHealth(float amount)
        {
            float oldMax = MaxHealth;
            float newMax = oldMax + amount;
            
            if (newMax < 1f)
            {
                Debug.LogError($"{name}: Attempted to reduce MaxHealth below 1. Clamping to 1.");
                newMax = 1f;
            }
            
            MaxHealth = newMax;
            
            if (CurrentHealth > newMax)
            {
                float previous = CurrentHealth;
                CurrentHealth = newMax;

                float delta = CurrentHealth - previous;
                OnHealthChanged?.Invoke(delta);
            }
        }
    }
}