using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VAMF.Editor.Components {
    public static class Thumbnail {
        private static readonly Dictionary<string, Texture2D> ThumbnailCache = new Dictionary<string, Texture2D>();
        private static Texture2D _dummyThumbnail;
        private static Texture2D _errorThumbnail;

        private static void InitializeSystemTextures() {
            _dummyThumbnail ??= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VAMF/Editor/dummy.png");
            _errorThumbnail ??= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VAMF/Editor/dummy.png");
        }

        public static void ClearCache() {
            foreach (var texture in ThumbnailCache.Values) {
                if (texture is not null) {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            ThumbnailCache.Clear();
        }

        public static void DrawThumbnail(string relativeThumbnailFilePath, int size) {
            InitializeSystemTextures();

            string thumbnailFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VAMF",
                relativeThumbnailFilePath
            ).Replace("\\", "/");

            if(string.IsNullOrEmpty(thumbnailFilePath)) {
                GUILayout.Label(new GUIContent(_dummyThumbnail), GUILayout.Width(size), GUILayout.Height(size));
                return;
            }

            if(ThumbnailCache.TryGetValue(thumbnailFilePath, out Texture2D cachedThumbnail)) {
                if(cachedThumbnail is not null) {
                    GUILayout.Label(new GUIContent(cachedThumbnail), GUILayout.Width(size), GUILayout.Height(size));
                    return;
                }else {
                    ThumbnailCache.Remove(thumbnailFilePath);
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
                        if (ThumbnailCache.TryGetValue(thumbnailFilePath, out var oldTexture)) {
                            if (oldTexture is not null) {
                                UnityEngine.Object.DestroyImmediate(oldTexture);
                            }
                        }
                        ThumbnailCache[thumbnailFilePath] = thumbnail;
                        GUILayout.Label(new GUIContent(thumbnail), GUILayout.Width(size), GUILayout.Height(size));
                        return;
                    }else {
                        UnityEngine.Object.DestroyImmediate(thumbnail);
                    }
                }
            }catch(Exception e) {
                Debug.LogError($"Failed to read thumbnail file: {e.Message}");
            }

            GUILayout.Label(new GUIContent(_errorThumbnail), GUILayout.Width(size), GUILayout.Height(size));
        }
    }
}
