using UnityEngine;
using UnityEditor;

/*
 * VLight
 * Copyright Brian Su 2011-2014
*/
public class VolumeLightCreator : EditorWindow
{

    [MenuItem("GameObject/Create Other/V-Lights Spot", false, 100)]
    public static void StandardLight()
    {
        if (ShowWarning())
        {
            Undo.RegisterSceneUndo("V-Lights Create Light");
            GameObject volumeLightContainer = CreateVolumeLight(VLight.LightTypes.Spot);
            Selection.activeGameObject = volumeLightContainer;
        }
    }

    [MenuItem("GameObject/Create Other/V-Lights Spot With Light", false, 100)]
    public static void SpotWithLight()
    {
        if (ShowWarning())
        {
            Undo.RegisterSceneUndo("V-Lights Create Light");
            GameObject volumeLightContainer = CreateVolumeLight(VLight.LightTypes.Spot);
            GameObject pointLight = new GameObject("Spot light");
            Light light = pointLight.AddComponent<Light>();
            light.shadows = LightShadows.Soft;
            light.type = LightType.Spot;
            light.spotAngle = 45;
            light.range = 6;
            pointLight.transform.parent = volumeLightContainer.transform;
            pointLight.transform.localPosition = Vector3.zero;
            pointLight.transform.Rotate(90, 0, 0);
            Selection.activeGameObject = volumeLightContainer;
        }
    }

    [MenuItem("GameObject/Create Other/V-Lights Point", false, 100)]
    public static void PointLight()
    {
        if (ShowWarning())
        {
            Undo.RegisterSceneUndo("V-Lights Create Light");
            GameObject volumeLightContainer = CreateVolumeLight(VLight.LightTypes.Point);
            Selection.activeGameObject = volumeLightContainer;
        }
    }

    [MenuItem("GameObject/Create Other/V-Lights Point With Light", false, 100)]
    public static void PointWithLight()
    {
        if (ShowWarning())
        {
            Undo.RegisterSceneUndo("V-Lights Create Light");
            GameObject volumeLightContainer = CreateVolumeLight(VLight.LightTypes.Point);
            GameObject pointLight = new GameObject("Point light");
            Light light = pointLight.AddComponent<Light>();
            light.shadows = LightShadows.Soft;
            light.type = LightType.Point;
            light.spotAngle = 45;
            light.range = 6;
            pointLight.transform.parent = volumeLightContainer.transform;
            pointLight.transform.localPosition = Vector3.zero;
            Selection.activeGameObject = volumeLightContainer;
        }
    }

    private static GameObject CreateVolumeLight(VLight.LightTypes type)
    {
        VLight[] otherLights = GameObject.FindObjectsOfType(typeof(VLight)) as VLight[];
        GameObject volumeLightContainer = new GameObject("V-Light " + otherLights.Length);
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.MoveToView(volumeLightContainer.transform);
        }
        VLight light = volumeLightContainer.AddComponent<VLight>();

        volumeLightContainer.GetComponent<Camera>().enabled = false;
        volumeLightContainer.GetComponent<Camera>().fieldOfView = 45;
        volumeLightContainer.GetComponent<Camera>().nearClipPlane = 0.1f;
        volumeLightContainer.GetComponent<Camera>().farClipPlane = 1;
        volumeLightContainer.GetComponent<Camera>().renderingPath = RenderingPath.VertexLit;
        volumeLightContainer.GetComponent<Camera>().orthographicSize = 2.5f;

        switch (type)
        {
            case VLight.LightTypes.Spot:
                light.lightType = VLight.LightTypes.Spot;
                break;
            case VLight.LightTypes.Point:
                volumeLightContainer.GetComponent<Camera>().orthographic = true;
                volumeLightContainer.GetComponent<Camera>().nearClipPlane = -volumeLightContainer.GetComponent<Camera>().farClipPlane;
                volumeLightContainer.GetComponent<Camera>().orthographicSize = volumeLightContainer.GetComponent<Camera>().farClipPlane * 2;
                light.lightType = VLight.LightTypes.Point;
                break;
        }

        int layer = LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME);
        if (layer != -1)
        {
            volumeLightContainer.layer = layer;
            volumeLightContainer.GetComponent<Camera>().cullingMask = ~(1 << layer);
        }

        volumeLightContainer.transform.Rotate(90, 0, 0);
        return volumeLightContainer;
    }

    private static GameObject CreateVolumeFog()
    {
        VLightFog[] otherFogObjects = GameObject.FindObjectsOfType(typeof(VLightFog)) as VLightFog[];
        GameObject volumeLightContainer = new GameObject("V-Light Fog " + otherFogObjects.Length);
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.MoveToView(volumeLightContainer.transform);
        }
        VLightFog fog = volumeLightContainer.AddComponent<VLightFog>();
        fog.fogRadius = 10;

        volumeLightContainer.GetComponent<Camera>().enabled = false;
        volumeLightContainer.GetComponent<Camera>().fieldOfView = 45;
        volumeLightContainer.GetComponent<Camera>().nearClipPlane = 0.1f;
        volumeLightContainer.GetComponent<Camera>().farClipPlane = 1;
        volumeLightContainer.GetComponent<Camera>().renderingPath = RenderingPath.VertexLit;
        volumeLightContainer.GetComponent<Camera>().orthographicSize = 2.5f;
        volumeLightContainer.GetComponent<Camera>().orthographic = true;
        volumeLightContainer.GetComponent<Camera>().nearClipPlane = -volumeLightContainer.GetComponent<Camera>().farClipPlane;
        volumeLightContainer.GetComponent<Camera>().orthographicSize = volumeLightContainer.GetComponent<Camera>().farClipPlane * 2;

        int layer = LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME);
        if (layer != -1)
        {
            volumeLightContainer.layer = layer;
            volumeLightContainer.GetComponent<Camera>().cullingMask = ~(1 << layer);
        }

        volumeLightContainer.transform.Rotate(90, 0, 0);
        return volumeLightContainer;
    }

    private static bool ShowWarning()
    {
        bool continueAfterWarning = true;
        if (LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME) == -1)
        {
            continueAfterWarning = EditorUtility.DisplayDialog("Warning",
                "You don't have a layer in your project called\n\"" + VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME + "\".\n" +
                "Without this layer realtime shadows, interleaved sampling and high speed off screen rendering will not work. Continue using volumetric lights?", "Continue", "Cancel");
        }
        return continueAfterWarning;
    }
}