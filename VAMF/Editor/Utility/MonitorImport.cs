using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace VAMF.Editor.Utility {
    [Serializable]
    public class FileHashInfo {
        [FormerlySerializedAs("FilePath")] public string filePath;
        [FormerlySerializedAs("FileHash")] public string fileHash;
    }

    [Serializable]
    public class PackageImportHistory {
        [FormerlySerializedAs("Files")] public List<FileHashInfo> files = new();
    }

    public static class MonitorImport {
        private static readonly List<string> ImportedAssetPaths = new();
        private static readonly HashSet<string> PreExistingAssetPaths = new();
        private static bool _isImportingPackage;
        private static string _outputJsonPath = "Assets/VAMF/Data/import_history.json";

        private const string OutputPathPrefKey = "VAMF_MonitorImport_OutputPath";
        
        [InitializeOnLoadMethod]
        private static void Initialize() {
            LoadSettings();
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            AssetDatabase.importPackageStarted += OnImportPackageStarted;
        }
        
        private static void LoadSettings() {
            if(EditorPrefs.HasKey(OutputPathPrefKey)) {
                _outputJsonPath = EditorPrefs.GetString(OutputPathPrefKey);
            }else {
                EditorPrefs.SetString(OutputPathPrefKey, _outputJsonPath);
            }
        }

        private static void OnImportPackageCompleted(string packageName) {
            ProcessImportedPackage(packageName);
            _isImportingPackage = false;
            ImportedAssetPaths.Clear();
            PreExistingAssetPaths.Clear();
        }

        private static void OnImportPackageCancelled(string packageName) {
            _isImportingPackage = false;
            ImportedAssetPaths.Clear();
            PreExistingAssetPaths.Clear();
        }

        private static void OnImportPackageFailed(string packageName, string errorMessage) {
            _isImportingPackage = false;
            ImportedAssetPaths.Clear();
            PreExistingAssetPaths.Clear();
        }

        private static void OnImportPackageStarted(string packageName) {
            _isImportingPackage = true;
            ImportedAssetPaths.Clear();
            PreExistingAssetPaths.Clear();
            
            var allAssets = AssetDatabase.GetAllAssetPaths();
            foreach(var assetPath in allAssets) {
                PreExistingAssetPaths.Add(assetPath);
            }
        }

        private static void ProcessImportedPackage(string packageName) {
            SaveMaterialFiles();
            SaveFileHashesToJson(packageName);
        }
        
        private static void SaveMaterialFiles() {
            try {
                foreach (var assetPath in ImportedAssetPaths.Where(assetPath => assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                             File.Exists(assetPath))) {
                    try {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                        if (material != null) {
                            EditorUtility.SetDirty(material);
                        }
                    }catch(Exception ex) {
                        Debug.LogError($"Error occurred while saving material: {assetPath}, Error: {ex.Message}");
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }catch(Exception ex) {
                Debug.LogError($"Error in SaveMaterialFiles: {ex.Message}");
            }
        }
        
        public static bool IsImportingPackage() {
            return _isImportingPackage;
        }
        
        public static void AddImportedAsset(string assetPath) {
            if(_isImportingPackage && !PreExistingAssetPaths.Contains(assetPath) && !ImportedAssetPaths.Contains(assetPath)) {
                ImportedAssetPaths.Add(assetPath);
            }
        }

        private static void SaveFileHashesToJson(string packageName) {
            var fileHashInfos = (from assetPath in ImportedAssetPaths where File.Exists(assetPath) let hash = CalculateFileHash(assetPath) select new FileHashInfo { filePath = assetPath, fileHash = hash }).ToList();

            var history = LoadOrCreateImportHistory();
            history.files.AddRange(fileHashInfos);
            
            try {
                var json = JsonUtility.ToJson(history, true);
                
                var directory = Path.GetDirectoryName(_outputJsonPath);
                if(!Directory.Exists(directory)) {
                    if (directory != null) Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_outputJsonPath, json);
                AssetDatabase.ImportAsset(_outputJsonPath);
            } catch(Exception ex) {
                Debug.LogError($"Error saving import history to JSON: {ex.Message}");
            }
        }
        
        private static PackageImportHistory LoadOrCreateImportHistory() {
            if (!File.Exists(_outputJsonPath))
                return new PackageImportHistory {
                    files = new List<FileHashInfo>()
                };
            try {
                var json = File.ReadAllText(_outputJsonPath);
                var history = JsonUtility.FromJson<PackageImportHistory>(json);
                    
                if(history is { files: not null }) {
                    return history;
                }
            }catch(Exception ex) {
                Debug.LogError($"Error loading import history file: {_outputJsonPath}, Error: {ex.Message}");
                Debug.Log("Creating new import history file.");
            }

            return new PackageImportHistory {
                files = new List<FileHashInfo>()
            };
        }
        
        private static string CalculateFileHash(string filePath) {
            try {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = md5.ComputeHash(stream);
                var sb = new StringBuilder();
                        
                foreach (var t in hashBytes) {
                    sb.Append(t.ToString("x2"));
                }
                        
                return sb.ToString();
            }catch(Exception ex) {
                Debug.LogError($"Error calculating hash for file: {filePath}, Error: {ex.Message}");
                return "error_calculating_hash";
            }
        }

        public static void RemoveDeletedAssetFromJson(string assetPath) {
            if (string.IsNullOrEmpty(assetPath)) return;
            
            try {
                var history = LoadOrCreateImportHistory();
                var wasRemoved = false;
                
                history.files.RemoveAll(file => {
                    var shouldRemove = file.filePath == assetPath;
                    if (!shouldRemove) return false;
                    wasRemoved = true;
                    Debug.Log($"Removed deleted file from import history: {assetPath}");
                    return true;
                });

                if(!wasRemoved) return;
                var json = JsonUtility.ToJson(history, true);
                    
                var directory = Path.GetDirectoryName(_outputJsonPath);
                if(!Directory.Exists(directory)) {
                    if (directory != null) Directory.CreateDirectory(directory);
                }
                    
                File.WriteAllText(_outputJsonPath, json);
                AssetDatabase.ImportAsset(_outputJsonPath);
            } catch(Exception ex) {
                Debug.LogError($"Error removing deleted asset from JSON: {assetPath}, Error: {ex.Message}");
            }
        }

        public static void UpdateAssetPathInJson(string oldPath, string newPath) {
            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath)) return;
            
            try {
                var history = LoadOrCreateImportHistory();
                var wasUpdated = false;
                
                foreach(var fileInfo in history.files.Where(fileInfo => fileInfo.filePath == oldPath)) {
                    fileInfo.filePath = newPath;
                    wasUpdated = true;
                    Debug.Log($"Updated asset path: {oldPath} -> {newPath}");
                }

                if(!wasUpdated) return;
                var json = JsonUtility.ToJson(history, true);
                    
                var directory = Path.GetDirectoryName(_outputJsonPath);
                if(!Directory.Exists(directory)) {
                    if (directory != null) Directory.CreateDirectory(directory);
                }
                    
                File.WriteAllText(_outputJsonPath, json);
                AssetDatabase.ImportAsset(_outputJsonPath);
            } catch(Exception ex) {
                Debug.LogError($"Error updating asset path in JSON: {oldPath} -> {newPath}, Error: {ex.Message}");
            }
        }
    }

    public class PackageAssetPostprocessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths) {
            
            if(MonitorImport.IsImportingPackage()) {
                foreach(var assetPath in importedAssets) {
                    MonitorImport.AddImportedAsset(assetPath);
                }
            }
            
            if(deletedAssets is { Length: > 0 }) {
                foreach (var deletedAssetPath in deletedAssets) {
                    MonitorImport.RemoveDeletedAssetFromJson(deletedAssetPath);
                }
            }

            if(movedAssets == null || movedFromAssetPaths == null ||
                movedAssets.Length <= 0 || movedAssets.Length != movedFromAssetPaths.Length) return;
            for (var i = 0; i < movedAssets.Length; i++) {
                MonitorImport.UpdateAssetPathInJson(movedFromAssetPaths[i], movedAssets[i]);
            }
        }
    }

}

