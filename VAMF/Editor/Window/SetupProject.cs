using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class SetupProject : EditorWindow {
    private GameObject prefabObject;
    private string errorMessage = "";
    private bool isValid = false;
    
    [MenuItem("VAMF/Setup Project", priority = 0)]
    public static void ShowWindow() {
        SetupProject window = GetWindow<SetupProject>("Setup Project");
        window.minSize = new Vector2(350, 150);
        window.maxSize = new Vector2(350, 150);
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Setup Project", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GameObject previousObject = prefabObject;
        prefabObject = (GameObject)EditorGUILayout.ObjectField("Select Base Avatar", prefabObject, typeof(GameObject), false);
        
        if(previousObject != prefabObject) {
            errorMessage = "";
            isValid = false;
        }
        
        if(prefabObject != null) {
            if(string.IsNullOrEmpty(errorMessage)) {
                isValid = CheckVariant(out errorMessage);
            }
        }else {
            errorMessage = "Select Base Avatar";
            isValid = false;
        }
        
        GUILayout.Space(10);
        
        if(!string.IsNullOrEmpty(errorMessage)) {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
        
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!isValid);
        if(GUILayout.Button("Start Modification")) {
            StartModification();
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(10);
    }
    
    private bool CheckVariant(out string error) {
        error = "";
        
        if(prefabObject == null) {
            error = "Base Avatar is not selected";
            return false;
        }
        
        PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabObject);
        
        if(prefabType != PrefabAssetType.Variant) {
            error = "The selected object is not a Prefab Variant\n" +
                   "Current type: " + prefabType.ToString();
            return false;
        }
        
        GameObject parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObject);
        
        if(parentPrefab == null) {
            error = "Parent Prefab not found";
            return false;
        }
        
        string assetPath = AssetDatabase.GetAssetPath(parentPrefab);
        bool isFbx = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        
        if(!isFbx) {
            error = "The parent prefab is not an FBX file\n" +
                   "File path: " + assetPath;
            return false;
        }
        
        return true;
    }
    
    private void StartModification() {
        if(!isValid || prefabObject == null) {
            return;
        }
        
        GameObject parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObject);
        string assetPath = AssetDatabase.GetAssetPath(parentPrefab);
        string modifyFolderPath = "Assets/_Modify";
        
        if(!AssetDatabase.IsValidFolder(modifyFolderPath)) {
            try {
                string parentFolder = "Assets";
                string folderName = "_Modify";
                AssetDatabase.CreateFolder(parentFolder, folderName);
                AssetDatabase.Refresh();
                Debug.Log($"Created directory: {modifyFolderPath}");
            }catch(System.Exception e) {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to create Modify folder: {e.Message}",
                    "OK");
                return;
            }
        }else {
            string fullPath = Path.Combine(Application.dataPath, "_Modify");
            bool isEmpty = !Directory.EnumerateFileSystemEntries(fullPath).Any();
            
            if(!isEmpty) {
                bool shouldClear = EditorUtility.DisplayDialog(
                    "Confirmation",
                    "Existing files found in the Modify folder.\n" +
                    "Continuing will delete all files in this folder.\n" +
                    "Do you want to continue?",
                    "Yes, delete and continue",
                    "No, cancel"
                );
                
                if(shouldClear) {
                    try {
                        string[] files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                        
                        foreach(string file in files) {
                            string filePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                            AssetDatabase.DeleteAsset(filePath);
                        }
                        
                        string[] directories = Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories)
                            .OrderByDescending(d => d.Length).ToArray();
                            
                        foreach(string dir in directories) {
                            string dirPath = "Assets" + dir.Substring(Application.dataPath.Length).Replace('\\', '/');
                            AssetDatabase.DeleteAsset(dirPath);
                        }
                        
                        AssetDatabase.Refresh();
                        Debug.Log("Deleted contents of the Modify folder");
                    }catch(System.Exception e) {
                        EditorUtility.DisplayDialog("Error", 
                            $"Failed to delete contents of the Modify folder: {e.Message}",
                            "OK");
                        return;
                    }
                }else {
                    Debug.Log("Operation cancelled");
                    return;
                }
            }
        }
        
        string newPrefabPath = $"{modifyFolderPath}/{prefabObject.name}_Modified.prefab";
        
        if(System.IO.File.Exists(newPrefabPath)) {
            AssetDatabase.DeleteAsset(newPrefabPath);
        }
        
        GameObject instanceInScene = PrefabUtility.InstantiatePrefab(prefabObject) as GameObject;
        GameObject newPrefabVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(instanceInScene, newPrefabPath, InteractionMode.AutomatedAction);
        
        if(newPrefabVariant != null) {
            Selection.activeGameObject = instanceInScene;
            
            EditorUtility.DisplayDialog("Success", 
                "Modification successful!\n" +
                "Created new prefab variant: " + newPrefabPath + "\n" +
                "The prefab has been placed in the hierarchy.",
                "OK");
        } else {
            if(instanceInScene != null) {
                DestroyImmediate(instanceInScene);
            }
            
            EditorUtility.DisplayDialog("Error", 
                "Failed to create prefab variant in: " + newPrefabPath,
                "OK");
        }
    }
}
