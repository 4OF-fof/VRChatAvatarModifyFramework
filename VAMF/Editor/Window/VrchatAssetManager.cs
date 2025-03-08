using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VAMF.Editor.Components;
using VAMF.Editor.Components.CustomPopup;
using VAMF.Editor.Schemas;
using VAMF.Editor.Utility;

namespace VAMF.Editor.Window {
    public class VrchatAssetManager : EditorWindow {
        private AssetType _selectedAssetType = AssetType.Unregistered;
        private Vector2 _scrollPosition = Vector2.zero;

        private string _searchName = "";
        private string _searchDescription = "";
        private string _selectedSupportAvatar = "";

        private List<AssetData> _assetDataList;
        private List<AssetData> _filteredAssetList;
        private string[] _avatarNames;
        private Dictionary<string, string> _avatarNameToUid;

        [MenuItem("VAMF/Package Manager", priority = 1)]
        public static void ShowWindow() {
            GetWindow<VrchatAssetManager>("Package Manager", typeof(SceneView));
        }

        void OnEnable() {
            ContentsPath.Initialize();
            Thumbnail.ClearCache();
            AssetDataController.AutoRegisterAssetData();
            _assetDataList = AssetDataController.GetAllAssetData();
            _filteredAssetList = _assetDataList;
            _searchName = "";
            _searchDescription = "";
            _selectedSupportAvatar = "None";
            UpdateAvatarList();
        }

        void OnDestroy() {
            Thumbnail.ClearCache();
            _assetDataList = null;
            _filteredAssetList = null;
        }

        void OnGUI() {
            {/*-------------------- Header --------------------*/}

            GUILayout.Space(10);
            using(new GUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    GUILayout.Label("VRChat Unity Package Manager", Style.WindowTitle);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope(GUILayout.Height(25))) {
                    if(GUILayout.Button("Sync Avatar List", Style.SyncButton)) {
                        EditorApplication.delayCall += () => {
                            Thumbnail.ClearCache();
                            AssetDataController.AutoRegisterAssetData();
                            _assetDataList = AssetDataController.GetAllAssetData();
                            UpdateAvatarList();
                            _filteredAssetList = _assetDataList;
                            _searchName = "";
                            _searchDescription = "";
                            _selectedSupportAvatar = "None";
                        };
                    }
                }
            }
            GUILayout.Space(10);

            using(new GUILayout.HorizontalScope()) {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
                GUILayout.Box("", Style.DivLine);
                GUI.color = oldColor;
            }

            {/*-------------------- End Header --------------------*/}

            {/*-------------------- Asset Type --------------------*/}
            GUILayout.Space(10);
            using(new GUILayout.VerticalScope()) {
                var count = 0;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                foreach(AssetType type in Enum.GetValues(typeof(AssetType))) {
                    if(GUILayout.Toggle(_selectedAssetType == type, type.ToString(), Style.AssetTypeButton)) {
                        _selectedAssetType = type;
                    }
                    count++;
                    // five AssetTypes per line
                    if (count % 5 != 0) continue;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            {/*-------------------- End Asset Type --------------------*/}

            {/*-------------------- Asset List --------------------*/}

            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition)) {
                _scrollPosition = scrollView.scrollPosition;
                var count = 0;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // selected AssetType && isLatest
                foreach (var asset in _filteredAssetList.Where(asset => asset.assetType == _selectedAssetType && asset.isLatest).ToList()) {
                    using (new GUILayout.VerticalScope(Style.AssetBox)) {
                        Thumbnail.DrawThumbnail(asset.thumbnailFilePath, 200);
                        if(GUILayout.Button(asset.name, Style.AssetButton)) {
                            DetailWindow.ShowWindow(asset);
                        }
                    }
                    count++;
                    // five assets per line
                    if (count % 5 != 0) continue;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            {/*-------------------- End Asset List --------------------*/}

            {/*-------------------- Search Bar --------------------*/}

            using(new GUILayout.HorizontalScope()) {
                GUILayout.Space(20);
                using(new GUILayout.VerticalScope(GUILayout.Width(position.width * 0.25f))) {
                    GUILayout.Space(8);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("Name:", GUILayout.Width(50));
                        GUI.SetNextControlName("NameSearchField");
                        string newSearchName = GUILayout.TextField(_searchName, Style.SearchTextField);
                        _searchName = newSearchName;
                        if(Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "NameSearchField") {
                            Event.current.Use();
                            FilterAssets();
                        }
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope(GUILayout.Width(position.width * 0.4f))) {
                    GUILayout.Space(8);
                    using(new GUILayout.HorizontalScope()) {
                        GUILayout.Label("Description:", GUILayout.Width(70));
                        GUI.SetNextControlName("DescSearchField");
                        string newSearchDescription = GUILayout.TextField(_searchDescription, Style.SearchTextField);
                        _searchDescription = newSearchDescription;
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
                    int selectedIndex = Array.IndexOf(_avatarNames, _selectedSupportAvatar);
                    int newSelectedIndex = EditorGUILayout.Popup("", selectedIndex, _avatarNames, GUILayout.Width(100));
                    
                    if(newSelectedIndex != selectedIndex) {
                        _selectedSupportAvatar = _avatarNames[newSelectedIndex];
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(20);
                GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope()) {
                    GUILayout.Space(8);
                    if(GUILayout.Button("Reset", Style.SearchButton)) {
                        _searchName = "";
                        _searchDescription = "";
                        _selectedSupportAvatar = "None";
                        _filteredAssetList = _assetDataList;
                        GUI.FocusControl(null);
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(10);
                using(new GUILayout.VerticalScope()) {
                    GUILayout.Space(8);
                    if(GUILayout.Button("Search", Style.SearchButton)) {
                        FilterAssets();
                    }
                    GUILayout.Space(7);
                }
                GUILayout.Space(20);
            }

            {/*-------------------- End Search Bar --------------------*/}
        }

        private void UpdateAvatarList() {
            var avatars = _assetDataList.Where(asset => asset.assetType == AssetType.Avatar && asset.isLatest).ToList();
            _avatarNameToUid = new Dictionary<string, string>();
            var namesList = new List<string> { "None" };
            
            foreach(var avatar in avatars) {
                namesList.Add(avatar.name);
                _avatarNameToUid[avatar.name] = avatar.uid;
            }
            
            _avatarNames = namesList.ToArray();
            _selectedSupportAvatar = "None";
        }

        private void FilterAssets() {
            _filteredAssetList = _assetDataList
                .Where(asset => 
                    (string.IsNullOrEmpty(_searchName) || asset.name.ToLower().Contains(_searchName.ToLower())) &&
                    (string.IsNullOrEmpty(_searchDescription) || asset.description.ToLower().Contains(_searchDescription.ToLower())) &&
                    (_selectedSupportAvatar == "None" || 
                     (_avatarNameToUid.ContainsKey(_selectedSupportAvatar) && 
                      (asset.uid == _avatarNameToUid[_selectedSupportAvatar] || 
                       (asset.supportAvatar is { Count: > 0 } &&
                        asset.supportAvatar.Contains(_avatarNameToUid[_selectedSupportAvatar]))))))
                .ToList();
        }

        public void RefreshAssetList() {
            Thumbnail.ClearCache();
            _assetDataList = AssetDataController.GetAllAssetData();
            UpdateAvatarList();
            FilterAssets();
            Repaint();
        }

        private static class Style {
            public static readonly GUIStyle WindowTitle;
            public static readonly GUIStyle SyncButton;
            public static readonly GUIStyle AssetTypeButton;
            public static readonly GUIStyle DivLine;
            public static readonly GUIStyle AssetBox;
            public static readonly GUIStyle AssetButton;
            public static readonly GUIStyle SearchTextField;
            public static readonly GUIStyle SearchButton;

            static Style() {
                WindowTitle = new GUIStyle(EditorStyles.boldLabel) {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 25,
                    margin = new RectOffset(150, 0, 0, 0)
                };

                DivLine = new GUIStyle {
                    normal = {
                        background = EditorGUIUtility.whiteTexture
                    },
                    margin = new RectOffset(20, 20, 0, 0),
                    fixedHeight = 2
                };

                SyncButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedWidth = 130,
                    fixedHeight = 25,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 20, 0, 0)
                };

                AssetBox = new GUIStyle(EditorStyles.helpBox) {
                    margin = new RectOffset(5, 5, 5, 5),
                    fixedWidth = 210,
                    fixedHeight = 235,
                    alignment = TextAnchor.MiddleCenter
                };

                AssetButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedWidth = 200,
                    fixedHeight = 25,
                    alignment = TextAnchor.MiddleCenter
                };

                AssetTypeButton = new GUIStyle(EditorStyles.toolbarButton) {
                    fixedWidth = 200
                };

                SearchTextField = new GUIStyle(EditorStyles.textField) {
                    fixedHeight = 20
                };

                SearchButton = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 12,
                    fixedWidth = 80,
                    fixedHeight = 20,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
    }
}