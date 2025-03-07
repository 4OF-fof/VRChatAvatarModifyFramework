using System.Collections.Generic;
using System.IO;
using UnityEditor;
using VAMF.Editor.Schemas;

namespace VAMF.Editor.Utility {
    public static class UnityPackageManager {
        public static void ImportAsset(AssetData assetData) {
            List<string> importPackageList = new List<string>();

            List<string> dependenciesList = SearchDependencies(assetData.dependencies);
            dependenciesList.Add(assetData.uid);
            foreach (string dependency in dependenciesList) {
                AssetData dependencyAssetData = AssetDataController.GetAssetData(dependency);
                if(dependencyAssetData != null) {
                    importPackageList.Add(dependencyAssetData.name);
                }
            }

            if(importPackageList.Count > 0) {
                string message = "The following packages will be imported:\n" + string.Join("\n", importPackageList);
                bool userChoice = EditorUtility.DisplayDialog(
                    "Package Import Confirmation",
                    message,
                    "OK",
                    "Cancel"
                );
                
                if(userChoice) {
                    foreach (string dependency in dependenciesList) {
                        AssetData dependencyAssetData = AssetDataController.GetAssetData(dependency);
                        if(dependencyAssetData != null) {
                            string unityPackagePath = Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                                "VAMF",
                                dependencyAssetData.filePath
                            ).Replace("\\", "/");
                            AssetDatabase.ImportPackage(unityPackagePath, false);
                        }
                    }
                }
            }
        }

        private static List<string> SearchDependencies(List<string> dependencies) {
            if (dependencies == null || dependencies.Count == 0) {
                return new List<string>();
            }

            HashSet<string> visited = new HashSet<string>();
            List<string> result = new List<string>();

            foreach (string dependency in dependencies) {
                Dfs(dependency, visited, result);
            }

            return result;
        }

        private static void Dfs(string currentUid, HashSet<string> visited, List<string> result) {
            if (!visited.Add(currentUid)) {
                return;
            }

            AssetData assetData = AssetDataController.GetAssetData(currentUid);
            if (assetData?.dependencies != null) {
                foreach (string dependencyUid in assetData.dependencies) {
                    Dfs(dependencyUid, visited, result);
                }
            }

            result.Add(currentUid);
        }
    }
}
