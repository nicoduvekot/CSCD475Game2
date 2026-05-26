using UnityEngine;
using UnityEditor;

namespace Units.Editor
{
    [CustomEditor(typeof(BaseUnit), true)]
    public class BaseUnitEditor : UnityEditor.Editor
    {
        private float _debugDamage = 10f;
        private UnitOwner _selectedOwner = UnitOwner.Player;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            BaseUnit unit = (BaseUnit)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Tools", EditorStyles.boldLabel);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime tools are only available in Play Mode.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.LabelField("Owner", unit.Owner.ToString());
            
            EditorGUILayout.Space(5);
            
            _selectedOwner = (UnitOwner)EditorGUILayout.EnumPopup("Set Owner", _selectedOwner);
            
            if (GUILayout.Button("Apply Owner to This Unit"))
            {
                unit.debugInitializeOwner(_selectedOwner);
                Debug.Log($"[Editor] Set owner of {unit.name} to {_selectedOwner}");
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            // Damage amount field
            _debugDamage = EditorGUILayout.FloatField("Damage Amount", _debugDamage);

            // Apply damage button
            // if (GUILayout.Button("Apply Damage"))
            // {
            //     unit.TakeDamage(_debugDamage);
            //     Debug.Log($"[Editor] {unit.name} took {_debugDamage} damage from editor action");
            // }
        }
    }
}