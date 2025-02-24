using UnityEditor;
using UnityEngine;
using VRC.Core;

public class SaveAsModifiedAvatar : EditorWindow {
    private Camera previewCamera;
    private GameObject cameraObject;

    [MenuItem("GameObject/Save as Modified Avatar", priority = -1000000)]
    private static void ShowWindow() {
        var window = GetWindow<SaveAsModifiedAvatar>("Save as Modified Avatar");
        window.Show();
    }

    [MenuItem("GameObject/Save as Modified Avatar", true)]
    private static bool ValidateShowWindow() {
        return Selection.activeGameObject != null
        && Selection.activeGameObject.GetComponent<PipelineManager>() != null;
    }

    private void OnEnable() {
        cameraObject = new GameObject("Preview Camera");
        previewCamera = cameraObject.AddComponent<Camera>();
        
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
        
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition() {
        if(Selection.activeGameObject == null) return;

        //Calculate diagonal length of bounding box from all avatar renderers
        var renderers = Selection.activeGameObject.GetComponentsInChildren<Renderer>();
        if(renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for(int i = 1; i < renderers.Length; i++) {
            bounds.Encapsulate(renderers[i].bounds);
        }
        float diagonal = bounds.size.magnitude;
        
        //Calculate distance needed to show full body
        float fov = 60f;
        previewCamera.fieldOfView = fov;
        float distance = (diagonal * 0.25f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        
        //Set camera position (offset 5% downward)
        previewCamera.transform.position = bounds.center + Vector3.forward * distance + Vector3.down * (bounds.size.y * 0.05f);
        
        //Set camera rotation
        Vector3 lookAtPoint = new Vector3(bounds.center.x, previewCamera.transform.position.y, bounds.center.z);
        previewCamera.transform.LookAt(lookAtPoint);
    }

    private void OnDisable() {
        if(cameraObject != null) {
            DestroyImmediate(cameraObject);
        }
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Save as Modified Avatar");
    }
}
