using UnityEngine;

namespace Selection
{
    public interface ISelectable
    {
        MonoBehaviour Behaviour { get; }
        
        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        
        public void OnSelected() { }
        public void OnDeselected() { }
        
        public void OnCommand(Vector3 worldPos, ISelectable targetSelectable) { }
        
        public void OnPreviewCommand(Vector3 worldPos, ISelectable targetSelectable) { }
        public void OnPreviewCancel() { }
        
        //Consider the Mono behavior usage above if trying to bring this back
        //public GameObject GetGameObject();
    }
}