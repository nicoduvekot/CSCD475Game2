using System.Collections.Generic;
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
            
        }

        public override void CollectObservations(VectorSensor sensor) // 7 total
        {
            // Economy and Time
            ObserveTime(sensor);                    // 1 sensor
            ObserveResources(sensor);               // 3 sensors
            ObserveTechUpgrades(sensor);            // 3 sensors
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
    }
}