using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VAMF.Editor.Schemas;
using VAMF.Editor.Utility;
using VAMF.Editor.Window;

namespace VAMF.Editor.Components.CustomPopup {
    public class DetailWindow: EditorWindow {
        private bool _editMode;
        private Vector2 _scrollPosition = Vector2.zero;

        private AssetData _assetData;
        private AssetData _tmpAssetData;
        private Stack<AssetData> _history = new();

        public static void ShowWindow(AssetData assetData, Stack<AssetData> previousHistory = null) {
            if(assetData == null) {
                Debug.LogError("AssetData is null");
                return;
            }

            var window = GetWindow<DetailWindow>($"Asset Details");
        
            var windowSize = new Vector2(610, 400);
            window.minSize = windowSize;
            window.maxSize = windowSize;
        
            window._assetData = assetData;
            window._history = previousHistory ?? new Stack<AssetData>();
            window._editMode = false;

            window.Show();
        }

        private void OnGUI() {
            {/*-------------------- Header --------------------*/}

            GUILayout.Space(10);
            using(new GUILayout.HorizontalScope()) {
                if(_history.Count > 0) {
                    if(GUILayout.Button("Back", Style.BackButton)) {
                        var previousAsset = _history.Pop();
                        var newHistory = new Stack<AssetData>(_history.ToArray().Reverse());
                        ShowWindow(previousAsset, newHistory);
                        return;
                    }
                }
                GUILayout.FlexibleSpace();
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    if(!_editMode && _history.Count == 0) {
                        GUILayout.Label("Asset Details", Style.WindowTitle);
                    }else if(!_editMode && _history.Count > 0) {
                        GUILayout.Label("Asset Details", Style.WindowTitleBack);
                    }else if(_editMode && _history.Count == 0) {
                        GUILayout.Label("Edit Asset Details", Style.WindowTitleEdit);
                    }else if(_editMode && _history.Count > 0) {
                        GUILayout.Label("Edit Asset Details", Style.WindowTitleEditBack);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    if(!_editMode) {
                        if(GUILayout.Button("Edit", Style.EditButton)) {
                            _editMode = true;
                            _tmpAssetData = _assetData.Clone();
                        }
                    } else {
                        using(new GUILayout.HorizontalScope()) {
                            if(GUILayout.Button("Save", Style.SaveButton)) {
                                _assetData = _tmpAssetData.Clone();
                                AssetDataController.UpdateAssetData(_assetData.uid, _assetData);
                                _tmpAssetData = null;
                                _editMode = false;
                                var mainWindow = GetWindow<VrchatAssetManager>();
                                if (mainWindow is not null) {
                                    mainWindow.RefreshAssetList();
                                    EditorApplication.delayCall += () => {
                                        mainWindow.RefreshAssetList();
                                    };
                                }
                            }
                            if(GUILayout.Button("Cancel", Style.CancelButton)) {
                                _tmpAssetData = null;
                                _editMode = false;
                            }
                        }
                    }
                }
            }
            GUILayout.Space(10);

            {/*-------------------- End Header --------------------*/}


            using(new GUILayout.HorizontalScope()) {
                using(new GUILayout.VerticalScope(GUILayout.Width(200))) {
                    GUILayout.FlexibleSpace();
                    if(!_editMode) {
                        Thumbnail.DrawThumbnail(_assetData.thumbnailFilePath, 200);
                        if(GUILayout.Button("Get Thumbnail from Booth URL", Style.GetThumbnailButton)) {
                            SetThumbnailFromBooth(_assetData);
                        }
                    }else {
                        if (_tmpAssetData != null) {
                            Thumbnail.DrawThumbnail(_tmpAssetData.thumbnailFilePath, 200);
                            if (GUILayout.Button("Get Thumbnail from Booth URL", Style.GetThumbnailButton)) {
                                SetThumbnailFromBooth(_tmpAssetData);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.Space(55);
                }
                using(new GUILayout.VerticalScope()) {
                    if(!_editMode) {
                        AssetDetails();
                        GUILayout.Space(10);
                        using(new GUILayout.HorizontalScope()) {
                            GUILayout.FlexibleSpace();
                            if(GUILayout.Button("Import Asset", Style.ImportButton)) {
                                UnityPackageManager.ImportAsset(_assetData);
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }else {
                        EditAssetDetails();
                    }
                }
            }
        }

        private void AssetDetails() {
            using(new GUILayout.VerticalScope(Style.DetailBox)) {
                using(var scrollView = new GUILayout.ScrollViewScope(_scrollPosition)) {
                    _scrollPosition = scrollView.scrollPosition;
                    GUILayout.Label("Name", Style.DetailTitle);
                    GUILayout.Label(_assetData.name, Style.DetailValue);

                    GUILayout.Label("Type", Style.DetailTitle);
                    GUILayout.Label(_assetData.assetType.ToString(), Style.DetailValue);

                    GUILayout.Label("URL", Style.DetailTitle);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label(_assetData.url, Style.DetailValue);
                        if(!string.IsNullOrEmpty(_assetData.url)) {
                            if(GUILayout.Button("Open", Style.OpenUrlButton)) {
                                Application.OpenURL(_assetData.url);
                            }
                        }
                    }

                    GUILayout.Label("File Path", Style.DetailTitle);
                    GUILayout.Label(_assetData.sourceFilePath, Style.DetailValue);

                    GUILayout.Label("Thumbnail Path", Style.DetailTitle);
                    GUILayout.Label(_assetData.thumbnailFilePath, Style.DetailValue);

                    if(_assetData.supportAvatar.Count > 0) {
                        GUILayout.Label("Support Avatar", Style.DetailTitle);
                        foreach(var avatarUid in _assetData.supportAvatar) {
                            var supportAvatar = AssetDataController.GetAssetData(avatarUid);
                            if(supportAvatar != null) {
                                if(GUILayout.Button(supportAvatar.name, Style.DependencyLinkStyle)) {
                                    var newHistory = new Stack<AssetData>(_history);
                                    newHistory.Push(_assetData);
                                    ShowWindow(supportAvatar, newHistory);
                                    return;
                                }
                            }
                        }
                    }

                    if(_assetData.dependencies.Count > 0) {
                        GUILayout.Label("Dependencies", Style.DetailTitle);
                        foreach(var dependencyUid in _assetData.dependencies) {
                            var dependencyAsset = AssetDataController.GetAssetData(dependencyUid);
                            if(dependencyAsset != null) {
                                if(GUILayout.Button(dependencyAsset.name, Style.DependencyLinkStyle)) {
                                    var newHistory = new Stack<AssetData>(_history);
                                    newHistory.Push(_assetData);
                                    ShowWindow(dependencyAsset, newHistory);
                                    return;
                                }
                            }
                        }
                    }

                    if(_assetData.oldVersions.Count > 0) {
                        GUILayout.Label("Old Versions", Style.DetailTitle);
                        foreach(var oldVersion in _assetData.oldVersions) {
                            var oldVersionAsset = AssetDataController.GetAssetData(oldVersion);
                            if(oldVersionAsset != null) {
                                if(GUILayout.Button(oldVersionAsset.name, Style.DependencyLinkStyle)) {
                                    var newHistory = new Stack<AssetData>(_history);
                                    newHistory.Push(_assetData);
                                    ShowWindow(oldVersionAsset, newHistory);
                                    return;
                                }
                            }
                        }
                    }

                    GUILayout.Label("Description", Style.DetailTitle);
                    GUILayout.Label(_assetData.description, Style.DetailValue);
                }
            }
        }

        private void EditAssetDetails() {
            using(new GUILayout.VerticalScope(Style.DetailBoxEdit)) {
                using(var scrollView = new GUILayout.ScrollViewScope(_scrollPosition)) {
                    _scrollPosition = scrollView.scrollPosition;
                    GUILayout.Label("Name", Style.DetailTitle);
                    _tmpAssetData.name = GUILayout.TextField(_tmpAssetData.name);

                    GUILayout.Label("Type", Style.DetailTitle);
                    _tmpAssetData.assetType = (AssetType)EditorGUILayout.EnumPopup(_tmpAssetData.assetType);

                    GUILayout.Label("URL", Style.DetailTitle);
                    _tmpAssetData.url = GUILayout.TextField(_tmpAssetData.url);

                    GUILayout.Label("File Path", Style.DetailTitle);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label(_assetData.sourceFilePath, Style.DetailValue);
                        if(Path.GetExtension(_assetData.sourceFilePath).ToLower() == ".zip") {
                            if(GUILayout.Button("Select Unity Package", Style.SelectUnityPackageButton)) {
                                EditorApplication.delayCall += () => {
                                    AssetDataController.UpdateUnityPackage(_assetData);
                                };
                            }
                        }
                    }

                    GUILayout.Label("Thumbnail Path", Style.DetailTitle);
                    using(new GUILayout.HorizontalScope()) {
                        _tmpAssetData.thumbnailFilePath = GUILayout.TextField(_tmpAssetData.thumbnailFilePath, GUILayout.ExpandWidth(true));
                        if(GUILayout.Button("...", GUILayout.Width(30))) {
                            var path = EditorUtility.OpenFilePanel("Select Thumbnail", Constants.ThumbnailsDirPath, "png,jpg");
                            if(!string.IsNullOrEmpty(path)) {
                                if(path.StartsWith(Constants.ThumbnailsDirPath)) {
                                    path = path.Replace("\\", "/").Replace(Constants.ThumbnailsDirPath + "/", "Thumbnail");
                                    _tmpAssetData.thumbnailFilePath = path;
                                }else {
                                    EditorUtility.DisplayDialog("Error", $"Thumbnail must be placed in the VAMF/Thumbnail folder.\n\nSelected path: {path}", "OK");
                                }
                            }
                        }
                    }

                    GUILayout.Label("Support Avatar", Style.DetailTitle);
                    if (_tmpAssetData.supportAvatar is { Count: > 0 }) {
                        using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                            string avatarToRemove = null;
                            foreach(var avatarUid in _tmpAssetData.supportAvatar) {
                                var avatarAsset = AssetDataController.GetAssetData(avatarUid);
                                if (avatarAsset == null) continue;
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(avatarAsset.name, Style.DetailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        avatarToRemove = avatarUid;
                                    }
                                }
                            }
                            if(avatarToRemove != null) {
                                _tmpAssetData.supportAvatar.Remove(avatarToRemove);
                            }
                        }
                    }
                    if(GUILayout.Button("Add Support Avatar")) {
                        ItemSelector.ShowWindow(uid =>
                            {
                                if (uid == null) return;
                                _tmpAssetData.supportAvatar ??= new List<string>();
                                _tmpAssetData.supportAvatar.Add(uid);
                                Repaint();
                            }, AssetDataController.GetAllAssetData().Where(asset => asset.assetType == AssetType.Avatar && asset.isLatest).ToList(),
                            _tmpAssetData.supportAvatar, _tmpAssetData.uid);
                    }

                    GUILayout.Label("Dependencies", Style.DetailTitle);
                    if (_tmpAssetData.dependencies is { Count: > 0 }) {
                        using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                            string dependencyToRemove = null;
                            foreach(var dependencyUid in _tmpAssetData.dependencies) {
                                var dependencyAsset = AssetDataController.GetAssetData(dependencyUid);
                                if (dependencyAsset == null) continue;
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(dependencyAsset.name, Style.DetailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        dependencyToRemove = dependencyUid;
                                    }
                                }
                            }
                            if(dependencyToRemove != null) {
                                _tmpAssetData.dependencies.Remove(dependencyToRemove);
                            }
                        }
                    }
                    if(GUILayout.Button("Add Dependency")) {
                        ItemSelector.ShowWindow(uid =>
                        {
                            if (uid == null) return;
                            _tmpAssetData.dependencies ??= new List<string>();
                            _tmpAssetData.dependencies.Add(uid);
                            Repaint();
                        }, AssetDataController.GetAllAssetData(), _tmpAssetData.dependencies, _tmpAssetData.uid);
                    }

                    GUILayout.Label("Old Versions", Style.DetailTitle);
                    if (_tmpAssetData.oldVersions is { Count: > 0 }) {
                        using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                            string versionToRemove = null;
                            foreach(var oldVersion in _tmpAssetData.oldVersions) {
                                var oldVersionAsset = AssetDataController.GetAssetData(oldVersion);
                                if (oldVersionAsset == null) continue;
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(oldVersionAsset.name, Style.DetailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        versionToRemove = oldVersion;
                                    }
                                }
                            }
                            if(versionToRemove != null) {
                                _tmpAssetData.oldVersions.Remove(versionToRemove);
                                var removedVersionAsset = AssetDataController.GetAssetData(versionToRemove);
                                if (removedVersionAsset != null) {
                                    removedVersionAsset.isLatest = true;
                                    AssetDataController.UpdateAssetData(removedVersionAsset.uid, removedVersionAsset);
                                }
                            }
                        }
                    }
                    if(GUILayout.Button("Add Old Version")) {
                        ItemSelector.ShowWindow(uid =>
                        {
                            if (uid == null) return;
                            _tmpAssetData.oldVersions ??= new List<string>();
                            _tmpAssetData.oldVersions.Add(uid);
                            var oldVersionAsset = AssetDataController.GetAssetData(uid);
                            if (oldVersionAsset != null) {
                                oldVersionAsset.isLatest = false;
                                AssetDataController.UpdateAssetData(oldVersionAsset.uid, oldVersionAsset);
                            }
                            Repaint();
                        }, AssetDataController.GetAllAssetData(), _tmpAssetData.oldVersions, _tmpAssetData.uid);
                    }

                    GUILayout.Label("Description", Style.DetailTitle);
                    var height = Style.DescriptionTextArea.CalcHeight(new GUIContent(_tmpAssetData.description), EditorGUIUtility.currentViewWidth - 40);
                    _tmpAssetData.description = EditorGUILayout.TextArea(
                        _tmpAssetData.description,
                        Style.DescriptionTextArea,
                        GUILayout.Height(Mathf.Max(80, height))
                    );

                    var newHeight = Style.DescriptionTextArea.CalcHeight(new GUIContent(_tmpAssetData.description), EditorGUIUtility.currentViewWidth - 40);
                    if(newHeight > height) {
                        _scrollPosition.y += (newHeight - height);
                    }
                }
            }
        }

        private async void SetThumbnailFromBooth(AssetData assetData) {
            try {
                Thumbnail.ClearCache();
                var thumbnailUrl = await WebRequest.GetThumbnailUrl(assetData.url);
                var thumbnailFilePath = await WebRequest.GetThumbnail(thumbnailUrl);
                assetData.thumbnailFilePath = thumbnailFilePath;
                AssetDataController.UpdateAssetData(assetData.uid, assetData);
                Repaint();
            }
            catch (Exception e) {
                Debug.LogError($"Error: {e.Message}");
            }
        }

        private void OnDestroy() {
            Thumbnail.ClearCache();
            _assetData = null;
            _editMode = false;
        }

        private static class Style {
            public static readonly GUIStyle WindowTitle;
            public static readonly GUIStyle WindowTitleEdit;
            public static readonly GUIStyle WindowTitleBack;
            public static readonly GUIStyle WindowTitleEditBack;
            public static readonly GUIStyle EditButton;
            public static readonly GUIStyle SaveButton;
            public static readonly GUIStyle CancelButton;
            public static readonly GUIStyle GetThumbnailButton;
            public static readonly GUIStyle DetailBox;
            public static readonly GUIStyle DetailBoxEdit;
            public static readonly GUIStyle DetailTitle;
            public static readonly GUIStyle DetailValue;
            public static readonly GUIStyle ImportButton;
            public static readonly GUIStyle SelectUnityPackageButton;
            public static readonly GUIStyle BackButton;
            public static readonly GUIStyle OpenUrlButton;
            public static readonly GUIStyle DependencyLinkStyle;
            public static readonly GUIStyle DescriptionTextArea;

            static Style() {
                WindowTitle = new GUIStyle(EditorStyles.boldLabel) {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 25,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(100, 0, 4, 0)
                };

                WindowTitleEdit = new GUIStyle(WindowTitle) {
                    margin = new RectOffset(175, 0, 4, 0),
                    fixedHeight = 25
                };

                WindowTitleBack = new GUIStyle(WindowTitle) {
                    margin = new RectOffset(30, 0, 4, 0)
                };

                WindowTitleEditBack = new GUIStyle(WindowTitleEdit) {
                    margin = new RectOffset(105, 0, 4, 0)
                };

                EditButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 15,
                    fixedHeight = 25,
                    fixedWidth = 80,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 20, 4, 0)
                };

                SaveButton = new GUIStyle(EditButton) {
                    margin = new RectOffset(0, 5, 4, 0)
                };

                CancelButton = new GUIStyle(EditButton) {
                    margin = new RectOffset(0, 10, 4, 0)
                };

                GetThumbnailButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedHeight = 20,
                    fixedWidth = 200,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(4, 0, 5, 0)
                };

                DetailBox = new GUIStyle(EditorStyles.helpBox) {
                    fixedHeight = 300,
                    margin = new RectOffset(5, 10, 0, 0)
                };

                DetailBoxEdit = new GUIStyle(DetailBox) {
                    fixedHeight = 350
                };

                DetailTitle = new GUIStyle(EditorStyles.boldLabel) {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };

                DetailValue = new GUIStyle(EditorStyles.label) {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 5, 5)
                };

                DependencyLinkStyle = new GUIStyle(DetailValue) {
                    normal = {
                        textColor = new Color(0.3f, 0.5f, 1.0f)
                    },
                    hover = {
                        textColor = new Color(0.4f, 0.6f, 1.0f)
                    },
                    active = {
                        textColor = new Color(0.2f, 0.4f, 0.9f)
                    }
                };

                ImportButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 15,
                    fixedHeight = 25,
                    fixedWidth = 350,
                    alignment = TextAnchor.MiddleCenter
                };

                SelectUnityPackageButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedHeight = 20,
                    fixedWidth = 150,
                    alignment = TextAnchor.MiddleCenter
                };

                BackButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedHeight = 25,
                    fixedWidth = 60,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(10, 0, 4, 0)
                };

                OpenUrlButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedHeight = 20,
                    fixedWidth = 60,
                    alignment = TextAnchor.MiddleCenter
                };

                DescriptionTextArea = new GUIStyle(EditorStyles.textArea) {
                    fontSize = 12,
                    wordWrap = true,
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }
        }
    }
}

