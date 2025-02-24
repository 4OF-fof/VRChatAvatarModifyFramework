using UnityEditor;
using UnityEngine;
using VRC.Core;
using System.Collections.Generic;

public class SaveAsModifiedAvatar : EditorWindow {
    private Camera previewCamera;
    private GameObject cameraObject;
    private RenderTexture renderTexture;
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private AssetData newAssetData = new AssetData();
    private Vector2 scrollPosition = Vector2.zero;
    private const int PREVIEW_LAYER = 30;

    [MenuItem("GameObject/Save as Modified Avatar", priority = -1000000)]
    private static void ShowWindow() {
        var window = GetWindow<SaveAsModifiedAvatar>("Save as Modified Avatar");
        window.minSize = new Vector2(768, 512);
        window.maxSize = new Vector2(768, 512);
        window.Show();
    }

    [MenuItem("GameObject/Save as Modified Avatar", true)]
    private static bool ValidateShowWindow() {
        return Selection.activeGameObject != null
        && Selection.activeGameObject.GetComponent<PipelineManager>() != null;
    }

    private void OnEnable() {
        cameraObject = new GameObject("Preview Camera");
        previewCamera = cameraObject.AddComponent<Camera>();
        
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
        
        // Create and setup render texture
        renderTexture = new RenderTexture(512, 512, 24);
        renderTexture.antiAliasing = 8;
        previewCamera.targetTexture = renderTexture;
        previewCamera.aspect = 1.0f;
        previewCamera.cullingMask = 1 << PREVIEW_LAYER;
        
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition() {
        if(Selection.activeGameObject == null) return;

        // Store and change layer settings for preview
        StoreAndChangeLayer(Selection.activeGameObject);

        //Calculate diagonal length of bounding box from all avatar renderers
        var renderers = Selection.activeGameObject.GetComponentsInChildren<Renderer>();
        if(renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for(int i = 1; i < renderers.Length; i++) {
            bounds.Encapsulate(renderers[i].bounds);
        }
        float diagonal = bounds.size.magnitude;
        
        //Calculate distance needed to show full body
        float fov = 60f;
        previewCamera.fieldOfView = fov;
        float distance = (diagonal * 0.25f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        
        //Set camera position (offset 5% downward)
        previewCamera.transform.position = bounds.center + Vector3.forward * distance + Vector3.down * (bounds.size.y * 0.05f);
        
        //Set camera rotation
        Vector3 lookAtPoint = new Vector3(bounds.center.x, previewCamera.transform.position.y, bounds.center.z);
        previewCamera.transform.LookAt(lookAtPoint);
    }

    private void StoreAndChangeLayer(GameObject obj) {
        originalLayers.Clear();
        var transforms = obj.GetComponentsInChildren<Transform>(true);
        foreach(var transform in transforms) {
            originalLayers[transform.gameObject] = transform.gameObject.layer;
            transform.gameObject.layer = PREVIEW_LAYER;
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

    private void OnDisable() {
        RestoreOriginalLayers();
        if(cameraObject != null) {
            DestroyImmediate(cameraObject);
        }
        if(renderTexture != null) {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }

    private void OnGUI() {
        using(new GUILayout.HorizontalScope()) {
            if(renderTexture != null) {
                // Calculate centered rect for preview
                Rect previewRect = GUILayoutUtility.GetRect(512, 512);
                EditorGUI.DrawPreviewTexture(previewRect, renderTexture);
                Repaint();
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
