using System.Collections.Generic;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace VAMF.Editor.Components.CustomPopup {
    public class UnityPackageSelector: EditorWindow {
        private Vector2 _scrollPosition;

        private int _selectedIndex;
        private static int _result;

        private List<ZipArchiveEntry> _unityPackages;

        public static int ShowWindow(List<ZipArchiveEntry> packages) {
            var window = GetWindow<UnityPackageSelector>("Unity Package Selector", true);
            window._unityPackages = packages;
            window.minSize = new Vector2(500, 300);
            window.maxSize = new Vector2(500, 300);
            window.ShowModal();
            return _result;
        }

        private void OnGUI() {
            GUILayout.Label("Please select a package to import.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            using(new GUILayout.ScrollViewScope(_scrollPosition)) {
                for(var i = 0; i < _unityPackages.Count; i++) {
                    using(new GUILayout.HorizontalScope(EditorStyles.helpBox)) {
                        if (GUILayout.Toggle(_selectedIndex == i, "", GUILayout.Width(20))) {
                            _selectedIndex = i;
                        }
                    
                        GUILayout.Label(_unityPackages[i].Name);
                    }
                }
            }

            GUILayout.Space(20);

            using(new EditorGUI.DisabledScope(_selectedIndex < 0)) {
                if(!GUILayout.Button("Select", GUILayout.Height(30))) return;
                _result = _selectedIndex;
                Close();
            }
        }

        private void OnDestroy() {
            _result = _selectedIndex;
        }
    }
}
