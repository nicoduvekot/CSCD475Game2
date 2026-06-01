using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;          // used for buttons
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button tutorialButton;

    void awake()
    {
        
    }

    void Start() 
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(onExitClick);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(onNewGameClick);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(onTutorialClick);
        }
    }

    // Closes the game out when putton is selected
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

    // This loads the new game scene
    private void onNewGameClick()
    {
        SceneManager.LoadScene(1);
    }

    private void onTutorialClick()
    {
        // Load scene from tutorial
        SceneManager.LoadScene(2);
    }
}
