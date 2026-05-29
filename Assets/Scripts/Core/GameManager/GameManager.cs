using UnityEngine;
using TMPro;
using Units;       // Imports the name space for the enum types. 
using System.Collections.Generic;
using Resource; // Imports the name space for the enum types for Resources
using System;

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

    // Varable used for resource cap
    private int resourceCap = 2000;

    // Used for event update for RL training
    public event Action<UnitOwner> OnGameEnded;

    // Variables for time situation
    [SerializeField] private float timeRemaining = 0;
    private float gameTime = 1200;
    private bool paused = true;
    private float updateResourceInterval = 1f;  // used for counting the time it needs for each update, change this if you want to make it faster or slower
    private float resourceUpdateTimer = 0f;     // variable for counting time

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timeRemaining = gameTime;
        start();
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
            if(pScore > eScore)
            {
                gameOver(UnitOwner.Enemy);
            }
            else
            {
                gameOver(UnitOwner.Player);
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

            // Used to make sure the value doesn't go above the cap
            if (player[type] > resourceCap)
            {
                player[type] = resourceCap;
            }
        }
        else if (owner == UnitOwner.Enemy)
        {
            enemy[type] += (int) (amount * techController.Instance.getResourcesModifier(UnitOwner.Enemy));

            // Used to make sure the value doesn't go above the cap
            if(enemy[type] > resourceCap)
            {
                enemy[type] = resourceCap;
            }
        }

        // Used so display works better
        displayResources();
    }

    // Get resource percentage for RL agent
    public double getResourcePercentage(UnitOwner owner, int type)
    {
        if(type < 0 || type > 2)
        {
            Debug.Log("Incorect value for type was passed to getResourcePercentage");
            return 0;
        }

        if(owner == UnitOwner.Player)
        {
            return (double) (player[type] / resourceCap);
        }
        else if(owner == UnitOwner.Enemy)
        {
            return (double) (enemy[type] / resourceCap);
        }
        else
        {
            Debug.Log("Incorect value for owner was passed to getResourcePercentage");
            return 0;

        }
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

    // Resets the game, used for RL training
    public void reset()
    {
        pause();
        techController.Instance.reset();

        for (int i = 0; i < 3; i++)
        {
            player[i] = 0;
            enemy[i] = 0;
        }
        pScore = 0;
        eScore = 0;
        setTime((int) gameTime);
        start();
    }


    // Generates the resources every generation time
    public void GenerateResources()
    {
        List<BuildingScript> buildings = MapGenerateScript.getBuildingList();
        int PlayerScore = 0;
        int EnemyScore = 0;

        // This is for the base resources that is given while you have your capital. The passed in number is a magic number as of right now.
        for(int i = 0; i < 3; i++)
        {
            addResource(UnitOwner.Player, i, 10);
            addResource(UnitOwner.Enemy, i, 10);
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

    public void gameOver(UnitOwner owner)
    {
        if (owner == UnitOwner.Player)
        {
            Debug.Log("Player won the game!");
        }
        else if(owner == UnitOwner.Enemy)
        {
            Debug.Log("Enemy won the game!");
        }
        else
        {
            Debug.Log("UnitOwner must be player or enemy for losing the game");
        }

        // Used for RL
        OnGameEnded?.Invoke(owner);
    }

    public void gameSpeed(float speed)
    {
        Time.timeScale = speed;
    }
    
    public int GetFood(UnitOwner owner)  => GetResourceAmount(owner, 0);
    public int GetIron(UnitOwner owner)  => GetResourceAmount(owner, 1);
    public int GetWood(UnitOwner owner)  => GetResourceAmount(owner, 2);
    
    private int GetResourceAmount(UnitOwner owner, int type) 
        => owner == UnitOwner.Player ? player[type] : enemy[type];
}
