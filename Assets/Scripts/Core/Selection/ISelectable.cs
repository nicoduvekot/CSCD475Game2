using UnityEngine;

namespace Selection
{
    public interface ISelectable
    {
        // implied that this interface belongs on only Monobehaviours
        MonoBehaviour Behaviour { get; }
        
        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public void OnSelected() { }
        public void OnDeselected() { }
        public void OnCommand(Vector3 worldPos) { }
    }
}