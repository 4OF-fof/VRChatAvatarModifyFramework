using UnityEditor;
using UnityEngine;
using VRC.Core;

public class SaveAsModifiedAvatar : EditorWindow {
    [MenuItem("GameObject/Save as Modified Avatar", priority = -1000000)]
    private static void ShowWindow() {
        var window = GetWindow<SaveAsModifiedAvatar>("Save as Modified Avatar");
        window.Show();
    }

    [MenuItem("GameObject/Save as Modified Avatar", true)]
    private static bool ValidateShowWindow() {
        return Selection.activeGameObject != null
        && Selection.activeGameObject.GetComponent<PipelineManager>() != null;
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Save as Modified Avatar");
    }
}
