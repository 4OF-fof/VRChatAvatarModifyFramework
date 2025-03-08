using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VAMF.Editor.Schemas;

namespace VAMF.Editor.Utility {
    public static class UnityPackageManager {
        public static void ImportAsset(AssetData assetData) {
            var dependenciesList = SearchDependencies(assetData.dependencies);
            dependenciesList.Add(assetData.uid);
            var importPackageList = (from dependency 
                                     in dependenciesList 
                                     select AssetDataController.GetAssetData(dependency) 
                                     into dependencyAssetData 
                                     where dependencyAssetData != null 
                                     select dependencyAssetData.name).ToList();
            if (importPackageList.Count <= 0) return;
            var message = "The following packages will be imported:\n" + string.Join("\n", importPackageList);
            var userChoice = EditorUtility.DisplayDialog(
                "Package Import Confirmation",
                message,
                "OK",
                "Cancel"
            );

            if (!userChoice) return;
            foreach (var unityPackagePath 
                     in from dependency 
                     in dependenciesList 
                     select AssetDataController.GetAssetData(dependency) 
                     into dependencyAssetData 
                     where dependencyAssetData != null 
                     select ContentsPath.RootDirPath + "/" + dependencyAssetData.filePath) {
                AssetDatabase.ImportPackage(unityPackagePath, false);
            }
        }

        private static List<string> SearchDependencies(List<string> dependencies) {
            if (dependencies == null || dependencies.Count == 0) {
                return new List<string>();
            }

            var visited = new HashSet<string>();
            var result = new List<string>();

            foreach (var dependency in dependencies) {
                Dfs(dependency, visited, result);
            }

            return result;
        }

        private static void Dfs(string currentUid, HashSet<string> visited, List<string> result) {
            if (!visited.Add(currentUid)) {
                return;
            }

            var assetData = AssetDataController.GetAssetData(currentUid);
            if (assetData?.dependencies != null) {
                foreach (var dependencyUid in assetData.dependencies) {
                    Dfs(dependencyUid, visited, result);
                }
            }

            result.Add(currentUid);
        }
    }
}
