using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Window {
    public class VrchatUnityPackageManager : EditorWindow {
        private AssetType selectedAssetType = AssetType.Unregistered;
        private Vector2 scrollPosition = Vector2.zero;

        private string searchName = "";
        private string searchDescription = "";
        private string selectedSupportAvatar = "";

        private List<AssetData> assetDataList;
        private List<AssetData> filteredAssetList;
        private string[] avatarNames;
        private Dictionary<string, string> avatarNameToUid;

        [MenuItem("VAMF/Package Manager", priority = 1)]
        public static void ShowWindow() {
            GetWindow<VrchatUnityPackageManager>("Package Manager", typeof(SceneView));
        }

        void OnEnable() {
            Thumbnail.ClearCache();
            Utility.AssetDataController.AutoRegisterAssetData();
            assetDataList = Utility.AssetDataController.GetAllAssetData();
            filteredAssetList = assetDataList;
            searchName = "";
            searchDescription = "";
            selectedSupportAvatar = "None";
            UpdateAvatarList();
        }

        void OnDestroy() {
            Thumbnail.ClearCache();
            assetDataList = null;
            filteredAssetList = null;
        }

        void OnGUI() {
            {/*-------------------- Header --------------------*/}

            GUILayout.Space(10);
            using(new GUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    GUILayout.Label("VRChat Unity Package Manager", Style.windowTitle);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    if(GUILayout.Button("Sync Avatar List", Style.syncButton)) {
                        EditorApplication.delayCall += () => {
                            Thumbnail.ClearCache();
                            Utility.AssetDataController.AutoRegisterAssetData();
                            assetDataList = Utility.AssetDataController.GetAllAssetData();
                            UpdateAvatarList();
                            filteredAssetList = assetDataList;
                            searchName = "";
                            searchDescription = "";
                            selectedSupportAvatar = "None";
                        };
                    }
                }
            }
            GUILayout.Space(10);

            using(new GUILayout.HorizontalScope()) {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
                GUILayout.Box("", Style.divLine);
                GUI.color = oldColor;
            }

            {/*-------------------- End Header --------------------*/}

            {/*-------------------- Asset Type --------------------*/}
            GUILayout.Space(10);
            using(new GUILayout.VerticalScope()) {
                int count = 0;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                foreach(AssetType type in Enum.GetValues(typeof(AssetType))) {
                    if(GUILayout.Toggle(selectedAssetType == type, type.ToString(), Style.assetTypeButton)) {
                        selectedAssetType = type;
                    }
                    count++;
                    // five Assetypes per line
                    if(count % 5 == 0) {
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            {/*-------------------- End Asset Type --------------------*/}

            {/*-------------------- Asset List --------------------*/}

            using (var scrollView = new GUILayout.ScrollViewScope(scrollPosition)) {
                scrollPosition = scrollView.scrollPosition;
                int count = 0;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // selected AssetType && isLatest
                foreach (var asset in filteredAssetList.Where(asset => asset.assetType == selectedAssetType && asset.isLatest).ToList()) {
                    using (new GUILayout.VerticalScope(Style.assetBox)) {
                        Thumbnail.DrawThumbnail(asset.thumbnailFilePath, 200);
                        if(GUILayout.Button(asset.name, Style.assetButton)) {
                            DetailWindow.ShowWindow(asset);
                        }
                    }
                    count++;
                    // five assets per line
                    if(count % 5 == 0) {
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            {/*-------------------- End Asset List --------------------*/}

            {/*-------------------- Search Bar --------------------*/}

            using(new GUILayout.HorizontalScope()) {
                GUILayout.Space(20);
                using(new GUILayout.VerticalScope(GUILayout.Width(position.width * 0.2f))) {
                    GUILayout.Space(8);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("Name:", GUILayout.Width(50));
                        GUI.SetNextControlName("NameSearchField");
                        string newSearchName = GUILayout.TextField(searchName, Style.searchTextField);
                        if(newSearchName != searchName) {
                            searchName = newSearchName;
                        }
                        if(Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "NameSearchField") {
                            Event.current.Use();
                            FilterAssets();
                        }
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope(GUILayout.Width(position.width * 0.35f))) {
                    GUILayout.Space(8);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("Description:", GUILayout.Width(70));
                        GUI.SetNextControlName("DescSearchField");
                        string newSearchDescription = GUILayout.TextField(searchDescription, Style.searchTextField);
                        if(newSearchDescription != searchDescription) {
                            searchDescription = newSearchDescription;
                        }
                        if(Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "DescSearchField") {
                            Event.current.Use();
                            FilterAssets();
                        }
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope()) {
                    GUILayout.Space(8);
                    int selectedIndex = Array.IndexOf(avatarNames, selectedSupportAvatar);
                    int newSelectedIndex = EditorGUILayout.Popup("", selectedIndex, avatarNames, GUILayout.Width(100));
                    
                    if(newSelectedIndex != selectedIndex) {
                        selectedSupportAvatar = avatarNames[newSelectedIndex];
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(45);
                GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope()) {
                    GUILayout.Space(8);
                    if(GUILayout.Button("Reset", Style.searchButton)) {
                        searchName = "";
                        searchDescription = "";
                        selectedSupportAvatar = "None";
                        filteredAssetList = assetDataList;
                        GUI.FocusControl(null);
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope()) {
                    GUILayout.Space(8);
                    if(GUILayout.Button("Search", Style.searchButton)) {
                        FilterAssets();
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(20);
            }

            {/*-------------------- End Search Bar --------------------*/}
        }

        private void UpdateAvatarList() {
            var avatars = assetDataList.Where(asset => asset.assetType == AssetType.Avatar && asset.isLatest).ToList();
            avatarNameToUid = new Dictionary<string, string>();
            var namesList = new List<string> { "None" };
            
            foreach(var avatar in avatars) {
                namesList.Add(avatar.name);
                avatarNameToUid[avatar.name] = avatar.uid;
            }
            
            avatarNames = namesList.ToArray();
            selectedSupportAvatar = "None";
        }

        private void FilterAssets() {
            filteredAssetList = assetDataList
                .Where(asset => 
                    (string.IsNullOrEmpty(searchName) || asset.name.ToLower().Contains(searchName.ToLower())) &&
                    (string.IsNullOrEmpty(searchDescription) || asset.description.ToLower().Contains(searchDescription.ToLower())) &&
                    (selectedSupportAvatar == "None" || 
                     (avatarNameToUid.ContainsKey(selectedSupportAvatar) && 
                      (asset.uid == avatarNameToUid[selectedSupportAvatar] || 
                       (asset.supportAvatar != null && 
                        asset.supportAvatar.Count > 0 &&
                        asset.supportAvatar.Contains(avatarNameToUid[selectedSupportAvatar]))))))
                .ToList();
        }

        public void RefreshAssetList() {
            Thumbnail.ClearCache();
            assetDataList = Utility.AssetDataController.GetAllAssetData();
            UpdateAvatarList();
            FilterAssets();
            Repaint();
        }

        private class Style {
            public static GUIStyle windowTitle;
            public static GUIStyle syncButton;
            public static GUIStyle assetTypeButton;
            public static GUIStyle divLine;
            public static GUIStyle assetBox;
            public static GUIStyle assetButton;
            public static GUIStyle searchTextField;
            public static GUIStyle searchButton;

            static Style() {
                windowTitle = new GUIStyle(EditorStyles.boldLabel);
                windowTitle.fontSize = 20;
                windowTitle.fontStyle = FontStyle.Bold;
                windowTitle.alignment = TextAnchor.MiddleCenter;
                windowTitle.fixedHeight = 25;
                windowTitle.margin = new RectOffset(150, 0, 0, 0);

                divLine = new GUIStyle();
                divLine.normal.background = EditorGUIUtility.whiteTexture;
                divLine.margin = new RectOffset(20, 20, 0, 0);
                divLine.fixedHeight = 2;

                syncButton = new GUIStyle(EditorStyles.miniButton);
                syncButton.fontSize = 12;
                syncButton.fixedWidth = 130;
                syncButton.fixedHeight = 25;
                syncButton.alignment = TextAnchor.MiddleCenter;
                syncButton.margin = new RectOffset(0, 20, 0, 0);

                assetBox = new GUIStyle(EditorStyles.helpBox);
                assetBox.margin = new RectOffset(5, 5, 5, 5);
                assetBox.fixedWidth = 210;
                assetBox.fixedHeight = 235;
                assetBox.alignment = TextAnchor.MiddleCenter;

                assetButton = new GUIStyle(EditorStyles.miniButton);
                assetButton.fontSize = 12;
                assetButton.fixedWidth = 200;
                assetButton.fixedHeight = 25;
                assetButton.alignment = TextAnchor.MiddleCenter;

                assetTypeButton = new GUIStyle(EditorStyles.toolbarButton);
                assetTypeButton.fixedWidth = 200;

                searchTextField = new GUIStyle(EditorStyles.textField);
                searchTextField.fixedHeight = 20;

                searchButton = new GUIStyle(EditorStyles.miniButton);
                searchButton.fontSize = 12;
                searchButton.fixedWidth = 80;
                searchButton.fixedHeight = 20;
                searchButton.alignment = TextAnchor.MiddleCenter;
            }
        }
    }
}