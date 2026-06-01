using System.Collections.Generic;
using System.Linq;
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
        
        // cache field for gameManager and techController
        private GameManager _gameManager;
        private TechController _techController;

        private bool _hasCached;
        private bool _isFirstEpisode;
        
        private float _episodeStartTime;
        public float episodeLengthSeconds = 60f;
        
        private bool _nextEpisodeResetsGame;
        
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
            // first start up cache
            if (!_hasCached)
            {
                // get tech and game instance references
                _gameManager = GameManager.Instance;
                _techController = TechController.Instance;
                
                // subscribe to end of game event
                _gameManager.OnGameEnded += HandleGameEnded;
                
                // these only needs to happen once
                CacheBuildingReferences();
                CacheWalkableTiles();
                
                DebugPrintWalkableTiles();

                // ensure prev fog count is init
                ResetPrevCounts();
                
                // init difficulty settings
                SetDifficulty();
                
                // flag that we have done first game start up
                _hasCached = true;
            }
            
            // track when this episode started
            _episodeStartTime = Time.time;

            // if this episode is a new game start
            if (!_nextEpisodeResetsGame) 
                return;
            
            // these only occur on game over episode resets
            // logic was removed, but I kept the space in case I needed to use it again
            // well, logic was move to HandleGameEnded as a state needed to be reset PRIOR to the next episode
            
            _nextEpisodeResetsGame = false;
        }
        
        // hook into unity function to tick the time
        private void Update()
        {
            if (Time.time - _episodeStartTime >= episodeLengthSeconds)
            {
                EndEpisode();
            }
        }

        private void OnDestroy()
        {
            _gameManager.OnGameEnded -= HandleGameEnded;
            
            foreach (TileScript tile in _spawnTiles)
                tile.OnUnitCreated -= HandleUnitCreated;

            foreach (BuildingScript building in _allBuildings)
                building.OnBuildingCaptured -= HandleCapturedEvent;
        }

        // this is where we design state knowledge
        public override void CollectObservations(VectorSensor sensor) // 141 total
        {
            ObserveTime(sensor);                    // 1 sensor
            ObserveBuildingOwnership(sensor);       // 9 sensors
            ObserveMyUnits(sensor);                 // 12 sensors
            ObserveVisibleEnemyUnits(sensor);       // 3 sensors
            ObserveResources(sensor);               // 3 sensors
            ObserveTechUpgrades(sensor);            // 3 sensors
            ObserveFogCoverage(sensor);             // 2 sensors
            ObserveCapturePoints(sensor);           // 108 sensors
        }
        
        // called before agent choose action, disables these from option map
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            CheckRecruitmentMasks(actionMask);
            CheckTacticalMasks(actionMask);
            MaskUnitSelection(actionMask);
        }
        
        private void MaskUnitSelection(IDiscreteActionMask actionMask)
        {
            int count = _myUnits.Count;
            
            if (count == 0)
                return;

            // Disable all unit indices >= current unit count
            for (int i = count; i < GameManager.MaxUnitsListCount; i++)
                actionMask.SetActionEnabled(3, i, false);
        }
        
        // this is where we design action space
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (DifficultyCooldownActive())
                return;
            
            int economicAction = actions.DiscreteActions[0]; // 10 total
            HandleEconomicAction(economicAction);
            // Action Summary
            // 0 = do nothing
            // 1 = soldier at capital
            // 2 = archer at capital
            // 3 = horseman at capital
            // 4 = soldier at fort
            // 5 = archer at fort
            // 6 = horseman at fort
            // 7 = soldier upgrade
            // 8 = archer upgrade
            // 9 = horseman upgrade
            
            // the actual movement action "type"
            UnitAgentGoal goal = (UnitAgentGoal) actions.DiscreteActions[1];   // 11 total
            
            // Target index meaning depends on goal:
            //  - For tile-based goals: this is a tile index
            //  - For enemy-based goals: this is an enemy index
            //  - For Support: this is an ally unit index
            int targetIndex = actions.DiscreteActions[2];   // variable meaning
            
            // Which of OUR units is performing the goal
            int unitIndex = actions.DiscreteActions[3];     // our unit index
            
            HandleGoalAction(goal, targetIndex, unitIndex);
            // Action Summary
            //  0 = None
            //  1 = Move            :(myUnit and tile),
            //  2 = Capture         :(myUnit and tile),
            //  3 = Guard           :(myUnit and tile),
            //  4 = Secure          :(myUnit and tile),
            //  5 = Defend          :(myUnit and enemy),
            //  6 = Fight           :(myUnit and enemy),
            //  7 = Support        *:(myUnit and myUnit),
            //  8 = flyYouFools     :(myUnit and tile)

            RewardResourceIncome();
        }

        #region Tactical Action Logic
        
        private void DebugPrintWalkableTiles()
        {
            Debug.Log($"[TestAgent] Walkable Tile Count = {_allWalkableTiles.Count}");

            for (int i = 0; i < _allWalkableTiles.Count; i++)
            {
                TileScript tile = _allWalkableTiles[i];

                if (tile == null)
                {
                    Debug.Log($"[{i}] NULL TILE");
                    continue;
                }

                Vector3 pos = tile.transform.localPosition;

                Debug.Log(
                    $"[{i}] Tile @ ({pos.x:F1}, {pos.z:F1})  movement={tile.getMovement()}  fogP={tile.fogForPlayer} fogE={tile.fogForEnemy}"
                );
            }
        }
        
        private void HandleGoalAction(UnitAgentGoal goal, int targetIndex, int unitIndex)
        {
            if (goal == UnitAgentGoal.None)
                return;
            
            BaseUnit unit = GetMyUnitByIndex(unitIndex);
            
            switch (goal)
            {
                // Tile based goals
                case UnitAgentGoal.Move:
                case UnitAgentGoal.Capture:   // [ X ]
                case UnitAgentGoal.Guard:     // [ X ]
                case UnitAgentGoal.Secure:
                case UnitAgentGoal.FlyYouFools:
                    HandleUnitTileGoal(unit, goal, targetIndex);
                    break;
                
                // Enemy based goals
                case UnitAgentGoal.Defend:      // X
                case UnitAgentGoal.Fight:       // X
                    HandleUnitEnemyGoal(unit, goal, targetIndex);
                    break;
                
                // Ally based goals
                case UnitAgentGoal.Support:     // X
                    HandleUnitAllyGoal(unit, goal, targetIndex);
                    break;
                
                default:
                    Debug.LogWarning($"[TestAgent] goal: {goal}, being skipped");
                    break;
            }
        }
        
        private BuildingScript GetBuildingByIndex(int index)
        {
            return index switch
            {
                0 => _myCapital,
                1 => _enemyCapital,
                2 => _fortBuilding,
                3 => _myFoodBuilding,
                4 => _enemyFoodBuilding,
                5 => _myWoodBuilding,
                6 => _enemyWoodBuilding,
                7 => _topIronBuilding,
                8 => _bottomIronBuilding,
                _ => null
            };
        }
        
        private void HandleUnitTileGoal(BaseUnit unit, UnitAgentGoal goal, int tileIndex)
        {
            TileScript tile = GetTileByIndex(tileIndex);

            // null safety bail shape reward
            if (tile == null)
            {
                AddReward(-0.05f);
                return;
            }
            
            // slight negative for shaping when not selecting a capture point to capture
            if (goal is UnitAgentGoal.Capture or UnitAgentGoal.Secure
                && !IsCaptureTile(tile))
            {
                AddReward(-0.1f);
                return;
            }
            
            // if picking flyYouFools, and new tile not closer to capital - Shaping Reward
            if (goal == UnitAgentGoal.FlyYouFools)
            {
                // distance from both tiles to capital
                float oldDist = Vector3.Distance(unit.transform.position, _myCapital.transform.position);
                float newDist = Vector3.Distance(tile.transform.position, _myCapital.transform.position);

                // If the tile is NOT closer to capital → negative shaping
                if (newDist >= oldDist)
                {
                    AddReward(-0.05f);
                    return;
                }
            }
            
            StartGoalTracking(unit, goal);
                
            if (goal == UnitAgentGoal.Capture)
                unit.RequestCaptureLocation(tile);
            else if (goal == UnitAgentGoal.Secure)
                 unit.RequestSecureLocation(tile);
            else if (goal == UnitAgentGoal.Move)
                 unit.RequestMoveTo(tile);
            else if (goal == UnitAgentGoal.FlyYouFools)
                 unit.RequestFlee(tile);
            else if (goal == UnitAgentGoal.Guard)
                unit.RequestGuardLocation(tile, 10);
        }

        private void HandleUnitEnemyGoal(BaseUnit unit, UnitAgentGoal goal, int enemyIndex)
        {
            BaseUnit enemy = GetVisibleEnemyByIndex(enemyIndex);
            
            if (goal == UnitAgentGoal.Defend)
            {
                // shaping to support picking an ally under attack
                if (!EnemyThreatensOurBuilding(enemy))
                {
                    AddReward(-0.05f);
                    return;
                }
            }
            
            if (goal == UnitAgentGoal.Fight)
            {
                // shaping should give something based on distance the unit must travel?
                // this is handled by discount factor though?
            }
            
            StartGoalTracking(unit, goal);

            if (goal == UnitAgentGoal.Fight)
                unit.RequestAttackUnit(enemy, goal);
            else if (goal == UnitAgentGoal.Defend)
                unit.RequestAttackUnit(enemy, goal);
        }

        private void HandleUnitAllyGoal(BaseUnit unit, UnitAgentGoal goal, int allyIndex)
        {
            BaseUnit ally = GetMyUnitByIndex(allyIndex);
            
            UnitMotorState allyState = ally.MotorState;
            
            if (allyState != UnitMotorState.Fighting &&
                allyState != UnitMotorState.Pursuing)
            {
                AddReward(-0.05f);
                return;
            }
            
            BaseUnit enemy = ally.CurrentEnemyTarget();
            if (enemy == null)
            {
                AddReward(-0.05f);
                return;
            }

            StartGoalTracking(unit, goal);
            
            if (goal == UnitAgentGoal.Support)
            {
                StartGoalTracking(unit, goal);
                unit.RequestAttackUnit(enemy, goal);
                return;
            }
        }

        #endregion

        #region Unit Movement Helpers

        private TileScript GetClosestFogClusterTile()
        {
            TileScript exploreTile = null;
            float exploreDist = float.MaxValue;
            
            List<TileScript> fogTiles = _allWalkableTiles.FindAll(IsFogged);
            
            // bail flag for if you somehow can see all game tiles haha
            if (fogTiles.Count == 0)
                return null;

            foreach (TileScript tile in fogTiles)
            {
                // skip tiles where exploration might not "help"
                if (CountFogNeighbors(tile) < 3)
                    continue;

                float distance = Vector3.Distance(
                    _myCapital.transform.position,
                    tile.transform.position
                );

                if (distance < exploreDist)
                {
                    exploreDist = distance;
                    exploreTile = tile;
                }
            }
            
            return exploreTile;
        }
        
        private int CountFogNeighbors(TileScript tile)
        {
            int count = 0;

            foreach (TileScript n in tile.GetNeighbours())
            {
                // safety skip
                if (n == null) 
                    continue;
                
                // count++ if has fog
                if (IsFogged(n)) 
                    count++;
            }

            return count;
        }

        private bool IsFogged(TileScript tile)
        {
            return team switch
            {
                UnitOwner.Player => tile.fogForPlayer,
                UnitOwner.Enemy => tile.fogForEnemy,
                _ => false
            };
        }

        #endregion

        #region New Helpers

        // yes name so unique lmao, I gave up
        private TileScript GetTileByIndex(int index)
        {
            if (index < 0 || index >= _allWalkableTiles.Count)
                return null;
            
            return _allWalkableTiles[index];
        }

        private BaseUnit GetMyUnitByIndex(int index)
        {
            if (index < 0 || index >= GameManager.MaxUnitsListCount)
                return null;
            
            if (index >= _myUnits.Count)
                return null;
            
            return _myUnits[index];
        }

        private BaseUnit GetVisibleEnemyByIndex(int index)
        {
            if (index < 0 || index >= GameManager.MaxUnitsListCount)
                return null;

            if (index >= _visibleEnemyUnits.Count)
                return null;

            return _visibleEnemyUnits[index];
        }

        private bool IsCaptureTile(TileScript tile)
        {
            return _myCapitalCapturePoints.Contains(tile)
                   || _fortCapturePoints.Contains(tile)
                   || _myFoodCapturePoints.Contains(tile)
                   || _enemyFoodCapturePoints.Contains(tile)
                   || _myWoodCapturePoints.Contains(tile)
                   || _enemyWoodCapturePoints.Contains(tile)
                   || _topIronCapturePoints.Contains(tile)
                   || _bottomIronCapturePoints.Contains(tile);
        }

        private bool EnemyThreatensOurBuilding(BaseUnit enemy)
        {
            foreach (BuildingScript building in _allBuildings)
            {
                if (building.getOwner() != team)
                    continue;

                // Get capture tiles for this building
                List<TileScript> tiles = GetCaptureTilesForBuilding(building);
                if (tiles == null || tiles.Count == 0)
                    continue;

                // Check if enemy is near any capture tile
                foreach (TileScript tile in tiles)
                {
                    float dist = Vector3.Distance(enemy.transform.position, tile.transform.position);

                    // Threat radius = 1.5f (tweakable)
                    if (dist <= 1.5f)
                        return true;
                }
            }

            return false;
        }

        #endregion
    
        #region Movement Masking

        private void CheckTacticalMasks(IDiscreteActionMask actionMask)
        {
            MaskBuildingsWeFullyOwn(actionMask);
        }

        private void MaskBuildingsWeFullyOwn(IDiscreteActionMask actionMask)
        {
            // Building Index branch is = 2
            
            // we will disable a building from being an option for the action
            // if it meets this iteration checks (building is null or full owned by us)

            for (int buildingIndex = 0; buildingIndex < 9; buildingIndex++)
            {
                BuildingScript building = GetBuildingByIndex(buildingIndex);
                
                if (building == null)
                {
                    // Disable action to this building if it is null (safety)
                    actionMask.SetActionEnabled(2, buildingIndex, false);
                    continue;
                }
                
                // disable actions to this building if we fully own
                if (AllBuildingCapturePointsAreOurs(building))
                {
                    actionMask.SetActionEnabled(2, buildingIndex, false);
                }
            }
        }

        private bool AllBuildingCapturePointsAreOurs(BuildingScript building)
        {
            // NRE bail
            if (building == null)
                return false;
            
            List<TileScript> tiles = GetCaptureTilesForBuilding(building);
            
            // null or empty bail
            if (tiles == null || tiles.Count == 0)
                return false;
            
            foreach (TileScript tile in tiles)
            {
                // if any tile is empty - false
                if (tile.OccupyingUnit == null)
                    return false;

                // if any tile is occupied by enemy - false
                if (tile.OccupyingUnit.Owner != team)
                    return false;
            }
            
            // all tiles were occupied by our team
            return true;
        }

        #endregion
    
        #region Economic Action Logic

        private void HandleEconomicAction(int economicAction)
        {
            // branch 0 => 6 + 1 action
            switch (economicAction)
            {
                case 0:
                    // do nothing
                    break;
                
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
                
                case 7:
                    TryUpgradeUnit(0); // upgrade for soldier
                    break;
                
                case 8:
                    TryUpgradeUnit(1); // upgrade for archer
                    break;
                
                case 9:
                    TryUpgradeUnit(2); // upgrade for horseman
                    break;
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
            AddReward(+0.02f); // small shape reward
            building.recruitUnit(type);
        }

        private void TryUpgradeUnit(int type)
        {
            double cost = _techController.getUnitCost(team, type);

            // unit type reminder:
            // 0 = soldier
            // 1 = archer
            // 2 = horseman
            
            bool canAfford = type switch
            {
                0 => _gameManager.GetIron(team) >= cost, // soldier upgrade = iron
                1 => _gameManager.GetWood(team) >= cost, // archer upgrade = wood
                2 => _gameManager.GetFood(team) >= cost, // horseman upgrade = food
                _ => false
            };

            // bail
            if (!canAfford)
                return;
            
            AddReward(+0.03f); // small shape reward
            _techController.upgradeUnit(team, type);
        }

        #endregion
        
        #region  Economic Masking

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
        
        private readonly List<TileScript> _spawnTiles = new(18);
        
        private void CacheBuildingReferences()
        {
            _allBuildings = MapGenerateScript.getBuildingList();
            
            foreach (BuildingScript building in _allBuildings)
            {
                building.OnBuildingCaptured += HandleCapturedEvent;
                
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
            RegisterSpawnTiles(_enemyCapital);
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
        
        private readonly List<BaseUnit> _myUnits = new();

        private int _mySoldierCount;
        private int _myArcherCount;
        private int _myHorsemanCount;
        
        private int _myNoGoalCount;
        private int _myMoveGoalCount;
        private int _myCaptureGoalCount;
        private int _myGuardGoalCount;
        private int _mySecureGoalCount;
        private int _myDefendGoalCount;
        private int _myFightGoalCount;
        private int _mySupportGoalCount;
        private int _myFleeGoalCount;
        
        // CHEAT WARNING
        // This is the list of units the own, regardless of if we see them or not
        // DO NOT OBSERVE THIS
        private readonly List<BaseUnit> _enemyUnits = new();

        // instead only observe units if we CAN see them
        private readonly List<BaseUnit> _visibleEnemyUnits  = new();
        
        private int _enemyVisibleSoldierCount;
        private int _enemyVisibleArcherCount;
        private int _enemyVisibleHorsemanCount;

        private void HandleUnitCreated(BaseUnit unit)
        {
            // bail if this is somehow null
            if (unit == null)
                return;

            if (unit.Owner == team)
                _myUnits.Add(unit);
            else
                _enemyUnits.Add(unit);
            
            unit.OnUnitDeath += HandleUnitDeath;
        }
        
        private void HandleUnitDeath(BaseUnit deadUnit)
        {
            deadUnit.OnUnitDeath -= HandleUnitDeath;
            
            bool wasEnemy = deadUnit.Owner != team;
            
            if (wasEnemy)
                AddReward(+0.1f);
            else
            {
                AddReward(-0.1f);
                
                // this removes the unit from action tracking
                ManualRemoveFromGoalRecords(deadUnit);
            }

            _myUnits.Remove(deadUnit);
            _enemyUnits.Remove(deadUnit);
            
            _visibleEnemyUnits.Remove(deadUnit);
        }

        private void ObserveMyUnits(VectorSensor sensor)
        {
            UpdateMyUnitCounters();
            
            sensor.AddObservation(NormalizeMyUnitCount(_mySoldierCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myArcherCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myHorsemanCount));
            
            sensor.AddObservation(NormalizeMyUnitCount(_myNoGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myMoveGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myCaptureGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myGuardGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_mySecureGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myDefendGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myFightGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_mySupportGoalCount));
            sensor.AddObservation(NormalizeMyUnitCount(_myFleeGoalCount));
        }

        private void UpdateMyUnitCounters()
        {
            // reset
            _mySoldierCount = 0;
            _myArcherCount = 0;
            _myHorsemanCount = 0;

            _myNoGoalCount = 0;
            _myMoveGoalCount = 0;
            _myCaptureGoalCount = 0;
            _myGuardGoalCount = 0;
            _mySecureGoalCount = 0;
            _myDefendGoalCount = 0;
            _myFightGoalCount = 0;
            _mySupportGoalCount = 0;
            _myFleeGoalCount = 0;
            
            // Iterate backwards for null safety cleanup
            for (int i = _myUnits.Count - 1; i >= 0; i--)
            {
                BaseUnit unit = _myUnits[i];

                // null safety removal
                if (unit == null)
                {
                    _myUnits.RemoveAt(i);
                    _activeGoals.Remove(unit);
                    continue;
                }

                // unit type counter
                switch (unit.UnitType)
                {
                    case 0: _mySoldierCount++; break;
                    case 1: _myArcherCount++; break;
                    case 2: _myHorsemanCount++; break;
                }

                // unit state counter
                switch (unit.CurrentGoal)
                {
                    case UnitAgentGoal.None: _myNoGoalCount++; break;

                    case UnitAgentGoal.Move: _myMoveGoalCount++; break;

                    case UnitAgentGoal.Capture: _myCaptureGoalCount++; break;

                    case UnitAgentGoal.Guard: _myGuardGoalCount++; break;

                    case UnitAgentGoal.Secure: _mySecureGoalCount++; break;

                    case UnitAgentGoal.Defend: _myDefendGoalCount++; break;

                    case UnitAgentGoal.Fight: _myFightGoalCount++; break;
                    
                    case UnitAgentGoal.Support: _mySupportGoalCount++; break;

                    case UnitAgentGoal.FlyYouFools: _myFleeGoalCount++; break;
                    
                    default:
                        Debug.LogError($"[TestAgent] Not Tracking unit goal: {unit.CurrentGoal}");
                        break;
                }
            }
        }

        private float NormalizeMyUnitCount(int count) => NormalizeUnitCount(count, _myUnits);
        private float NormalizeEnemyUnitCount(int count) => NormalizeUnitCount(count, _enemyUnits);

        private float NormalizeUnitCount(int count, List<BaseUnit> unitList)
        {
            int total = unitList.Count;

            if (total == 0)
                return 0;
            
            return (float) count / total;
        }

        private void ObserveVisibleEnemyUnits(VectorSensor sensor)
        {
            // WARNING : Only observe the visible enemy units list
            UpdateVisibleEnemyCounters();
            
            sensor.AddObservation(NormalizeEnemyUnitCount(_enemyVisibleSoldierCount));
            sensor.AddObservation(NormalizeEnemyUnitCount(_enemyVisibleArcherCount));
            sensor.AddObservation(NormalizeEnemyUnitCount(_enemyVisibleHorsemanCount));
        }

        private void UpdateVisibleEnemyCounters()
        {
            // reset
            _enemyVisibleSoldierCount = 0;
            _enemyVisibleArcherCount = 0;
            _enemyVisibleHorsemanCount = 0;
            
            // backwards iterate for null safety cleanup
            for (int i = _enemyUnits.Count - 1; i >= 0; i--)
            {
                BaseUnit enemy = _enemyUnits[i];

                // null safety removal
                if (enemy == null)
                {
                    _enemyUnits.RemoveAt(i);
// ReSharper disable ExpressionIsAlwaysNull
                    // UNITY FAKE NULL
                    _visibleEnemyUnits.Remove(enemy);
// ReSharper restore ExpressionIsAlwaysNull
                    continue;
                }

                if (EnemyUnitIsVisible(enemy))
                {
                    if (!_visibleEnemyUnits.Contains(enemy))
                        _visibleEnemyUnits.Add(enemy);
                    
                    // update visible enemy type counter
                    switch (enemy.UnitType)
                    {
                        case 0: _enemyVisibleSoldierCount++; break;
                        case 1: _enemyVisibleArcherCount++; break;
                        case 2: _enemyVisibleHorsemanCount++; break;
                        default : Debug.LogError($"[TestAgent] Not tracking enemy unit type of: {enemy.UnitType}"); break;
                    }
                }
                else
                {
                    _visibleEnemyUnits.Remove(enemy);
                }
            }
        }

        private bool EnemyUnitIsVisible(BaseUnit enemyUnit)
        {
            TileScript tile = enemyUnit.CurrentHex;
            
            // NRE safety bail
            if (tile == null) 
                return false;

            // can our team see the tile the enemy unit is on?
            return team switch
            {
                UnitOwner.Player => !tile.fogForPlayer,
                UnitOwner.Enemy => !tile.fogForEnemy,
                _ => false
            };
        }
        
        #endregion // Unit Logic

        #region Game State Observations

        private void ObserveResources(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 0)); // 0 = food
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 1)); // 1 = wood
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 2)); // 2 = iron
        }

        private void ObserveTechUpgrades(VectorSensor sensor)
        {
            sensor.AddObservation(_techController.getLevelUnit(team, 0)); // 0 = soldier
            sensor.AddObservation(_techController.getLevelUnit(team, 1)); // 1 = archer
            sensor.AddObservation(_techController.getLevelUnit(team, 2)); // 2 = horseman
        }

        private void ObserveTime(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.GetTimeNormalized());
        }

        #endregion
    
        #region Fog Observations
        
        private readonly List<TileScript> _allWalkableTiles = new();
        
        // used for reward system
        private readonly Dictionary<TileScript, bool> _previousFogState = new();
        
        private int _foggedTileCount;
        private int _visibleTileCount;

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

        private void ObserveFogCoverage(VectorSensor sensor)
        {
            UpdateFogCounters();
            
            sensor.AddObservation(NormalizeFogCount(_foggedTileCount));
            sensor.AddObservation(NormalizeFogCount(_visibleTileCount));
        }

        private void UpdateFogCounters()
        {
            _foggedTileCount = 0;
            _visibleTileCount = 0;
            
            for (int i = _allWalkableTiles.Count - 1; i >= 0; i--)
            {
                TileScript tile = _allWalkableTiles[i];

                // null safety bail + log error - but do not remove
                if (tile == null)
                {
                    Debug.LogError("[TestAgent] A walkable tile became null!?");
                    continue;
                }

                // record fog bool
                bool fogged = team switch
                {
                    UnitOwner.Player => tile.fogForPlayer,
                    UnitOwner.Enemy => tile.fogForEnemy,
                    _ => false
                };

                // update counter
                if (fogged)
                    _foggedTileCount++;
                else
                    _visibleTileCount++;
            }
        }

        private float NormalizeFogCount(int count)
        {
            int total = _allWalkableTiles.Count;

            if (total == 0)
            {
                Debug.LogError($"[TestAgent] Team: '{team}' is observing no walkable tiles");
                return 0;
            }
            
            return (float) count / total;
        }
        
        private void RewardFogReveals()
        {
            foreach (TileScript tile in _allWalkableTiles)
            {
                bool wasFogged = _previousFogState[tile];
                bool isFogged = IsFogged(tile);

                // very small reward for reveal of fog
                if (wasFogged && !isFogged)
                    AddReward(+0.005f);

                // update fog memory dict
                _previousFogState[tile] = isFogged;
            }
        }

        #endregion

        #region Capture Observations

        private void ObserveCapturePoints(VectorSensor sensor)
        {
            // 9 buildings // 2 observations foreach of the 6 tiles
            // 9 x 12
            // 108 observation space
            
            ObserveCapturePointsForBuilding(sensor, _myCapitalCapturePoints);
            ObserveCapturePointsForBuilding(sensor, _enemyCapitalCapturePoints);
            
            ObserveCapturePointsForBuilding(sensor, _myFoodCapturePoints);
            ObserveCapturePointsForBuilding(sensor, _enemyFoodCapturePoints);
            
            ObserveCapturePointsForBuilding(sensor, _myWoodCapturePoints);
            ObserveCapturePointsForBuilding(sensor, _enemyWoodCapturePoints);
            
            ObserveCapturePointsForBuilding(sensor, _topIronCapturePoints);
            ObserveCapturePointsForBuilding(sensor, _bottomIronCapturePoints);
            
            ObserveCapturePointsForBuilding(sensor, _fortCapturePoints);
        }

        private void ObserveCapturePointsForBuilding(
            VectorSensor sensor,
            List<TileScript> capturePoints)
        {
            foreach (TileScript tile in capturePoints)
            {
                // check if visible
                bool fogged = team switch
                {
                    UnitOwner.Player => tile.fogForPlayer,
                    UnitOwner.Enemy => tile.fogForEnemy,
                    _ => false
                };
                
                int visible = fogged ? 0 : 1;
                sensor.AddObservation(visible);
                
                // state of capture
                int state;

                if (fogged)
                    state = 0; // unknown state
                else if (tile.OccupyingUnit == null)
                    state = 1; // empty
                else if (tile.OccupyingUnit.Owner == team)
                    state = 2; // our unit
                else
                    state = 3; // enemy unit
                
                sensor.AddObservation(state);
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

        #region Tile Utility
        
        private List<TileScript> GetCaptureTilesForBuilding(BuildingScript building)
        {
            if (building == _myCapital) return _myCapitalCapturePoints;
            if (building == _enemyCapital) return _enemyCapitalCapturePoints;

            if (building == _myFoodBuilding) return _myFoodCapturePoints;
            if (building == _enemyFoodBuilding) return _enemyFoodCapturePoints;

            if (building == _myWoodBuilding) return _myWoodCapturePoints;
            if (building == _enemyWoodBuilding) return _enemyWoodCapturePoints;

            if (building == _topIronBuilding) return _topIronCapturePoints;
            if (building == _bottomIronBuilding) return _bottomIronCapturePoints;

            if (building == _fortBuilding) return _fortCapturePoints;

            Debug.LogError("[TestAgent] Unknown building passed to GetCapturePoints");
            return null;
        }

        #endregion
        
        #region Get Unit Utility Core Helpers

        // NOTE: if no origin, it will use our capital as closest reference
        private BaseUnit GetClosestUnit(
            int? type = null,
            UnitAgentGoal? goal = null,
            Vector3? origin = null)
        {
            Vector3 originPos = origin ?? _myCapital.transform.position;
            
            BaseUnit closest = null;
            float closestDist = float.MaxValue;
            
            // backwards iterate for, you guessed it, null safety removal
            for (int i = _myUnits.Count - 1; i >= 0; i--)
            {
                BaseUnit unit = _myUnits[i];

                // null safety removal
                if (unit == null)
                {
                    _myUnits.RemoveAt(i);
                    continue;
                }

                // filter type if provided
                if (type.HasValue && unit.UnitType != type.Value)
                    continue;

                // filter state if provided
                if (goal.HasValue && unit.CurrentGoal != goal.Value)
                    continue;

                float d = Vector3.Distance(unit.transform.position, originPos);

                if (d < closestDist)
                {
                    closestDist = d;
                    closest = unit;
                }
            }
            
            return closest;
        }

        private List<BaseUnit> FilterMyUnits(
            int? type = null,
            UnitAgentGoal? goal = null)
        {
            List<BaseUnit> result = new();
            
            // backwards iteration for null safety removal
            for (int i = _myUnits.Count - 1; i >= 0; i--)
            {
                BaseUnit unit = _myUnits[i];

                // null safety removal
                if (unit == null)
                {
                    _myUnits.RemoveAt(i);
                    continue;
                }

                // filter type if provided
                if (type.HasValue && unit.UnitType != type.Value)
                    continue;

                // filter state if provided
                if (goal.HasValue && unit.CurrentGoal != goal.Value)
                    continue;

                result.Add(unit);
            }
            return result;
        }
        
        #endregion

        #region Goal Recording System
        
        // dictionary storing the current active actions
        private readonly Dictionary<BaseUnit, UnitAgentGoal> _activeGoals = new();

        /// <summary>
        /// Call this to start a tactical action recording.
        /// </summary>
        /// <param name="unit">The unit performing the action</param>
        /// <param name="action">THe action being taken</param>
        private void StartGoalTracking(BaseUnit unit, UnitAgentGoal action)
        {
            // NRE safety bail
            if (unit == null)
                return;
            
            // if the unit already had an action, clear it
            if (_activeGoals.ContainsKey(unit))
            {
                unit.OnGoalResolved -= HandleUnitGoalResolved;
                _activeGoals.Remove(unit);
            }
            
            _activeGoals[unit] = action;
            unit.OnGoalResolved += HandleUnitGoalResolved;
        }

        /// <summary>
        /// Intermediate between Start Action and Finish Action
        ///
        /// Allows a Unit to report a completed action
        ///
        /// Intended to be called by the unit's OnTacticalActionCompleted event
        /// </summary>
        /// <param name="unit">The Unit that reported finishing an action</param>
        /// <param name="resolvedGoal">The Action the unit is reporting completed</param>
        /// <param name="result"></param>
        /// <param name="rewardFromUnit"></param>
        private void HandleUnitGoalResolved(
            BaseUnit unit, 
            UnitAgentGoal resolvedGoal,
            GoalResult result,
            float rewardFromUnit)
        {
            // get the action mapped to this unit, if the unit exists in mapping
            if (!_activeGoals.TryGetValue(unit, out UnitAgentGoal expected))
                return; // else bail (unit was not in mapping)

            // if the goal resolved was what we asked it to do
            if (resolvedGoal == expected)
            {
                // success means use units reward function
                if (result == GoalResult.Success)
                    AddReward(rewardFromUnit);

                // goal was terminated because agent gave it new action
                else if (result == GoalResult.OverriddenByAgent &&
                         GoalAllowsPartialReward(resolvedGoal))
                    AddReward(rewardFromUnit * 0.25f); 
                // discount that we overwrote this
            }
            
            unit.OnGoalResolved -= HandleUnitGoalResolved;
            _activeGoals.Remove(unit);
        }
        
        private bool GoalAllowsPartialReward(UnitAgentGoal goal)
        {
            return goal == UnitAgentGoal.Capture ||
                   goal == UnitAgentGoal.Secure;
        }

        /// <summary>
        /// Call this to clear the tactical recording system
        /// </summary>
        private void ClearTacticalActions()
        {
            foreach (BaseUnit unit in _activeGoals.Keys)
                unit.OnGoalResolved -= HandleUnitGoalResolved;
            
            _activeGoals.Clear();
        }

        private void ManualRemoveFromGoalRecords(BaseUnit unit)
        {
            unit.OnGoalResolved -= HandleUnitGoalResolved;
            _activeGoals.Remove(unit);
        }

        #endregion

        #region Reward Helpers
        
        private int _prevFood, _prevWood, _prevIron;
        
        private void RewardResourceIncome()
        {
            int food = _gameManager.GetFood(team);
            int wood = _gameManager.GetWood(team);
            int iron = _gameManager.GetIron(team);
            
            if (food > _prevFood) AddReward((food - _prevFood) * 0.001f);
            if (wood > _prevWood) AddReward((wood - _prevWood) * 0.001f);
            if (iron > _prevIron) AddReward((iron - _prevIron) * 0.001f);
            
            _prevFood = food;
            _prevWood = wood;
            _prevIron = iron;
        }

        private void HandleCapturedEvent(BuildingScript building, UnitOwner newOwner)
        {
            if (newOwner == team)
                AddReward(+1f);
            else
                AddReward(-1f);
        }

        #endregion

        #region End Game

        private void HandleGameEnded(UnitOwner winner)
        {
            if (winner == team)
                AddReward(+1f);
            else
                AddReward(-1f);

            ResetPrevCounts();
            EndGameAgentClearLists();

            _nextEpisodeResetsGame = true;
            
            EndEpisode();
        }

        private void EndGameAgentClearLists()
        {
            ClearTacticalActions();
            
            UnsubscribeDeathEventsForUnits(_myUnits);
            UnsubscribeDeathEventsForUnits(_enemyUnits);
            
            _myUnits.Clear();
            _enemyUnits.Clear();
            _visibleEnemyUnits.Clear();
        }

        private void UnsubscribeDeathEventsForUnits(List<BaseUnit> unitList)
        {
            foreach (BaseUnit unit in unitList)
                if (unit != null)
                    unit.OnUnitDeath -= HandleUnitDeath;
        }

        #endregion

        private void ResetPrevCounts()
        {
            // reset prev resource counters
            _prevFood = 0;
            _prevWood = 0;
            _prevIron = 0;
            
            // reset prev fog state
            _previousFogState.Clear();
            foreach (TileScript tile in _allWalkableTiles)
                _previousFogState[tile] = IsFogged(tile);
        }
    }
    
    /// <summary>
    /// Intended to be used between Units and RL-Agent
    /// </summary>
    public enum TacticalAction
    {
        None,
        ScoutFog,
        AttackEnemy,
        SupportAlly,
        AttackBuilding,
        DefendBuilding,
        GuardBuilding
    }
}