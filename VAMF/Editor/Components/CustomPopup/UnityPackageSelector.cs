using UnityEditor;
using UnityEngine;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

public class UnityPackageSelector: EditorWindow {
    private Vector2 scrollPosition;

    private int selectedIndex = 0;
    private static int result = 0;

    private List<ZipArchiveEntry> unityPackages;

    public static int ShowWindow(List<ZipArchiveEntry> packages) {
        var window = GetWindow<UnityPackageSelector>("Unity Package Selector", true);
        window.unityPackages = packages;
        window.minSize = new Vector2(500, 300);
        window.maxSize = new Vector2(500, 300);
        window.ShowModal();
        return result;
    }

    void OnGUI() {
        GUILayout.Label("Please select a package to import.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        using(new GUILayout.ScrollViewScope(scrollPosition)) {
            for (int i = 0; i < unityPackages.Count; i++) {
                using(new GUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    if (GUILayout.Toggle(selectedIndex == i, "", GUILayout.Width(20))) {
                        selectedIndex = i;
                    }
                    
                    GUILayout.Label(unityPackages[i].Name);
                }
            }
        }

        GUILayout.Space(20);

        using (new EditorGUI.DisabledScope(selectedIndex < 0)) {
            if (GUILayout.Button("Select", GUILayout.Height(30))) {
                result = selectedIndex;
                Close();
            }
        }
    }

    void OnDestroy() {
        result = selectedIndex;
    }
}
