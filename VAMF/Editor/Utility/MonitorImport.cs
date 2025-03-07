using System;
using System.Collections.Generic;
using System.IO;
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
        [FormerlySerializedAs("Files")] public List<FileHashInfo> files = new List<FileHashInfo>();
    }

    public class MonitorImport {
        private static List<string> _importedAssetPaths = new List<string>();
        private static HashSet<string> _preExistingAssetPaths = new HashSet<string>();
        private static string _currentImportingPackage = null;
        private static bool _isImportingPackage = false;
        private static string _outputJsonPath = "Assets/VAMF/Data/import_history.json";
        
        public const string OutputPathPrefKey = "VAMF_MonitorImport_OutputPath";
        
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
            _currentImportingPackage = null;
            _isImportingPackage = false;
            _importedAssetPaths.Clear();
            _preExistingAssetPaths.Clear();
        }

        private static void OnImportPackageCancelled(string packageName) {
            _currentImportingPackage = null;
            _isImportingPackage = false;
            _importedAssetPaths.Clear();
            _preExistingAssetPaths.Clear();
        }

        private static void OnImportPackageFailed(string packageName, string errorMessage) {
            _currentImportingPackage = null;
            _isImportingPackage = false;
            _importedAssetPaths.Clear();
            _preExistingAssetPaths.Clear();
        }

        private static void OnImportPackageStarted(string packageName) {
            _currentImportingPackage = packageName;
            _isImportingPackage = true;
            _importedAssetPaths.Clear();
            _preExistingAssetPaths.Clear();
            
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            foreach(string assetPath in allAssets) {
                _preExistingAssetPaths.Add(assetPath);
            }
        }

        private static void ProcessImportedPackage(string packageName) {
            SaveMaterialFiles();
            SaveFileHashesToJson(packageName);
        }
        
        private static void SaveMaterialFiles() {
            try {
                foreach (string assetPath in _importedAssetPaths) {
                    if(assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) && File.Exists(assetPath)) {
                        try {
                            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                            if (material != null) {
                                EditorUtility.SetDirty(material);
                            }
                        }catch(Exception ex) {
                            Debug.LogError($"Error occurred while saving material: {assetPath}, Error: {ex.Message}");
                        }
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            } catch(Exception ex) {
                Debug.LogError($"Error in SaveMaterialFiles: {ex.Message}");
            }
        }
        
        public static bool IsImportingPackage() {
            return _isImportingPackage;
        }
        
        public static void AddImportedAsset(string assetPath) {
            if(_isImportingPackage && !_preExistingAssetPaths.Contains(assetPath) && !_importedAssetPaths.Contains(assetPath)) {
                _importedAssetPaths.Add(assetPath);
            }
        }

        private static void SaveFileHashesToJson(string packageName) {
            List<FileHashInfo> fileHashInfos = new List<FileHashInfo>();
            
            foreach(string assetPath in _importedAssetPaths) {
                if(File.Exists(assetPath)) {
                    string hash = CalculateFileHash(assetPath);
                    fileHashInfos.Add(new FileHashInfo {
                        filePath = assetPath,
                        fileHash = hash
                    });
                }
            }
            
            PackageImportHistory history = LoadOrCreateImportHistory();
            history.files.AddRange(fileHashInfos);
            
            try {
                string json = JsonUtility.ToJson(history, true);
                
                string directory = Path.GetDirectoryName(_outputJsonPath);
                if(!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_outputJsonPath, json);
                AssetDatabase.ImportAsset(_outputJsonPath);
            } catch(Exception ex) {
                Debug.LogError($"Error saving import history to JSON: {ex.Message}");
            }
        }
        
        private static PackageImportHistory LoadOrCreateImportHistory() {
            if(File.Exists(_outputJsonPath)) {
                try {
                    string json = File.ReadAllText(_outputJsonPath);
                    PackageImportHistory history = JsonUtility.FromJson<PackageImportHistory>(json);
                    
                    if(history != null && history.files != null) {
                        return history;
                    }
                }catch(Exception ex) {
                    Debug.LogError($"Error loading import history file: {_outputJsonPath}, Error: {ex.Message}");
                    Debug.Log("Creating new import history file.");
                }
            }
            
            return new PackageImportHistory {
                files = new List<FileHashInfo>()
            };
        }
        
        private static string CalculateFileHash(string filePath) {
            try {
                using(var md5 = MD5.Create()) {
                    using(var stream = File.OpenRead(filePath)) {
                        byte[] hashBytes = md5.ComputeHash(stream);
                        StringBuilder sb = new StringBuilder();
                        
                        for(int i = 0; i < hashBytes.Length; i++) {
                            sb.Append(hashBytes[i].ToString("x2"));
                        }
                        
                        return sb.ToString();
                    }
                }
            }catch(Exception ex) {
                Debug.LogError($"Error calculating hash for file: {filePath}, Error: {ex.Message}");
                return "error_calculating_hash";
            }
        }

        public static void RemoveDeletedAssetFromJson(string assetPath) {
            if (string.IsNullOrEmpty(assetPath)) return;
            
            try {
                PackageImportHistory history = LoadOrCreateImportHistory();
                bool wasRemoved = false;
                
                history.files.RemoveAll(file => {
                    bool shouldRemove = file.filePath == assetPath;
                    if (shouldRemove) {
                        wasRemoved = true;
                        Debug.Log($"Removed deleted file from import history: {assetPath}");
                    }
                    return shouldRemove;
                });
                
                if (wasRemoved) {
                    string json = JsonUtility.ToJson(history, true);
                    
                    string directory = Path.GetDirectoryName(_outputJsonPath);
                    if(!Directory.Exists(directory)) {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllText(_outputJsonPath, json);
                    AssetDatabase.ImportAsset(_outputJsonPath);
                }
            } catch(Exception ex) {
                Debug.LogError($"Error removing deleted asset from JSON: {assetPath}, Error: {ex.Message}");
            }
        }

        public static void UpdateAssetPathInJson(string oldPath, string newPath) {
            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath)) return;
            
            try {
                PackageImportHistory history = LoadOrCreateImportHistory();
                bool wasUpdated = false;
                
                foreach (var fileInfo in history.files) {
                    if (fileInfo.filePath == oldPath) {
                        fileInfo.filePath = newPath;
                        wasUpdated = true;
                        Debug.Log($"Updated asset path: {oldPath} -> {newPath}");
                    }
                }
                
                if (wasUpdated) {
                    string json = JsonUtility.ToJson(history, true);
                    
                    string directory = Path.GetDirectoryName(_outputJsonPath);
                    if(!Directory.Exists(directory)) {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllText(_outputJsonPath, json);
                    AssetDatabase.ImportAsset(_outputJsonPath);
                }
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
                foreach(string assetPath in importedAssets) {
                    MonitorImport.AddImportedAsset(assetPath);
                }
            }
            
            if (deletedAssets != null && deletedAssets.Length > 0) {
                foreach (string deletedAssetPath in deletedAssets) {
                    MonitorImport.RemoveDeletedAssetFromJson(deletedAssetPath);
                }
            }
            
            if (movedAssets != null && movedFromAssetPaths != null && 
                movedAssets.Length > 0 && movedAssets.Length == movedFromAssetPaths.Length) {
                for (int i = 0; i < movedAssets.Length; i++) {
                    MonitorImport.UpdateAssetPathInJson(movedFromAssetPaths[i], movedAssets[i]);
                }
            }
        }
    }

}

