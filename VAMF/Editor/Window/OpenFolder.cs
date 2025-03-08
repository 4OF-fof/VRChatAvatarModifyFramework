using System.IO;
using UnityEditor;
using VAMF.Editor.Utility;

namespace VAMF.Editor.Window {
    public class OpenFolder : EditorWindow {
        [MenuItem("VAMF/Open VAMF Folder", priority = 101)]
        private static void OpenVamfFolder() {
            ContentsPath.Initialize();
            EditorUtility.RevealInFinder(ContentsPath.AssetsDirPath);
        }
    }
} 