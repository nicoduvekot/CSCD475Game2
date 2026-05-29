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
            // sensor.AddObservation is done here
            // each on is Vector Observation Space Size ++ in inspector
        }

        // use this to mask actions
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            // this allows you to say for branch [x] disable choice y
        }

        // this is where you set up action spaces
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // if using continuous actions - it is one branch with that amount of action space
            
            // an action buffer has an amount of actions in it
            
            // not continuous is discrete
            // - each branch can yield one action choice out of it
            // - multiple branches should be used for unrelated action spaces
            
            // buffer[0] means branch 0, if it has 4 possible actions, give it value 5, 1 for the "empty" action
        }

        // user input influence
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // if you want to manually get the agent started
            // or test that actions are working as designed?
        }
    }
}