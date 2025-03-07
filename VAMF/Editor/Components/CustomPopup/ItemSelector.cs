using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VAMF.Editor.Schemas;

namespace VAMF.Editor.Components.CustomPopup {
    public class ItemSelector: EditorWindow {

        private string _searchItem = "";
        private Vector2 _scrollPosition = Vector2.zero;
        private List<AssetData> _filteredAssetList;
        private List<AssetData> _assetDataList;
        private System.Action<string> _onItemSelected;
        private List<string> _ignoreList;
        private string _selfUid;

        public static void ShowWindow(System.Action<string> callback, List<AssetData> assetDataList, List<string> ignoreList = null, string selfUid = null) {
            var window = CreateInstance<ItemSelector>();
            window.minSize = new Vector2(200, 250);
            window.maxSize = new Vector2(200, 250);
            window._onItemSelected = callback;
            window._assetDataList = assetDataList.ToList();
            window._ignoreList = ignoreList ?? new List<string>();
            window._selfUid = selfUid;
        
            Vector2 mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(mousePosition.x, mousePosition.y, 200, 250);

            window.ShowPopup();
        }

        void OnGUI() {
            string previousSearch = _searchItem;
            using(new GUILayout.HorizontalScope()) {
                GUILayout.Label("Search:", GUILayout.Width(47));
                _searchItem = GUILayout.TextField(_searchItem, Style.SearchTextField);
            }

            if(_filteredAssetList == null) {
                FilterAssets();
            }

            if(previousSearch != _searchItem) {
                FilterAssets();
            }

            using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
                using(var scrollView = new GUILayout.ScrollViewScope(_scrollPosition)) {
                    _scrollPosition = scrollView.scrollPosition;
                    if (_filteredAssetList != null)
                        foreach (var assetData in _filteredAssetList) {
                            using (new GUILayout.HorizontalScope()) {
                                bool isExistingDependency = _ignoreList.Contains(assetData.uid);
                                using (new EditorGUI.DisabledScope(isExistingDependency)) {
                                    if (GUILayout.Button("+", Style.SelectButton)) {
                                        if (_onItemSelected != null) {
                                            _onItemSelected.Invoke(assetData.uid);
                                        }
                                    }
                                }

                                GUILayout.Label(assetData.name);
                            }
                        }
                }
            }
        }

        void OnEnable() {
            wantsMouseMove = true;
        }

        void OnLostFocus() {
            _searchItem = "";
            Close();
        }

        private void FilterAssets() {
            _filteredAssetList = _assetDataList
                .Where(asset => 
                    (string.IsNullOrEmpty(_searchItem) || asset.name.ToLower().Contains(_searchItem.ToLower())) &&
                    (_selfUid == null                  || asset.uid != _selfUid))
                .ToList();
            Repaint();
        }

        private static class Style {
            public static readonly GUIStyle SearchTextField;
            public static readonly GUIStyle SelectButton;

            static Style() {
                SearchTextField = new GUIStyle(EditorStyles.textField) {
                    fixedHeight = 20
                };

                SelectButton = new GUIStyle(EditorStyles.miniButton) {
                    fixedWidth = 20
                };
            }
        }
    }
}

