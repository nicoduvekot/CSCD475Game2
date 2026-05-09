using UnityEngine;

namespace DataDefinitions
{
    [CreateAssetMenu(menuName = "Building Data", fileName = "NewBuilding")]
    public class BuildingData : ScriptableObject
    {
        public string buildingName; // name of building
        public ResourceType resourceType; // what resource type is required for this
        public int modifier = 1; // how much of the resource this provides
        public float buildTime = 3f; // how long it takes to build / destroy this
    }
}