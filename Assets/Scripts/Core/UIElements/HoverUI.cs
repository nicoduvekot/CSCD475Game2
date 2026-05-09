using UnityEngine;
using TMPro;

namespace Core.UIElements
{
    public class HoverUI : MonoBehaviour
    {
        public static HoverUI Instance { get; private set; }
        
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text text;
        
        private void Awake()
        {
            Debug.Log("Awake is called by Hover UI");
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Show(string message)
        {
            text.text = message;
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }
    }
}