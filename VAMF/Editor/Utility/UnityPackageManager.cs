using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Utility {
    public class UnityPackageManager {
        public static void ImportAsset(AssetData assetData) {
            string assetPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "VAMF",
                assetData.filePath
            ).Replace("\\", "/");

            List<string> ImportPackageList = new List<string>();

            List<string> dependenciesList = SearchDependencies(assetData.dependencies);
            dependenciesList.Add(assetData.uid);
            foreach (string dependency in dependenciesList) {
                AssetData dependencyAssetData = AssetDataController.GetAssetData(dependency);
                if(dependencyAssetData != null) {
                    ImportPackageList.Add(dependencyAssetData.name);
                }
            }

            if(ImportPackageList.Count > 0) {
                string message = "The following packages will be imported:\n" + string.Join("\n", ImportPackageList);
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
                            string UnityPackagePath = Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                                "VAMF",
                                dependencyAssetData.filePath
                            ).Replace("\\", "/");
                            AssetDatabase.ImportPackage(UnityPackagePath, false);
                        }
                    }
                }
            }
        }

        public static List<string> SearchDependencies(List<string> dependencies) {
            if (dependencies == null || dependencies.Count == 0) {
                return new List<string>();
            }

            HashSet<string> visited = new HashSet<string>();
            List<string> result = new List<string>();

            foreach (string dependency in dependencies) {
                DFS(dependency, visited, result);
            }

            return result;
        }

        private static void DFS(string currentUid, HashSet<string> visited, List<string> result) {
            if (visited.Contains(currentUid)) {
                return;
            }

            visited.Add(currentUid);

            AssetData assetData = AssetDataController.GetAssetData(currentUid);
            if (assetData?.dependencies != null) {
                foreach (string dependencyUid in assetData.dependencies) {
                    DFS(dependencyUid, visited, result);
                }
            }

            result.Add(currentUid);
        }
    }
}
