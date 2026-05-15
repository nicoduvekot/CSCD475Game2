using UnityEngine;
using TMPro;

namespace Core.UIElements
{
    public class StateDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        
        private Transform _followTarget;
        private Camera _mainCamera;
        
        public void Initialize(Transform followTarget)
        {
            _followTarget = followTarget;
            _mainCamera = Camera.main;
        }
        
        public void SetText(string text)
        {
            label.text = text;
        }
        
        private void Update()
        {
            if (_followTarget == null)
                return;
            
            transform.position = _followTarget.position + Vector3.up * 2f;
            
            if (_mainCamera != null)
                transform.forward = _mainCamera.transform.forward;
        }
    }
}