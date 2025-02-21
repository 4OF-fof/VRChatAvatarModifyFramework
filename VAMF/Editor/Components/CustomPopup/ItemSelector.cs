using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemSelector: EditorWindow {

    private string searchItem = "";
    private Vector2 scrollPosition = Vector2.zero;
    private List<AssetData> filteredAssetList;
    private List<AssetData> assetDataList;
    private System.Action<string> onItemSelected;
    private List<string> ignoreList;
    private string selfUid;

    public static void ShowWindow(System.Action<string> callback, List<AssetData> assetDataList, List<string> ignoreList = null, string selfUid = null) {
        var window = CreateInstance<ItemSelector>();
        window.minSize = new Vector2(200, 250);
        window.maxSize = new Vector2(200, 250);
        window.onItemSelected = callback;
        window.assetDataList = assetDataList.Where(asset => asset.isLatest).ToList();
        window.ignoreList = ignoreList ?? new List<string>();
        window.selfUid = selfUid;
        
        Vector2 mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        window.position = new Rect(mousePosition.x, mousePosition.y, 200, 250);

        window.ShowPopup();
    }

    void OnGUI() {
        string previousSearch = searchItem;
        using(new GUILayout.HorizontalScope()) {
            GUILayout.Label("Search:", GUILayout.Width(47));
            searchItem = GUILayout.TextField(searchItem, Style.searchTextField);
        }

        if(filteredAssetList == null) {
            FilterAssets();
        }

        if(previousSearch != searchItem) {
            FilterAssets();
        }

        using(new GUILayout.VerticalScope(EditorStyles.helpBox)) {
            using(var scrollView = new GUILayout.ScrollViewScope(scrollPosition)) {
                scrollPosition = scrollView.scrollPosition;
                foreach(var assetData in filteredAssetList) {
                    using(new GUILayout.HorizontalScope()) {
                        bool isExistingDependency = ignoreList.Contains(assetData.uid);
                        using(new EditorGUI.DisabledScope(isExistingDependency)) {
                            if(GUILayout.Button("+", Style.selectButton)) {
                                if(onItemSelected != null) {
                                    onItemSelected.Invoke(assetData.uid);
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
        searchItem = "";
        Close();
    }

    private void FilterAssets() {
        filteredAssetList = assetDataList
            .Where(asset => 
                (string.IsNullOrEmpty(searchItem) || asset.name.ToLower().Contains(searchItem.ToLower())) &&
                (selfUid == null || asset.uid != selfUid))
            .ToList();
        Repaint();
    }

    private class Style {
        public static GUIStyle searchTextField;
        public static GUIStyle selectButton;

        static Style() {
            searchTextField = new GUIStyle(EditorStyles.textField);
            searchTextField.fixedHeight = 20;

            selectButton = new GUIStyle(EditorStyles.miniButton);
            selectButton.fixedWidth = 20;
        }
    }
}

