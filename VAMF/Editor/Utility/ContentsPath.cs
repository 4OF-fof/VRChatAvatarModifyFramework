using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using VAMF.Editor.Schemas;

namespace VAMF.Editor.Utility {
    public static class ContentsPath {
        private static readonly string DocumentsPath = Environment.SpecialFolder.MyDocuments.ToString().Replace("\\", "/");
        public static readonly string RootDirPath = $"{DocumentsPath}/VAMF";
        public static readonly string AssetsDirPath = $"{RootDirPath}/Assets";
        public static readonly string ThumbnailsDirPath = $"{RootDirPath}/Thumbnails";
        public static readonly string BoothThumbnailsDirPath = $"{ThumbnailsDirPath}/Booth";
        public static readonly string UnzipDirPath = $"{RootDirPath}/Unzip";
        public static readonly string ModifiedDirPath = $"{RootDirPath}/Modified";
        public static readonly string DataFilePath = $"{RootDirPath}/AssetsData.json";

        public static void Initialize() {
            if(!Directory.Exists(RootDirPath)) {
                Directory.CreateDirectory(RootDirPath);
            }
            if(!Directory.Exists(AssetsDirPath)) {
                Directory.CreateDirectory(AssetsDirPath);
            }
            if(!Directory.Exists(ThumbnailsDirPath)) {
                Directory.CreateDirectory(ThumbnailsDirPath);
            }
            if(!Directory.Exists(BoothThumbnailsDirPath)) {
                Directory.CreateDirectory(BoothThumbnailsDirPath);
            }
            if(!Directory.Exists(UnzipDirPath)) {
                Directory.CreateDirectory(UnzipDirPath);
            }
            if(!Directory.Exists(ModifiedDirPath)) {
                Directory.CreateDirectory(ModifiedDirPath);
            }
            if (File.Exists(DataFilePath)) return;
            var assetDataList = new AssetDataList {
                assetList = new List<AssetData>()
            };
            var jsonContent = JsonUtility.ToJson(assetDataList, true);
            File.WriteAllText(DataFilePath, jsonContent);
        }
    }
}