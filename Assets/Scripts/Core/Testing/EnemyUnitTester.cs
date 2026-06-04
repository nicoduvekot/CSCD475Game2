using System.Collections.Generic;
using Units;
using UnityEngine;

namespace Core.Testing
{
    public class EnemyUnitTester : MonoBehaviour
    {
        [SerializeField] private UnitOwner team;
        
        [SerializeField] private TileScript capitalTile;
        private BuildingScript _capitalBuilding;
        private List<TileScript> _capitalSpawnTiles = new(6);
        
        private GameManager _gameManager;

        private BaseUnit _guardUnit;
        private bool _guardUnitSpawning;

        private BaseUnit _captureUnit;
        private bool _captureUnitSpawning;

        private void Start()
        {
            // cache reference to game manager
            _gameManager = GameManager.Instance;
            
            // cache references to needed info: bail and report if core reference is null
            if (capitalTile == null)
                Debug.LogError("[EnemyUnitTester] capitalTile is null");
            else
            {
                // cache building used to spawn units
                _capitalBuilding = capitalTile.AttachedBuilding;
                
                // cache reference to spawn tiles of capital
                _capitalSpawnTiles = capitalTile.GetNeighbours();
                
                // for each spawn tile, register to OnUnitCreated event
                foreach (TileScript tile in _capitalSpawnTiles)
                {
                    Debug.Log("Barney");
                    tile.OnUnitCreated += HandleUnitCreated;
                }
            }
        }

        private void Update()
        {
            UpdatePatrolUnit();
            UpdateGuardUnit();
        }

        #region Guard Unit Core

        private void UpdateGuardUnit()
        {
            
        }

        #endregion

        #region Patrol Unit Logic
        
        // patrol unit internals
        private BaseUnit _patrolUnit;
        private bool _patrolUnitSpawning;
        private bool _patrolUnitInitialized;
        
        // patrol unit respawn timer system
        [SerializeField, Min(0f)] 
        private float patrolRespawnTime;
        private float _patrolRespawnTimer;
        
        private void UpdatePatrolUnit()
        {
            // patrol unit is in queue to be built, bail.
            // this flag will become false when created,
            // see: HandleUnitCreated in Helpers
            if (_patrolUnitSpawning) return;

            // core functionality
            if (_patrolUnitInitialized)
            {
                UpdatePatrolUnitCore();
                return;
            }
            
            // NOTE: Set timer initially in start?
            // timer on respawn still active
            if (_patrolRespawnTimer > 0f) // patrol unit is null, try spawn a new one
            {
                // increment
                _patrolRespawnTimer -= Time.deltaTime;
                return;
            }
            
            // if we can afford, spawn a new
            _patrolUnitSpawning = TrySpawnPatrolUnit();
        }

        private bool TrySpawnPatrolUnit()
        {
            // can't afford unit, bail.
            if (_gameManager.GetWood(team) < 100) return false;
            
            // recruit patrol unit (hard coded as horseman spawn)
            _capitalBuilding.recruitUnit(2); // horseman
            _patrolUnitSpawning = true;
            return true;
        }

        #endregion

        #region Patrol Unit Core
        
        // inspector settings
        [Tooltip("The Tiles this unit will patrol, in this order")]
        [SerializeField] 
        private List<TileScript> patrolPoints;
        
        [Tooltip("The time the patrol unit will wait before moving again\nMust be >= 0")]
        [SerializeField, Min(0f)]
        private float patrolWaitTime;

        // core fields (need reset between units)
        private bool _patrolUnitIsMoving;
        private int _patrolIndex = -1;
        
        // helper fields (auto reset internally)
        private TileScript _currentPatrolPoint;
        private float _patrolWaitTimer;

        private void UpdatePatrolUnitCore()
        {
            // patrol unit is currently moving
            if (_patrolUnitIsMoving)
            {
                // has not reached patrol point yet, bail.
                if (_patrolUnit.CurrentHex != _currentPatrolPoint) return;
                
                // reached patrol point
                _patrolUnitIsMoving = false;
                // reset wait timer
                _patrolWaitTimer = patrolWaitTime;
                return;
            }

            // if time on timer check
            if (_patrolWaitTimer > 0f)
            {
                // increment timer and bail.
                _patrolWaitTimer -= Time.deltaTime;
                return;
            }

            // wait timer over, move to next tile
            _currentPatrolPoint = GetNextPatrolPoint();
            _patrolUnitIsMoving = true;
            _patrolUnit.AutomateMoveTo(_currentPatrolPoint);
        }

        private TileScript GetNextPatrolPoint()
        {
            // NRE and Empty bail.
            if (patrolPoints == null || patrolPoints.Count == 0)
                return null;
            
            // increment index
            _patrolIndex++;
            
            // restart patrol route condition
            if (_patrolIndex >= patrolPoints.Count)
                _patrolIndex = 0;
            
            return patrolPoints[_patrolIndex];
        }

        #endregion

        #region Unit Create and Death Event Hooks

        private void HandleUnitCreated(BaseUnit unit)
        {
            switch (unit.UnitType)
            {
                case 0: // soldier unit created
                    _guardUnit = unit;
                    _guardUnitSpawning = false;
                    _guardUnit.OnUnitDeath += HandleGuardUnitDeath;
                    break;
                case 1: // archer unit created
                    break;
                case 2: // horseman unit created
                    _patrolUnit = unit;
                    _patrolUnitSpawning = false;
                    _patrolUnitInitialized = true;
                    _patrolUnit.OnUnitDeath += HandlePatrolUnitDeath;
                    break;
            }
        }

        private void HandleGuardUnitDeath(BaseUnit unit)
        {
            
        }

        private void HandlePatrolUnitDeath(BaseUnit unit)
        {
            // unit that triggered reset was not actually the active patrol unit
            if (_patrolUnit != unit)
            {
                Debug.LogError("[EnemyUnitTester] HandlePatrolUnitDeath triggered by non-patrol unit entity"); 
                return;
            }

            // unsubscribe!
            _patrolUnit.OnUnitDeath -= HandlePatrolUnitDeath;
            
            // reset patrol unit internals
            _patrolUnitInitialized = false;
            _patrolUnitSpawning = false;
            _patrolUnit = null;
            
            // reset patrol unit core
            _patrolUnitIsMoving = false;
            _patrolIndex = -1;
            
            // reset helper fields
            _currentPatrolPoint = null;
            _patrolWaitTimer = 0f;
            
            // reset respawn timer
            _patrolRespawnTimer = patrolRespawnTime;
        }

        #endregion
    }
}