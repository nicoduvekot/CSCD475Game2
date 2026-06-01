using UnityEngine;
using TMPro;
using Units;       // Imports the name space for the enum types. 
using System.Collections.Generic;
using Resource; // Imports the name space for the enum types for Resources

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
    [SerializeField] private int[] player = new int[] { 0, 0, 0 };
    [SerializeField] private int[] enemy = new int[] { 0, 0, 0 };
    private int pScore = 0;
    private int eScore = 0;

    // Variables for time situation
    [SerializeField] private float timeRemaining = 30;
    private bool paused = false;
    private float updateResourceInterval = 1f;  // used for counting the time it needs for each update
    private float resourceUpdateTimer = 0f;     // variable for counting time

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    
    void Update()
    {
        if (!paused && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            resourceUpdateTimer += Time.deltaTime;
            displayTime(timeRemaining);
            displayResources();
        }
        else if (!paused && timeRemaining <= 0)
        {
            // end of the game, code for this is reqired for that
            if(pScore >= eScore)
            {
                gameOver(UnitOwner.Player);
            }
            else
            {
                gameOver(UnitOwner.Enemy);
            }
        }

        if(resourceUpdateTimer >= updateResourceInterval)
        {
            // Calls resource generator
            GenerateResources();

            // Reset the timer
            resourceUpdateTimer -= updateResourceInterval;
        }
    }

    // Sets the base resources for both the player and enemy to the values.
    public void setBaseResources(int a, int b, int c)
    {
        player[0] = a; player[1] = b; player[2] = c;
        enemy[0]  = a; enemy[1]  = b; enemy[2]  = c;
    }

    // Adds resources of the type using 0 index
    // Might need to change depending on 
    public void addResource(UnitOwner owner, int type, int amount)
    {
        if(type > 2 || type < 0)
        {
            return;
        }

        if (owner == UnitOwner.Player)
        {
            player[type] += (int) (amount * techController.Instance.getResourcesModifier(UnitOwner.Player));
        }
        else if (owner == UnitOwner.Enemy)
        {
            enemy[type] += (int) (amount * techController.Instance.getResourcesModifier(UnitOwner.Enemy));
        }

        // Used so display works better
        displayResources();
    }

    //spends resources of the amounts
    public bool spendResources(UnitOwner owner, int a, int b, int c)
    {
        if (owner == UnitOwner.Player)
        {
            // Only triggers if there are not enough
            if (a > player[0] || b > player[1] || c > player[2])
            {
                return false;
            }
            else
            {
                player[0] -= a;
                player[1] -= b;
                player[2] -= c;

                // Used so display works better
                displayResources();

                return true;
            }
        }
        else if (owner == UnitOwner.Enemy)
        {
            // Only triggers if there are not enough
            if (a > enemy[0] || b > enemy[1] || c > enemy[2])
            {
                return false;
            }
            else
            {
                enemy[0] -= a;
                enemy[1] -= b;
                enemy[2] -= c;

                // Used so display works better
                displayResources();

                return true;
            }
        }

        return false;
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


    // Generates the resources every generation time
    public void GenerateResources()
    {
        List<BuildingScript> buildings = MapGenerateScript.getBuildingList();
        int PlayerScore = 0;
        int EnemyScore = 0;

        // 
        for(int i = 0; i < 3; i++)
        {
            addResource(UnitOwner.Player, i, 5);
            addResource(UnitOwner.Enemy, i, 5);
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            ResourceType temp = buildings[i].getResource();
            UnitOwner owner = buildings[i].getOwner();

            if (buildings[i].getOwner() == UnitOwner.Player || buildings[i].getOwner() == UnitOwner.Enemy)
            {
                switch (temp)
                {
                    case ResourceType.Food:
                        addResource(owner, 0, buildings[i].resourceGeneration);
                        break;
                    case ResourceType.Iron:
                        addResource(owner, 1, buildings[i].resourceGeneration);
                        break;
                    case ResourceType.Wood:
                        addResource(owner, 2, buildings[i].resourceGeneration);
                        break;
                }
            }

            if(buildings[i].getOwner() == UnitOwner.Player)
            {
                PlayerScore += 100;
                EnemyScore += 100;
            }
        }

        pScore = PlayerScore;
        eScore = EnemyScore;
    }

    // Used to end the game. Calls the GameOverPopup to end the game
    public void gameOver(UnitOwner owner)
    {
        if (owner == UnitOwner.Player)
        {
            Debug.Log("Player won the game!");
            pause();
            GameoverPopup.Instance.show(UnitOwner.Player); // Line 241
        }
        else if(owner == UnitOwner.Enemy)
        {
            Debug.Log("Enemy won the game!");
            pause();
            GameoverPopup.Instance.show(UnitOwner.Enemy);
        }
        else
        {
            Debug.Log("UnitOwner must be player or enemy for losing the game");
        }
    }

    // Used to set the game speed. Mostly done for RL
    // Time scale is based around normal being 1.0
    public void gameSpeed(float speed)
    {
        Time.timeScale = speed;
    }

    public int getPScore()
    {
        return pScore;
    }

    public int getEScore()
    {
        return eScore;
    }

    // Used to get the total time that is left
    public float getTimeLeft()
    {
        return timeRemaining;
    }
}
