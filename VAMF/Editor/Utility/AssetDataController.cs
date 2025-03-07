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
            string assetFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF/Assets"
            ).Replace("\\", "/");

            if (!Directory.Exists(assetFolderPath)) {
                Directory.CreateDirectory(assetFolderPath);
            }

            List<AssetData> assetList = GetAllAssetData();

            string modifyFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF",
                "Modified"
            ).Replace("\\", "/");

            assetList.RemoveAll(asset => {
                string assetFullPath = Path.Combine(assetFolderPath, asset.sourceFilePath.Replace("Assets/", "")).Replace("\\", "/");
                string modifyFullPath = Path.Combine(modifyFolderPath, asset.sourceFilePath.Replace("Modified/", "")).Replace("\\", "/");
                
                if(!File.Exists(assetFullPath) && !File.Exists(modifyFullPath)) {
                    Debug.Log($"Removing asset data for non-existent file: {asset.name} ({asset.sourceFilePath})");
                    return true;
                }
                return false;
            });

            foreach (string unityPackageFile in Directory.GetFiles(assetFolderPath, "*.unitypackage")) {
                string relativeUnityPackageFilePath = unityPackageFile.Replace("\\", "/").Replace(assetFolderPath, "Assets");
                if(assetList.Find(asset => asset.sourceFilePath == relativeUnityPackageFilePath) == null) {
                    AssetData assetData = new AssetData {
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
            }

            foreach (string zipFile in Directory.GetFiles(assetFolderPath, "*.zip")) {
                string relativeZipFilePath = zipFile.Replace("\\", "/").Replace(assetFolderPath, "Assets");
                if(assetList.Find(asset => asset.sourceFilePath == relativeZipFilePath) == null) {
                    string newUid = Guid.NewGuid().ToString();
                    string unzipFilePath = UnzipFile(zipFile, newUid);
                    if(unzipFilePath != null) {
                        AssetData assetData = new AssetData {
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
                }
            }
            SaveAssetDataList(assetList);
        }

        public static AssetData GetAssetData(string uid) {
            List<AssetData> assetList = GetAllAssetData();
            if(assetList.Find(asset => asset.uid == uid) == null) {
                Debug.LogError($"Asset data not found: {uid}");
                return null;
            }else if (assetList.FindAll(asset => asset.uid == uid).Count > 1) {
                Debug.LogError($"Asset uid is not unique: {uid}");
                return null;
            }else {
                return assetList.Find(asset => asset.uid == uid);
            }
        }

        public static List<AssetData> GetAllAssetData() {
            string assetDataPath = GetAssetDataPath();

            try {
                string jsonContent = File.ReadAllText(assetDataPath);
                AssetDataList assetDataList = JsonUtility.FromJson<AssetDataList>(jsonContent);
                if (assetDataList == null) {
                    Debug.LogError($"Asset data file is empty or corrupted: {assetDataPath}");
                    return new List<AssetData>();
                }
                return assetDataList.assetList;
            }catch(Exception e) {
                Debug.LogError($"Error reading asset data: {e.Message}");
                return new List<AssetData>();
            }
        }

        public static void AddAssetData(AssetData assetData) {
            List<AssetData> assetList = GetAllAssetData();
            if(assetList.Find(asset => asset.uid == assetData.uid) != null) {
                Debug.LogError($"Asset uid is not unique: {assetData.uid}");
                return;
            }
            assetList.Add(assetData);
            SaveAssetDataList(assetList);
        }

        public static void UpdateAssetData(string uid, AssetData assetData) {
            List<AssetData> assetList = GetAllAssetData();
            int index = assetList.FindIndex(asset => asset.uid == uid);
            if(index != -1) {
                assetList[index] = assetData;
            }else {
                Debug.LogError($"Target asset not found: {uid}");
                return;
            }
            SaveAssetDataList(assetList);
        }

        public static void UpdateUnityPackage(AssetData assetData) {
            string assetFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF"
            ).Replace("\\", "/");
            
            string fullPath = Path.Combine(assetFolderPath, assetData.sourceFilePath);
            fullPath = Path.GetFullPath(fullPath);

            string unzipFilePath = UnzipFile(fullPath, assetData.uid, true);

            AssetData newAssetData = GetAssetData(assetData.uid);
            newAssetData.filePath = unzipFilePath;

            UpdateAssetData(assetData.uid, newAssetData);
        }

        private static string GetAssetDataPath() {
            string dataRootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF"
            ).Replace("\\", "/");

            string assetDataPath = Path.Combine(dataRootPath, "VAMFAssetData.json").Replace("\\", "/");
            
            if (!Directory.Exists(dataRootPath)) {
                Directory.CreateDirectory(dataRootPath);
            }
            if (!File.Exists(assetDataPath)) {
                AssetDataList assetDataList = new AssetDataList {
                    assetList = new List<AssetData>()
                };
                string jsonContent = JsonUtility.ToJson(assetDataList, true);
                File.WriteAllText(assetDataPath, jsonContent);
                Debug.Log($"Asset data file created at: {assetDataPath}");
            }
            
            return assetDataPath;
        }

        private static void SaveAssetDataList(List<AssetData> newAssetList) {
            string assetDataPath = GetAssetDataPath();
            AssetDataList assetDataList = new AssetDataList {
                assetList = newAssetList
            };
            string jsonContent = JsonUtility.ToJson(assetDataList, true);
            File.WriteAllText(assetDataPath, jsonContent);
        }

        private static string UnzipFile(string zipFilePath, string uid = null, bool forceDialog = false) {
            string unzipFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF/Unzip"
            ).Replace("\\", "/");

            if(!Directory.Exists(unzipFolderPath)) {
                Directory.CreateDirectory(unzipFolderPath);
            }

            try {
                using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
                var unityPackages = archive.Entries.Where(entry => entry.Name.EndsWith(".unitypackage")).ToList();
                int targetIndex = 0;

                if(unityPackages.Count == 0) {
                    Debug.LogWarning("Cannot find UnityPackage in zip file.");
                    return null;
                }else if(unityPackages.Count > 1 || forceDialog) {
                    targetIndex = UnityPackageSelector.ShowWindow(unityPackages);
                }

                string guidToUse = uid ?? Guid.NewGuid().ToString();
                string extractPath = Path.Combine(unzipFolderPath, unityPackages[targetIndex].Name)
                    .Replace(".unitypackage", $"_{guidToUse}.unitypackage");

                if(File.Exists(extractPath)) {
                    File.Delete(extractPath);
                }

                unityPackages[targetIndex].ExtractToFile(extractPath);
                return extractPath.Replace("\\", "/").Replace(unzipFolderPath, "Unzip");
            }catch(Exception e) {
                Debug.LogError($"Error occurred while unzipping file: {e.Message}");
                return null;
            }
        }
    }
}