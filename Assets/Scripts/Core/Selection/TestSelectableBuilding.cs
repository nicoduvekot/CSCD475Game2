using UnityEngine;

namespace Selection
{
    public class TestSelectableBuilding : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;

        public void OnHoverEnter() { Debug.Log($"{name} building is being hovered over"); }
        public void OnHoverExit() { Debug.Log($"{name} building is not longer being hovered over"); }
        public void OnSelected() { Debug.Log($"{name} building is selected operation"); }
        public void OnDeselected() { Debug.Log($"{name} building is no longer selected"); }
        public void OnCommand(Vector3 worldPos) { Debug.Log($"{name} building was given a command click"); }
    }
}