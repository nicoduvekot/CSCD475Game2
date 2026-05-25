using UnityEngine;
using TMPro; //used for buttons
using UnityEngine.UI;

public class UIButtonManager : MonoBehaviour
{
    // Objects for buttons
    [SerializeField] private Button exitButton;
    [SerializeField] private Button techButton;

    void Awake()
    {
        // runtime hookup so OnClick works even if Inspector won't show the method
        if (exitButton != null && techButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(onExitClick);

            techButton.onClick.RemoveAllListeners();
            techButton.onClick.AddListener(onTechClick);
        }
    }

    private void onExitClick()
    {
#if UNITY_EDITOR
        // Exits Play Mode when testing in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Closes the application in a standalone build (PC, Mac, Android, etc.)
        Application.Quit();
#endif
    }

    private void onTechClick()
    {
        techPopup.Instance.Show();
    }

}
