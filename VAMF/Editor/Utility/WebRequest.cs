using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Utility {
    [Serializable]
    public class BoothResponse {
        public BoothImage[] images;
    }

    [Serializable]
    public class BoothImage {
        public string original;
    }

    public class WebRequest {
        public static async Task<string> GetThumbnail(string thumbnailUrl) {
            string thumbnailFilePath;
            if(thumbnailUrl == null) {
                Debug.LogError("Thumbnail URL is null");
                return null;
            }
            string thumbnailFolderPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "VAMF/Thumbnail"
            ).Replace("\\", "/");
            if(!Directory.Exists(thumbnailFolderPath)) {
                Directory.CreateDirectory(thumbnailFolderPath);
            }
            using(var client = new HttpClient()) {
                string thumbnailFileName = "booth_" + thumbnailUrl.Split('/')[thumbnailUrl.Split('/').Length - 2] + ".jpg";
                thumbnailFilePath = Path.Combine(thumbnailFolderPath, thumbnailFileName);
                if(File.Exists(thumbnailFilePath)) {
                    return thumbnailFilePath.Replace("\\", "/").Replace(thumbnailFolderPath, "Thumbnail");
                }
                using(var response = await client.GetAsync(thumbnailUrl)) {
                    using(var fileStream = File.Create(thumbnailFilePath)) {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
                return thumbnailFilePath.Replace("\\", "/").Replace(thumbnailFolderPath, "Thumbnail");
            }
        }

        public static async Task<string> GetThumbnailUrl(string url) {
            if(url == null) {
                Debug.LogError("URL file is null");
                return null;
            }
            using(var client = new HttpClient()) {
                string jsonUrl = url + ".json";
                string jsonResponse = await client.GetStringAsync(jsonUrl);
                var boothData = JsonUtility.FromJson<BoothResponse>(jsonResponse);
                if (boothData == null || boothData.images == null || boothData.images.Length == 0) {
                    Debug.LogError("Invalid Booth data format");
                    return null;
                }
                return boothData.images[0].original;
            }
        }
    }
}
