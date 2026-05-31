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

    // privately gets set within GameOver flag
    // next frame does the reset,
    // frame after we play again like a new game started.
    public bool ResetNextFrame { get; private set; }

    // Varable used for resource cap
    private int resourceCap = 2000;

    // Used for event update for RL training
    public event Action<UnitOwner> OnGameEnded;

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
        timeRemaining = gameTime;
        start();
    }

    void Start()
    {
        SetupGameManagerTracking();
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

    void LateUpdate()
    {
        if (!ResetNextFrame) return;
        
        reset();
        ResetNextFrame = false;
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
            player[type] += (int) (amount * TechController.Instance.getResourcesModifier(UnitOwner.Player));

            // Used to make sure the value doesn't go above the cap
            if (player[type] > resourceCap)
            {
                player[type] = resourceCap;
            }
        }
        else if (owner == UnitOwner.Enemy)
        {
            enemy[type] += (int) (amount * TechController.Instance.getResourcesModifier(UnitOwner.Enemy));

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
    public float getResourcePercentage(UnitOwner owner, int type)
    {
        if(type < 0 || type > 2)
        {
            Debug.Log("Incorect value for type was passed to getResourcePercentage");
            return 0;
        }

        if(owner == UnitOwner.Player)
        {
            return ((float)player[type] / resourceCap);
        }
        else if(owner == UnitOwner.Enemy)
        {
            return ((float)enemy[type] / resourceCap);
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
        TechController.Instance.reset();

        DestroyAllUnits();
        
        foreach (BuildingScript building in _allBuildings)
        {
            if (building == _playerCapital)
                building.ResetBuilding(UnitOwner.Player);
            else if (building == _enemyCapital)
                building.ResetBuilding(UnitOwner.Enemy);
            else
                building.ResetBuilding(UnitOwner.World);
        }

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
        int PlayerScore = 0;
        int EnemyScore = 0;

        // This is for the base resources that is given while you have your capital. The passed in number is a magic number as of right now.
        for(int i = 0; i < 3; i++)
        {
            addResource(UnitOwner.Player, i, 10);
            addResource(UnitOwner.Enemy, i, 10);
        }

        for (int i = 0; i < _allBuildings.Count; i++)
        {
            ResourceType temp = _allBuildings[i].getResource();
            UnitOwner owner = _allBuildings[i].getOwner();

            if (_allBuildings[i].getOwner() == UnitOwner.Player || _allBuildings[i].getOwner() == UnitOwner.Enemy)
            {
                switch (temp)
                {
                    case ResourceType.Food:
                        addResource(owner, 0, _allBuildings[i].resourceGeneration);
                        break;
                    case ResourceType.Iron:
                        addResource(owner, 1, _allBuildings[i].resourceGeneration);
                        break;
                    case ResourceType.Wood:
                        addResource(owner, 2, _allBuildings[i].resourceGeneration);
                        break;
                }
            }

            if(_allBuildings[i].getOwner() == UnitOwner.Player)
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

        // Used for RL please ensure reset happens AFTER the event call, thanks - nico
        // currently this happens because LateUpdate actually does the reset
        OnGameEnded?.Invoke(owner);
        
        // flag for late update to do the game reset
        ResetNextFrame = true;
    }

    // Used to set the game speed. Mostly done for RL
    // Time scale is based around normal being 1.0
    public void gameSpeed(float speed)
    {
        Time.timeScale = speed;
    }

    public float GetTimeNormalized() => timeRemaining / gameTime;

    public int GetFood(UnitOwner owner)  => GetResourceAmount(owner, 0);
    public int GetIron(UnitOwner owner)  => GetResourceAmount(owner, 1);
    public int GetWood(UnitOwner owner)  => GetResourceAmount(owner, 2);
    
    private int GetResourceAmount(UnitOwner owner, int type) 
        => owner == UnitOwner.Player ? player[type] : enemy[type];

    #region Game Tracking Operations
    
    // all building in game (cap = 9 : for 9 possible buildings)
    private List<BuildingScript> _allBuildings = new(9);
    // all spawn tiles in game (cap = 18 : for 3 spawn buildings x 6 tiles each)
    private readonly List<TileScript> _allSpawnTiles = new(18);
    // reference to the player capital building
    private BuildingScript _playerCapital;
    // reference to the enemy capital building
    private BuildingScript _enemyCapital;
    // list of all the units in the game (only add cap if there ever is a unit cap (then x per team))
    private readonly List<BaseUnit> _allUnits = new();

    /// <summary>
    /// Called at start in order to set up GameManager tracking references
    ///
    /// Author : Nico
    ///
    /// Gets all buildings in the game,
    /// stores as <see cref="_allBuildings"/>
    /// 
    /// for all buildings, also stores both capitals as:
    /// <see cref="_playerCapital"/> for the capital who player owns
    /// <see cref="_enemyCapital"/> for the capital the enemy owns
    ///
    /// Stores reference to all spawn tiles as:
    /// <see cref="_allSpawnTiles"/>
    /// so that GameManger can subscribe to unit spawning
    /// See: <see cref="RegisterSpawnTiles"/>
    /// </summary>
    private void SetupGameManagerTracking()
    {
        // clear units
        _allUnits.Clear();
        _allSpawnTiles.Clear();
        
        _allBuildings = MapGenerateScript.getBuildingList();

        foreach (BuildingScript building in _allBuildings)
        {
            // get the buildings resource
            ResourceType buildingResource = building.getResource();

            // if not resource type fort, skip (only resource type fort can spawn)
            if (buildingResource != ResourceType.Fort) continue;
            
            // we need to track capitals
            if (building.isCapital)
            {
                if (building.getOwner() == UnitOwner.Player) 
                    _playerCapital = building;

                if (building.getOwner() == UnitOwner.Enemy) 
                    _enemyCapital = building;
            }
            // capitals and forts can spawn units
            RegisterSpawnTiles(building);
        }
    }
    
    /// <summary>
    /// Used to Register the spawn tiles of fort buildings
    /// Called by <see cref="SetupGameManagerTracking"/>
    ///
    /// Author : Nico
    ///
    /// For the passed in building,
    /// Will add each tile around it as a spawn tile
    /// and subscribe to that tiles OnUnitCreated event
    ///
    /// so that each spawn event can call <see cref="HandleUnitCreated"/>
    /// </summary>
    /// <param name="building">
    /// The Building for which we need to get SpawnTiles for
    /// This operation will use any building passed in, but prefer to:
    /// Only give it a building that can actually spawn units
    /// </param>
    private void RegisterSpawnTiles(BuildingScript building)
    {
        // for each neighbor tile
        foreach (TileScript tile in building.GetNeighbourTiles())
        {
            // if we already registerd this tile, skip
            if (_allSpawnTiles.Contains(tile)) 
                continue;
                
            // add the tile and subscribe
            _allSpawnTiles.Add(tile);
            tile.OnUnitCreated += HandleUnitCreated;
        }
    }
    
    /// <summary>
    /// To be called by unit creation event
    ///
    /// Author : Nico
    ///
    /// We register to a tiles unit creation event in:
    /// <see cref="RegisterSpawnTiles"/>
    /// Allowing us to know when a unit was spawned
    ///
    /// This adds the unit to unit tracking, so we can destroy every unit on game reset
    /// </summary>
    /// <param name="unit">
    /// The unit that was created, passed in by the event
    /// </param>
    private void HandleUnitCreated(BaseUnit unit)
    {
        // safety bail
        if (unit == null)
            return;

        // already in list bail
        if (_allUnits.Contains(unit))
            return;
        
        // add unit and subscribe
        _allUnits.Add(unit);
        unit.OnUnitDeath += HandleUnitDeath;
    }
    
    /// <summary>
    /// Called when a unit dies in game
    ///
    /// Author : Nico
    ///
    /// NOTE: Unit handles destruction here
    /// We handle removal from list and unsubscription
    /// </summary>
    /// <param name="unit">
    /// The Unit that died, passed in by unit on death event
    /// <see cref="Units.BaseUnit.OnUnitDeath"/>
    /// </param>
    private void HandleUnitDeath(BaseUnit unit)
    {
        // safety bail
        if (unit == null)
            return;
        
        // unsubscribe! and remove
        unit.OnUnitDeath -= HandleUnitDeath;
        _allUnits.Remove(unit);
    }
    
    // Destroys all units in '_allUnits' list
    // Author Nico, sorry I didn't summary this one,
    // Realized I was getting carried away summarizing them as this stage
    private void DestroyAllUnits()
    {
        // backwards iteration just so we could report errors at specific unit
        for (int i = _allUnits.Count - 1; i >= 0; i--)
        {
            BaseUnit unit = _allUnits[i];

            if (unit == null)
            {
                Debug.LogWarning("[GameManager] Null unit found during reset");
                _allUnits.RemoveAt(i);
                continue;
            }

            DestroyUnit(unit);
        }
    }

    // Destroys the passed in unit
    // Author Nico, sorry I didn't summary this one,
    // Realized I was getting carried away summarizing them as this stage
    private void DestroyUnit(BaseUnit unit)
    {
        // safety bail
        if (unit == null)
            return;
        
        // unsubscribe!
        unit.OnUnitDeath -= HandleUnitDeath;
        
        // remove the unit
        _allUnits.Remove(unit);
        
        // destroy the unit
        unit.MarkForDestruction();
    }

    #endregion
    
}
