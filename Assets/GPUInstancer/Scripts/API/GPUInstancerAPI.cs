using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{

    public static class GPUInstancerAPI
    {

        /// <summary>
        ///     <para>Main GPU Instancer initialization Method. Generates the necessary GPUInstancer runtime data from predifined 
        ///     GPU Instancer prototypes that are registered in the manager, and generates all necessary GPU buffers for instancing.</para>
        ///     <para>Use this as the final step after you setup a GPU Instancer manager and all its prototypes.</para>
        ///     <para>Note that you can also use this to re-initialize the GPU Instancer prototypes that are registered in the manager at runtime.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        public static void InitializeGPUInstancer(GPUInstancerManager manager)
        {
            manager.InitializeRuntimeDataAndBuffers();
        }

        /// <summary>
        ///     <para>Sets the active camera for a specific manager. This camera is used by GPU Instancer for various calculations (including culling operations). </para>
        ///     <para>Use this right after you add or change your camera at runtime. </para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="camera">The camera that GPU Instancer will use.</param>
        public static void SetCamera(GPUInstancerManager manager, Camera camera)
        {
            manager.SetCamera(camera);
        }

        /// <summary>
        ///     <para>Sets the active camera for all managers. This camera is used by GPU Instancer for various calculations (including culling operations). </para>
        ///     <para>Use this right after you add or change your camera at runtime. </para>
        /// </summary>
        /// <param name="camera">The camera that GPU Instancer will use.</param>
        public static void SetCamera(Camera camera)
        {
            if (GPUInstancerManager.activeManagerList != null)
                GPUInstancerManager.activeManagerList.ForEach(m => m.SetCamera(camera));
        }

        /// <summary>
        ///     <para>Returns a list of active managers. Use this if you want to access the managers at runtime.</para>
        /// </summary>
        /// <returns>The List of active managers. Null if no active managers present.</returns>
        public static List<GPUInstancerManager> GetActiveManagers()
        {
            return GPUInstancerManager.activeManagerList == null ? null : GPUInstancerManager.activeManagerList.ToList();
        }

        #region Prefab Instancing

        /// <summary>
        ///     <para>Registers a list of prefab instances with GPU Instancer. You must use <see cref="InitializeGPUInstancer"/> after registering these prefabs for final initialization.</para>
        ///     <para>The prefabs of the instances in this list must be previously defined in the given manager (either at runtime or editor time).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstanceList">The list of prefabs instances to GPU instance.</param>
        public static void RegisterPrefabInstanceList(GPUInstancerPrefabManager manager, List<GPUInstancerPrefab> prefabInstanceList)
        {
            manager.RegisterPrefabInstanceList(prefabInstanceList);
        }

        /// <summary>
        ///     <para>Adds a new prefab instance for GPU instancing to an already initialized list of registered instances. </para>
        ///     <para>Use this if you want to add another instance of a prefab after you have initialized a list of prefabs with <see cref="InitializeGPUInstancer"/>.</para>
        ///     <para>The prefab of this instance must be previously defined in the given manager (either at runtime or editor time).</para>
        ///     <para>Note that the prefab must be enabled for adding and removal in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that the number of total instances is limited by the count of already initialized instances plus the extra amount you define in the manager.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to add.</param>
        public static void AddPrefabInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.AddPrefabInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Removes a prefab instance from an already initialized list of registered instances. </para>
        ///     <para>Use this if you want to remove a prefab instance after you have initialized a list of prefabs with <see cref="InitializeGPUInstancer"/> 
        ///     (usually before destroying the GameObject).</para>
        ///     <para>The prefab of this instance must be previously defined in the given manager (either at runtime or editor time).</para>
        ///     <para>Note that the prefab must be enabled for adding and removal in the manager in order for this to work (for performance reasons).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to remove.</param>
        public static void RemovePrefabInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.RemovePrefabInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Disables GPU instancing and enables Unity renderers for the given prefab instance without removing it from the list of registerd prefabs.</para>
        ///     <para>Use this if you want to pause GPU Instancing for a prefab (e.g. to enable physics).</para>
        ///     <para>Note that the prefab must be enabled for runtime modifications in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that you can also add <seealso cref="GPUInstancerModificationCollider"/> to a game object to use its collider to automatically 
        ///     enable/disable instancing when a prefab instance enters/exits its collider.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to disable the GPU Instancing of.</param>
        public static void DisableIntancingForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.DisableIntancingForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Enables GPU instancing and disables Unity renderers for the given prefab instance without re-adding it to the list of registerd prefabs.</para>
        ///     <para>Use this if you want to unpause GPU Instancing for a prefab.</para>
        ///     <para>Note that the prefab must be enabled for runtime modifications in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that you can also add <seealso cref="GPUInstancerModificationCollider"/> to a game object to use its collider to automatically 
        ///     enable/disable instancing when a prefab instance enters/exits its collider.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to enable the GPU Instancing of.</param>
        public static void EnableInstancingForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.EnableInstancingForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Updates and synchronizes the GPU Instancer transform data (position, rotation and scale) for the given prefab instance.</para>
        ///     <para>Use this if you want to update, rotate, and/or scale prefab instances after initialization.</para>
        ///     <para>The updated values are taken directly from the transformation operations made beforehand on the instance's Unity transform component. 
        ///     (These operations will not reflect on the GPU Instanced prefab automatically unless you use this method).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to update the transform values of. The instance's Unity transform component must be updated beforehand.</param>
        public static void UpdateTransformDataForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.UpdateTransformDataForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Specifies a variation buffer for a GPU Instancer prototype that is defined in the prefab's shader. Required to use <see cref="AddVariation{T}"/></para>
        ///     <prara>Use this if you want any type of variation between this prototype's instances.</prara>
        ///     <para>To define the buffer necessary for this variation in your shader, you need to create a StructuredBuffer field of the relevant type in that shader. 
        ///     You can then access this buffer with "gpuiTransformationMatrix[unity_InstanceID]"</para>
        ///     <para>see <seealso cref="ColorVariations"/> and its demo scene for an example</para>
        /// </summary>
        /// 
        /// <example> 
        ///     This sample shows how to use the variation buffer in your shader:
        /// 
        ///     <code><![CDATA[
        ///     ...
        ///     fixed4 _Color;
        /// 
        ///     #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        ///         StructuredBuffer<float4> colorBuffer;
        ///     #endif
        ///     ...
        ///     void surf (Input IN, inout SurfaceOutputStandard o) {
        ///     ...
        ///         #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        ///             uint index = gpuiTransformationMatrix[unity_InstanceID];
        ///             col = colorBuffer[index];
        ///         #else
        ///             col = _Color;
        ///         #endif
        ///     ...
        ///     }
        ///     ]]></code>
        /// 
        ///     See "GPUInstancer/ColorVariationShader" for the full example.
        /// 
        /// </example>
        /// 
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader</typeparam>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prototype">The GPU Instancer prototype to define variations.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        public static void DefinePrototypeVariationBuffer<T>(GPUInstancerPrefabManager manager, GPUInstancerPrefabPrototype prototype, string bufferName)
        {
            manager.DefinePrototypeVariationBuffer<T>(prototype, bufferName);
        }

        /// <summary>
        ///     <para>Sets the variation value for this prefab instance. The variation buffer for the prototype must be defined 
        ///     with <see cref="DefinePrototypeVariationBuffer{T}"/> before using this.</para>
        /// </summary>
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader.</typeparam>
        /// <param name="prefabInstance">The prefab instance to add the variation to.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        /// <param name="value">The value of the variation.</param>
        public static void AddVariation<T>(GPUInstancerPrefab prefabInstance, string bufferName, T value)
        {
            prefabInstance.AddVariation(bufferName, value);
        }



        #endregion Prefab Instancing

        #region Detail Instancing

        /// <summary>
        ///     <para>Sets the Unity terrain to the GPU Instancer manager and generates the instance prototypes from Unity detail 
        ///     prototypes that are defined on the given Unity terrain component.</para>
        ///     <para>Use this to initialize the GPU Instancer detail manager if you want to generate your terrain at runtime. 
        ///     See <seealso cref="TerrainGenerator"/> and its demo scene for an example.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="terrain"></param>
        public static void SetupManagerWithTerrain(GPUInstancerTerrainManager manager, Terrain terrain)
        {
            manager.SetupManagerWithTerrain(terrain);
        }

        /// <summary>
        ///     <para>Updates and synchronizes the GPU Instancer detail prototypes with the modifications made in the manager at runtime.</para>
        ///     <para>Use this if you want to make changes to the detail prototypes at runtime. Prototypes in the manager must be modified before using this.</para>
        ///     <para>For example usages, see: <see cref="DetailDemoSceneController"/> and <seealso cref="TerrainGenerator"/></para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="updateMeshes">Whether GPU Instancer should also update meshes. Send this value as "true" if you change properties 
        /// related to cross quadding, noise spread and/or detail scales</param>
        public static void UpdateDetailInstances(GPUInstancerDetailManager manager, bool updateMeshes = false)
        {
            GPUInstancerUtility.ForceDetailBufferUpdate(manager);
            GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(manager.runtimeDataList, manager.terrainSettings, updateMeshes);
        }

        /// <summary>
        ///     <para>Starts listening the detail initialization process and runs the given callback function when initialization finishes.</para>
        ///     <para>GPU Instancer does not lock Unity updates when initializing detail prototype instances and instead, does this in a background process. 
        ///     Each prototype will show on the terrain upon its own initialization. Use this method to get notified when all prototypes are initialized.</para>
        ///     <para>The most common usage for this is to show a loading bar. For an example, see: <seealso cref="DetailDemoSceneController"/></para>
        /// </summary>
        /// <param name="callback">The callback function to run upon initialization completion. Can be any function that doesn't take any parameters.</param>
        public static void StartListeningDetailInitialization(UnityAction callback)
        {
            GPUInstancerUtility.StartListening(GPUInstancerEventType.DetailInitializationFinished, callback);
        }

        /// <summary>
        ///     <para>Stops listening the detail initialization process and unregisters the given callback function that was registered with <see cref="StartListeningDetailInitialization"/>.</para>
        ///     <para>Use this in your callback function to unregister it (e.g. after hiding the loading bar).</para>
        ///     <para>For an example, see: <seealso cref="DetailDemoSceneController"/></para>
        /// </summary>
        /// <param name="callback">The callback function that was registered with <see cref="StartListeningDetailInitialization"/></param>
        public static void StopListeningDetailInitialization(UnityAction callback)
        {
            GPUInstancerUtility.StopListening(GPUInstancerEventType.DetailInitializationFinished, callback);
        }

        #endregion
    }
}
