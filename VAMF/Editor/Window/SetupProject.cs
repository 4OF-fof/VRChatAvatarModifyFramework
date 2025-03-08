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
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null) return;
            var window = GetWindow<SetupProject>("Setup Project");
            window.minSize = new Vector2(350, 150);
            window.maxSize = new Vector2(350, 150);
            window._prefabObject = selectedObject;
            window.Show();
        }

        [MenuItem("Assets/Start Modification", true)]
        private static bool ValidateShowWindowFromContext() {
            var selectedObject = Selection.activeGameObject;
            return selectedObject != null;
        }

        private void OnGUI() {
            GUILayout.Label("Start Modification", EditorStyles.boldLabel);
            GUILayout.Space(10);
        
            var previousObject = _prefabObject;
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
        
            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_prefabObject);
        
            if(parentPrefab is null) {
                error = "Parent Prefab not found";
                return false;
            }
        
            var assetPath = AssetDatabase.GetAssetPath(parentPrefab);
            var isFbx = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

            if (isFbx || _fbxWarningResult.HasValue) return true;
            Repaint();
            return false;

        }
    
        private void StartModification() {
            if(!_isValid || _prefabObject is null) {
                return;
            }
            
            const string modifyFolderPath = "Assets/_Modify";
        
            if(!AssetDatabase.IsValidFolder(modifyFolderPath)) {
                try {
                    const string parentFolder = "Assets";
                    const string folderName = "_Modify";
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
                var fullPath = Path.Combine(Application.dataPath, "_Modify");
                var isEmpty = !Directory.EnumerateFileSystemEntries(fullPath).Any();
            
                if(!isEmpty) {
                    var shouldClear = EditorUtility.DisplayDialog(
                        "Confirmation",
                        "Existing files found in the Modify folder.\n"       +
                        "Continuing will delete all files in this folder.\n" +
                        "Do you want to continue?",
                        "Yes, delete and continue",
                        "No, cancel"
                    );
                
                    if(shouldClear) {
                        try {
                            var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                        
                            foreach(var file in files) {
                                var filePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                                AssetDatabase.DeleteAsset(filePath);
                            }
                        
                            var directories = Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories)
                                .OrderByDescending(d => d.Length).ToArray();
                            
                            foreach(var dir in directories) {
                                var dirPath = "Assets" + dir.Substring(Application.dataPath.Length).Replace('\\', '/');
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
        
            var newPrefabPath = $"{modifyFolderPath}/{_prefabObject.name}_Modified.prefab";
        
            if(File.Exists(newPrefabPath)) {
                AssetDatabase.DeleteAsset(newPrefabPath);
            }
        
            var instanceInScene = PrefabUtility.InstantiatePrefab(_prefabObject) as GameObject;
            var newPrefabVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(instanceInScene, newPrefabPath, InteractionMode.AutomatedAction);
        
            if(newPrefabVariant is not null) {
                Selection.activeGameObject = instanceInScene;
            
                EditorUtility.DisplayDialog("Success", 
                    "Modification successful!\n"   +
                    "Created new prefab variant: " + newPrefabPath + "\n" +
                    "The prefab has been placed in the hierarchy.",
                    "OK");
            } else {
                if(instanceInScene is not null) {
                    DestroyImmediate(instanceInScene);
                }
            
                EditorUtility.DisplayDialog("Error", 
                    "Failed to create prefab variant in: " + newPrefabPath,
                    "OK");
            }
        }

        private void Update() {
            if (_prefabObject == null) return;
            var parentPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_prefabObject);
            if (parentPrefab == null) return;
            var assetPath = AssetDatabase.GetAssetPath(parentPrefab);
            var isFbx = assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

            if (isFbx || _fbxWarningResult.HasValue) return;
            EditorUtility.DisplayDialog(
                "Warning",
                "The parent prefab is not an FBX file.\n" +
                "File path: " + assetPath,
                "OK"
            );
            _fbxWarningResult = true;
            Repaint();
        }
    }
}
