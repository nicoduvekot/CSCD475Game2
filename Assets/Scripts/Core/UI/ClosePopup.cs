using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; //used for buttons
using UnityEngine.UI;

public class ClosePopup : MonoBehaviour
{
    public static ClosePopup Instance { get; private set; }

    [SerializeField] private CanvasGroup popup;

    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (yesButton != null && noButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(onYesClick);

            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(onNoClick);
        }

        hide();
    }

    private void hide()
    {
        popup.alpha = 0f;
        popup.interactable = false;
        popup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void show()
    {
        popup.alpha = 1f;
        popup.interactable = true;
        popup.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    private void onNoClick()
    {
        hide();
        GameManager.Instance.start();
    }

    private void onYesClick()
    {
        SceneManager.LoadScene(0);
    }
}
