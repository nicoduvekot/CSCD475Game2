using UnityEngine;
using TMPro;

namespace Core.UIElements
{
    public class UnitStateDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        
        private IDisplayStateUI _source;
        private Transform _followTarget;
        private Camera _mainCamera;
        
        public void Initialize(IDisplayStateUI source, Transform followTarget)
        {
            _source = source;
            _followTarget = followTarget;

            _mainCamera = Camera.main;
        }
        
        public void SetText(string text)
        {
            label.text = text;
        }
        
        private void Update()
        {
            if (_source == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            label.text = _source.StateLabel;
            
            transform.position = _followTarget.position + Vector3.up * 2f;
            
            if (_mainCamera != null)
                transform.forward = _mainCamera.transform.forward;
        }
    }
}