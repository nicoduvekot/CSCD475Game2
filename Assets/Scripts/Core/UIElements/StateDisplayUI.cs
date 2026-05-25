using UnityEngine;
using TMPro;

namespace Core.UIElements
{
    public class StateDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Transform followTarget;
        [SerializeField] private float verticalOffset = 0.3f;
        
        private Camera _mainCamera;
        
        private void Start()
        {
            _mainCamera = Camera.main;
        }
        
        public void SetText(string text)
        {
            label.text = text;
        }
        
        private void LateUpdate()
        {
            if (followTarget != null)
                transform.position = followTarget.position + followTarget.up * verticalOffset;
            
            if (_mainCamera != null)
                transform.forward = _mainCamera.transform.forward;
        }
        
        // to be used by perspective visibility
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}