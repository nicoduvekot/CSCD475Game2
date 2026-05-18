using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Objects for timer/UI
    [SerializeField] private TMP_Text timer;
    [SerializeField] private TMP_Text playerResource1;
    [SerializeField] private TMP_Text playerResource2;
    [SerializeField] private TMP_Text playerResource3;
    [SerializeField] private TMP_Text enemyResource1;
    [SerializeField] private TMP_Text enemyResource2;
    [SerializeField] private TMP_Text enemyResource3;

    // Objects for player and enemy resource totals
    // Represent the three resources one can have
    private int[] player = new int[] { 0, 0, 0 };
    private int[] enemy = new int[] { 0, 0, 0 };

    // Variables for time situation
    [SerializeField] private float TimeRemaining = 1200;
    private bool paused = true;

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
        if (!paused && TimeRemaining > 0)
        {
            TimeRemaining -= Time.deltaTime;
            DisplayTime(timeRemaining);
        }
        else if (!paused)
        {
            // end of the game, code for this is reqired for that

        }
    }

    // Sets the base resources for both the player and enemy to the values.
    public void SetBaseResources(int a, int b, int c)
    {
        player[0] = a; player[1] = b; player[2] = c;
        enemy[0]  = a; enemy[1]  = b; enemy[2]  = c;
    }

    // Adds resources of the type 
    public void AddResource(int type, int amount)
    {
        if(type > 2 || type < 0)
        {
            return;
        }

        player[type] = amount + player[type];
    }

    private void DisplayResources()
    {
        playerResource1.text = player[0];
        playerResource2.text = player[1];
        playerResource3.text = player[2];
    }

    private void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Can be called to set the time for the game
    public void setTime(int seconds)
    {
        TimeRemaining = seconds;
    }

    // Pause function, can be called to stop the game
    public void pause()
    {
        paused = true;
    }

    // Unpauses the game.
    public void start()
    {
        paused = false;
    }
}
