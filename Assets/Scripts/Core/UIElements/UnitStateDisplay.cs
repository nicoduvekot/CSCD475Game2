using UnityEngine;
using TMPro;

namespace Core.UIElements
{
    public class UnitStateDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        
        private IProgressSource _source;
        private Transform _followTarget;
        private Camera _uiCamera;
        
        public void Initialize(IProgressSource source, Transform followTarget)
        {
            _source = source;
            _followTarget = followTarget;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _uiCamera = canvas.worldCamera;

            //gameObject.SetActive(false);
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
            
            if (_source.IsInProgress)
            {
                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);

                label.text = _source.ProgressLabel;
            }
            else
            {
                label.text = "idle";
            }
            
            transform.position = _followTarget.position + Vector3.up * 2f;
            
            transform.forward = _uiCamera != null ? _uiCamera.transform.forward : Camera.main.transform.forward;
        }
    }
}