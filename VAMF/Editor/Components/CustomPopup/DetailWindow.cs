using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;

public class DetailWindow: EditorWindow {
    private bool editMode = false;
    private Vector2 scrollPosition = Vector2.zero;

    private AssetData assetData;
    private AssetData tmpAssetData;
    private Stack<AssetData> history = new Stack<AssetData>();

    public static void ShowWindow(AssetData assetData, Stack<AssetData> previousHistory = null) {
        if(assetData == null) {
            Debug.LogError("AssetData is null");
            return;
        }

        var window = GetWindow<DetailWindow>($"Asset Details");
        
        Vector2 windowSize = new Vector2(610, 400);
        window.minSize = windowSize;
        window.maxSize = windowSize;
        
        window.assetData = assetData;
        window.history = previousHistory ?? new Stack<AssetData>();
        window.editMode = false;

        window.Show();
    }

    void OnGUI() {
        {/*-------------------- Header --------------------*/}

        GUILayout.Space(10);
        using(new GUILayout.HorizontalScope()) {
            if(history.Count > 0) {
                if(GUILayout.Button("Back", Style.backButton)) {
                    var previousAsset = history.Pop();
                    var newHistory = new Stack<AssetData>(history.ToArray().Reverse());
                    ShowWindow(previousAsset, newHistory);
                    return;
                }
            }
            GUILayout.FlexibleSpace();
            using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                if(!editMode && history.Count == 0) {
                    GUILayout.Label("Asset Details", Style.windowTitle);
                }else if(!editMode && history.Count > 0) {
                    GUILayout.Label("Asset Details", Style.windowTitle_back);
                }else if(editMode && history.Count == 0) {
                    GUILayout.Label("Edit Asset Details", Style.windowTitle_edit);
                }else if(editMode && history.Count > 0) {
                    GUILayout.Label("Edit Asset Details", Style.windowTitle_edit_back);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                if(!editMode) {
                    if(GUILayout.Button("Edit", Style.editButton)) {
                        editMode = true;
                        tmpAssetData = assetData.Clone();
                    }
                } else {
                    using(new GUILayout.HorizontalScope()) {
                        if(GUILayout.Button("Save", Style.saveButton)) {
                            assetData = tmpAssetData.Clone();
                            Utility.AssetDataController.UpdateAssetData(assetData.uid, assetData);
                            tmpAssetData = null;
                            editMode = false;
                            var mainWindow = EditorWindow.GetWindow<Window.VrchatUnityPackageManager>();
                            if (mainWindow != null) {
                                mainWindow.RefreshAssetList();
                                EditorApplication.delayCall += () => {
                                    mainWindow.RefreshAssetList();
                                };
                            }
                        }
                        if(GUILayout.Button("Cancel", Style.cancelButton)) {
                            tmpAssetData = null;
                            editMode = false;
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
                if(!editMode) {
                    Thumbnail.DrawThumbnail(assetData.thumbnailFilePath, 200);
                    if(GUILayout.Button("Get Thumbnail from Booth URL", Style.getThumbnailButton)) {
                        SetThumbnailFromBooth(assetData);
                    }
                }else {
                    Thumbnail.DrawThumbnail(tmpAssetData.thumbnailFilePath, 200);
                    if(GUILayout.Button("Get Thumbnail from Booth URL", Style.getThumbnailButton)) {
                        SetThumbnailFromBooth(tmpAssetData);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(55);
            }
            using(new GUILayout.VerticalScope()) {
                if(!editMode) {
                    AssetDetails();
                    GUILayout.Space(10);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("Import Asset", Style.importButton)) {
                            Utility.UnityPackageManager.ImportAsset(assetData);
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
        using(new GUILayout.VerticalScope(Style.detailBox)) {
            using(var scrollView = new GUILayout.ScrollViewScope(scrollPosition)) {
                scrollPosition = scrollView.scrollPosition;
                GUILayout.Label("Name", Style.detailTitle);
                GUILayout.Label(assetData.name, Style.detailValue);

                GUILayout.Label("Type", Style.detailTitle);
                GUILayout.Label(assetData.assetType.ToString(), Style.detailValue);

                GUILayout.Label("URL", Style.detailTitle);
                using(new GUILayout.HorizontalScope()) {
                    GUILayout.Label(assetData.url, Style.detailValue);
                    if(!string.IsNullOrEmpty(assetData.url)) {
                        if(GUILayout.Button("Open", Style.openUrlButton)) {
                            Application.OpenURL(assetData.url);
                        }
                    }
                }

                GUILayout.Label("File Path", Style.detailTitle);
                GUILayout.Label(assetData.sourceFilePath, Style.detailValue);

                GUILayout.Label("Thumbnail Path", Style.detailTitle);
                GUILayout.Label(assetData.thumbnailFilePath, Style.detailValue);

                if(assetData.supportAvatar.Count > 0) {
                    GUILayout.Label("Support Avatar", Style.detailTitle);
                    foreach(var avatarUid in assetData.supportAvatar) {
                        var supportAvatar = Utility.AssetDataController.GetAssetData(avatarUid);
                        if(supportAvatar != null) {
                            if(GUILayout.Button(supportAvatar.name, Style.dependencyLinkStyle)) {
                                var newHistory = new Stack<AssetData>(history);
                                newHistory.Push(assetData);
                                ShowWindow(supportAvatar, newHistory);
                                return;
                            }
                        }
                    }
                }

                if(assetData.dependencies.Count > 0) {
                    GUILayout.Label("Dependencies", Style.detailTitle);
                    foreach(var dependencyUid in assetData.dependencies) {
                        var dependencyAsset = Utility.AssetDataController.GetAssetData(dependencyUid);
                        if(dependencyAsset != null) {
                            if(GUILayout.Button(dependencyAsset.name, Style.dependencyLinkStyle)) {
                                var newHistory = new Stack<AssetData>(history);
                                newHistory.Push(assetData);
                                ShowWindow(dependencyAsset, newHistory);
                                return;
                            }
                        }
                    }
                }

                if(assetData.oldVersions.Count > 0) {
                    GUILayout.Label("Old Versions", Style.detailTitle);
                    foreach(var oldVersion in assetData.oldVersions) {
                        var oldVersionAsset = Utility.AssetDataController.GetAssetData(oldVersion);
                        if(oldVersionAsset != null) {
                            if(GUILayout.Button(oldVersionAsset.name, Style.dependencyLinkStyle)) {
                                var newHistory = new Stack<AssetData>(history);
                                newHistory.Push(assetData);
                                ShowWindow(oldVersionAsset, newHistory);
                                return;
                            }
                        }
                    }
                }

                GUILayout.Label("Description", Style.detailTitle);
                GUILayout.Label(assetData.description, Style.detailValue);
            }
        }
    }

    private void EditAssetDetails() {
        using(new GUILayout.VerticalScope(Style.detailBox_edit)) {
            using(var scrollView = new GUILayout.ScrollViewScope(scrollPosition)) {
                scrollPosition = scrollView.scrollPosition;
                GUILayout.Label("Name", Style.detailTitle);
                tmpAssetData.name = GUILayout.TextField(tmpAssetData.name);

                GUILayout.Label("Type", Style.detailTitle);
                tmpAssetData.assetType = (AssetType)EditorGUILayout.EnumPopup(tmpAssetData.assetType);

                GUILayout.Label("URL", Style.detailTitle);
                tmpAssetData.url = GUILayout.TextField(tmpAssetData.url);

                GUILayout.Label("File Path", Style.detailTitle);
                using(new GUILayout.HorizontalScope()) {
                    GUILayout.Label(assetData.sourceFilePath, Style.detailValue);
                    if(System.IO.Path.GetExtension(assetData.sourceFilePath).ToLower() == ".zip") {
                        if(GUILayout.Button("Select Unity Package", Style.selectUnityPackageButton)) {
                            EditorApplication.delayCall += () => {
                                Utility.AssetDataController.UpdateUnityPackage(assetData);
                            };
                        }
                    }
                }

                GUILayout.Label("Thumbnail Path", Style.detailTitle);
                using(new GUILayout.HorizontalScope()) {
                    tmpAssetData.thumbnailFilePath = GUILayout.TextField(tmpAssetData.thumbnailFilePath, GUILayout.ExpandWidth(true));
                    if(GUILayout.Button("...", GUILayout.Width(30))) {
                        string thumbnailRootPath = Path.Combine(
                            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                            "VAMF/Thumbnail"
                        ).Replace("\\", "/");
                        string path = EditorUtility.OpenFilePanel("Select Thumbnail", thumbnailRootPath, "png,jpg");
                        if(!string.IsNullOrEmpty(path)) {
                            if(path.StartsWith(thumbnailRootPath)) {
                                path = path.Replace("\\", "/").Replace(thumbnailRootPath + "/", "Thumbnail");
                                tmpAssetData.thumbnailFilePath = path;
                            }else {
                                EditorUtility.DisplayDialog("Error", $"Thumbnail must be placed in the VAMF/Thumbnail folder.\n\nSelected path: {path}", "OK");
                            }
                        }
                    }
                }

                GUILayout.Label("Support Avatar", Style.detailTitle);
                if (tmpAssetData.supportAvatar != null && tmpAssetData.supportAvatar.Count > 0) {
                    using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                        string avatarToRemove = null;
                        foreach(var avatarUid in tmpAssetData.supportAvatar) {
                            var avatarAsset = Utility.AssetDataController.GetAssetData(avatarUid);
                            if(avatarAsset != null) {
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(avatarAsset.name, Style.detailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        avatarToRemove = avatarUid;
                                    }
                                }
                            }
                        }
                        if(avatarToRemove != null) {
                            tmpAssetData.supportAvatar.Remove(avatarToRemove);
                        }
                    }
                }
                if(GUILayout.Button("Add Support Avatar")) {
                    ItemSelector.ShowWindow(uid => {
                        if(uid != null) {
                            if(tmpAssetData.supportAvatar == null) {
                                tmpAssetData.supportAvatar = new List<string>();
                            }
                            tmpAssetData.supportAvatar.Add(uid);
                            Repaint();
                        }
                    }, Utility.AssetDataController.GetAllAssetData().Where(asset => asset.assetType == AssetType.Avatar).ToList(),
                    tmpAssetData.supportAvatar, tmpAssetData.uid);
                }

                GUILayout.Label("Dependencies", Style.detailTitle);
                if (tmpAssetData.dependencies != null && tmpAssetData.dependencies.Count > 0) {
                    using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                        string dependencyToRemove = null;
                        foreach(var dependencyUid in tmpAssetData.dependencies) {
                            var dependencyAsset = Utility.AssetDataController.GetAssetData(dependencyUid);
                            if(dependencyAsset != null) {
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(dependencyAsset.name, Style.detailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        dependencyToRemove = dependencyUid;
                                    }
                                }
                            }
                        }
                        if(dependencyToRemove != null) {
                            tmpAssetData.dependencies.Remove(dependencyToRemove);
                        }
                    }
                }
                if(GUILayout.Button("Add Dependency")) {
                    ItemSelector.ShowWindow(uid => {
                        if(uid != null) {
                            if(tmpAssetData.dependencies == null) {
                                tmpAssetData.dependencies = new List<string>();
                            }
                            tmpAssetData.dependencies.Add(uid);
                            Repaint();
                        }
                    }, Utility.AssetDataController.GetAllAssetData(), tmpAssetData.dependencies, tmpAssetData.uid);
                }

                GUILayout.Label("Old Versions", Style.detailTitle);
                if (tmpAssetData.oldVersions != null && tmpAssetData.oldVersions.Count > 0) {
                    using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                        string versionToRemove = null;
                        foreach(var oldVersion in tmpAssetData.oldVersions) {
                            var oldVersionAsset = Utility.AssetDataController.GetAssetData(oldVersion);
                            if(oldVersionAsset != null) {
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Label(oldVersionAsset.name, Style.detailValue);
                                    if(GUILayout.Button("×", GUILayout.Width(20))) {
                                        versionToRemove = oldVersion;
                                    }
                                }
                            }
                        }
                        if(versionToRemove != null) {
                            tmpAssetData.oldVersions.Remove(versionToRemove);
                            var removedVersionAsset = Utility.AssetDataController.GetAssetData(versionToRemove);
                            if (removedVersionAsset != null) {
                                removedVersionAsset.isLatest = true;
                                Utility.AssetDataController.UpdateAssetData(removedVersionAsset.uid, removedVersionAsset);
                            }
                        }
                    }
                }
                if(GUILayout.Button("Add Old Version")) {
                    ItemSelector.ShowWindow(uid => {
                        if(uid != null) {
                            if(tmpAssetData.oldVersions == null) {
                                tmpAssetData.oldVersions = new List<string>();
                            }
                            tmpAssetData.oldVersions.Add(uid);
                            var oldVersionAsset = Utility.AssetDataController.GetAssetData(uid);
                            if (oldVersionAsset != null) {
                                oldVersionAsset.isLatest = false;
                                Utility.AssetDataController.UpdateAssetData(oldVersionAsset.uid, oldVersionAsset);
                            }
                            Repaint();
                        }
                    }, Utility.AssetDataController.GetAllAssetData(), tmpAssetData.oldVersions, tmpAssetData.uid);
                }

                GUILayout.Label("Description", Style.detailTitle);
                float height = Style.descriptionTextArea.CalcHeight(new GUIContent(tmpAssetData.description), EditorGUIUtility.currentViewWidth - 40);
                float previousHeight = height;
                tmpAssetData.description = GUILayout.TextArea(
                    tmpAssetData.description,
                    Style.descriptionTextArea,
                    GUILayout.Height(Mathf.Max(80, height))
                );

                float newHeight = Style.descriptionTextArea.CalcHeight(new GUIContent(tmpAssetData.description), EditorGUIUtility.currentViewWidth - 40);
                if(newHeight > previousHeight) {
                    scrollPosition.y += (newHeight - previousHeight);
                }
            }
        }
    }

    private async void SetThumbnailFromBooth(AssetData assetData) {
        Thumbnail.ClearCache();
        try {
            string thumbnailUrl = await Utility.WebRequest.GetThumbnailUrl(assetData.url);
            string thumbnailFilePath = await Utility.WebRequest.GetThumbnail(thumbnailUrl);
            assetData.thumbnailFilePath = thumbnailFilePath;
            Utility.AssetDataController.UpdateAssetData(assetData.uid, assetData);
            Repaint();
        }catch(Exception ex) {
            Debug.LogError($"Error: {ex.Message}");
        }
    }

    void OnDestroy() {
        Thumbnail.ClearCache();
        assetData = null;
        editMode = false;
    }

    private class Style {

        public static GUIStyle windowTitle;
        public static GUIStyle windowTitle_edit;
        public static GUIStyle windowTitle_back;
        public static GUIStyle windowTitle_edit_back;
        public static GUIStyle editButton;
        public static GUIStyle saveButton;
        public static GUIStyle cancelButton;
        public static GUIStyle getThumbnailButton;
        public static GUIStyle detailBox;
        public static GUIStyle detailBox_edit;
        public static GUIStyle detailTitle;
        public static GUIStyle detailValue;
        public static GUIStyle importButton;
        public static GUIStyle selectUnityPackageButton;
        public static GUIStyle backButton;
        public static GUIStyle openUrlButton;
        public static GUIStyle selectFileButton;
        public static GUIStyle dependencyLinkStyle;
        public static GUIStyle descriptionTextArea;

        static Style() {
            windowTitle = new GUIStyle(EditorStyles.boldLabel);
            windowTitle.fontSize = 17;
            windowTitle.fontStyle = FontStyle.Bold;
            windowTitle.fixedHeight = 25;
            windowTitle.alignment = TextAnchor.MiddleCenter;
            windowTitle.margin = new RectOffset(100, 0, 4, 0);

            windowTitle_edit = new GUIStyle(windowTitle);
            windowTitle_edit.margin = new RectOffset(175, 0, 4, 0);
            windowTitle_edit.fixedHeight = 25;

            windowTitle_back = new GUIStyle(windowTitle);
            windowTitle_back.margin = new RectOffset(30, 0, 4, 0);

            windowTitle_edit_back = new GUIStyle(windowTitle_edit);
            windowTitle_edit_back.margin = new RectOffset(105, 0, 4, 0);

            editButton = new GUIStyle(EditorStyles.miniButton);
            editButton.fontSize = 15;
            editButton.fixedHeight = 25;
            editButton.fixedWidth = 80;
            editButton.alignment = TextAnchor.MiddleCenter;
            editButton.margin = new RectOffset(0, 20, 4, 0);

            saveButton = new GUIStyle(editButton);
            saveButton.margin = new RectOffset(0, 5, 4, 0);

            cancelButton = new GUIStyle(editButton);
            cancelButton.margin = new RectOffset(0, 10, 4, 0);

            getThumbnailButton = new GUIStyle(EditorStyles.miniButton);
            getThumbnailButton.fontSize = 12;
            getThumbnailButton.fixedHeight = 20;
            getThumbnailButton.fixedWidth = 200;
            getThumbnailButton.alignment = TextAnchor.MiddleCenter;
            getThumbnailButton.margin = new RectOffset(4, 0, 5, 0);

            detailBox = new GUIStyle(EditorStyles.helpBox);
            detailBox.fixedHeight = 300;
            detailBox.margin = new RectOffset(5, 10, 0, 0);

            detailBox_edit = new GUIStyle(detailBox);
            detailBox_edit.fixedHeight = 350;

            detailTitle = new GUIStyle(EditorStyles.boldLabel);
            detailTitle.fontSize = 15;
            detailTitle.fontStyle = FontStyle.Bold;

            detailValue = new GUIStyle(EditorStyles.label);
            detailValue.fontSize = 12;
            detailValue.alignment = TextAnchor.MiddleLeft;
            detailValue.margin = new RectOffset(0, 0, 5, 5);

            dependencyLinkStyle = new GUIStyle(detailValue);
            dependencyLinkStyle.normal.textColor = new Color(0.3f, 0.5f, 1.0f);
            dependencyLinkStyle.hover.textColor = new Color(0.4f, 0.6f, 1.0f);
            dependencyLinkStyle.active.textColor = new Color(0.2f, 0.4f, 0.9f);

            importButton = new GUIStyle(EditorStyles.miniButton);
            importButton.fontSize = 15;
            importButton.fixedHeight = 25;
            importButton.fixedWidth = 350;
            importButton.alignment = TextAnchor.MiddleCenter;

            selectUnityPackageButton = new GUIStyle(EditorStyles.miniButton);
            selectUnityPackageButton.fontSize = 12;
            selectUnityPackageButton.fixedHeight = 20;
            selectUnityPackageButton.fixedWidth = 150;
            selectUnityPackageButton.alignment = TextAnchor.MiddleCenter;

            backButton = new GUIStyle(EditorStyles.miniButton);
            backButton.fontSize = 12;
            backButton.fixedHeight = 25;
            backButton.fixedWidth = 60;
            backButton.alignment = TextAnchor.MiddleCenter;
            backButton.margin = new RectOffset(10, 0, 4, 0);

            openUrlButton = new GUIStyle(EditorStyles.miniButton);
            openUrlButton.fontSize = 12;
            openUrlButton.fixedHeight = 20;
            openUrlButton.fixedWidth = 60;
            openUrlButton.alignment = TextAnchor.MiddleCenter;

            selectFileButton = new GUIStyle(EditorStyles.miniButton);
            selectFileButton.fontSize = 12;
            selectFileButton.fixedHeight = 20;
            selectFileButton.fixedWidth = 150;
            selectFileButton.alignment = TextAnchor.MiddleCenter;

            descriptionTextArea = new GUIStyle(EditorStyles.textArea);
            descriptionTextArea.fontSize = 12;
            descriptionTextArea.wordWrap = true;
            descriptionTextArea.padding = new RectOffset(8, 8, 8, 8);
            descriptionTextArea.margin = new RectOffset(0, 0, 5, 5);
        }
    }
}

