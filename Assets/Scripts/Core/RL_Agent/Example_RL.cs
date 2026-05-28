using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace RL_Agent
{
    public class Example_RL : Agent
    {
        public override void Initialize()
        {
            
        }

        // set up episode values
        public override void OnEpisodeBegin()
        {
            
        }

        // read the state
        public override void CollectObservations(VectorSensor sensor)
        {
            
        }

        // use this to mask actions
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            
        }

        // user input influence
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            
        }
    }
}