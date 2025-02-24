using UnityEditor;
using UnityEngine;
using VRC.Core;
using System.Collections.Generic;
using System;

public class SaveAsModifiedAvatar : EditorWindow {
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private AssetData newAssetData = new AssetData();
    private Vector2 scrollPosition = Vector2.zero;
    private const int PREVIEW_LAYER = 30;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private GameObject currentTarget = null;
    private int previousCullingMask;
    private bool isInitialized = false;

    [MenuItem("GameObject/Save as Modified Avatar", priority = -1000000)]
    private static void ShowWindow() {
        var window = GetWindow<SaveAsModifiedAvatar>("Save as Modified Avatar");
        window.minSize = new Vector2(768, 512);
        window.maxSize = new Vector2(768, 512);
        
        if (Selection.activeGameObject != null) {
            window.currentTarget = Selection.activeGameObject;
            window.StoreAndChangeLayer(window.currentTarget);
            window.FocusOnSelectedObject();
            window.UpdateSceneViewSettings();
        }
        
        window.Show();
    }

    [MenuItem("GameObject/Save as Modified Avatar", true)]
    private static bool ValidateShowWindow() {
        return Selection.activeGameObject != null
        && Selection.activeGameObject.GetComponent<PipelineManager>() != null;
    }

    private void OnEnable() {
        if (!isInitialized) {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) {
                previousCullingMask = sceneView.camera.cullingMask;
            }
            isInitialized = true;
        }

        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += OnEditorUpdate;
        
        if (Selection.activeGameObject != null) {
            currentTarget = Selection.activeGameObject;
            StoreAndChangeLayer(currentTarget);
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
            sceneView.camera.cullingMask = previousCullingMask;
        }
    }

    private void UpdateSceneViewSettings() {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && currentTarget != null) {
            sceneView.camera.cullingMask = 1 << PREVIEW_LAYER;
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
        originalLayers.Clear();
        
        var transforms = obj.GetComponentsInChildren<Transform>(true);
        foreach(var transform in transforms) {
            if (transform != null && transform.gameObject != null) {
                originalLayers[transform.gameObject] = transform.gameObject.layer;
            }
        }

        foreach(var transform in transforms) {
            if (transform != null && transform.gameObject != null) {
                transform.gameObject.layer = PREVIEW_LAYER;
            }
        }
    }

    private void RestoreOriginalLayers() {
        foreach(var kvp in originalLayers) {
            if(kvp.Key != null) {
                kvp.Key.layer = kvp.Value;
            }
        }
        originalLayers.Clear();
    }

    private void OnGUI() {
        using(new GUILayout.HorizontalScope()) {
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            if (Event.current.type == EventType.Repaint) {
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.camera != null) {
                    RenderTexture tempRT = RenderTexture.GetTemporary(512, 512, 24);
                    RenderTexture previousRT = sceneView.camera.targetTexture;
                    Color previousBackgroundColor = sceneView.camera.backgroundColor;
                    CameraClearFlags previousClearFlags = sceneView.camera.clearFlags;
                    
                    sceneView.camera.targetTexture = tempRT;
                    sceneView.camera.backgroundColor = backgroundColor;
                    sceneView.camera.clearFlags = CameraClearFlags.SolidColor;
                    sceneView.camera.Render();
                    
                    GUI.DrawTexture(previewRect, tempRT);
                    
                    sceneView.camera.targetTexture = previousRT;
                    sceneView.camera.backgroundColor = previousBackgroundColor;
                    sceneView.camera.clearFlags = previousClearFlags;
                    RenderTexture.ReleaseTemporary(tempRT);
                }
            }
            
            GUILayout.Space(20);
            using(var scrollView = new GUILayout.ScrollViewScope(scrollPosition)) {
                scrollPosition = scrollView.scrollPosition;

                GUILayout.Label("Name", Style.detailTitle);
                newAssetData.name = GUILayout.TextField(newAssetData.name, GUILayout.Width(210));

                GUILayout.Label("Description", Style.detailTitle);
                float descriptionHeight = EditorStyles.textArea.CalcHeight(new GUIContent(newAssetData.description), 210);
                newAssetData.description = EditorGUILayout.TextArea(
                    newAssetData.description,
                    new GUIStyle(EditorStyles.textArea) { wordWrap = true },
                    GUILayout.Height(Mathf.Max(100, descriptionHeight)),
                    GUILayout.Width(210)
                );

                GUILayout.Label("Dependencies", Style.detailTitle);
                if (newAssetData.dependencies != null && newAssetData.dependencies.Count > 0) {
                    using(new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(210))) {
                        string dependencyToRemove = null;
                        foreach(var dependencyUid in newAssetData.dependencies) {
                            var dependencyAsset = Utility.AssetDataController.GetAssetData(dependencyUid);
                            if(dependencyAsset != null) {
                                using (new GUILayout.HorizontalScope(GUILayout.Width(180))) {
                                    GUILayout.Label(dependencyAsset.name, Style.detailValue, GUILayout.Width(180));
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        dependencyToRemove = dependencyUid;
                                    }
                                }
                            }
                        }
                        if(dependencyToRemove != null) {
                            newAssetData.dependencies.Remove(dependencyToRemove);
                        }
                    }
                }
                if(GUILayout.Button("Add Dependency", GUILayout.Width(210))) {
                    ItemSelector.ShowWindow(uid => {
                        if(uid != null) {
                            if(newAssetData.dependencies == null) {
                                newAssetData.dependencies = new List<string>();
                            }
                            newAssetData.dependencies.Add(uid);
                            Repaint();
                        }
                    }, Utility.AssetDataController.GetAllAssetData(), newAssetData.dependencies);
                }

                GUILayout.Label("Old Versions", Style.detailTitle);
                if (newAssetData.oldVersions != null && newAssetData.oldVersions.Count > 0) {
                    using(new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(210))) {
                        string versionToRemove = null;
                        foreach(var oldVersion in newAssetData.oldVersions) {
                            var oldVersionAsset = Utility.AssetDataController.GetAssetData(oldVersion);
                            if(oldVersionAsset != null) {
                                using (new GUILayout.HorizontalScope(GUILayout.Width(180))) {
                                    GUILayout.Label(oldVersionAsset.name, Style.detailValue, GUILayout.Width(180));
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        versionToRemove = oldVersion;
                                    }
                                }
                            }
                        }
                        if(versionToRemove != null) {
                            newAssetData.oldVersions.Remove(versionToRemove);
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
                            if(newAssetData.oldVersions == null) {
                                newAssetData.oldVersions = new List<string>();
                            }
                            newAssetData.oldVersions.Add(uid);
                            var oldVersionAsset = Utility.AssetDataController.GetAssetData(uid);
                            if (oldVersionAsset != null) {
                                oldVersionAsset.isLatest = false;
                                Utility.AssetDataController.UpdateAssetData(oldVersionAsset.uid, oldVersionAsset);
                            }
                            Repaint();
                        }
                    }, Utility.AssetDataController.GetAllAssetData(), newAssetData.oldVersions);
                }

                GUILayout.Label("Background Color", Style.detailTitle);
                backgroundColor = EditorGUILayout.ColorField(backgroundColor, GUILayout.Width(210));
                GUILayout.Space(10);

                GUILayout.FlexibleSpace();

                if(GUILayout.Button("Save", GUILayout.Width(210))) {
                    if(newAssetData.name == "") {
                        EditorUtility.DisplayDialog("Error", "Name is required", "OK");
                        return;
                    }
                    string uid = Guid.NewGuid().ToString();
                    newAssetData.uid = uid;
                    //TODO: Thumbnail and UnityPackage
                    newAssetData.isLatest = true;
                    Utility.AssetDataController.AddAssetData(newAssetData);
                }
                GUILayout.Space(10);
            }
        }
    }
        private class Style {

        public static GUIStyle detailTitle;
        public static GUIStyle detailValue;

        static Style() {
            detailTitle = new GUIStyle(EditorStyles.boldLabel);
            detailTitle.fontSize = 15;
            detailTitle.fontStyle = FontStyle.Bold;
            detailTitle.margin = new RectOffset(0, 0, 5, 5);
            detailValue = new GUIStyle(EditorStyles.label);
            detailValue.fontSize = 12;
            detailValue.alignment = TextAnchor.MiddleLeft;
            detailValue.margin = new RectOffset(0, 0, 5, 5);
        }
    }
}
