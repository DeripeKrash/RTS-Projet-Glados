using AI;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    // [CustomEditor(typeof(BlackBoard))]
    // public class BlackboardInspector : UnityEditor.Editor
    // {
    //     private BlackBoard _blackBoard;
    //     
    //     public override void OnInspectorGUI()
    //     {
    //         _blackBoard = target as BlackBoard;
    //
    //
    //         EditorGUILayout.LabelField("blackboard");
    //
    //         if (_blackBoard.Blackboard.Keys.Count == 0)
    //         {
    //             EditorGUILayout.LabelField("    (Empty)");
    //         }
    //
    //         foreach (string key in _blackBoard.Blackboard.Keys)
    //         {
    //             GUILayout.BeginHorizontal();
    //
    //             EditorGUILayout.LabelField("    " + key);
    //             EditorGUILayout.LabelField("    " + _blackBoard.Blackboard[key]);
    //             GUILayout.EndHorizontal();
    //         }
    //
    //         base.OnInspectorGUI();
    //     }
    // }
}