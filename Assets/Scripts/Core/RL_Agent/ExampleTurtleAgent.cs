using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

namespace RL_Agent
{
    public class ExampleTurtleAgent : Agent
    {
        [SerializeField] private Transform goal;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float rotationSpeed = 180f;
        
        [SerializeField] private Material turtleMaterial;
        
        private Renderer _renderer;

        private int _currentEpisode;
        private float _cumulativeReward;
        
        public override void Initialize()
        {
            _renderer = GetComponent<Renderer>();
            _currentEpisode = 0;
            _cumulativeReward = 0f;
        }

        // agent resets episode
        public override void OnEpisodeBegin()
        {
            _currentEpisode++;
            _cumulativeReward = 0f;
            _renderer.material = turtleMaterial;

            SpawnObjects();
        }
        
        private void SpawnObjects()
        {
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3(0f, 0.15f, 0f);
            
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomDirection = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward;
            
            float randomDistance = Random.Range(1f, 2.5f);
            
            Vector3 goalPosition = transform.localPosition + randomDirection * randomDistance;
            
            goal.localPosition = new Vector3(goalPosition.x, 0.3f, goalPosition.z);
        }

        // agent reads state
        public override void CollectObservations(VectorSensor sensor)
        {
            float goalPosXNormalized = goal.position.x / 5f;
            float goalPosZNormalized = goal.position.z / 5f;
            
            float turtlePosXNormalized = goal.position.x / 5f;
            float turtlePosZNormalized = goal.position.z / 5f;
            
            float turtleRotationNormalized = transform.localRotation.eulerAngles.y / 360f * 2f - 1f;
            
            sensor.AddObservation(goalPosXNormalized);
            sensor.AddObservation(goalPosZNormalized);
            sensor.AddObservation(turtlePosXNormalized);
            sensor.AddObservation(turtlePosZNormalized);
            sensor.AddObservation(turtleRotationNormalized);
        }

        // agent chooses action
        public override void OnActionReceived(ActionBuffers actions)
        {
            MoveAgent(actions.DiscreteActions);
            
            AddReward(-2f / MaxStep);
            
            _cumulativeReward = GetCumulativeReward();
        }

        private void MoveAgent(ActionSegment<int> actions)
        {
            int action = actions[0];

            switch (action)
            {
                case 1: // move forward
                    transform.position += transform.forward * moveSpeed * Time.deltaTime;
                    break;
                case 2: // Rotate left
                    transform.Rotate(0f, -rotationSpeed * Time.deltaTime, 0f);
                    break;
                case 3: // Rotate Right
                    transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Goal"))
            {
                GoalReached();
            }
        }

        private void GoalReached()
        {
            AddReward(1f);
            _cumulativeReward = GetCumulativeReward();
            
            EndEpisode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                AddReward(-0.05f);
                
                if (_renderer != null)
                    _renderer.material.color = Color.red;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
                AddReward(-0.01f * Time.deltaTime);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                if (_renderer != null)
                    _renderer.material = turtleMaterial;
            }
        }
    }
}