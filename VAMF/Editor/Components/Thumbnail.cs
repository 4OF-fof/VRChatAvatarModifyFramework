using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class Thumbnail {
    private static Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();
    private static Texture2D dummyThumbnail;
    private static Texture2D errorThumbnail;

    private static void InitializeSystemTextures() {
        if(dummyThumbnail == null) {
            dummyThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VAMF/Editor/dummy.png");
        }
        if(errorThumbnail == null) {
            errorThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VAMF/Editor/dummy.png");
        }
    }

    public static void ClearCache() {
        foreach (var texture in thumbnailCache.Values) {
            if (texture != null) {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        thumbnailCache.Clear();
    }

    public static void DrawThumbnail(string relativeThumbnailFilePath, int size) {
        InitializeSystemTextures();

        string thumbnailFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            "VAMF",
            relativeThumbnailFilePath
        ).Replace("\\", "/");

        if(string.IsNullOrEmpty(thumbnailFilePath)) {
            GUILayout.Label(new GUIContent(dummyThumbnail), GUILayout.Width(size), GUILayout.Height(size));
            return;
        }

        if(thumbnailCache.TryGetValue(thumbnailFilePath, out Texture2D cachedThumbnail)) {
            if(cachedThumbnail != null) {
                GUILayout.Label(new GUIContent(cachedThumbnail), GUILayout.Width(size), GUILayout.Height(size));
                return;
            }else {
                thumbnailCache.Remove(thumbnailFilePath);
            }
        }

        try {
            if(File.Exists(thumbnailFilePath)) {
                byte[] fileData;
                using(var fileStream = new FileStream(thumbnailFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    fileData = new byte[fileStream.Length];
                    fileStream.Read(fileData, 0, fileData.Length);
                }

                var thumbnail = new Texture2D(1, 1);
                if(thumbnail.LoadImage(fileData)) {
                    if (thumbnailCache.ContainsKey(thumbnailFilePath)) {
                        var oldTexture = thumbnailCache[thumbnailFilePath];
                        if (oldTexture != null) {
                            UnityEngine.Object.DestroyImmediate(oldTexture);
                        }
                    }
                    thumbnailCache[thumbnailFilePath] = thumbnail;
                    GUILayout.Label(new GUIContent(thumbnail), GUILayout.Width(size), GUILayout.Height(size));
                    return;
                }else {
                    UnityEngine.Object.DestroyImmediate(thumbnail);
                }
            }
        }catch(Exception e) {
            Debug.LogError($"Failed to read thumbnail file: {e.Message}");
        }

        GUILayout.Label(new GUIContent(errorThumbnail), GUILayout.Width(size), GUILayout.Height(size));
    }
}
