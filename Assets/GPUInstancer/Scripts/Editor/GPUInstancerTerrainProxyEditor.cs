using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerTerrainProxy))]
    public class GPUInstancerTerrainProxyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GPUInstancerTerrainProxy terrainProxy = (GPUInstancerTerrainProxy)target;

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.goToGPUInstancer, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero, 
                () =>
                {
                    if (terrainProxy.detailManager != null && terrainProxy.detailManager.gameObject != null)
                        Selection.activeGameObject = terrainProxy.detailManager.gameObject;
                });
            EditorGUILayout.HelpBox(GPUInstancerEditorConstants.HELPTEXT_terrainProxyWarning, MessageType.Warning);
        }
    }
}