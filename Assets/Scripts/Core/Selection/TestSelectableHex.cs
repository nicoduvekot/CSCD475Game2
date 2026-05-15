using UnityEngine;
using DataDefinitions;
using Core.UIElements;
using Units;

namespace Selection
{
    public class TestSelectableHex : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;
        
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private BuildingData attachedBuilding;
        
        public ResourceType ResourceType => resourceType;
        public BuildingData AttachedBuilding => attachedBuilding;
        
        private BaseUnit _occupyingUnit;
        private MonoBehaviour _occupyingBuilding;

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
        
        public bool TryGetOccupant(out MonoBehaviour occupant)
        {
            if (_occupyingBuilding != null)
            {
                occupant = _occupyingBuilding;
                return true;
            }

            if (_occupyingUnit != null)
            {
                occupant = _occupyingUnit;
                return true;
            }

            occupant = null;
            return false;
        }
        
        public bool TrySetUnitOccupant(BaseUnit unit)
        {
            if (_occupyingBuilding != null)
            {
                Debug.LogError($"Hex {name} has a building. Units cannot occupy this hex.");
                return false;
            }

            _occupyingUnit = unit;
            return true;
        }
        
        public bool TryClearUnitOccupant(BaseUnit unit)
        {
            if (unit == null)
            {
                Debug.LogError($"Hex {name}: TryClearUnitOccupant called with null requester.");
                return false;
            }
            
            if (_occupyingUnit == null)
            {
                Debug.LogWarning($"Hex {name}: No unit to clear, but {unit.name} attempted to clear occupancy.");
                return false;
            }
            
            if (_occupyingUnit != unit)
            {
                Debug.LogError(
                    $"Hex {name}: {unit.name} attempted to clear occupancy, " +
                    $"but the current occupant is {_occupyingUnit.name}. Only the occupant should clear itself."
                );
                return false;
            }

            _occupyingUnit = null;
            return true;
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