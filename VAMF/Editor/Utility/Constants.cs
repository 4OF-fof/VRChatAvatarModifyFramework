using System;

namespace VAMF.Editor.Utility {
    public class Constants {
        public static readonly string DocumentsPath = Environment.SpecialFolder.MyDocuments.ToString().Replace("\\", "/");
        public static readonly string RootDirPath = $"{DocumentsPath}/VAMF";
        public static readonly string AssetsDirPath = $"{RootDirPath}/Assets";
        public static readonly string ThumbnailsDirPath = $"{RootDirPath}/Thumbnails";
        public static readonly string BoothThumbnailsDirPath = $"{ThumbnailsDirPath}/Booth";
        public static readonly string UnzipDirPath = $"{RootDirPath}/Unzip";
        public static readonly string ModifiedDirPath = $"{RootDirPath}/Modified";
        public static readonly string DataFilePath = $"{RootDirPath}/AssetsData.json";
    }
}