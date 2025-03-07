using System;
using System.Collections.Generic;

namespace VAMF.Editor.Schemas {
    [Serializable]
    public enum AssetType {
        Unregistered,
        Avatar,
        Hair,
        Cloth,
        Accessory,
        Gimmick,
        Texture,
        World,
        Other,
        Modified
    }

    [Serializable]
    public class AssetData {
        public string uid;
        public string name;
        public string filePath;
        public string sourceFilePath;
        public string url;
        public string thumbnailFilePath;
        public string description;
        public bool isLatest;
        public List<string> supportAvatar;
        public List<string> dependencies;
        public List<string> oldVersions;
        public AssetType assetType;

        public AssetData Clone() {
            return new AssetData {
                uid = uid,
                name = name,
                filePath = filePath,
                sourceFilePath = sourceFilePath,
                url = url,
                thumbnailFilePath = thumbnailFilePath,
                description = description,
                isLatest = isLatest,
                supportAvatar = supportAvatar,
                dependencies = new List<string>(dependencies ?? new List<string>()),
                oldVersions = new List<string>(oldVersions   ?? new List<string>()),
                assetType = assetType
            };
        }
    }

    [Serializable]
    public class AssetDataList {
        public List<AssetData> assetList = new List<AssetData>();
    }
}