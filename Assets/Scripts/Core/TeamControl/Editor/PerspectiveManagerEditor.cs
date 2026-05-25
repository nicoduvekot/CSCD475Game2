using UnityEditor;
using UnityEngine;

namespace TeamControl.Editor
{
    [CustomEditor(typeof(PerspectiveManager))]
    public class PerspectiveManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PerspectiveManager manager = (PerspectiveManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

            // Dropdown for perspective
            Perspective newPerspective = (Perspective)EditorGUILayout.EnumPopup(
                "Current Perspective",
                manager.CurrentPerspective
            );

            // If changed, apply immediately
            if (newPerspective != manager.CurrentPerspective)
            {
                if (Application.isPlaying)
                {
                    manager.SetPerspective(newPerspective);
                }
                else
                {
                    // Edit mode preview
                    manager.SetPerspective(newPerspective);
                    EditorUtility.SetDirty(manager);
                }
            }
        }
    }
}