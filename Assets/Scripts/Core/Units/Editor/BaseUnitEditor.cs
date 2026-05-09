using UnityEngine;
using UnityEditor;

namespace Units.Editor
{
    [CustomEditor(typeof(BaseUnit), true)]
    public class BaseUnitEditor : UnityEditor.Editor
    {
        private float debugDamage = 10f;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

                debugDamage = EditorGUILayout.FloatField("Damage Amount", debugDamage);

                if (GUILayout.Button("Apply Damage"))
                {
                    BaseUnit unit = (BaseUnit)target;
                    unit.TakeDamage(debugDamage);
                }
            }
        }
    }
}