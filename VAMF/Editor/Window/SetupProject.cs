using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VAMF.Editor.Window {
    public class SetupProject : EditorWindow {
        private GameObject _prefabObject;
        private string _errorMessage = "";
        private bool _isValid;
        private bool? _fbxWarningResult;
    
        [MenuItem("Assets/Start Modification", priority = 0)]
        private static void ShowWindowFromContext() {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null) {
                SetupProject window = GetWindow<SetupProject>("Setup Project");
                window.minSize = new Vector2(350, 150);
                window.maxSize = new Vector2(350, 150);
                window._prefabObject = selectedObject;
                window.Show();
            }
        }

        [MenuItem("Assets/Start Modification", true)]
        private static bool ValidateShowWindowFromContext() {
            GameObject selectedObject = Selection.activeGameObject;
            return selectedObject != null;
        }

        void OnGUI() {
            GUILayout.Label("Start Modification", EditorStyles.boldLabel);
            GUILayout.Space(10);
        
            GameObject previousObject = _prefabObject;
            _prefabObject = (GameObject)EditorGUILayout.ObjectField("Select Base Avatar", _prefabObject, typeof(GameObject), false);
        
            if(previousObject != _prefabObject) {
                _errorMessage = "";
                _isValid = false;
                _fbxWarningResult = null;
            }
        
            if(_prefabObject is not null) {
                if(string.IsNullOrEmpty(_errorMessage)) {
                    _isValid = CheckVariant(out _errorMessage);
                }
            }else {
                _errorMessage = "Select Base Avatar";
                _isValid = false;
            }
        
            GUILayout.Space(10);
        
            if(!string.IsNullOrEmpty(_errorMessage)) {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }
        
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!_isValid);
            if(GUILayout.Button("Start Modification")) {
                StartModification();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);
        }
    
        private bool CheckVariant(out string error) {
            error = "";
        
            if(_prefabObject is null) {
                error = "Base Avatar is not selected";
                return false;
            }
        
            GameObject parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_prefabObject);
        
            if(parentPrefab == null) {
                error = "Parent Prefab not found";
                return false;
            }
        
            string assetPath = AssetDatabase.GetAssetPath(parentPrefab);
            bool isFbx = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        
            if(!isFbx && !_fbxWarningResult.HasValue) {
                Repaint();
                return false;
            }
        
            return true;
        }
    
        private void StartModification() {
            if(!_isValid || _prefabObject is null) {
                return;
            }
            
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
                        "Existing files found in the Modify folder.\n"       +
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
        
            string newPrefabPath = $"{modifyFolderPath}/{_prefabObject.name}_Modified.prefab";
        
            if(File.Exists(newPrefabPath)) {
                AssetDatabase.DeleteAsset(newPrefabPath);
            }
        
            GameObject instanceInScene = PrefabUtility.InstantiatePrefab(_prefabObject) as GameObject;
            GameObject newPrefabVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(instanceInScene, newPrefabPath, InteractionMode.AutomatedAction);
        
            if(newPrefabVariant != null) {
                Selection.activeGameObject = instanceInScene;
            
                EditorUtility.DisplayDialog("Success", 
                    "Modification successful!\n"   +
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

        void Update() {
            if(_prefabObject != null) {
                GameObject parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_prefabObject);
                if(parentPrefab != null) {
                    string assetPath = AssetDatabase.GetAssetPath(parentPrefab);
                    bool isFbx = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
                
                    if(!isFbx && !_fbxWarningResult.HasValue) {
                        EditorUtility.DisplayDialog(
                            "Warning",
                            "The parent prefab is not an FBX file.\n" +
                            "File path: "                             + assetPath,
                            "OK"
                        );
                        _fbxWarningResult = true;
                        Repaint();
                    }
                }
            }
        }
    }
}
