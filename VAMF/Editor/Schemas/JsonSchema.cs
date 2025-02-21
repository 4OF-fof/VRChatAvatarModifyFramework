using System;
using System.Collections.Generic;

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
            uid = this.uid,
            name = this.name,
            filePath = this.filePath,
            sourceFilePath = this.sourceFilePath,
            url = this.url,
            thumbnailFilePath = this.thumbnailFilePath,
            description = this.description,
            isLatest = this.isLatest,
            supportAvatar = this.supportAvatar,
            dependencies = new List<string>(this.dependencies ?? new List<string>()),
            oldVersions = new List<string>(this.oldVersions ?? new List<string>()),
            assetType = this.assetType
        };
    }
}

[Serializable]
public class AssetDataList {
    public List<AssetData> assetList = new List<AssetData>();
}