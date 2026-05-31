using Units;
using UnityEditor;
using UnityEngine;

namespace Resource.Editor
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
        // a reference to the game manager
        private GameManager _gameManager;
        // a reference to the selected enum dropdown
        private UnitOwner _winner;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            // this is the game object currently being inspected
            // it takes the target (a GameObject)
            // and casts it as a GameManager
            _gameManager = (GameManager)target;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Runtime Tools", EditorStyles.boldLabel);
            
            // standard practice for not allowing runtime calls to happen when editor is not playing
            // NOTE: anything below this will not appear in editor, unless editor is playing
            // Anything above will still function
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime tools are only available in Play Mode.",
                    MessageType.Info
                );
                return;
            }
            
            // This button is for use of manual call to:
            // GameManager reset functionality
            if (GUILayout.Button("Reset Game Manager"))
            {
                _gameManager.reset();
            }
            
            
            EditorGUILayout.Space(10);
            
            // this enum dropdown is so inspector can pick the winner for the override function
            _winner = (UnitOwner)EditorGUILayout.EnumPopup("Set Owner", _winner);
            
            // this button is for use of manual call to:
            // GameManager gameOver function
            // NOTE: uses the enum dropdown value above as the winner
            if (GUILayout.Button("End Game Override"))
            {
                if (_winner == UnitOwner.World)
                {
                    Debug.LogError($"[TestAgentEditor]: '{_winner}' can not win a game");
                    return;
                }
            
                _gameManager.gameOver(_winner);
            }
        }
    }
}