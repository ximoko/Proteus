using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    public static class GPUInstancerEditorConstants
    {
        public static readonly string GPUI_VERSION = "GPU Instancer v0.8";

        // Editor Text
        public static readonly string TEXT_maxDetailDistance = "Max Detail Distance";
        public static readonly string TEXT_windVector = "Wind Vector";
        public static readonly string TEXT_healthyDryNoiseTexture = "Healthy / Dry Noise Texture";
        public static readonly string TEXT_windWaveNormalTexture = "Wind Wave Normal Texture";
        public static readonly string TEXT_autoSPCellSize = "Auto SP Cell Size";
        public static readonly string TEXT_preferedSPCellSize = "Prefered SP Cell Size";
        public static readonly string TEXT_storeDetailInstanceData = "Store Detail Instance Data";
        public static readonly string TEXT_showGPUInstancingInfo = "Show GPU Instancing Info";
        public static readonly string TEXT_showRenderedAmount = "Show Rendered Amount";
        public static readonly string TEXT_simulateAtEditor = "Simulate At Scene Camera";
        public static readonly string TEXT_simulateAtEditorPrep = "Preparing Simulation";
        public static readonly string TEXT_simulateAtEditorStop = "Stop Simulation";
        public static readonly string TEXT_generatePrototypes = "Generate Prototypes";
        public static readonly string TEXT_prototypes = "Prototypes";
        public static readonly string TEXT_add = "Add\n<size=8>Click / Drop</size>";
        public static readonly string TEXT_isShadowCasting = "Is Shadow Casting";
        public static readonly string TEXT_isFrustumCulling = "Is Frustum Culling";
        public static readonly string TEXT_isOcclusionCulling = "Is Occlusion Culling";
        public static readonly string TEXT_frustumOffset = "Frustum Offset";
        public static readonly string TEXT_maxDistance = "Max Distance";
        public static readonly string TEXT_actions = "Actions";
        public static readonly string TEXT_delete = "Delete";
        public static readonly string TEXT_crossQuads = "Cross Quads";
        public static readonly string TEXT_quadCount = "Quad Count";
        public static readonly string TEXT_isBillboard = "Billboard";
        public static readonly string TEXT_billboardDistance = "CQ Billboard Distance";
        public static readonly string TEXT_billboardDistanceDebug = "CQ Distance Debug";
        public static readonly string TEXT_billboardDistanceDebugColor = "Debug Color";
        public static readonly string TEXT_detailScale = "Detail Scale";
        public static readonly string TEXT_useCustomMaterialForTextureDetail = "Use Custom Material";
        public static readonly string TEXT_textureDetailCustomMaterial = "Custom Material";
        public static readonly string TEXT_detailHealthyColor = "Detail Healthy Color";
        public static readonly string TEXT_detailDryColor = "Detail Dry Color";
        public static readonly string TEXT_noiseSpread = "Noise Spread";
        public static readonly string TEXT_ambientOcclusion = "Ambient Occlusion";
        public static readonly string TEXT_gradientPower = "Gradient Power";
        public static readonly string TEXT_windCounterSway = "Wind Counter Sway";
        public static readonly string TEXT_windIdleSway = "Wind Idle Sway";
        public static readonly string TEXT_windWavesOn = "Wind Waves";
        public static readonly string TEXT_windWaveTintColor = "Wind Wave Tint Color";
        public static readonly string TEXT_windWaveSize = "Wind Wave Size";
        public static readonly string TEXT_windWaveTint = "Wind Wave Tint";
        public static readonly string TEXT_windWaveSway = "Wind Wave Sway";
        public static readonly string TEXT_deleteConfirmation = "Delete Confirmation";
        public static readonly string TEXT_deleteAreYouSure = "Are you sure you want to delete the prototype?";
        public static readonly string TEXT_cancel = "Cancel";
        public static readonly string TEXT_paintOnTerrain = "Paint On Terrain";
        public static readonly string TEXT_enableRuntimeModifications = "Enable Runtime Modifications";
        public static readonly string TEXT_addRemoveInstancesAtRuntime = "Add/Remove Instances At Runtime";
        public static readonly string TEXT_extraBufferSize = "Extra Buffer Size";
        public static readonly string TEXT_startWithRigidBody = "Start With RigidBody";
        public static readonly string TEXT_registerPrefabsInScene = "Register Prefabs in Scene";
        public static readonly string TEXT_registeredPrefabs = "Registered Prefabs";
        public static readonly string TEXT_applyChangesToTerrain = "Apply Changes To Terrain";
        public static readonly string TEXT_generatePrototypesConfirmation = "Generate Prototypes Confirmation";
        public static readonly string TEXT_generatePrototypeAreYouSure = "Are you sure you want to generate prototypes from terrain?";
        public static readonly string TEXT_debug = "Debug";
        public static readonly string TEXT_detailGlobal = "Global Detail Values";
        public static readonly string TEXT_sceneSettings = "Scene Settings";
        public static readonly string TEXT_autoSelectCamera = "Auto Select Camera";
        public static readonly string TEXT_useCamera = "Use Camera";
        public static readonly string TEXT_terrain = "Terrain";
        public static readonly string TEXT_prefabObject = "Prefab Object";
        public static readonly string TEXT_setTerrain = "Set Terrain\n<size=8>Click / Drop</size>";
        public static readonly string TEXT_removeTerrain = "Unset Terrain";
        public static readonly string TEXT_removeTerrainConfirmation = "Unset Terrain";
        public static readonly string TEXT_removeTerrainAreYouSure = "Are you sure you want to unset Terrain Instancing Data?";
        public static readonly string TEXT_unset = "Unset";
        public static readonly string TEXT_goToGPUInstancer = "Go To GPU Instancer";
        public static readonly string TEXT_showHelpTooltip = "Show Help";
        public static readonly string TEXT_hideHelpTooltip = "Hide Help";

        // Editor HelpText
        public static readonly string HELPTEXT_camera = "The camera to use for GPU Instancing. When \"Auto Select Camera\" checkbox is active, GPU Instancer will use the first camera with the \"MainCamera\" tag at startup. If the checkbox is inactive, the desired camera can be set manually. GPU Instancer uses this camera for various calculations including culling operations.";
        public static readonly string HELPTEXT_terrain = "The \"Paint On Terrain\" button is used to navigate to the Unity terrain component that this manager is referencing. Details should be painted on the terrain using Unity's native tools as usual. Detail data on the terrain will be automatically detected by GPU Instancer.\n\nThe \"Unset Terrain\" button is used to disable GPU Instancing on the terrain. It can later be enabled again by using the \"Set Terrain\" button.";
        public static readonly string HELPTEXT_simulator = "The \"Simulate At Scene Camera\" button can be used to render the terrain details on the scene camera using the current GPU Instancer setup. This simulation is designed to provide a fast sneak peak of the GPU Instanced terrain details without having to enter play mode.";
        public static readonly string HELPTEXT_maxDetailDistance = "\"Max Detail Distance\" defines the maximum distance from the camera within which the terrain details will be rendered. Details that are farther than the specified distance will not be visible. This setting also provides the upper limit for the maximum view distance of each detail prototype.";
        public static readonly string HELPTEXT_windVector = "The \"Wind Vector\" specifies the [X, Z] vector (world axis) of the wind for all the prototypes (in this terrain) that use the \"GPUInstancer/Foliage\" shader (which is also the default shader for the texture type grass details). This vector supplies both direction and magnitude information for wind.";
        public static readonly string HELPTEXT_healthyDryNoiseTexture = "The \"Healthy / Dry Noise Texture\" can be used to specify the Healthy Color / Dry Color variation for all the prototypes that use the \"GPUInstancer/Foliage\" shader in this terrain (which is also the default shader for the texture type grass details). Texture type detail prototypes are also scaled by this noise. This image must be a greyscale noise texture.";
        public static readonly string HELPTEXT_windWaveNormalTexture = "The \"Wind Wave Normal Texture\" can be used to specify the vectors of all wind animations and coloring for all the prototypes that use the \"GPUInstancer/Foliage\" shader in this terrain (which is also the default shader for the texture type grass details). This image must be a normal map noise texture.";
        public static readonly string HELPTEXT_spatialPartitioningCellSize = "Detail Manager uses spatial partitioning for loading and unloading detail instances from GPU memory according to camera position. Detail instances are grouped in cells with a calculated size. By selecting \"Auto SP Cell Size\", you can let the manager decide which cell size to use. If you deselect \"Auto SP Cell Size\", you can determine a \"Prefered SP Cell Size\".";
        public static readonly string HELPTEXT_generatePrototypes = "The \"Generate Prototypes\" button can be used to synchronize the detail prototypes on the Unity terrain and GPU Instancer. It will reset the detail prototype properties with those from the Unity terrain, and use default values for properties that don't exist on the Unity terrain.";
        public static readonly string HELPTEXT_prototypes = "\"Prototypes\" show the list of objects that will be used in GPU Instancer. To modify a prototype, click on its image.";
        public static readonly string HELPTEXT_addprototypeprefab = "Click on \"Add\" button and select a prefab to add a prefab prototype to the manager. Note that prefab manager only accepts user created prefabs. It will not accept prefabs that are generated when importing your 3D model assets.";
        public static readonly string HELPTEXT_addprototypedetail = "Click on \"Add\" button and select a texture or prefab to add a detail prototype to the manager.";
        public static readonly string HELPTEXT_registerPrefabsInScene = "The \"Register Prefabs In Scene\" button can be used to register the prefab instances that are currently in the scene, so that they can be used by GPU Instancer. For adding new instances at runtime check API documentation.";
        public static readonly string HELPTEXT_setTerrain = "Detail Manager requires a Unity terrain to render detail instances. You can specify a Unity terrain to use with GPU Instancer by either clicking on the \"Set Terrain\" button and choosing a Unity terrain, or dragging and dropping a Unity terrain on it.";

        public static readonly string HELPTEXT_isShadowCasting = "\"Is Shadow Casting\" specifies whether the object will cast shadows or not. Shadow casting requires extra shadow passes in the shader resulting in additional rendering operations. GPU Instancer uses various techniques that boost the performance of these operations, but turning shadow casting off completely will increase performance.";
        public static readonly string HELPTEXT_isFrustumCulling = "\"Is Frustum Culling\" specifies whether the objects that are not in the selected camera's view frustum will be rendered or not. If enabled, GPU Instancer will not render the objects that are outside the selected camera's view frustum. This will increase performance. It is recommended to turn frustum culling on unless there are multiple cameras rendering the scene at the same time.";
        public static readonly string HELPTEXT_isOcclusionCulling = "<HELPTEXT_isOcclusionCulling>";
        public static readonly string HELPTEXT_frustumOffset = "\"Frustum Offset\" defines the size of the area around the camera frustum planes within which objects will be rendered while frustum culling is enabled. GPU Instancer does frustum culling on the GPU which provides a performance boost. However, if there is a performance hit (usually while rendering an extreme amount of objects in the frustum), and if the camera is moving very fast at the same time, rendering can lag behind the camera movement. This could result in some objects not being rendered around the frustum edges. This offset expands the calculated frustum area so that the renderer can keep up with the camera movement in those cases.";
        public static readonly string HELPTEXT_maxDistanceDetail = "\"Max Distance\" defines the maximum distance from the selected camera within which this prototype will be rendered. This value is limited by the general \"Max Detail Distance\" above.";
        public static readonly string HELPTEXT_maxDistance = "\"Max Distance\" defines the maximum distance from the selected camera within which this prototype will be rendered.";
        public static readonly string HELPTEXT_crossQuads = "If \"Cross Quads\" is enabled, a mesh with multiple quads will be generated for this detail texture (instead of a single quad or billboard). The generated quads will be equiangular to each other. Cross quadding means more polygons for a given prototype, so turning this off will increase performance if there will be a huge number of instances of this detail prototype.";
        public static readonly string HELPTEXT_quadCount = "\"Quad Count\" defines the number of generated equiangular quads for this detail texture. Using less quads will increase performance (especially where there will be a huge number of instances of this prototype).";
        public static readonly string HELPTEXT_isBillboard = "If \"Is Billboard\" is enabled, the generated mesh for this prototype will be billboarded. Note that billboarding will turn off automatically if cross quads are enabled.";
        public static readonly string HELPTEXT_billboardDistance = "When Cross Quads is enabled, \"Cross Quad Billboard Distance\" specifies the distance from the selected camera where the objects will be drawn as billboards to increase performance further. This is useful beacause at a certain distance, the difference between a multiple quad mesh and a billboard is barely visible. The value used here is similar to the screenRelativeTransitionHeight property of Unity LOD groups.";
        public static readonly string HELPTEXT_billboardDistanceDebug = "When Cross Quads is enabled, \"Cross Quad Billboard Distance Debug\" can be used to test the Cross Quad billboarding distance.";
        public static readonly string HELPTEXT_billboardDistanceDebugColor = "\"Billboard Distance Debug Color\" can be used to override the billboarded prototypes' colors while testing billboarding distance when using Cross Quads.";
        public static readonly string HELPTEXT_detailScale = "\"Detail Scale\" can be used to set a range for the instance sizes for detail prototypes. For the texture type detail prototypes, this range applies to the \"Healthy / Dry Noise Texture\" above. The values here correspond to the width and height values for the detail prototypes in the Unity terrain. \nX: Minimum Width - Y: Maximum Width\nZ: Minimum Height - W: Maximum Height";
        public static readonly string HELPTEXT_useCustomMaterialForTextureDetail = "\"Use Custom Material\" can be used to specify that a custom material will be used for a detail prototype instead of the default generated material that uses the \"GPUInstancer/Foliage\" shader. Note that Cross Quads are available for custom materials as well, but their ability to billboard at a distance will not be avalilable since GPU Instancer handles billboarding in the \"GPUInstancer/Foliage\" shader.";
        public static readonly string HELPTEXT_textureDetailCustomMaterial = "\"Custom Material\" can be used to specify the custom material to use for a detail prototype instead of the default \"GPUInstancer/Foliage\" shader. The shader that this material uses will be automatically set up for use with GPU Instancer. This process creates a copy of the original shader (in the same folder) that has the necessary lines of code. Note that this process will NOT modify the material.";
        public static readonly string HELPTEXT_detailHealthyColor = "\"Healthy\" color of the the Healthy / Dry noise variation for the prototypes that use the \"GPUInstancer/Foliage\" shader. This corresponds to the \"Healhy Color\" property of the detail prototypes in the Unity terrain. The Healthy / Dry noise texture can be changed globally (terrain specific) for all detail prototypes that use the \"GPUInstancer/Foliage\" shader from the \"Healthy/Dry Noise Texture\" property above.";
        public static readonly string HELPTEXT_detailDryColor = "\"Dry\" color of the the Healthy / Dry noise variation for the prototypes that use the \"GPUInstancer/Foliage\" shader. This corresponds to the \"Dry Color\" property of the detail prototypes in the Unity terrain. The Healthy / Dry noise texture can be changed globally (terrain specific) for all detail prototypes that use the \"GPUInstancer/Foliage\" shader from the \"Healthy/Dry Noise Texture\" property above.";
        public static readonly string HELPTEXT_noiseSpread = "The \"Noise Spread\" property specifies the size of the \"Healthy\" and \"Dry\" patches for the Detail Prototypes that use the \"GPUInstancer/Foliage\" shader. This corresponds to the \"Noise Spread\" propety of the detail prototypes in the Unity terrain. A higher number results in smaller patches. The Healthy / Dry noise texture can be changed globally for all detail prototypes that use the \"GPUInstancer/Foliage\" shader from the \"Healthy/Dry Noise Texture\" property above.";
        public static readonly string HELPTEXT_ambientOcclusion = "The amount of \"Ambient Occlusion\" to apply to the objects that use the \"GPUInstancer/Foliage\" shader.";
        public static readonly string HELPTEXT_gradientPower = "\"GPUInstancer/Foliage\" shader provides an option to darken the lower regions of the mesh that uses it. This results in a more realistic look for foliage. You can set the amount of this darkening effect with the \"Gradient Power\" property, or turn it off completly by setting the slider to zero.";
        public static readonly string HELPTEXT_windIdleSway = "\"Wind Idle Sway\" specifies the amount of idle wind animation for the detail prototypes that use the \"GPUInstancer/Foliage\" shader. This is the wind animation that occurs where wind waves are not present. Turning the slider down to zero disables this idle wind animation.";
        public static readonly string HELPTEXT_windWavesOn = "\"Wind Waves On\" specifies whether there will be wind waves for the detail prototypes that use the \"GPUInstancer/Foliage\" shader. The normals texture that is used to calculate winds can be changed globally (terrain specific) for all detail prototypes that use the \"GPUInstancer/Foliage\" shader from the \"Wind Wave Normal Texture\" property above.";
        public static readonly string HELPTEXT_windWaveTintColor = "\"Wind Wave Tint Color\" is a shader property that acts similar to the \"Grass Tint\" property of the Unity terrain, except it applies on a per-prototype basis. This color applies to the \"Wind Wave Tint\" properties of all the objects that use the \"GPUInstancer/Foliage\" shader (which is also the default shader for the texture type grass details).";
        public static readonly string HELPTEXT_windWaveSize = "\"Wind Wave Size\" specifies the size of the wind waves for the detail prototypes that use the \"GPUInstancer/Foliage\" shader.";
        public static readonly string HELPTEXT_windWaveTint = "\"Wind Wave Tint\" specifies how much the \"Wind Wave Tint\" color applies to the wind wave effect for the detail prototypes that use the \"GPUInstancer/Foliage\" shader. Turning the slider down to zero disables wind wave coloring.";
        public static readonly string HELPTEXT_windWaveSway = "\"Wind Wave Sway\" specifies the amount of wind animation that is applied by the wind waves for the detail prototypes that use the \"GPUInstancer/Foliage\" shader. This is the wind animation that occurs in addition to the idle wind animation. Turning the slider down to zero disables this extra wave animation.";

        public static readonly string HELPTEXT_enableRuntimeModifications = "If \"Enable Runtime Modifications\" is enabled, transform data (position, rotation, scale) for the prefab instances can be modified at runtime with API calls.";
        public static readonly string HELPTEXT_addRemoveInstancesAtRuntime = "If \"Add/Remove Instances At Runtime\" is enabled, new prefab instances can be added or existing ones can be removed at runtime with API calls";
        public static readonly string HELPTEXT_extraBufferSize = "\"Extra Buffer Size\" specifies the amount of prefab instances that can be added without reinitializing compute buffers at runtime. Instances can be added at runtime with API calls.";
        public static readonly string HELPTEXT_startWithRigidBody = "If \"Start With RigidBody\" is enabled, prefab instances that have a RigidBody component will start with an active RigidBody and it will be active until they go to Sleep state (stop moving).";

        public static readonly string HELPTEXT_applyChangesToTerrain = "The \"Apply Changes To Terrain\" button can be used to modify the Unity terrain component with the changes that are made in Detail Manager.";
        public static readonly string HELPTEXT_delete = "The \"Delete\" button deletes this prototype and removes all related data.";

        public static readonly string HELPTEXT_terrainProxyWarning = "Adding and removing detail prototypes should be done only from the GPU Instancer Detail Manager. Unity terrain component can be used to paint detail prototypes on the terrain.";

        public static readonly string ERRORTEXT_cameraNotFound = "Main Camera cannot be found. GPU Instancer needs either an existing camera with the \"Main Camera\" tag on the scene to autoselect it, or a manually specified camera. If you add your camera at runtime, please use the \"GPUInstancerAPI.SetCamera\" API function.";

        internal static class Contents
        {
            public static GUIContent removeTerrain = new GUIContent(TEXT_removeTerrain);
            public static GUIContent setTerrain = new GUIContent(TEXT_setTerrain);
            public static GUIContent generatePrototypes = new GUIContent(TEXT_generatePrototypes);
            public static GUIContent paintOnTerrain = new GUIContent(TEXT_paintOnTerrain);
            public static GUIContent applyChangesToTerrain = new GUIContent(TEXT_applyChangesToTerrain);
            public static GUIContent delete = new GUIContent(TEXT_delete);
            public static GUIContent registerPrefabsInScene = new GUIContent(TEXT_registerPrefabsInScene);
            public static GUIContent goToGPUInstancer = new GUIContent(TEXT_goToGPUInstancer);
            public static GUIContent add = new GUIContent(TEXT_add);
            public static GUIContent useCamera = new GUIContent(TEXT_useCamera);
            public static GUIContent simulateAtEditor = new GUIContent(TEXT_simulateAtEditor);
            public static GUIContent simulateAtEditorStop = new GUIContent(TEXT_simulateAtEditorStop);
            public static GUIContent simulateAtEditorPrep = new GUIContent(TEXT_simulateAtEditorPrep);
        }

        internal static class Styles
        {
            public static GUIStyle label = new GUIStyle("Label");
            public static GUIStyle boldLabel = new GUIStyle("BoldLabel");
            public static GUIStyle button = new GUIStyle("Button");
            public static GUIStyle foldout = new GUIStyle("Foldout");
            public static GUIStyle box = new GUIStyle("Box");
            public static GUIStyle helpButton = new GUIStyle("Button")
            {
                padding = new RectOffset(2, 2, 2, 2)
            };
            public static GUIStyle helpButtonSelected = new GUIStyle("Button")
            {
                padding = new RectOffset(2, 2, 2, 2),
                normal = helpButton.active
            };
        }

        internal static class Colors
        {
            public static Color lightBlue = new Color(0.5f, 0.6f, 0.8f, 1);
            public static Color darkBlue = new Color(0.07f, 0.27f, 0.35f, 1);
            public static Color lightGreen = new Color(0.2f, 1f, 0.2f, 1);
            public static Color green = new Color(0, 0.4f, 0, 1);
        }

        public static void DrawCustomLabel(string text, GUIStyle style, bool center = true)
        {
            if (center)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(text, style);

            if (center)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void DrawColoredButton(GUIContent guiContent, Color backgroundColor, Color textColor, FontStyle fontStyle, Rect buttonRect, UnityAction clickAction,
            bool isRichText = false, bool dragDropEnabled = false, UnityAction<Object> dropAction = null)
        {
            Color oldBGColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            Styles.button.normal.textColor = textColor;
            Styles.button.active.textColor = textColor;
            Styles.button.hover.textColor = textColor;
            Styles.button.fontStyle = fontStyle;
            Styles.button.richText = isRichText;

            if (buttonRect == Rect.zero)
            {
                if (GUILayout.Button(guiContent, Styles.button))
                {
                    if (clickAction != null)
                        clickAction.Invoke();
                }
            }
            else
            {
                if (GUI.Button(buttonRect, guiContent, Styles.button))
                {
                    if (clickAction != null)
                        clickAction.Invoke();
                }
            }

            GUI.backgroundColor = oldBGColor;

            if (dragDropEnabled && dropAction != null)
            {
                Event evt = Event.current;
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!buttonRect.Contains(evt.mousePosition))
                            return;

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (Object dragged_object in DragAndDrop.objectReferences)
                            {
                                dropAction(dragged_object);
                            }
                        }
                        break;
                }
            }
        }

        // Toolbar buttons
        [MenuItem("GPU Instancer/Add Prefab Manager")]
        private static void ToolbarAddPrefabManager()
        {
            GameObject go = new GameObject("GPUI Prefab Manager");
            go.AddComponent<GPUInstancerPrefabManager>();

            Selection.activeGameObject = go;
        }

        [MenuItem("GPU Instancer/Add Detail Manager For Terrains")]
        private static void ToolbarAddDetailManager()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            GameObject go = null;
            if (terrains != null && terrains.Length > 0)
            {
                foreach (Terrain terrain in terrains)
                {
                    if (terrain.GetComponent<GPUInstancerTerrainProxy>() == null || terrain.GetComponent<GPUInstancerTerrainProxy>().detailManager == null)
                    {
                        go = new GameObject("GPUI Detail Manager (" + terrain.terrainData.name + ")");
                        GPUInstancerDetailManager detailManager = go.AddComponent<GPUInstancerDetailManager>();
                        detailManager.SetupManagerWithTerrain(terrain);
                    }
                    else
                    {
                        go = terrain.GetComponent<GPUInstancerTerrainProxy>().detailManager.gameObject;
                    }
                }

                Selection.activeGameObject = go;
            }
            else
            {
                EditorUtility.DisplayDialog("GPU Instancer", "Detail Manager requires a terrain added to the scene.", "OK");
            }
        }

        [MenuItem("GPU Instancer/Add Detail Manager For Terrains", validate = true)]
        private static bool ValidateToolbarAddDetailManager()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            return terrains != null && terrains.Length > 0;
        }
    }
}