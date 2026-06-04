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
        
        // MAGIC: HardCode Warning! This is NOT the best way to do this!
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
            
            foreach (BuildingScript building in _allBuildings)
                _previousControl[building] = building.GetControlPercent();

        }

        public override void CollectObservations(VectorSensor sensor) // 158 total
        {
            ObserveTime(sensor);                    //   1 sensor
            ObserveResources(sensor);               //  10 sensors
            ObserveTechUpgrades(sensor);            //  12 sensors
            ObserveBuildingOwnership(sensor);       //  27 sensors
            ObserveCapturePoints(sensor);           // 108 sensors
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            EconomicActionMasks(actionMask);
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // before handling actions, reward capturing status
            RewardCaptureProgress();
            
            int economicAction = actionBuffers.DiscreteActions[0];
            HandleEconomicAction(economicAction);
            // Economic Action Summary:
            //  0 = Do Nothing.
            
            //  1 = recruit soldier at capital.
            //  2 = recruit archer at capital.
            //  3 = recruit horseman at capital.
            
            //  4 = recruit soldier at fort.
            //  5 = recruit archer at fort.
            //  6 = recruit horseman at fort.
            
            //  7 = upgrade solider.
            //  8 = upgrade archer.
            //  9 = upgrade horseman.
            
            // 10 = upgrade resources.
        }

        #region Economy And Time Observations

        private void ObserveTime(VectorSensor sensor)
        {
            sensor.AddObservation(_gameManager.GetTimeNormalized());
        }
        
        private void ObserveResources(VectorSensor sensor)
        {
            // observe the actual amount of resources we have, normalized as percent of total we can have
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 0)); // 0 = food
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 1)); // 1 = wood
            sensor.AddObservation(_gameManager.getResourcePercentage(team, 2)); // 2 = iron
            
            // observe how the amount of resources we have influences recruitment capability
            sensor.AddObservation(_gameManager.GetFood(team) >= soldierUnitCost ? 1f : 0f);
            sensor.AddObservation(_gameManager.GetIron(team) >= archerUnitCost ? 1f : 0f);
            sensor.AddObservation(_gameManager.GetWood(team) >= horsemanUnitCost ? 1f : 0f);
            
            // observe the current cost of unit type upgrades
            sensor.AddObservation((float)(_techController.getUnitCost(team, 0) / 2000f));
            sensor.AddObservation((float)(_techController.getUnitCost(team, 1) / 2000f));
            sensor.AddObservation((float)(_techController.getUnitCost(team, 2) / 2000f));

            // observe the current cost of resource upgrade
            sensor.AddObservation((float)(_techController.getResourceCost(team) / 2000f));
        }

        private void ObserveTechUpgrades(VectorSensor sensor)
        {
            // observe the current tech for a unit type
            sensor.AddObservation(_techController.getLevelUnit(team, 0)); // 0 = soldier
            sensor.AddObservation(_techController.getLevelUnit(team, 1)); // 1 = archer
            sensor.AddObservation(_techController.getLevelUnit(team, 2)); // 2 = horseman
            
            // observe the current resource upgrade level
            sensor.AddObservation(_techController.getLevelResources(team));
            
            // observe the state of being able to afford the next tech upgrade
            sensor.AddObservation(CanAffordUpgrade(0) ? 1f : 0f); // soldier upgrade
            sensor.AddObservation(CanAffordUpgrade(1) ? 1f : 0f); // archer upgrade
            sensor.AddObservation(CanAffordUpgrade(2) ? 1f : 0f); // horseman upgrade
            
            // observe when we can no longer ever purchase another upgrade for units
            sensor.AddObservation(CanEverAffordUpgrade(0) ? 1f : 0f);
            sensor.AddObservation(CanEverAffordUpgrade(1) ? 1f : 0f);
            sensor.AddObservation(CanEverAffordUpgrade(2) ? 1f : 0f);
            
            // observe resource upgrade affordability and if we can never afford again
            sensor.AddObservation(CanAffordResourceUpgrade() ? 1f : 0f);
            sensor.AddObservation(CanEverAffordResourceUpgrade() ? 1f : 0f);
        }

        private bool CanAffordUpgrade(int type)
        {
            double cost = _techController.getUnitCost(team, type);

            switch (type)
            {
                case 0: // soldier upgrade uses food
                    return _gameManager.GetFood(team) >= cost;

                case 1: // archer upgrade uses iron
                    return _gameManager.GetIron(team) >= cost;

                case 2: // horseman upgrade uses wood
                    return _gameManager.GetWood(team) >= cost;

                default:
                    Debug.LogError("[NewAgent] Invalid type in CanAffordUpgrade");
                    return false;
            }
        }

        private bool CanEverAffordUpgrade(int type)
        {
            double cost =  _techController.getUnitCost(team, type);

            const double maxResource = 2000.0;
            
            return cost <= maxResource;
        }

        private bool CanAffordResourceUpgrade()
        {
            double cost = _techController.getResourceCost(team);
            
            bool hasFood = _gameManager.GetFood(team) >= cost;
            bool hasWood = _gameManager.GetWood(team) >= cost;
            bool hasIron = _gameManager.GetIron(team) >= cost;
            
            return hasFood && hasWood && hasIron;
        }

        private bool CanEverAffordResourceUpgrade()
        {
            double cost = _techController.getResourceCost(team);
            
            const double maxResource = 2000.0;
            
            return cost <= maxResource;
        }

        #endregion

        #region Economic Action Masks

        private void EconomicActionMasks(IDiscreteActionMask actionMask)
        {
            MaskRecruitment(actionMask);
            MaskUpgrades(actionMask);
        }

        private void MaskRecruitment(IDiscreteActionMask actionMask)
        {
            // retrieve resource values
            int foodAmount = _gameManager.GetFood(team);
            int ironAmount = _gameManager.GetIron(team);
            int woodAmount = _gameManager.GetWood(team);
            
            // mask actions which agent can not afford
            
            // food = soldier
            if (foodAmount < soldierUnitCost)
            {
                // mask at capital
                actionMask.SetActionEnabled(0, 1, false);
                // mask at fort
                actionMask.SetActionEnabled(0, 4, false);
            }
            
            // iron = archer
            if (ironAmount < archerUnitCost)
            {
                // mask at capital
                actionMask.SetActionEnabled(0, 2, false);
                // mask at fort
                actionMask.SetActionEnabled(0, 5, false);
            }

            // wood = horseman
            if (woodAmount < horsemanUnitCost)
            {
                // mask at capital
                actionMask.SetActionEnabled(0, 3, false);
                // mask at fort
                actionMask.SetActionEnabled(0, 6, false);
            }

            // if we don't own the fort, mask the recruitment at fort options
            bool ownsFort = _fortBuilding.getOwner() == team;
            if (!ownsFort)
            {
                actionMask.SetActionEnabled(0, 4, false); // soldier at fort
                actionMask.SetActionEnabled(0, 5, false); // archer at fort
                actionMask.SetActionEnabled(0, 6, false); // horseman at fort
            }
        }

        private void MaskUpgrades(IDiscreteActionMask actionMask)
        {
            // when we can't afford it
            
            // mask soldier upgrade when can't afford
            if (!CanAffordUpgrade(0))
                actionMask.SetActionEnabled(0, 7, false);

            // mask archer upgrade when can't afford
            if (!CanAffordUpgrade(1))
                actionMask.SetActionEnabled(0, 8, false);

            // mask horseman upgrade when can't afford
            if (!CanAffordUpgrade(2))
                actionMask.SetActionEnabled(0, 9, false);
            
            // mask resource upgrade when can't afford
            if (!CanAffordResourceUpgrade())
                actionMask.SetActionEnabled(0, 10, false);
            
            // when an upgrade will never be affordable again:
            
            // mask soldier upgrade if never affordable again
            if (!CanEverAffordUpgrade(0))
                actionMask.SetActionEnabled(0, 7, false);
            
            // mask archer upgrade if never affordable again
            if (!CanEverAffordUpgrade(1))
                actionMask.SetActionEnabled(0, 8, false);

            // mask horseman upgrade if never affordable again
            if (!CanEverAffordUpgrade(2))
                actionMask.SetActionEnabled(0, 9, false);

            // mask resource upgrade if never affordable again
            if (!CanEverAffordResourceUpgrade())
                actionMask.SetActionEnabled(0, 10, false);
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
                    RecruitUnit(0, _myCapital); // Soldier
                    break;

                case 2:
                    RecruitUnit(1, _myCapital); // Archer
                    break;

                case 3:
                    RecruitUnit(2, _myCapital); // Horseman
                    break;
                
                case 4:
                    RecruitUnit(0, _fortBuilding); // Soldier
                    break;

                case 5:
                    RecruitUnit(1, _fortBuilding); // Archer
                    break;

                case 6:
                    RecruitUnit(2, _fortBuilding); // Horseman
                    break;

                case 7:
                    UpgradeTech(0); // Soldier tech
                    break;

                case 8:
                    UpgradeTech(1); // Archer tech
                    break;

                case 9:
                    UpgradeTech(2); // Horseman tech
                    break;
                
                case 10:
                    UpgradeResources(); // resource upgrade
                    break;
            }
        }

        private void RecruitUnit(int type, BuildingScript building)
        {
            // report error if agent does not own building: this should be masked!
            if (building.getOwner() != team)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried recruiting at building it does not own");
                return;
            }
            
            // report error if the building can not recruit: should never be an option!
            if (building.getResource() != ResourceType.Fort)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried recruiting at building that does not recruit");
                return;
            }
            
            // report error if no available recruit slots? (not currently masking though)

            // MAGIC: HardCode Warning! This is NOT the best way to do this!
            bool hasResources = type switch
            {
                0 => _gameManager.GetFood(team) >= soldierUnitCost,     // hard-coded warning
                1 => _gameManager.GetIron(team) >= archerUnitCost,      // hard-coded warning
                2 => _gameManager.GetWood(team) >= horsemanUnitCost,    // hard-coded warning
                _ => false
            };
            
            // report if the agent did not have the resource to recruit: this should be masked!
            if (!hasResources)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried recruitment it could not afford");
                return;
            }
            
            // do the recruitment
            building.recruitUnit(type);
        }

        private void UpgradeTech(int type)
        {
            double cost = _techController.getUnitCost(team, type);
            
            // report if agent will never afford the upgrade: this should be masked!
            if (cost > 2000)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried to upgrade a > 2000 cost unit upgrade");
                return;
            }
            
            bool canAfford = type switch
            {
                0 => _gameManager.GetFood(team) >= cost,
                1 => _gameManager.GetIron(team) >= cost,
                2 => _gameManager.GetWood(team) >= cost,
                _ => false
            };
            
            // report if agent could not afford the unit upgrade: this should be masked!
            if (!canAfford)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried a unit upgrade it could not afford");
                return;
            }
            
            // do the upgrade
            _techController.upgradeUnit(team, type);
        }

        private void UpgradeResources()
        {
            double cost = _techController.getResourceCost(team);
            
            // report if agent will never afford the resource upgrade: this should be masked!
            if (cost > 2000)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried to upgrade a > 2000 cost resource upgrade");
                return;
            }
            
            bool hasFood = _gameManager.GetFood(team) >= cost;
            bool hasWood = _gameManager.GetWood(team) >= cost;
            bool hasIron = _gameManager.GetIron(team) >= cost;
            
            // report if agent could not afford the resource upgrade: this should be masked!
            if (!hasFood || !hasWood || !hasIron)
            {
                Debug.LogError("[NewAgent] ERROR: Agent tried a resource upgrade it could not afford");
                return;
            }
            
            // do the upgrade
            _techController.upgradeResource(team);
        }

        #endregion

        #region Unit Observations

        private void FillerFunction()
        {
        }

        #endregion

        #region Building Observations
        
        private Dictionary<BuildingScript, float> _previousControl = new();

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

            TileScript tile = building.occupantTile;
            bool fogged = team switch
            {
                UnitOwner.Player => tile.fogForPlayer,
                UnitOwner.Enemy => tile.fogForEnemy,
                _ => false
            };
            
            // observation for visibility status
            sensor.AddObservation(fogged ? 0f : 1f);

            if (fogged)
            {
                sensor.AddObservation(0f); // ownership masked
                sensor.AddObservation(0f); // control masked
                return;
            }
            
            // visible observations only

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

        #region Continous Reward Shaping Helpers

        // Note: if agent feels too capital obsessed, bring this value down and vice versa.
        private const float CAPITAL_WEIGHT = 5f;
        private const float NORMAL_WEIGHT = 1f;
        
        private void RewardCaptureProgress()
        {
            foreach (BuildingScript building in _allBuildings)
            {
                float current = building.GetControlPercent();
                float previous = _previousControl[building];
                
                float delta = current - previous;

                TileScript tile = building.occupantTile;
                bool fogged = team switch
                {
                    UnitOwner.Player => tile.fogForPlayer,
                    UnitOwner.Enemy => tile.fogForEnemy,
                    _ => false
                };

                // respect Fog Of War
                if (!fogged)
                {
                    // weight based on capital status
                    float weight = building.isCapital ? CAPITAL_WEIGHT : NORMAL_WEIGHT;
                    
                    // if we are actively capturing, reward! (small shaping)
                    if (delta > 0 && building.getOwner() == team)
                        AddReward(delta * 0.001f * weight);
                    
                    // if enemy is capturing, punish! (small shaping)
                    // Note: delta will be negative, therefore it is punishment
                    if (delta < 0 && building.getOwner() != team)
                        AddReward(delta * 0.001f * weight);
                }
                // update dict
                _previousControl[building] = current;
            }
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