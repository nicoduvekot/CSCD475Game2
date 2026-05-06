using UnityEngine;

namespace Selection
{
    public class TestSelectableUnit : MonoBehaviour, ISelectable
    {
        public MonoBehaviour Behaviour => this;

        public void OnHoverEnter() { Debug.Log($"{name} unit is being hovered over"); }
        public void OnHoverExit() { Debug.Log($"{name} unit is not longer being hovered over"); }
        public void OnSelected() { Debug.Log($"{name} unit is selected operation"); }
        public void OnDeselected() { Debug.Log($"{name} unit is no longer selected"); }
        public void OnCommand(Vector3 worldPos) { Debug.Log($"{name} unit was given a command click for {worldPos}"); }
    }
}