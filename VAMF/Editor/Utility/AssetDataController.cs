using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using VAMF.Editor.Components.CustomPopup;
using VAMF.Editor.Schemas;

namespace VAMF.Editor.Utility {
    public static class AssetDataController {
        public static void AutoRegisterAssetData() {
            if (!Directory.Exists(Constants.AssetsDirPath)) {
                Directory.CreateDirectory(Constants.AssetsDirPath);
            }

            var assetList = GetAllAssetData();

            assetList.RemoveAll(asset => {
                var assetFullPath = Constants.RootDirPath + "/" + asset.sourceFilePath;
                var modifyFullPath = Constants.RootDirPath + "/" + asset.sourceFilePath;

                if (File.Exists(assetFullPath) || File.Exists(modifyFullPath)) return false;
                Debug.Log($"Removing asset data for non-existent file: {asset.name} ({asset.sourceFilePath})");
                return true;
            });

            foreach (var unityPackageFile in Directory.GetFiles(Constants.AssetsDirPath, "*.unitypackage")) {
                var relativeUnityPackageFilePath = unityPackageFile.Replace("\\", "/").Replace(Constants.RootDirPath, "");
                if (assetList.Find(asset => asset.sourceFilePath == relativeUnityPackageFilePath) != null) continue;
                var assetData = new AssetData {
                    uid = Guid.NewGuid().ToString(),
                    name = Path.GetFileNameWithoutExtension(unityPackageFile),
                    filePath = relativeUnityPackageFilePath,
                    sourceFilePath = relativeUnityPackageFilePath,
                    url = "",
                    thumbnailFilePath = "",
                    description = "",
                    isLatest = true,
                    supportAvatar = new List<string>(),
                    dependencies = new List<string>(),
                    oldVersions = new List<string>(),
                    assetType = 0,
                };
                assetList.Add(assetData);
            }

            foreach (var zipFile in Directory.GetFiles(Constants.AssetsDirPath, "*.zip")) {
                var relativeZipFilePath = zipFile.Replace("\\", "/").Replace(Constants.RootDirPath, "");
                if (assetList.Find(asset => asset.sourceFilePath == relativeZipFilePath) != null) continue;
                var newUid = Guid.NewGuid().ToString();
                var unzipFilePath = UnzipFile(zipFile, newUid);
                if (unzipFilePath == null) continue;
                var assetData = new AssetData {
                    uid = newUid,
                    name = Path.GetFileNameWithoutExtension(zipFile),
                    filePath = unzipFilePath,
                    sourceFilePath = relativeZipFilePath,
                    url = "",
                    thumbnailFilePath = "",
                    description = "",
                    isLatest = true,
                    supportAvatar = new List<string>(),
                    dependencies = new List<string>(),
                    oldVersions = new List<string>(),
                    assetType = 0,
                };
                assetList.Add(assetData);
            }
            SaveAssetDataList(assetList);
        }

        public static AssetData GetAssetData(string uid) {
            var assetList = GetAllAssetData();
            if(assetList.Find(asset => asset.uid == uid) == null) {
                Debug.LogError($"Asset data not found: {uid}");
                return null;
            }
            if (assetList.FindAll(asset => asset.uid == uid).Count > 1) {
                Debug.LogError($"Asset uid is not unique: {uid}");
                return null;
            }

            return assetList.Find(asset => asset.uid == uid);
        }

        public static List<AssetData> GetAllAssetData() {
            var assetDataPath = GetAssetDataPath();

            try {
                var jsonContent = File.ReadAllText(assetDataPath);
                var assetDataList = JsonUtility.FromJson<AssetDataList>(jsonContent);
                if(assetDataList != null) return assetDataList.assetList;
                Debug.LogError($"Asset data file is empty or corrupted: {assetDataPath}");
                return new List<AssetData>();
            }catch(Exception e) {
                Debug.LogError($"Error reading asset data: {e.Message}");
                return new List<AssetData>();
            }
        }

        public static void AddAssetData(AssetData assetData) {
            var assetList = GetAllAssetData();
            if(assetList.Find(asset => asset.uid == assetData.uid) != null) {
                Debug.LogError($"Asset uid is not unique: {assetData.uid}");
                return;
            }
            assetList.Add(assetData);
            SaveAssetDataList(assetList);
        }
        
        public static void UpdateAssetData(string uid, AssetData assetData) {
            var assetList = GetAllAssetData();
            var index = assetList.FindIndex(asset => asset.uid == uid);
            if(index != -1) {
                assetList[index] = assetData;
            }else {
                Debug.LogError($"Target asset not found: {uid}");
                return;
            }
            SaveAssetDataList(assetList);
        }

        public static void UpdateUnityPackage(AssetData assetData) {
            var fullPath = Path.Combine(Constants.RootDirPath, assetData.sourceFilePath);
            fullPath = Path.GetFullPath(fullPath);

            var unzipFilePath = UnzipFile(fullPath, assetData.uid, true);

            var newAssetData = GetAssetData(assetData.uid);
            newAssetData.filePath = unzipFilePath;

            UpdateAssetData(assetData.uid, newAssetData);
        }

        private static string GetAssetDataPath() {
            if (!Directory.Exists(Constants.RootDirPath)) {
                Directory.CreateDirectory(Constants.RootDirPath);
            }

            if (File.Exists(Constants.DataFilePath)) return Constants.DataFilePath;
            var assetDataList = new AssetDataList {
                assetList = new List<AssetData>()
            };
            var jsonContent = JsonUtility.ToJson(assetDataList, true);
            File.WriteAllText(Constants.DataFilePath, jsonContent);
            Debug.Log($"Asset data file created at: {Constants.DataFilePath}");

            return Constants.DataFilePath;
        }

        private static void SaveAssetDataList(List<AssetData> newAssetList) {
            var assetDataPath = GetAssetDataPath();
            var assetDataList = new AssetDataList {
                assetList = newAssetList
            };
            var jsonContent = JsonUtility.ToJson(assetDataList, true);
            File.WriteAllText(assetDataPath, jsonContent);
        }

        private static string UnzipFile(string zipFilePath, string uid = null, bool forceDialog = false) {
            if(!Directory.Exists(Constants.UnzipDirPath)) {
                Directory.CreateDirectory(Constants.UnzipDirPath);
            }

            try {
                using var archive = ZipFile.OpenRead(zipFilePath);
                var unityPackages = archive.Entries.Where(entry => entry.Name.EndsWith(".unitypackage")).ToList();
                var targetIndex = 0;

                if(unityPackages.Count == 0) {
                    Debug.LogWarning("Cannot find UnityPackage in zip file.");
                    return null;
                }else if(unityPackages.Count > 1 || forceDialog) {
                    targetIndex = UnityPackageSelector.ShowWindow(unityPackages);
                }

                var guidToUse = uid ?? Guid.NewGuid().ToString();
                var extractPath = Path.Combine(Constants.UnzipDirPath, unityPackages[targetIndex].Name)
                    .Replace(".unitypackage", $"_{guidToUse}.unitypackage").Replace("\\", "/");
                if(File.Exists(extractPath)) {
                    File.Delete(extractPath);
                }

                unityPackages[targetIndex].ExtractToFile(extractPath);
                return extractPath.Replace(Constants.UnzipDirPath, "Unzip");
            }catch(Exception e) {
                Debug.LogError($"Error occurred while unzipping file: {e.Message}");
                return null;
            }
        }
    }
}