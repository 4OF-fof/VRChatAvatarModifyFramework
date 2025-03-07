using System.IO;
using UnityEditor;

namespace VAMF.Editor.Window {
    public class OpenFolder : EditorWindow {
        [MenuItem("VAMF/Open VAMF Folder", priority = 101)]
        private static void OpenVamfFolder() {
            string dataRootPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "VAMF/Assets"
                ).Replace("\\", "/");

            if(!Directory.Exists(dataRootPath)) {
                Directory.CreateDirectory(dataRootPath);
            }

            EditorUtility.RevealInFinder(dataRootPath);
        }
    }
} 