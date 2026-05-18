using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Objects for timer/UI
    [SerializeField] private TMP_Text timer;
    [SerializeField] private TMP_Text playerScore;
    [SerializeField] private TMP_Text playerResource1;
    [SerializeField] private TMP_Text playerResource2;
    [SerializeField] private TMP_Text playerResource3;

    // Objects for player and enemy resource totals
    // Represent the three resources one can have
    private int[] player = new int[] { 0, 0, 0 };
    private int[] enemy = new int[] { 0, 0, 0 };
    private int pScore = 0;
    private int eScore = 0;

    // Variables for time situation
    [SerializeField] private float timeRemaining = 1200;
    private bool paused = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    
    void Update()
    {
        if (!paused && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            displayTime(timeRemaining);
            displayResources();
        }
        else if (!paused)
        {
            // end of the game, code for this is reqired for that

        }
    }

    // Sets the base resources for both the player and enemy to the values.
    public void setBaseResources(int a, int b, int c)
    {
        player[0] = a; player[1] = b; player[2] = c;
        enemy[0]  = a; enemy[1]  = b; enemy[2]  = c;
    }

    // Adds resources of the type 
    // Might need to change depending on 
    public void addResource(int type, int amount)
    {
        if(type > 2 || type < 0)
        {
            return;
        }

        player[type] = amount + player[type];
    }

    //spends resources of the amounts
    public bool spendResources(int a, int b, int c)
    {
        // Only triggers if there are not enough
        if (a < player[0] || b < player[1] || c < player[2])
        {
            return false;
        }
        else
        {
            player[0] =+ a;
            player[1] -= b;
            enemy[0] -= c;
            return true;
        }
    }

    private void displayResources()
    {
        playerResource1.text = player[0] + "";
        playerResource2.text = player[1] + "";
        playerResource3.text = player[2] + "";
        playerScore.text = pScore + "";
    }

    private void displayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Can be called to set the time for the game
    public void setTime(int seconds)
    {
        timeRemaining = seconds;
    }

    // Pause function, can be called to stop the game
    // Sets time scale to 0 so anything based on delta time doesn't update
    public void pause()
    {
        Time.timeScale = 0f;
        paused = true;
    }

    // Unpauses the game.
    // Sets the time scale to 1 so the game functions as normal again.
    public void start()
    {
        paused = false;
        Time.timeScale = 1f;
    }
}
