using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UtilitySystem))]
[CanEditMultipleObjects]
public class UtilitySystemEditor : Editor
{
    UtilitySystem system;

    private void OnEnable()
    {
        system = (UtilitySystem)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button("Add utility"))
        {
            GameObject newUtility = new GameObject("Utility_" + system.utilities?.Length);
            newUtility.transform.parent = system.transform;
            newUtility.AddComponent<Utility>();
        }
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
    }
}