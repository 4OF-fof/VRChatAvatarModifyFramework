using System.IO;
using UnityEditor;
using VAMF.Editor.Utility;

namespace VAMF.Editor.Window {
    public class OpenFolder : EditorWindow {
        [MenuItem("VAMF/Open VAMF Folder", priority = 101)]
        private static void OpenVamfFolder() {
            if(!Directory.Exists(Constants.AssetsDirPath)) {
                Directory.CreateDirectory(Constants.AssetsDirPath);
            }
            EditorUtility.RevealInFinder(Constants.AssetsDirPath);
        }
    }
} 