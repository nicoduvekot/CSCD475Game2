using System.Collections.Generic;
using Resource;
using Units;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

namespace RL_Agent
{
    public class TestAgent : Agent
    {
        // change this in inspector to set "team" of the agent
        public UnitOwner team;

        // parent object of all the tiles
        public Transform tilesParent;
        
        // cache field for gameManager
        private GameManager _gameManager;
        
        private bool _episodeResetsGame;
        
        [HideInInspector]
        public int soldierUnitCost = 100;
        [HideInInspector]
        public int archerUnitCost = 100;
        [HideInInspector]
        public int horsemanUnitCost = 100;
        
        // this will run after OnEnables and before Start unity functions
        public override void Initialize()
        {
            
        }

        // this is where we define each episode start definition
        public override void OnEpisodeBegin()
        {
            SetDifficulty();
            
            _gameManager = GameManager.Instance;
            _gameManager.OnGameEnded += HandleGameEnded;
            
            if (_episodeResetsGame)
            {
                _gameManager.reset();
                _episodeResetsGame = false;
            }

            CacheBuildingReferences();
            CacheWalkableTiles();
        }

        private void OnDestroy()
        {
            _gameManager.OnGameEnded -= HandleGameEnded;
        }

        // this is where we design state knowledge
        public override void CollectObservations(VectorSensor sensor)
        {
            ObserveBuildingOwnership(sensor);   // 9 sensors
            ObserveUnitCount(sensor);           // 3 sensors
            ObserveResources(sensor);           // 3 sensors
            ObserveTime(sensor);                // 1 sensor
        }
        
        // called before agent choose action, hides these from option map
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            CheckRecruitmentMasks(actionMask);
            //CheckMovementMasks(actionMask);
        }
        
        // this is where we design action space
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (DifficultyCooldownActive())
                return;
            
            int recruitmentAction = actions.DiscreteActions[0];
            HandleRecruitmentAction(recruitmentAction);
            
            int movementAction = actions.DiscreteActions[1];
            HandleMovementAction(movementAction);
        }

        #region Movement Action Logic

        //  branch 1 => 1 + 1 action  
        private void HandleMovementAction(int movementAction)
        {
            switch (movementAction)
            {
                case 1:
                    TileScript target = GetRandomFogTile();
                    UnitExploreTarget(target);
                    break;
            }
        }

        private bool IsFogForTeam(TileScript tile)
        {
            return team switch
            {
                UnitOwner.Player => tile.fogForPlayer,
                UnitOwner.Enemy => tile.fogForEnemy,
                _ => false
            };
        }

        private TileScript GetRandomFogTile()
        {
            List<TileScript> fogTiles = _allWalkableTiles.FindAll(IsFogForTeam);
            
            if (fogTiles.Count == 0)
                return null;

            int randomIndex = Random.Range(0, fogTiles.Count);
            return fogTiles[randomIndex];
        }

        private void UnitExploreTarget(TileScript target)
        {
            if (target == null)
                return;
            
            BaseUnit scout = GetRandomIdleUnit();
            if (scout == null)
                return;

            scout.OnCommand(target.transform.position, target);
        }

        private BaseUnit GetRandomIdleUnit()
        {
            List<BaseUnit> idleUnits = new();
            
            foreach (BaseUnit unit in _mySoldiers)
                if (unit != null && unit.IsIdle())
                    idleUnits.Add(unit);
            
            foreach (BaseUnit unit in _myArchers)
                if (unit != null && unit.IsIdle())
                    idleUnits.Add(unit);

            foreach (BaseUnit unit in _myHorsemen)
                if (unit != null && unit.IsIdle())
                    idleUnits.Add(unit);
            
            if (idleUnits.Count == 0)
                return null;
            
            int index = Random.Range(0, idleUnits.Count);
            return idleUnits[index];
        }

        #endregion
    
        #region Movement Masking

        // private void CheckMovementMasks(IDiscreteActionMask actionMask)
        // {
        //     
        // }

    #endregion
    
        #region Recruitment Action Logic

        private void HandleRecruitmentAction(int recruitmentAction)
        {
            // branch 0 => 6 + 1 action
            switch (recruitmentAction)
            {
                case 1:
                    TryRecruit(_myCapital, 0); // Soldier at capital
                    break;

                case 2:
                    TryRecruit(_myCapital, 1); // Archer at capital
                    break;

                case 3:
                    TryRecruit(_myCapital, 2); // Horseman at capital
                    break;

                case 4:
                    TryRecruit(_fortBuilding, 0); // Soldier at fort
                    break;

                case 5:
                    TryRecruit(_fortBuilding, 1); // Archer at fort
                    break;

                case 6:
                    TryRecruit(_fortBuilding, 2); // Horseman at fort
                    break;
                
                // considered a redundantly empty block
                // default:
                //     // Action 0 = do nothing
                //     break;
            }
        }

        private void TryRecruit(BuildingScript building, int type)
        {
            // NRE safety bail
            if (building == null)
                return;
            
            // ownership safety bail
            if (building.getOwner() != team)
                return;
            
            // afford the action safety bail switch
            switch (type)
            {
                case 0: // Soldier
                    if (_gameManager.GetFood(team) < soldierUnitCost)
                        return;
                    break;

                case 1: // Archer
                    if (_gameManager.GetIron(team) < archerUnitCost)
                        return;
                    break;

                case 2: // Horseman
                    if (_gameManager.GetWood(team) < horsemanUnitCost)
                        return;
                    break;
            }
            
            // retrieve list of spawn tiles for this building
            List<TileScript> spawnTiles =
                building == _myCapital ? _myCapitalCapturePoints :
                building == _fortBuilding ? _fortCapturePoints :
                null;

            // NRE safety bail
            if (spawnTiles == null)
                return;
            
            // no available space to recruit safety bail
            if (CountAvailableSpawnTiles(spawnTiles) == 0)
                return;
            
            // checks passed, do the recruitment action
            building.recruitUnit(type);
        }

        #endregion
        
        #region  Recruitment Masking

        /// <summary>
        /// This checks if we can afford units, or have space to recruit units
        /// If not, masks the action,
        /// prevents action map from choosing an action we can not do
        /// </summary>
        /// <param name="actionMask"></param>
        private void CheckRecruitmentMasks(IDiscreteActionMask actionMask)
        {
            // capital recruitment masking
            MaskRecruitmentForBuilding(
                actionMask,
                _myCapitalCapturePoints,
                soldierAction: 1,
                archerAction: 2,
                horsemanAction: 3
            );
            
            // if we don't own the fort, can't recruit there
            if (_fortBuilding.getOwner() != team)
            {
                actionMask.SetActionEnabled(0, 4, false);
                actionMask.SetActionEnabled(0, 5, false);
                actionMask.SetActionEnabled(0, 6, false);
            }
            else // fort recruitment masking
            {
                MaskRecruitmentForBuilding(
                    actionMask,
                    _fortCapturePoints,
                    soldierAction: 4,
                    archerAction: 5,
                    horsemanAction: 6
                );
            }
        }

        private void MaskRecruitmentForBuilding(
            IDiscreteActionMask actionMask,
            List<TileScript> spawnTiles,
            int soldierAction,
            int archerAction,
            int horsemanAction)
        {
            int foodAmount = _gameManager.GetFood(team);
            int ironAmount = _gameManager.GetIron(team);
            int woodAmount = _gameManager.GetWood(team);
            
            // if you don't have the resources to build a unit disable the action choice
            
            if (foodAmount < soldierUnitCost)
                actionMask.SetActionEnabled(0, soldierAction, false);
            
            if (ironAmount < archerUnitCost)
                actionMask.SetActionEnabled(0, archerAction, false);
            
            if (woodAmount < horsemanUnitCost)
                actionMask.SetActionEnabled(0, horsemanAction, false);

            // if there's no tiles available to spawn a unit, disable action choices
            if (CountAvailableSpawnTiles(spawnTiles) == 0)
            {
                actionMask.SetActionEnabled(0, soldierAction, false);
                actionMask.SetActionEnabled(0, archerAction, false);
                actionMask.SetActionEnabled(0, horsemanAction, false);
            }
        }

        private int CountAvailableSpawnTiles(List<TileScript> spawnTiles)
        {
            int count = 0;

            foreach (TileScript tile in spawnTiles)
            {
                if (tile.OccupyingUnit == null)
                    count++;
            }
            
            return count;
        }

        #endregion

        #region Building
        
        private readonly List<BuildingScript> _capitalBuildings = new(2);
        private BuildingScript _myCapital;
        private readonly List<TileScript> _myCapitalCapturePoints = new(6);
        private BuildingScript _enemyCapital;
        private readonly List<TileScript> _enemyCapitalCapturePoints = new(6);
        
        private BuildingScript _fortBuilding;
        private readonly List<TileScript> _fortCapturePoints = new(6);
        
        private readonly List<BuildingScript> _foodBuildings = new(2);
        private BuildingScript _myFoodBuilding;
        private readonly List<TileScript> _myFoodCapturePoints = new(6);
        private BuildingScript _enemyFoodBuilding;
        private readonly List<TileScript> _enemyFoodCapturePoints = new(6);
        
        private readonly List<BuildingScript> _woodBuildings = new(2);
        private BuildingScript _myWoodBuilding;
        private readonly List<TileScript> _myWoodCapturePoints = new(6);
        private BuildingScript _enemyWoodBuilding;
        private readonly List<TileScript> _enemyWoodCapturePoints = new(6);
        
        private readonly List<BuildingScript> _ironBuildings = new(2);
        private BuildingScript _topIronBuilding;
        private readonly List<TileScript> _topIronCapturePoints = new(6);
        private BuildingScript _bottomIronBuilding;
        private readonly List<TileScript> _bottomIronCapturePoints = new(6);
        
        private List<BuildingScript> _allBuildings = new(9);
        
        private readonly List<TileScript> _spawnTiles = new(12);
        
        private void CacheBuildingReferences()
        {
            _allBuildings = MapGenerateScript.getBuildingList();
            
            foreach (BuildingScript building in _allBuildings)
            {
                switch (building.getResource())
                {
                    case ResourceType.Food:
                        _foodBuildings.Add(building);
                        break;

                    case ResourceType.Wood:
                        _woodBuildings.Add(building);
                        break;

                    case ResourceType.Iron:
                        _ironBuildings.Add(building);
                        break;

                    case ResourceType.Fort:
                        if (building.isCapital)
                        {
                            _capitalBuildings.Add(building);
                        }
                        else
                        {
                            _fortBuilding = building;
                        }
                        break;
                    
                    default:
                        Debug.LogError("[Test_Agent] Unknown resource type");
                        break;
                }
            }
            
            _myCapital = _capitalBuildings.Find(x => x.getOwner() == team);
            _enemyCapital = _capitalBuildings.Find(x => x.getOwner() != team);
            
            AssignDistanceForPairs(_foodBuildings, out _myFoodBuilding, out _enemyFoodBuilding);
            AssignDistanceForPairs(_woodBuildings, out _myWoodBuilding, out _enemyWoodBuilding);
            AssignDistanceForPairs(_ironBuildings, out _topIronBuilding, out _bottomIronBuilding);
            
            PopulateCaptureTiles(_myCapital, _myCapitalCapturePoints);
            PopulateCaptureTiles(_enemyCapital, _enemyCapitalCapturePoints);
            PopulateCaptureTiles(_myFoodBuilding, _myFoodCapturePoints);
            PopulateCaptureTiles(_enemyFoodBuilding, _enemyFoodCapturePoints);
            PopulateCaptureTiles(_myWoodBuilding, _myWoodCapturePoints);
            PopulateCaptureTiles(_enemyWoodBuilding, _enemyWoodCapturePoints);
            PopulateCaptureTiles(_topIronBuilding, _topIronCapturePoints);
            PopulateCaptureTiles(_bottomIronBuilding, _bottomIronCapturePoints);
            PopulateCaptureTiles(_fortBuilding, _fortCapturePoints);
            
            RegisterSpawnTiles(_myCapital);
            RegisterSpawnTiles(_fortBuilding);
        }

        private void AssignDistanceForPairs(
            List<BuildingScript> pair,
            out BuildingScript closest,
            out BuildingScript furthest)
        {
            closest = null;
            furthest = null;
            
            float closestDist = float.MaxValue;
            float furthestDist = float.MinValue;
            
            foreach (BuildingScript b in pair)
            {
                float d = Vector3.Distance(b.transform.position, _myCapital.transform.position);

                if (d < closestDist)
                {
                    closestDist = d;
                    closest = b;
                }

                if (d > furthestDist)
                {
                    furthestDist = d;
                    furthest = b;
                }
            }
        }

        private void PopulateCaptureTiles(BuildingScript building, List<TileScript> list)
        {
            foreach (TileScript tile in building.GetNeighbourTiles())
            {
                if (!list.Contains(tile))
                    list.Add(tile);
            }
        }

        private void RegisterSpawnTiles(BuildingScript building)
        {
            foreach (TileScript tile in building.GetNeighbourTiles())
            {
                if (_spawnTiles.Contains(tile)) 
                    continue;
                
                _spawnTiles.Add(tile);
                tile.OnUnitCreated += HandleUnitCreated;
            }
        }

        private void ObserveBuildingOwnership(VectorSensor sensor)
        {
            HelpObserveBuildingOwner(sensor, _myCapital);
            HelpObserveBuildingOwner(sensor, _enemyCapital);
            
            HelpObserveBuildingOwner(sensor, _fortBuilding);

            HelpObserveBuildingOwner(sensor, _myFoodBuilding);
            HelpObserveBuildingOwner(sensor, _enemyFoodBuilding);

            HelpObserveBuildingOwner(sensor, _myWoodBuilding);
            HelpObserveBuildingOwner(sensor, _enemyWoodBuilding);

            HelpObserveBuildingOwner(sensor, _topIronBuilding);
            HelpObserveBuildingOwner(sensor, _bottomIronBuilding);
        }

        private void HelpObserveBuildingOwner(VectorSensor sensor, BuildingScript b)
        {
            UnitOwner owner = b.getOwner();

            int ownerValue =
                owner == team ? 1 :
                owner == UnitOwner.World ? 0 : -1;
            // 1 is "we" own it
            // 0 is world owns it
            // -1 is other side owns it

            sensor.AddObservation(ownerValue);
        }
        
    #endregion // building logic

        #region Unit Logic

        private readonly List<BaseUnit> _mySoldiers = new();
        private readonly List<BaseUnit> _myArchers = new();
        private readonly List<BaseUnit> _myHorsemen = new();

        private int _visibleEnemySoldiers;
        private int _visibleEnemyArchers;
        private int _visibleEnemyHorsemen;

        private void HandleUnitCreated(BaseUnit unit, UnitOwner owner, int type)
        {
            // I don't own the unit that spawned - bail
            if (owner != team)
                return;
            
            switch (type)
            {
                case 0:
                    _mySoldiers.Add(unit);
                    break;
                
                case 1:
                    _myArchers.Add(unit);
                    break;
                
                case 2:
                    _myHorsemen.Add(unit);
                    break;
            }
        }

        private void ObserveUnitCount(VectorSensor sensor)
        {
            sensor.AddObservation(_mySoldiers.Count);
            sensor.AddObservation(_myArchers.Count);
            sensor.AddObservation(_myHorsemen.Count);
        }

        #endregion // Unit Logic

        #region Game State Observations

        private void ObserveResources(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 0)); // 0 = food
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 1)); // 1 = wood
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 2)); // 2 = iron
        }

        private void ObserveTime(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.GetTimeNormalized());
        }

        #endregion
    
        #region Tile Setup
        
        private readonly List<TileScript> _allWalkableTiles = new();

        private void CacheWalkableTiles()
        {
            _allWalkableTiles.Clear();
            
            TileScript[] tiles = tilesParent.GetComponentsInChildren<TileScript>();
            
            foreach (TileScript tile in tiles)
            {
                if (tile.getMovement() > 0)
                    _allWalkableTiles.Add(tile);
            }
        }
        
        #endregion
        
        #region Difficulty
    
        public enum Difficulty { Easy, Medium, Hard }
    
        [Header("Difficulty Settings")]
        
        public Difficulty difficulty = Difficulty.Medium;
            
        [SerializeField] private float easyModeCooldown = 2f;
        [SerializeField] private float mediumModeCooldown = 1f;
        [SerializeField] private float hardModeCooldown; // = 0f
            
        private float _actionCooldownTimer;
        private float _currentDifficultyCooldown;
        
        private bool DifficultyCooldownActive()
        {
            _actionCooldownTimer -= Time.deltaTime;
            
            if (_actionCooldownTimer > 0f) 
                return true;
            
            _actionCooldownTimer = _currentDifficultyCooldown;
            return false;
        }

        private void SetDifficulty()
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    _currentDifficultyCooldown = easyModeCooldown;
                    break;

                case Difficulty.Medium:
                    _currentDifficultyCooldown = mediumModeCooldown;
                    break;

                case Difficulty.Hard:
                    _currentDifficultyCooldown = hardModeCooldown;
                    break;
                    
                default:
                    Debug.LogError("[TestAgent] Unknown difficulty was attempted to be set. Defaulting to Easy.");
                    _currentDifficultyCooldown = easyModeCooldown;
                    break;
            }
        }
        
    #endregion

        private void HandleGameEnded(UnitOwner winner)
        {
            if (winner == team)
                AddReward(+1f);
            else
                AddReward(-1f);

            _episodeResetsGame = true;
            
            EndEpisode();
        }
    }
}