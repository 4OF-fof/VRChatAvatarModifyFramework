using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace VAMF.Editor.Utility {
    [Serializable]
    public class BoothResponse {
        public BoothImage[] images;
    }

    [Serializable]
    public class BoothImage {
        public string original;
    }

    public static class WebRequest {
        public static async Task<string> GetThumbnail(string thumbnailUrl) {
            if(thumbnailUrl == null) {
                Debug.LogError("Thumbnail URL is null");
                return null;
            }

            using var client = new HttpClient();
            var thumbnailFileName = "booth_" + thumbnailUrl.Split('/')[thumbnailUrl.Split('/').Length - 2] + ".jpg";
            var thumbnailFilePath = ContentsPath.BoothThumbnailsDirPath + "/" + thumbnailFileName;
            if(File.Exists(thumbnailFilePath)) {
                return thumbnailFilePath.Replace(ContentsPath.BoothThumbnailsDirPath, "Thumbnail/Booth");
            }
            using(var response = await client.GetAsync(thumbnailUrl)) {
                await using(var fileStream = File.Create(thumbnailFilePath)) {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
            return thumbnailFilePath.Replace(ContentsPath.BoothThumbnailsDirPath, "Thumbnail/Booth");
        }

        public static async Task<string> GetThumbnailUrl(string url) {
            if(url == null) {
                Debug.LogError("URL file is null");
                return null;
            }

            using var client = new HttpClient();
            var jsonUrl = url + ".json";
            var jsonResponse = await client.GetStringAsync(jsonUrl);
            var boothData = JsonUtility.FromJson<BoothResponse>(jsonResponse);
            if (boothData is { images: not null } && boothData.images.Length != 0)
                return boothData.images[0].original;
            Debug.LogError("Invalid Booth data format");
            return null;
        }
    }
}
