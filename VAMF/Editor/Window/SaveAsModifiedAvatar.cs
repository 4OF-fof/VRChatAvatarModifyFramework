using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VAMF.Editor.Components.CustomPopup;
using VAMF.Editor.Schemas;
using VRC.Core;

namespace VAMF.Editor.Window {
    public class SaveAsModifiedAvatar : EditorWindow {
        private Dictionary<GameObject, int> _originalLayers = new Dictionary<GameObject, int>();
        private AssetData _newAssetData = new AssetData();
        private Vector2 _scrollPosition = Vector2.zero;
        private const int PreviewLayer = 30;
        private Color _backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private GameObject _currentTarget = null;
        private int _previousCullingMask;
        private bool _isInitialized = false;
        private RenderTexture _previewRT = null;

        [MenuItem("GameObject/Save as Modified Avatar", priority = -1000000)]
        private static void ShowWindow() {
            var window = GetWindow<SaveAsModifiedAvatar>("Save as Modified Avatar");
            window.minSize = new Vector2(768, 512);
            window.maxSize = new Vector2(768, 512);
        
            if (Selection.activeGameObject != null) {
                window._currentTarget = Selection.activeGameObject;
                window.StoreAndChangeLayer(window._currentTarget);
                window.FocusOnSelectedObject();
                window.UpdateSceneViewSettings();
            }
        
            window.Show();
        }

        [MenuItem("GameObject/Save as Modified Avatar", true)]
        private static bool ValidateShowWindow() {
            return Selection.activeGameObject                                    != null
                   && Selection.activeGameObject.GetComponent<PipelineManager>() != null;
        }

        private void OnEnable() {
            if (!_isInitialized) {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null) {
                    _previousCullingMask = sceneView.camera.cullingMask;
                }
                _isInitialized = true;
            }

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        
            if (Selection.activeGameObject != null) {
                _currentTarget = Selection.activeGameObject;
                StoreAndChangeLayer(_currentTarget);
                FocusOnSelectedObject();
            }

            UpdateSceneViewSettings();
        }

        private void OnDestroy() {
            CleanUp();
        }

        private void CleanUp() {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            RestoreOriginalLayers();
        
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) {
                sceneView.camera.cullingMask = _previousCullingMask;
            }

            if (_previewRT != null) {
                RenderTexture.ReleaseTemporary(_previewRT);
                _previewRT = null;
            }
        }

        private void UpdateSceneViewSettings() {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && _currentTarget != null) {
                sceneView.camera.cullingMask = 1 << PreviewLayer;
                sceneView.Repaint();
            }
        }

        private void OnEditorUpdate() {
            Repaint();
        }

        private void OnSceneGUI(SceneView sceneView) {
            UpdateSceneViewSettings();
        }

        private void FocusOnSelectedObject() {
            if (Selection.activeGameObject == null) return;

            var renderers = Selection.activeGameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float scale = 0.6f;
            Vector3 center = bounds.center;
            bounds.size *= scale;
            bounds.center = center;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) {
                sceneView.Frame(bounds, false);
                sceneView.rotation = Quaternion.Euler(0, -180, 0);
            }
        }

        private void StoreAndChangeLayer(GameObject obj) {
            if (obj == null) return;
        
            RestoreOriginalLayers();
            _originalLayers.Clear();
        
            var transforms = obj.GetComponentsInChildren<Transform>(true);
            foreach(var transform in transforms) {
                if (transform != null && transform.gameObject != null) {
                    _originalLayers[transform.gameObject] = transform.gameObject.layer;
                }
            }

            foreach(var transform in transforms) {
                if (transform != null && transform.gameObject != null) {
                    transform.gameObject.layer = PreviewLayer;
                }
            }
        }

        private void RestoreOriginalLayers() {
            foreach(var kvp in _originalLayers) {
                if(kvp.Key != null) {
                    kvp.Key.layer = kvp.Value;
                }
            }
            _originalLayers.Clear();
        }

        private void UpdatePreviewTexture() {
            if (_previewRT == null) {
                _previewRT = RenderTexture.GetTemporary(512, 512, 24);
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null) {
                RenderTexture previousRT = sceneView.camera.targetTexture;
                Color previousBackgroundColor = sceneView.camera.backgroundColor;
                CameraClearFlags previousClearFlags = sceneView.camera.clearFlags;
            
                sceneView.camera.targetTexture = _previewRT;
                sceneView.camera.backgroundColor = _backgroundColor;
                sceneView.camera.clearFlags = CameraClearFlags.SolidColor;
                sceneView.camera.Render();
            
                sceneView.camera.targetTexture = previousRT;
                sceneView.camera.backgroundColor = previousBackgroundColor;
                sceneView.camera.clearFlags = previousClearFlags;
            }
        }

        private void OnGUI() {
            using(new GUILayout.HorizontalScope()) {
                Rect previewRect = GUILayoutUtility.GetRect(512, 512);
                if (Event.current.type == EventType.Repaint) {
                    UpdatePreviewTexture();
                    if (_previewRT != null) {
                        GUI.DrawTexture(previewRect, _previewRT);
                    }
                }
            
                GUILayout.Space(20);
                using(var scrollView = new GUILayout.ScrollViewScope(_scrollPosition)) {
                    _scrollPosition = scrollView.scrollPosition;

                    GUILayout.Label("Name", Style.DetailTitle);
                    _newAssetData.name = GUILayout.TextField(_newAssetData.name, GUILayout.Width(210));

                    GUILayout.Label("Description", Style.DetailTitle);
                    float descriptionHeight = EditorStyles.textArea.CalcHeight(new GUIContent(_newAssetData.description), 210);
                    _newAssetData.description = EditorGUILayout.TextArea(
                        _newAssetData.description,
                        new GUIStyle(EditorStyles.textArea) { wordWrap = true },
                        GUILayout.Height(Mathf.Max(100, descriptionHeight)),
                        GUILayout.Width(210)
                    );

                    GUILayout.Label("Dependencies", Style.DetailTitle);
                    if (_newAssetData.dependencies != null && _newAssetData.dependencies.Count > 0) {
                        using(new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(210))) {
                            string dependencyToRemove = null;
                            foreach(var dependencyUid in _newAssetData.dependencies) {
                                var dependencyAsset = Utility.AssetDataController.GetAssetData(dependencyUid);
                                if(dependencyAsset != null) {
                                    using (new GUILayout.HorizontalScope(GUILayout.Width(180))) {
                                        GUILayout.Label(dependencyAsset.name, Style.DetailValue, GUILayout.Width(180));
                                        if(GUILayout.Button("×", GUILayout.Width(20))) {
                                            dependencyToRemove = dependencyUid;
                                        }
                                    }
                                }
                            }
                            if(dependencyToRemove != null) {
                                _newAssetData.dependencies.Remove(dependencyToRemove);
                            }
                        }
                    }
                    if(GUILayout.Button("Add Dependency", GUILayout.Width(210))) {
                        ItemSelector.ShowWindow(uid => {
                            if(uid != null) {
                                if(_newAssetData.dependencies == null) {
                                    _newAssetData.dependencies = new List<string>();
                                }
                                _newAssetData.dependencies.Add(uid);
                                Repaint();
                            }
                        }, Utility.AssetDataController.GetAllAssetData(), _newAssetData.dependencies);
                    }

                    GUILayout.Label("Old Versions", Style.DetailTitle);
                    if (_newAssetData.oldVersions != null && _newAssetData.oldVersions.Count > 0) {
                        using(new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(210))) {
                            string versionToRemove = null;
                            foreach(var oldVersion in _newAssetData.oldVersions) {
                                var oldVersionAsset = Utility.AssetDataController.GetAssetData(oldVersion);
                                if(oldVersionAsset != null) {
                                    using (new GUILayout.HorizontalScope(GUILayout.Width(180))) {
                                        GUILayout.Label(oldVersionAsset.name, Style.DetailValue, GUILayout.Width(180));
                                        if(GUILayout.Button("×", GUILayout.Width(20))) {
                                            versionToRemove = oldVersion;
                                        }
                                    }
                                }
                            }
                            if(versionToRemove != null) {
                                _newAssetData.oldVersions.Remove(versionToRemove);
                                var removedVersionAsset = Utility.AssetDataController.GetAssetData(versionToRemove);
                                if (removedVersionAsset != null) {
                                    removedVersionAsset.isLatest = true;
                                    Utility.AssetDataController.UpdateAssetData(removedVersionAsset.uid, removedVersionAsset);
                                }
                            }
                        }
                    }
                    if(GUILayout.Button("Add Old Version", GUILayout.Width(210))) {
                        ItemSelector.ShowWindow(uid => {
                            if(uid != null) {
                                if(_newAssetData.oldVersions == null) {
                                    _newAssetData.oldVersions = new List<string>();
                                }
                                _newAssetData.oldVersions.Add(uid);
                                var oldVersionAsset = Utility.AssetDataController.GetAssetData(uid);
                                if (oldVersionAsset != null) {
                                    oldVersionAsset.isLatest = false;
                                    Utility.AssetDataController.UpdateAssetData(oldVersionAsset.uid, oldVersionAsset);
                                }
                                Repaint();
                            }
                        }, Utility.AssetDataController.GetAllAssetData(), _newAssetData.oldVersions);
                    }

                    GUILayout.Label("Background Color", Style.DetailTitle);
                    _backgroundColor = EditorGUILayout.ColorField(_backgroundColor, GUILayout.Width(210));
                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    if(GUILayout.Button("Save", GUILayout.Width(210))) {
                        if(string.IsNullOrEmpty(_newAssetData.name)) {
                            EditorUtility.DisplayDialog("Error", "Name is required", "OK");
                            return;
                        }
                        string uid = Guid.NewGuid().ToString();
                        _newAssetData.thumbnailFilePath = SaveThumbnail(uid);
                        string packagePath = ExportUnityPackage(_currentTarget, uid, _newAssetData.name);
                        _newAssetData.filePath = packagePath;
                        _newAssetData.sourceFilePath = packagePath;
                        _newAssetData.uid = uid;
                        _newAssetData.isLatest = true;
                        _newAssetData.assetType = AssetType.Modified;
                        Utility.AssetDataController.AddAssetData(_newAssetData);
                        Close();
                    }
                    GUILayout.Space(10);
                }
            }
        }
        private string SaveThumbnail(string uid) {
            string thumbnailDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF", "Thumbnail", "Modified"
            ).Replace("\\", "/");
            Directory.CreateDirectory(thumbnailDir);
            string thumbnailPath = Path.Combine(thumbnailDir, $"{uid}.png");

            if(_previewRT != null) {
                UpdatePreviewTexture();
            
                RenderTexture.active = _previewRT;
                Texture2D screenshot = new Texture2D(512, 512, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                screenshot.Apply();
            
                byte[] bytes = screenshot.EncodeToPNG();
                File.WriteAllBytes(thumbnailPath, bytes);
            
                DestroyImmediate(screenshot);
                RenderTexture.active = null;
                return thumbnailPath.Replace("\\", "/").Replace(thumbnailDir, "Thumbnail/Modified");
            }
            return null;
        }

        private string ExportUnityPackage(GameObject currentTarget, string uid, string packageName) {
            List<string> dependencies = GetDependencies(currentTarget);
            HashCheck(dependencies, currentTarget);

            string oldPath = "Assets/_Modify";
            string newPath = $"Assets/{packageName}";
            if (AssetDatabase.IsValidFolder(oldPath)) {
                AssetDatabase.MoveAsset(oldPath, newPath);
                AssetDatabase.Refresh();
            }

            dependencies = GetDependencies(currentTarget);

            List<string> unityPackageDependencies = dependencies.Where(dependency => dependency.StartsWith($"Assets/{packageName}")).ToList();
            string packageDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF",
                "Modified"
            ).Replace("\\", "/");
            if(!Directory.Exists(packageDir)) {
                Directory.CreateDirectory(packageDir);
            }
            string packagePath = Path.Combine(
                packageDir,
                $"{uid}.unitypackage"
            ).Replace("\\", "/");
            AssetDatabase.ExportPackage(unityPackageDependencies.ToArray(), packagePath);

            if(AssetDatabase.IsValidFolder(newPath)) {
                AssetDatabase.MoveAsset(newPath, oldPath);
                AssetDatabase.Refresh();
            }

            RestoreOriginalReferences(currentTarget);

            return packagePath.Replace("\\", "/").Replace(packageDir, "Modified");
        }

        private List<string> GetDependencies(GameObject currentTarget) {
            string currentTargetPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(currentTarget);
            return AssetDatabase.GetDependencies(currentTargetPrefab).ToList();
        }

        private void HashCheck(List<string> dependencies, GameObject currentTarget) {
            SaveMaterialFiles();

            foreach(var dependency in dependencies) {
                string hash = CalculateFileHash(dependency);
                string importHistoryPath = Path.Combine(
                    Application.dataPath,
                    "VAMF", "Data", "import_history.json"
                ).Replace("\\", "/");
                string json = File.ReadAllText(importHistoryPath);
                var importHistory = JsonUtility.FromJson<Utility.PackageImportHistory>(json);
                var importHistoryFile = importHistory.files.Find(file => file.filePath == dependency);
                if(importHistoryFile != null) {
                    if(importHistoryFile.fileHash != hash) {
                        MoveAsset(dependency, currentTarget);
                    }
                }
            }
        }

        private void SaveMaterialFiles() {
        
            string importHistoryPath = Path.Combine(
                Application.dataPath,
                "VAMF", "Data", "import_history.json"
            ).Replace("\\", "/");
        
            if(!File.Exists(importHistoryPath)) {
                Debug.LogWarning($"JSON file not found: {importHistoryPath}");
                return;
            }
        
            try {
                string jsonContent = File.ReadAllText(importHistoryPath);
                var importHistory = JsonUtility.FromJson<Utility.PackageImportHistory>(jsonContent);
            
                if(importHistory?.files == null) {
                    Debug.LogWarning("Failed to parse JSON file");
                    return;
                }
            
                int savedCount = 0;
            
                foreach(var fileInfo in importHistory.files) {
                    string assetPath = fileInfo.filePath;
                    if(assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) && File.Exists(assetPath)) {
                        try {
                            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                            if(material != null) {
                                EditorUtility.SetDirty(material);
                                savedCount++;
                            }
                        }catch(Exception ex) {
                            Debug.LogError($"Error saving material: {assetPath}, Error: {ex.Message}");
                        }
                    }
                }
            
                if(savedCount > 0) {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }catch(Exception ex) {
                Debug.LogError($"Error saving material files: {ex.Message}");
            }
        }

        private void MoveAsset(string dependency, GameObject currentTarget) {
            string assetPath = dependency;
            string directoryPath = Path.Combine(
                "Assets",
                "_Modify",
                "System"
            ).Replace("\\", "/");
        
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }
        
            string newAssetPath = Path.Combine(
                directoryPath,
                Path.GetFileName(assetPath)
            ).Replace("\\", "/");
        
            if(File.Exists(newAssetPath)) {
                AssetDatabase.DeleteAsset(newAssetPath);
            }
        
            AssetDatabase.CopyAsset(assetPath, newAssetPath);
            AssetDatabase.Refresh();
        
            UnityEngine.Object originalAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            UnityEngine.Object newAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newAssetPath);
        
            if(originalAsset != null && newAsset != null) {
                Component[] components = currentTarget.GetComponentsInChildren<Component>(true);
                foreach(Component component in components) {
                    if(component == null) continue;
                
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();
                
                    bool modified = false;
                
                    while(property.Next(true)) {
                        if(property.propertyType         == SerializedPropertyType.ObjectReference && 
                           property.objectReferenceValue == originalAsset) {
                            property.objectReferenceValue = newAsset;
                            modified = true;
                        }
                    }
                
                    if(modified) {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(component);
                    }
                }
            
                EditorSceneManager.MarkSceneDirty(currentTarget.scene);

                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(currentTarget);
                if (prefabRoot != null) {
                    PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
                }
            }
        }

        private static string CalculateFileHash(string filePath) {
            try {
                using(var md5 = MD5.Create()) {
                    using(var stream = File.OpenRead(filePath)) {
                        byte[] hashBytes = md5.ComputeHash(stream);
                        StringBuilder sb = new StringBuilder();
                    
                        for(int i = 0; i < hashBytes.Length; i++) {
                            sb.Append(hashBytes[i].ToString("x2"));
                        }
                    
                        return sb.ToString();
                    }
                }
            }catch(Exception ex) {
                Debug.LogError($"Error calculating hash for file: {filePath}, Error: {ex.Message}");
                return "error_calculating_hash";
            }
        }

        private void RestoreOriginalReferences(GameObject currentTarget) {
            string importHistoryPath = Path.Combine(
                Application.dataPath,
                "VAMF", "Data", "import_history.json"
            ).Replace("\\", "/");

            if (!File.Exists(importHistoryPath)) return;

            string json = File.ReadAllText(importHistoryPath);
            var importHistory = JsonUtility.FromJson<Utility.PackageImportHistory>(json);

            Component[] components = currentTarget.GetComponentsInChildren<Component>(true);
            foreach (Component component in components) {
                if (component == null) continue;

                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty property = serializedObject.GetIterator();
                bool modified = false;

                while (property.Next(true)) {
                    if (property.propertyType         == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue != null) {
                        string assetPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                        if (assetPath.StartsWith("Assets/_Modify/")) {
                            string fileName = Path.GetFileName(assetPath);
                            var originalFile = importHistory.files.Find(f => Path.GetFileName(f.filePath) == fileName);
                            if (originalFile != null) {
                                UnityEngine.Object originalAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(originalFile.filePath);
                                if (originalAsset != null) {
                                    property.objectReferenceValue = originalAsset;
                                    modified = true;
                                }
                            }
                        }
                    }
                }

                if (modified) {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(component);
                }
            }

            EditorSceneManager.MarkSceneDirty(currentTarget.scene);

            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(currentTarget);
            if (prefabRoot != null) {
                PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
            }
        }

        private class Style {

            public static GUIStyle DetailTitle;
            public static GUIStyle DetailValue;

            static Style() {
                DetailTitle = new GUIStyle(EditorStyles.boldLabel);
                DetailTitle.fontSize = 15;
                DetailTitle.fontStyle = FontStyle.Bold;
                DetailTitle.margin = new RectOffset(0, 0, 5, 5);
                DetailValue = new GUIStyle(EditorStyles.label);
                DetailValue.fontSize = 12;
                DetailValue.alignment = TextAnchor.MiddleLeft;
                DetailValue.margin = new RectOffset(0, 0, 5, 5);
            }
        }
    }
}
