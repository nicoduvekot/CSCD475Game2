using UnityEngine;
using Units;                // Imports the name space for the enum types. 
using TMPro;                // Used for buttons
using UnityEngine.UI;       // Also used for buttons

public class GameoverPopup : MonoBehaviour
{
    public static GameoverPopup Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Button menuButton;
    [SerializeField] private Button replayButton;

    [SerializeField] private TMP_Text victoryConditionText;
    [SerializeField] private TMP_Text scoreText;

    void awake()
    {
        if (canvasGroup == null) canvasGroup = transform.Find("GameOverCanvas").GetComponent<CanvasGroup>();

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(onMenuClick);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(onReplayClick);
        }
    }

    void Start()
    {
        hide();
    }

    public void hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Shows the final popup box for the end of the game.
    // owner is for the winner of the game and 
    public void show(UnitOwner owner)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);

        scoreText.text = "Player: " + GameManager.Instance.getPScore() +"\nEnemy: " + GameManager.Instance.getEScore();

        if(owner == UnitOwner.Player)
        {
            // Game must be over from losing your capital
            if(GameManager.Instance.getTimeLeft() > 1)
            {
                victoryConditionText.text = "You took the enemy capital!";
            }
            // Game must be over from time
            else
            {
                victoryConditionText.text = "You ended the game with more points.";
            }
        }
        else   // Must be enemy
        {
            // Game must be over from losing your capital
            if(GameManager.Instance.getTimeLeft() > 1)
            {
                victoryConditionText.text = "Your capital was captured";
            }
            // Game must be over from time
            else
            {
                victoryConditionText.text = "The enemy had more points then you";
            }
        }
    }

    private void onMenuClick()
    {
        // Needs to return to the menu

    }

    private void onReplayClick()
    {
        // Needs to allow to replay the game

    }

}
