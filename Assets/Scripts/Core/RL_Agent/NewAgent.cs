using System.Collections.Generic;
using Resource;
using Units;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RL_Agent
{
    public class NewAgent : Agent
    {
        [Header("Agent Settings")]
        
        [Tooltip("This is the team the agent is playing for")]
        [SerializeField] private UnitOwner team;
        
        [Tooltip("This is the parent of all tiles in game")]
        [SerializeField] private Transform tilesParent;
        
        // fields for caching reference to managers
        private TechController _techController;
        private GameManager _gameManager;
        
        // field for startup cache-ing usage
        private bool _hasCached;
        
        // MAGIC: Alert! This is NOT the best way to do this!
        [HideInInspector]
        public int soldierUnitCost = 100;
        [HideInInspector]
        public int archerUnitCost = 100;
        [HideInInspector]
        public int horsemanUnitCost = 100;
        
        #region Unity Functions

        private void Start()
        {
            // cache reference to managers
            _techController = TechController.Instance;
            _gameManager = GameManager.Instance;
            
            // subscribe to end of game event
            _gameManager.OnGameEnded += HandleGameEnded;
        }

        #endregion
        
        public override void Initialize()
        {
            
        }

        public override void OnEpisodeBegin()
        {
            if (!_hasCached)
            {
                CacheBuildingReferences();
            }

        }

        public override void CollectObservations(VectorSensor sensor) // 133 total
        {
            ObserveTime(sensor);                    //   1 sensor
            ObserveResources(sensor);               //   3 sensors
            ObserveTechUpgrades(sensor);            //   3 sensors
            ObserveBuildingOwnership(sensor);       //  18 sensors
            ObserveCapturePoints(sensor);           // 108 sensors
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            EconomicActionMasks(actionMask);
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            int economicAction = actionBuffers.DiscreteActions[0];
            HandleEconomicAction(economicAction);
            // Economic Action Summary:
            //  0 = Do Nothing.
            //  1 = recruit soldier.
            //  2 = recruit archer.
            //  3 = recruit horseman.
            //  4 = upgrade solider.
            //  5 = upgrade archer.
            //  6 = upgrade horseman.
        }

        #region Economy And Time Observations

        private void ObserveTime(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.GetTimeNormalized());
        }
        
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

        #endregion

        #region Economic Action Masks

        private void EconomicActionMasks(IDiscreteActionMask actionMask)
        {
            MaskRecruitment(actionMask, 1, 2, 3);
        }

        private void MaskRecruitment(
            IDiscreteActionMask actionMask,
            int soldierAction,
            int archerAction,
            int horsemanAction)
        {
            // retrieve resource values
            int foodAmount = _gameManager.GetFood(team);
            int ironAmount = _gameManager.GetIron(team);
            int woodAmount = _gameManager.GetWood(team);
            
            // mask actions which agent can not afford
            
            // food = soldier
            if (foodAmount < soldierUnitCost)
                actionMask.SetActionEnabled(0, soldierAction, false);
            
            // iron = archer
            if (ironAmount < archerUnitCost)
                actionMask.SetActionEnabled(0, archerAction, false);
            
            // wood = horseman
            if (woodAmount < horsemanUnitCost)
                actionMask.SetActionEnabled(0, horsemanAction, false);
        }

        #endregion

        #region Economic Action Space

        private void HandleEconomicAction(int economicAction)
        {
            switch (economicAction)
            {
                case 0:
                    // do nothing
                    break;

                case 1:
                    //TryRecruit(0); // Soldier
                    break;

                case 2:
                    //TryRecruit(1); // Archer
                    break;

                case 3:
                    //TryRecruit(2); // Horseman
                    break;

                case 4:
                    //TryUpgradeTech(0); // Soldier tech
                    break;

                case 5:
                    //TryUpgradeTech(1); // Archer tech
                    break;

                case 6:
                    //TryUpgradeTech(2); // Horseman tech
                    break;
            }
        }

        #endregion

        #region Unit Observations

        private void FillerFunction()
        {
        }

        #endregion

        #region Building Observations

        /// <summary>
        /// Way for Agent to observe the ownership states of building.
        /// "Normalized" as int, -1, 0, 1 where:
        /// -1 : enemy owns it
        ///  0 : "world" owns it
        ///  1: Team owns it
        /// </summary>
        /// <param name="sensor"></param>
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

        private void HelpObserveBuildingOwner(VectorSensor sensor, BuildingScript building)
        {
            if (building == null)
            {
                Debug.LogError("[NewAgent] Building trying to be observed is null");
                return;
            }

            UnitOwner owner = building.getOwner();

            // Where this is directly observing the current ownership
            int normalizedOwnership = owner == team ? 1 : owner == UnitOwner.World ? 0 : -1;
            sensor.AddObservation(normalizedOwnership);

            // where -1 is fully enemy controlled, +1 is fully our control,
            // and range between is based off of current control percent
            // observing the control percent change over time
            float normalizedControl = building.GetControlPercent() / 100f;
            sensor.AddObservation(normalizedControl);
        }

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

        private void ObserveCapturePointsForBuilding(VectorSensor sensor, List<TileScript> capitalTiles)
        {
            foreach (TileScript tile in capitalTiles)
            {
                // check if visible
                bool fogged = team switch
                {
                    UnitOwner.Player => tile.fogForPlayer,
                    UnitOwner.Enemy => tile.fogForEnemy,
                    _ => false
                };
                
                // observe the visibility state
                sensor.AddObservation(fogged ? 0f : 1f);
                
                // state of capture
                float normalizedOccupancy;

                if (fogged)
                    normalizedOccupancy = 0f;           // not visible = 0
                else if (tile.OccupyingUnit == null)
                    normalizedOccupancy = 0f;           // empty = 0
                else if (tile.OccupyingUnit.Owner == team)
                    normalizedOccupancy = 1f;           // our unit = 1
                else
                    normalizedOccupancy = -1f;          // enemy unit = -1
                
                // observe occupation
                sensor.AddObservation(normalizedOccupancy);
            }
        }

        #endregion

        #endregion

        #region Cache Operations
        
        private List<BuildingScript> _allBuildings = new(9);
        
        private readonly List<BuildingScript> _capitalBuildings = new(2);
        private BuildingScript _myCapital;
        private List<TileScript> _myCapitalCapturePoints = new(6);
        private BuildingScript _enemyCapital;
        private List<TileScript> _enemyCapitalCapturePoints = new(6);
        
        private BuildingScript _fortBuilding;
        private List<TileScript> _fortCapturePoints = new(6);
        
        private readonly List<BuildingScript> _foodBuildings = new(2);
        private BuildingScript _myFoodBuilding;
        private List<TileScript> _myFoodCapturePoints = new(6);
        private BuildingScript _enemyFoodBuilding;
        private List<TileScript> _enemyFoodCapturePoints = new(6);
        
        private readonly List<BuildingScript> _woodBuildings = new(2);
        private BuildingScript _myWoodBuilding;
        private List<TileScript> _myWoodCapturePoints = new(6);
        private BuildingScript _enemyWoodBuilding;
        private List<TileScript> _enemyWoodCapturePoints = new(6);
        
        private readonly List<BuildingScript> _ironBuildings = new(2);
        private BuildingScript _topIronBuilding;
        private List<TileScript> _topIronCapturePoints = new(6);
        private BuildingScript _bottomIronBuilding;
        private List<TileScript> _bottomIronCapturePoints = new(6);
        
        private readonly List<TileScript> _capturePointsHelperList = new(6);

        private void CacheBuildingReferences()
        {
            _allBuildings = MapGenerateScript.getBuildingList();

            foreach (BuildingScript building in _allBuildings)
            {
                // NRE safety bail
                if (building == null)
                {
                    Debug.LogError("[NewAgent] Building reference trying to be cached was null");
                    return;
                }

                // subscribe to building captured for reward setup
                building.OnBuildingCaptured += HandleCapturedEvent;
                
                // switch based on buildings resource value
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
                        Debug.LogError("[NewAgent] Unknown resource type");
                        break;
                }
            }
            
            // cache our capital and their capital building reference by ownership
            // Note: Important to do this first, as the next step uses this reference
            _myCapital = _capitalBuildings.Find(x => x.getOwner() == team);
            _enemyCapital = _capitalBuildings.Find(x => x.getOwner() != team);
            
            // for paired resource buildings cache the pair as the closest and furthest
            AssignDistanceForPair(_foodBuildings, out _myFoodBuilding, out _enemyFoodBuilding);
            AssignDistanceForPair(_woodBuildings, out _myWoodBuilding, out _enemyWoodBuilding);
            AssignDistanceForPair(_ironBuildings, out _topIronBuilding, out _bottomIronBuilding);

            // populate Capture tile references for all buildings
            _myCapitalCapturePoints = PopulateCaptureTiles(_myCapital);
            _enemyCapitalCapturePoints = PopulateCaptureTiles(_enemyCapital);
            
            _fortCapturePoints = PopulateCaptureTiles(_fortBuilding);
            
            _myFoodCapturePoints = PopulateCaptureTiles(_myFoodBuilding);
            _enemyFoodCapturePoints = PopulateCaptureTiles(_enemyFoodBuilding);
            
            _myWoodCapturePoints = PopulateCaptureTiles(_myWoodBuilding);
            _enemyWoodCapturePoints = PopulateCaptureTiles(_enemyWoodBuilding);
            
            _topIronCapturePoints = PopulateCaptureTiles(_topIronBuilding);
            _bottomIronCapturePoints = PopulateCaptureTiles(_bottomIronBuilding);
        }

        /// <summary>
        ///<para>
        /// For a pair of <see cref="BuildingScript"/>, gets closest and furthest from:
        /// <see cref="_myCapital"/>
        /// <br/>
        /// Outs the closest and furthest from this by Vector3 distance.
        /// </para>
        /// <para><b>Out Semantics Caution:</b><br/>
        /// If pair param list != 2, outs are null.<br/>
        /// If <see cref="_myCapital"/> reference not assigned prior to call, outs are null.
        /// </para>
        /// </summary>
        /// <param name="pair">Must be a List of 2</param>
        /// <param name="closest">Closest of pair to _myCapital</param>
        /// <param name="furthest">Furthest of pair to _myCapital</param>
        private void AssignDistanceForPair(
            List<BuildingScript> pair,
            out BuildingScript closest,
            out BuildingScript furthest)
        {
            closest = null;
            furthest = null;
            
            if (pair.Count != 2)
            {
                Debug.LogError("[NewAgent] Invalid pair count");
                return;
            }

            if (_myCapital == null)
            {
                Debug.LogError("[NewAgent] _myCapital building reference is null");
                return;
            }

            float closestDistance = float.MaxValue;
            float furthestDistance = float.MaxValue;

            foreach (BuildingScript building in pair)
            {
                float distance = Vector3.Distance(building.transform.position, _myCapital.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = building;
                }

                if (distance < furthestDistance)
                {
                    furthestDistance = distance;
                    furthest = building;
                }
            }
        }

        private List<TileScript> PopulateCaptureTiles(BuildingScript building)
        {
            // clear helper list
            _capturePointsHelperList.Clear();
            
            // get neighbors, and for each add to helper list
            foreach (TileScript tile in building.GetNeighbourTiles())
            {
                if (_capturePointsHelperList.Contains(tile))
                {
                    Debug.LogError("[NewAgent] Duplicate capture point detected");
                    continue;
                }

                _capturePointsHelperList.Add(tile);
            }
            
            return _capturePointsHelperList;
        }

        private void RegisterToSpawnTiles(BuildingScript building)
        {
            
        }

        #endregion

        #region Event Responders

        /// <summary>
        /// This is an event raised by buildings upon being captured.
        /// We give +1 if we captured this building, and -1 if the enemy captured it.
        /// Notes: Possible cheating?
        /// </summary>
        /// <param name="building"></param>
        /// <param name="newOwner"></param>
        private void HandleCapturedEvent(BuildingScript building, UnitOwner newOwner)
        {
            if (newOwner == team)
                AddReward(+1f);
            else
                AddReward(-1f);
        }

        #endregion

        #region End Game Episode Reset
        
        /// <summary>
        /// Called By GameManager when Game ends,
        /// Reward for winning, punish for losing
        ///
        /// EndEpisode must be called here, so next can begin 
        /// </summary>
        /// <param name="winner"></param>
        private void HandleGameEnded(UnitOwner winner)
        {
            // Reward Winning / Losing
            if (winner == team) AddReward(+1f);
            else AddReward(-1f);
            
            // Add Helper Resets Here if needed?
            
            EndEpisode();
        }

        #endregion
    }
}