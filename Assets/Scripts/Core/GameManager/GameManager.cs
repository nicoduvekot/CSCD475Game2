using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Objects for timer/UI
    [SerializeField] private TMP_Text timer;

    // Variables for time situation
    private float TimeRemaining = 1200;
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
            //end of the game
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void setTime(int seconds)
    {
        TimeRemaining = seconds;
    }

    public void pause()
    {
        paused = true;
    }

    public void start()
    {
        paused = false;
    }
}
