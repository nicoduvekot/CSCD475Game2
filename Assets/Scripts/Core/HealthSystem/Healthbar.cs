using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HealthSystem
{
    public class Healthbar : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Transform followTarget;

        [Header("Settings")] 
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private bool alignWithCamera = true;
        [SerializeField, Min(0.1f)] private float changeSpeed = 100f;

        private float _currentValue;

        private Health _health;
        private Camera _mainCamera;

        private void Awake()
        {
            _health = GetComponentInParent<Health>();

            if (_health != null) return;

            Debug.LogError($"{name}: No Health component found in parent hierarchy.");
            enabled = false;
        }

        private void Start()
        {
            // redundant safeguard against NRE
            if (!enabled || _health == null) return;

            _mainCamera = Camera.main;
            _currentValue = _health.CurrentHealth;

            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnHealthEmpty += HandleHealthEmpty;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnHealthEmpty -= HandleHealthEmpty;
            }
        }

        private void Update()
        {
            // redundant NRE bail out check
            if (_health == null)
            {
                Debug.LogError($"{name}: Update tried to use NRE _health reference");
                return;
            }

            _currentValue = Mathf.MoveTowards(
                _currentValue,
                _health.CurrentHealth,
                Time.deltaTime * changeSpeed
            );

            UpdateFill();
            UpdateText();
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (followTarget != null)
                transform.position = followTarget.position;
            
            if (alignWithCamera && _mainCamera != null)
                transform.forward = _mainCamera.transform.forward;
        }

        private void UpdateFill()
        {
            if (fillImage == null) return;
            
            float value = Mathf.InverseLerp(0f, _health.MaxHealth, _currentValue);
            fillImage.fillAmount = value;
        }

        private void UpdateText()
        {
            if (healthText == null) return;
            
            int current = Mathf.CeilToInt(_health.CurrentHealth);
            int max = Mathf.CeilToInt(_health.MaxHealth);
            
            healthText.text = $"{current} / {max}";
        }

        private void UpdateVisibility()
        {
            if (canvas == null) return;
            
            bool isEmpty = Mathf.Approximately(_currentValue, 0f);
            
            if (isEmpty && hideWhenEmpty)
            {
                if (canvas.gameObject.activeSelf)
                    canvas.gameObject.SetActive(false);
            }
            else
            {
                if (canvas.gameObject.activeSelf)
                    canvas.gameObject.SetActive(true);
            }
        }

        private void HandleHealthChanged(float delta) { }
        
        private void HandleHealthEmpty() { }
    }
}