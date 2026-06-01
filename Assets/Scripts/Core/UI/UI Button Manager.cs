using UnityEngine;
using TMPro; //used for buttons
using UnityEngine.UI;

public class UIButtonManager : MonoBehaviour
{
    // Objects for buttons
    [SerializeField] private Button menuButton; // Changed to open the popup
    [SerializeField] private Button techButton;

    

    void Start()
    {
        // runtime hookup so OnClick works even if Inspector won't show the method
        if (menuButton != null && techButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(onMenuClick);

            techButton.onClick.RemoveAllListeners();
            techButton.onClick.AddListener(onTechClick);
        }
    }

    private void onMenuClick()
    {
        ClosePopup.Instance.show();
        GameManager.Instance.pause();
    }

    private void onTechClick()
    {
        techPopup.Instance.Show();
    }
}
