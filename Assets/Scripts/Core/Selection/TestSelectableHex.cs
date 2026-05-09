using UnityEngine;
using DataDefinitions;
using Core.UIElements;

namespace Selection
{
    public class TestSelectableHex : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;
        
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private BuildingData attachedBuilding;
        
        public ResourceType ResourceType => resourceType;
        public BuildingData AttachedBuilding => attachedBuilding;

        private string HoverText
        {
            get
            {
                string baseText = $"This is a {resourceType} hex tile";

                if (attachedBuilding != null)
                {
                    baseText += 
                        $"\nWith a {attachedBuilding.buildingName} that provides " +
                        $"{attachedBuilding.modifier} {attachedBuilding.resourceType}";
                }

                return baseText;
            }
        }
        
        public void AttachBuilding(BuildingData data)
        {
            attachedBuilding = data;
        }
        
        public void DemolishBuilding()
        {
            attachedBuilding = null;
        }

        public void OnHoverEnter()
        {
            HoverUI.Instance.Show(HoverText);
        }

        public void OnHoverExit()
        {
            HoverUI.Instance.Hide();
        }
        
        public void OnSelected() { Debug.Log($"{name} hex tile is selected operation"); }
        public void OnDeselected() { Debug.Log($"{name} hex tile is no longer selected"); }
        public void OnCommand(Vector3 worldPos) { Debug.Log($"{name} hex tile was given a command click"); }
    }
}