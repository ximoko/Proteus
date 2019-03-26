using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{

    public static class GPUInstancerUtility
    {
        #region GPU Instancing

        /// <summary>
        /// Initializes GPU buffer related data for the instance prototypes. Instance transformation matrices must be generated before this.
        /// </summary>
        public static void InitializeGPUBuffers<T>(List<T> runtimeDataList) where T : GPUInstancerRuntimeData
        {
            if (runtimeDataList == null || runtimeDataList.Count == 0)
                return;

            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                InitializeGPUBuffer(runtimeDataList[i]);
            }
        }

        public static void InitializeGPUBuffer<T>(T runtimeData) where T : GPUInstancerRuntimeData
        {
            if (runtimeData == null || runtimeData.bufferSize == 0)
                return;

            if (runtimeData.instanceLODs == null || runtimeData.instanceLODs.Count == 0)
            {
                Debug.LogError("instance prototype with an empty LOD list detected. There must be at least one LOD defined per instance prototype.");
                return;
            }
            runtimeData.modified = true;

            #region Set Visibility Buffer
            // Setup the visibility compute buffer
            if (runtimeData.transformationMatrixVisibilityBuffer == null)
            {
                runtimeData.transformationMatrixVisibilityBuffer = new ComputeBuffer(runtimeData.bufferSize, GPUInstancerConstants.STRIDE_SIZE_MATRIX4X4);
                if (runtimeData.instanceDataArray != null)
                    runtimeData.transformationMatrixVisibilityBuffer.SetData(runtimeData.instanceDataArray);
                // clear instance data after buffer initialization
                //runtimeData.instanceData = null;
            }
            #endregion Set Visibility Buffer

            #region Set Args Buffer
            if (runtimeData.argsBuffer == null)
            {
                // Initialize indirect renderer buffer
                //if (runtimeData.argsBuffer != null)
                //    runtimeData.argsBuffer.Release();

                int totalSubMeshCount = 0;
                for (int i = 0; i < runtimeData.instanceLODs.Count; i++)
                {
                    for (int j = 0; j < runtimeData.instanceLODs[i].renderers.Count; j++)
                    {
                        totalSubMeshCount += runtimeData.instanceLODs[i].renderers[j].mesh.subMeshCount;
                    }
                }

                // Initialize indirect renderer buffer. First LOD's each renderer's all submeshes will be followed by second LOD's each renderer's submeshes and so on.
                runtimeData.args = new uint[5 * totalSubMeshCount];
                int argsLastIndex = 0;

                // Setup LOD Data:
                for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
                {
                    // setup LOD renderers:
                    for (int r = 0; r < runtimeData.instanceLODs[lod].renderers.Count; r++)
                    {
                        // Setup the indirect renderer buffer:
                        for (int j = 0; j < runtimeData.instanceLODs[lod].renderers[r].mesh.subMeshCount; j++)
                        {
                            runtimeData.args[argsLastIndex + j * 5] = runtimeData.instanceLODs[lod].renderers[r].mesh.GetIndexCount(j); // index count per instance
                            runtimeData.args[argsLastIndex + j * 5 + 1] = (uint)runtimeData.bufferSize;
                            runtimeData.args[argsLastIndex + j * 5 + 2] = runtimeData.instanceLODs[lod].renderers[r].mesh.GetIndexStart(j); // start index location
                            runtimeData.args[argsLastIndex + j * 5 + 3] = 0; // base vertex location
                            runtimeData.args[argsLastIndex + j * 5 + 4] = 0; // start instance location
                        }

                        argsLastIndex += runtimeData.instanceLODs[lod].renderers[r].materials.Count * 5;

                        // Cache LOD renderer args offsets in the LOD objects for better performance
                        runtimeData.instanceLODs[lod].renderers[r].argsBufferOffset = GetArgsOffsetForLODRenderer(runtimeData, lod, r);
                    }
                }

                runtimeData.argsBuffer = new ComputeBuffer(1, runtimeData.args.Length * sizeof(uint),
                    ComputeBufferType.IndirectArguments);

                runtimeData.argsBuffer.SetData(runtimeData.args);

                if (runtimeData.shadowCasterMaterial != null)
                {
                    if (runtimeData.shadowArgsBuffer != null)
                        runtimeData.shadowArgsBuffer.Release();

                    runtimeData.shadowArgsBuffer = new ComputeBuffer(1, runtimeData.args.Length * sizeof(uint),
                        ComputeBufferType.IndirectArguments);
                    runtimeData.shadowArgsBuffer.SetData(runtimeData.args);
                }
            }
            #endregion Set Args Buffer

            #region Set Append Buffers
            for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
            {
                if (runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer == null || runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer.count != runtimeData.bufferSize)
                {
                    // Create the LOD append buffers. Each LOD has its own append buffer.
                    if (runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer != null)
                        runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer.Release();

                    runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer =
                        new ComputeBuffer(runtimeData.bufferSize, GPUInstancerConstants.STRIDE_SIZE_INT,
                            ComputeBufferType.Append);
                }

                for (int r = 0; r < runtimeData.instanceLODs[lod].renderers.Count; r++)
                {
                    // Setup instance LOD renderer material property block shader buffers with the append buffer
                    runtimeData.instanceLODs[lod].renderers[r].mpb.SetBuffer(
                        GPUInstancerConstants.TRANSFORMATION_MATRIX_APPEND_BUFFER,
                        runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer);
                    runtimeData.instanceLODs[lod].renderers[r].mpb.SetBuffer(
                        GPUInstancerConstants.INSTANCE_DATA_BUFFER,
                        runtimeData.transformationMatrixVisibilityBuffer);
                    runtimeData.instanceLODs[lod].renderers[r].mpb.SetMatrix(
                        GPUInstancerConstants.RENDERER_TRANSFORM_OFFSET,
                        runtimeData.instanceLODs[lod].renderers[r].transformOffset);
                }
            }

            if (runtimeData.shadowCasterMaterial != null)
            {
                if (runtimeData.shadowAppendBuffer != null)
                    runtimeData.shadowAppendBuffer.Release();

                runtimeData.shadowAppendBuffer = new ComputeBuffer(runtimeData.bufferSize, GPUInstancerConstants.STRIDE_SIZE_INT,
                            ComputeBufferType.Append);

                runtimeData.shadowCasterMPB.SetBuffer(
                    GPUInstancerConstants.TRANSFORMATION_MATRIX_APPEND_BUFFER,
                    runtimeData.shadowAppendBuffer);
                runtimeData.shadowCasterMPB.SetBuffer(
                    GPUInstancerConstants.INSTANCE_DATA_BUFFER,
                    runtimeData.transformationMatrixVisibilityBuffer);
            }
            #endregion Set Append Buffers
        }

        /// <summary>
        /// Indirectly renders matrices for all prototypes. 
        /// Transform matrices are sent to a compute shader which does culling operations and appends them to the GPU (Unlimited buffer size).
        /// All GPU buffers must be already initialized.
        /// </summary>
        public static void UpdateGPUBuffers<T>(ComputeShader visibilityComputeShader, int[] instanceVisibilityComputeKernelIDs, List<T> runtimeDataList,
            GPUInstancerCameraData cameraData, Bounds instancingBounds, bool showRenderedAmount = false) where T : GPUInstancerRuntimeData
        {
            if (runtimeDataList == null)
                return;

            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                UpdateGPUBuffer(visibilityComputeShader, instanceVisibilityComputeKernelIDs, runtimeDataList[i], cameraData, instancingBounds, showRenderedAmount);
            }
        }



        /// <summary>
        /// Indirectly renders matrices for all prototypes. 
        /// Transform matrices are sent to a compute shader which does culling operations and appends them to the GPU (Unlimited buffer size).
        /// All GPU buffers must be already initialized.
        /// </summary>
        public static void UpdateGPUBuffer<T>(ComputeShader visibilityComputeShader, int[] instanceVisibilityComputeKernelIDs, T runtimeData,
            GPUInstancerCameraData cameraData, Bounds instancingBounds, bool showRenderedAmount = false) where T : GPUInstancerRuntimeData
        {
            if (runtimeData == null || runtimeData.args == null || runtimeData.transformationMatrixVisibilityBuffer == null || runtimeData.transformationMatrixVisibilityBuffer.count == 0)
                return;

            if (runtimeData.modified || cameraData.cameraChanged)
            {
                int instanceVisibilityComputeKernelId = instanceVisibilityComputeKernelIDs[runtimeData.instanceLODs.Count - 1 + (runtimeData.shadowCasterMaterial != null ? 5 : 0)];

                for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
                {
                    visibilityComputeShader.SetBuffer(instanceVisibilityComputeKernelId,
                        GPUInstancerConstants.TRANSFORMATION_MATRIX_APPEND_LOD_BUFFERS[lod],
                        runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer);
                    runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer.SetCounterValue(0);
                }
                if (runtimeData.shadowAppendBuffer != null)
                    runtimeData.shadowAppendBuffer.SetCounterValue(0);

                // Setup the compute shader
                visibilityComputeShader.SetBuffer(instanceVisibilityComputeKernelId,
                    GPUInstancerConstants.INSTANCE_DATA_BUFFER, runtimeData.transformationMatrixVisibilityBuffer);

#if UNITY_2017_3_OR_NEWER
                visibilityComputeShader.SetMatrix(GPUInstancerConstants.BUFFER_PARAMETER_MVP_MATRIX,
                    cameraData.mainCamera.projectionMatrix * cameraData.mainCamera.worldToCameraMatrix);
#else
                visibilityComputeShader.SetFloats(GPUInstancerConstants.BUFFER_PARAMETER_MVP_MATRIX, cameraData.mvpMatrixFloats);
#endif

                visibilityComputeShader.SetVector(GPUInstancerConstants.BUFFER_PARAMETER_BOUNDS_CENTER,
                    runtimeData.instanceBounds.center);
                visibilityComputeShader.SetVector(GPUInstancerConstants.BUFFER_PARAMETER_BOUNDS_EXTENTS,
                    runtimeData.instanceBounds.extents);
                visibilityComputeShader.SetBool(GPUInstancerConstants.BUFFER_PARAMETER_FRUSTUM_CULL_SWITCH,
                    Application.isPlaying ? runtimeData.prototype.isFrustumCulling : false);
                visibilityComputeShader.SetFloat(GPUInstancerConstants.BUFFER_PARAMETER_MAX_VIEW_DISTANCE,
                    runtimeData.prototype.maxDistance);
                visibilityComputeShader.SetVector(GPUInstancerConstants.BUFFER_PARAMETER_CAMERA_POSITION,
                    cameraData.mainCamera.transform.position);
                visibilityComputeShader.SetFloat(GPUInstancerConstants.BUFFER_PARAMETER_FRUSTUM_OFFSET,
                    runtimeData.prototype.frustumOffset);
                visibilityComputeShader.SetVector(GPUInstancerConstants.BUFFER_PARAMETER_LOD_SIZES,
                    runtimeData.lodSizes / QualitySettings.lodBias);
                visibilityComputeShader.SetFloat(GPUInstancerConstants.BUFFER_PARAMETER_FRUSTUM_HEIGHT,
                    cameraData.frustumHeight);
                if (runtimeData.shadowCasterMaterial != null)
                {
                    visibilityComputeShader.SetBuffer(instanceVisibilityComputeKernelId,
                        GPUInstancerConstants.TRANSFORMATION_MATRIX_APPEND_SHADOW_BUFFER, runtimeData.shadowAppendBuffer);
                    visibilityComputeShader.SetFloat(GPUInstancerConstants.BUFFER_PARAMETER_SHADOW_DISTANCE, QualitySettings.shadowDistance);
                }

                runtimeData.modified = false;

                // Dispatch the compute shader
                visibilityComputeShader.Dispatch(instanceVisibilityComputeKernelId,
                    Mathf.CeilToInt(runtimeData.transformationMatrixVisibilityBuffer.count / (float)GPUInstancerConstants.VISIBILITY_SHADER_THREAD_COUNT), 1, 1);

                // Copy (overwrite) the modified instance count of the append buffer to each index of the indirect renderer buffer (argsBuffer)
                // that represents a submesh's instance count. The offset is calculated in parallel to the Graphics.DrawMeshInstancedIndirect call,
                // which expects args[1] to be the instance count for the first LOD's first renderer. Every 4 index offset of args represents the 
                // next submesh in the renderer, followed by the next renderer and it's submeshes. After all submeshes of all renderers for the 
                // first LOD, the other LODs follow in the same manner.
                // For reference, see: https://docs.unity3d.com/ScriptReference/ComputeBuffer.CopyCount.html

                for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
                {
                    for (int r = 0; r < runtimeData.instanceLODs[lod].renderers.Count; r++)
                    {
                        for (int j = 0; j < runtimeData.instanceLODs[lod].renderers[r].mesh.subMeshCount; j++)
                        {
                            ComputeBuffer.CopyCount(runtimeData.instanceLODs[lod].transformationMatrixAppendBuffer,
                                runtimeData.argsBuffer,
                                // LOD renderer start location + LOD renderer material start location + 1 :
                                (runtimeData.instanceLODs[lod].renderers[r].argsBufferOffset * sizeof(uint)) + (j * sizeof(uint) * 5) + sizeof(uint));

                            if (runtimeData.shadowAppendBuffer != null)
                            {
                                ComputeBuffer.CopyCount(runtimeData.shadowAppendBuffer,
                                runtimeData.shadowArgsBuffer,
                                // LOD renderer start location + LOD renderer material start location + 1 :
                                (runtimeData.instanceLODs[lod].renderers[r].argsBufferOffset * sizeof(uint)) + (j * sizeof(uint) * 5) + sizeof(uint));
                            }
                        }
                    }
                }

                // WARNING: this will read back the instance matrices buffer after the compute shader operates on it. This will impact FPS greatly. Use only for debug.
                if (showRenderedAmount)
                    runtimeData.argsBuffer.GetData(runtimeData.args);
            }

            // Everything is ready; execute the instanced indirect rendering. We execute a drawcall for each submesh of each LOD.
            for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
            {
                for (int r = 0; r < runtimeData.instanceLODs[lod].renderers.Count; r++)
                {
                    for (int m = 0; m < runtimeData.instanceLODs[lod].renderers[r].materials.Count; m++)
                    {
                        Graphics.DrawMeshInstancedIndirect(runtimeData.instanceLODs[lod].renderers[r].mesh, m,
                            runtimeData.instanceLODs[lod].renderers[r].materials[m],
                            instancingBounds,
                            //runtimeData.argsBuffer, GetArgsOffsetForLOD(runtimeData, LOD) + sizeof(uint) * 5 * j,
                            runtimeData.argsBuffer,
                            (runtimeData.instanceLODs[lod].renderers[r].argsBufferOffset * sizeof(int)) +
                            sizeof(uint) * 5 * m,
                            runtimeData.instanceLODs[lod].renderers[r].mpb,
                            runtimeData.prototype.isShadowCasting ? ShadowCastingMode.On : ShadowCastingMode.Off, true);

                        if (lod == 0 && runtimeData.shadowCasterMaterial != null)
                        {
                            runtimeData.shadowCasterMPB.SetMatrix(
                                GPUInstancerConstants.RENDERER_TRANSFORM_OFFSET,
                                runtimeData.instanceLODs[lod].renderers[r].transformOffset);

                            Graphics.DrawMeshInstancedIndirect(runtimeData.instanceLODs[lod].renderers[r].mesh, m,
                            runtimeData.shadowCasterMaterial,
                            instancingBounds,
                            //runtimeData.argsBuffer, GetArgsOffsetForLOD(runtimeData, lod) + sizeof(uint) * 5 * j,
                            runtimeData.shadowArgsBuffer,
                            (runtimeData.instanceLODs[lod].renderers[r].argsBufferOffset * sizeof(int)) +
                            sizeof(uint) * 5 * m,
                            runtimeData.shadowCasterMPB,
                            ShadowCastingMode.ShadowsOnly, true);
                        }
                    }
                }
            }
        }

        #endregion GPU Instancing

        #region GPU Instancing Utility Methods

        /// <summary>
        /// Returns the argsbuffer offset for this LOD. Returns index offset by default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="runtimeData"></param>
        /// <param name="lod"></param>
        /// <param name="renderer"></param>
        /// <param name="inBytes">if true, returns byte-offset instead of index offset</param>
        /// <returns></returns>
        public static int GetArgsOffsetForLODRenderer<T>(T runtimeData, int lod, int renderer, bool inBytes = false) where T : GPUInstancerRuntimeData
        {
            int offset = 0;

            for (int i = 0; i < lod + 1; i++)
            {
                if (i == lod || i > runtimeData.instanceLODs.Count)
                {
                    for (int r = 0; r < renderer + 1; r++)
                    {
                        if (r == renderer || r > runtimeData.instanceLODs[i].renderers.Count)
                            return inBytes ? offset * sizeof(uint) : offset;

                        offset += runtimeData.instanceLODs[i].renderers[r].materials.Count * 5;
                    }
                }

                for (int r = 0; r < runtimeData.instanceLODs[i].renderers.Count; r++)
                {
                    offset += runtimeData.instanceLODs[i].renderers[r].materials.Count * 5;
                }
            }

            return inBytes ? offset * sizeof(uint) : offset;
        }
        #endregion GPU Instancing Utility Methods

        #region Prototype Release

        public static void ReleaseInstanceBuffers<T>(List<T> runtimeDataList) where T : GPUInstancerRuntimeData
        {
            if (runtimeDataList == null)
                return;

            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                if (runtimeDataList[i].instanceLODs != null && runtimeDataList[i].instanceLODs.Count > 0)
                {
                    for (int lod = 0; lod < runtimeDataList[i].instanceLODs.Count; lod++)
                    {
                        if (runtimeDataList[i].instanceLODs[lod].transformationMatrixAppendBuffer != null)
                            runtimeDataList[i].instanceLODs[lod].transformationMatrixAppendBuffer.Release();
                        runtimeDataList[i].instanceLODs[lod].transformationMatrixAppendBuffer = null;
                    }
                }

                if (runtimeDataList[i].transformationMatrixVisibilityBuffer != null)
                    runtimeDataList[i].transformationMatrixVisibilityBuffer.Release();
                runtimeDataList[i].transformationMatrixVisibilityBuffer = null;

                if (runtimeDataList[i].argsBuffer != null)
                    runtimeDataList[i].argsBuffer.Release();
                runtimeDataList[i].argsBuffer = null;

                if (runtimeDataList[i].shadowAppendBuffer != null)
                    runtimeDataList[i].shadowAppendBuffer.Release();
                runtimeDataList[i].shadowAppendBuffer = null;

                if (runtimeDataList[i].shadowArgsBuffer != null)
                    runtimeDataList[i].shadowArgsBuffer.Release();
                runtimeDataList[i].shadowArgsBuffer = null;
            }
        }

        public static void ClearInstanceData<T>(List<T> runtimeDataList) where T : GPUInstancerRuntimeData
        {
            if (runtimeDataList == null)
                return;

            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                runtimeDataList[i].instanceDataArray = null;
            }
        }

        #endregion Prototype Release

        #region Create Prototypes

        /// <summary>
        /// Returns a list of GPU Instancer compatible prototypes given the DetailPrototypes from a Unity Terrain.
        /// </summary>
        /// <param name="detailPrototypes">Unity Terrain Detail prototypes</param>
        /// <returns></returns>
        public static void SetDetailInstancePrototypes(GameObject gameObject, List<GPUInstancerPrototype> detailInstancePrototypes, DetailPrototype[] detailPrototypes, int quadCount,
            GPUInstancerShaderBindings shaderBindings, GPUInstancerTerrainSettings terrainSettings, bool forceNew)
        {
            if (forceNew)
                RemoveAssetsOfType(terrainSettings, typeof(GPUInstancerDetailPrototype));

            for (int i = 0; i < detailPrototypes.Length; i++)
            {
                if (!forceNew && detailInstancePrototypes.Exists(p => ((GPUInstancerDetailPrototype)p).detailPrototypeIndex == i))
                    continue;

                AddDetailInstancePrototypeFromTerrainPrototype(gameObject, detailInstancePrototypes, detailPrototypes[i], i, quadCount, shaderBindings, terrainSettings);
            }
            RemoveUnusedAssets(terrainSettings, detailInstancePrototypes, typeof(GPUInstancerDetailPrototype));
        }

        public static void AddDetailInstancePrototypeFromTerrainPrototype(GameObject gameObject, List<GPUInstancerPrototype> detailInstancePrototypes, DetailPrototype terrainDetailPrototype,
            int detailIndex, int quadCount, GPUInstancerShaderBindings shaderBindings, GPUInstancerTerrainSettings terrainSettings,
            GameObject replacementPrefab = null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(gameObject, "Detail prototype changed " + detailIndex);
#endif

            if (replacementPrefab == null && terrainDetailPrototype.prototype != null)
            {
                replacementPrefab = terrainDetailPrototype.prototype;
                while (replacementPrefab.transform.parent != null)
                    replacementPrefab = replacementPrefab.transform.parent.gameObject;
            }

            GPUInstancerDetailPrototype detailPrototype = ScriptableObject.CreateInstance<GPUInstancerDetailPrototype>();
            detailPrototype.detailPrototypeIndex = detailIndex;
            detailPrototype.detailRenderMode = terrainDetailPrototype.renderMode;
            detailPrototype.usePrototypeMesh = terrainDetailPrototype.usePrototypeMesh;
            detailPrototype.prefabObject = replacementPrefab;
            detailPrototype.prototypeTexture = terrainDetailPrototype.prototypeTexture;
            detailPrototype.useCrossQuads = quadCount > 1;
            detailPrototype.quadCount = quadCount;
            detailPrototype.detailHealthyColor = terrainDetailPrototype.healthyColor;
            detailPrototype.detailDryColor = terrainDetailPrototype.dryColor;
            detailPrototype.noiseSpread = terrainDetailPrototype.noiseSpread;
            detailPrototype.detailScale = new Vector4(terrainDetailPrototype.minWidth, terrainDetailPrototype.maxWidth, terrainDetailPrototype.minHeight, terrainDetailPrototype.maxHeight);
            detailPrototype.windWaveTintColor =
                Color.Lerp(detailPrototype.detailHealthyColor, detailPrototype.detailDryColor, 0.5f);
            detailPrototype.name = terrainDetailPrototype.prototype != null ? terrainDetailPrototype.prototype.name : terrainDetailPrototype.prototypeTexture.name + "_" + detailIndex + "_" + terrainDetailPrototype.prototypeTexture.GetInstanceID();
            detailPrototype.maxDistance = terrainSettings.maxDetailDistance;
            detailPrototype.isShadowCasting = terrainDetailPrototype.renderMode == DetailRenderMode.GrassBillboard && quadCount == 1 ? false : true;

            AddObjectToAsset(terrainSettings, detailPrototype);

            detailInstancePrototypes.Add(detailPrototype);

            if (terrainDetailPrototype.usePrototypeMesh)
                GenerateInstancedShadersForGameObject(detailPrototype.prefabObject, shaderBindings);
        }

        public static void AddDetailInstanceRuntimeDataToList(List<GPUInstancerRuntimeData> runtimeDataList, List<GPUInstancerPrototype> detailPrototypes, GPUInstancerShaderBindings shaderBindings, GPUInstancerTerrainSettings terrainSettings)
        {
            for (int i = 0; i < detailPrototypes.Count; i++)
            {
                GPUInstancerRuntimeData runtimeData = new GPUInstancerRuntimeData(detailPrototypes[i]);

                GPUInstancerDetailPrototype detailPrototype = (GPUInstancerDetailPrototype)detailPrototypes[i];

                if (detailPrototype.usePrototypeMesh)
                {
                    runtimeData.CreateRenderersFromGameObject(detailPrototypes[i].prefabObject, shaderBindings);

                    if (detailPrototypes[i].prefabObject.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial.shader.name == GPUInstancerConstants.SHADER_GPUI_FOLIAGE))
                    {
                        for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
                        {
                            for (int r = 0; r < runtimeData.instanceLODs[lod].renderers.Count; r++)
                            {
                                runtimeData.instanceLODs[lod].renderers[r].mpb.SetTexture("_HealthyDryNoiseTexture", terrainSettings.healthyDryNoiseTexture);
                                runtimeData.instanceLODs[lod].renderers[r].mpb.SetTexture("_WindWaveNormalTexture", terrainSettings.windWaveNormalTexture);
                                runtimeData.instanceLODs[lod].renderers[r].mpb.SetVector("_WindVector", terrainSettings.windVector);
                            }
                        }
                    }
                }
                else
                {
                    Material instanceMaterial;

                    if (detailPrototype.useCustomMaterialForTextureDetail && detailPrototype.textureDetailCustomMaterial != null)
                    {
                        instanceMaterial = shaderBindings.GetInstancedMaterial(detailPrototype.textureDetailCustomMaterial);
                        instanceMaterial.name = "InstancedMaterial_" + detailPrototype.prototypeTexture.name;

                        // Note: Cross quad distance billboarding is disabled for custom materials since GPU Instancer handles billboarding in the GPUInstancer/Foliage shader.
                        runtimeData.AddLod(CreateCrossQuadsMeshForDetailGrass(1, 1, detailPrototype.prototypeTexture.name,
                                detailPrototype.quadCount), new List<Material> { instanceMaterial }, new MaterialPropertyBlock(), 0f);

                        runtimeDataList.Add(runtimeData);

                        continue;
                    }

                    instanceMaterial = new Material(Shader.Find(GPUInstancerConstants.SHADER_GPUI_FOLIAGE));

                    instanceMaterial.SetTexture("_HealthyDryNoiseTexture", terrainSettings.healthyDryNoiseTexture);
                    instanceMaterial.SetTexture("_WindWaveNormalTexture", terrainSettings.windWaveNormalTexture);
                    instanceMaterial.SetVector("_WindVector", terrainSettings.windVector);

                    instanceMaterial.SetFloat("_IsBillboard", detailPrototype.useCrossQuads ? 0.0f : detailPrototype.isBillboard ? 1.0f : 0.0f);
                    instanceMaterial.SetTexture("_MainTex", detailPrototype.prototypeTexture);
                    instanceMaterial.SetColor("_HealthyColor", detailPrototype.detailHealthyColor);
                    instanceMaterial.SetColor("_DryColor", detailPrototype.detailDryColor);
                    instanceMaterial.SetFloat("_NoiseSpread", detailPrototype.noiseSpread);
                    instanceMaterial.SetFloat("_AmbientOcclusion", detailPrototype.ambientOcclusion);
                    instanceMaterial.SetFloat("_GradientPower", detailPrototype.gradientPower);

                    instanceMaterial.SetColor("_WindWaveTintColor", detailPrototype.windWaveTintColor);
                    instanceMaterial.SetFloat("_WindIdleSway", detailPrototype.windIdleSway);
                    instanceMaterial.SetFloat("_WindWavesOn", detailPrototype.windWavesOn ? 1.0f : 0.0f);
                    instanceMaterial.SetFloat("_WindWaveSize", detailPrototype.windWaveSize);
                    instanceMaterial.SetFloat("_WindWaveTint", detailPrototype.windWaveTint);
                    instanceMaterial.SetFloat("_WindWaveSway", detailPrototype.windWaveSway);


                    instanceMaterial.name = "InstancedMaterial_" + detailPrototype.prototypeTexture.name;

                    runtimeData.AddLod(CreateCrossQuadsMeshForDetailGrass(1, 1, detailPrototype.prototypeTexture.name,
                                                                          detailPrototype.useCrossQuads ? detailPrototype.quadCount : 1), new List<Material> { instanceMaterial }, new MaterialPropertyBlock(),
                                                                          detailPrototype.useCrossQuads ? GetDistanceRelativeHeight(detailPrototype) : 0f);

                    // Add grass LOD if cross quadding.
                    if (detailPrototype.useCrossQuads)
                    {
                        Material lodMaterial = new Material(instanceMaterial);
                        lodMaterial.SetFloat("_IsBillboard", 1.0f);

                        // LOD Debug:
                        if (detailPrototype.billboardDistanceDebug)
                        {
                            lodMaterial.SetColor("_HealthyColor", detailPrototype.billboardDistanceDebugColor);
                            lodMaterial.SetColor("_DryColor", detailPrototype.billboardDistanceDebugColor);
                        }

                        runtimeData.AddLod(CreateCrossQuadsMeshForDetailGrass(1, 1, detailPrototype.prototypeTexture.name,
                            1), new List<Material> { lodMaterial }, new MaterialPropertyBlock(), 0f);
                    }
                }

                runtimeDataList.Add(runtimeData);
            }
        }

        /// <summary>
        ///     <para>To increase rendering performance, GPU Instancer buffers are updated only if buffer related runtime data is changed or the camera position is changed.
        ///     While updating at runtime, it is safe to force a one-time buffer update since the camera can potentially be static. This will not compromise performance.</para>
        ///     <para>Note that this is not necessary for shader properties such as noise, wind and mesh settings. Use this when there is a potential update to frustum and distance culling settings.</para>
        /// </summary>
        /// <param name="detailManager">The manager that defines the prototypes you want to GPU instance.</param>
        public static void ForceDetailBufferUpdate(GPUInstancerDetailManager detailManager)
        {
            detailManager.cameraData.cameraChanged = true;
        }

        public static void UpdateDetailInstanceRuntimeDataList(List<GPUInstancerRuntimeData> runtimeDataList, GPUInstancerTerrainSettings terrainSettings, bool updateMeshes = false)
        {
            for (int i = 0; i < runtimeDataList.Count; i++)
            {
                GPUInstancerDetailPrototype detailPrototype = (GPUInstancerDetailPrototype)runtimeDataList[i].prototype;

                if (detailPrototype.usePrototypeMesh)
                {
                    if (detailPrototype.prefabObject.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial.shader.name == GPUInstancerConstants.SHADER_GPUI_FOLIAGE))
                    {
                        for (int lod = 0; lod < runtimeDataList[i].instanceLODs.Count; lod++)
                        {
                            for (int r = 0; r < runtimeDataList[i].instanceLODs[lod].renderers.Count; r++)
                            {
                                runtimeDataList[i].instanceLODs[lod].renderers[r].mpb.SetTexture("_HealthyDryNoiseTexture", terrainSettings.healthyDryNoiseTexture);
                                runtimeDataList[i].instanceLODs[lod].renderers[r].mpb.SetTexture("_WindWaveNormalTexture", terrainSettings.windWaveNormalTexture);
                                runtimeDataList[i].instanceLODs[lod].renderers[r].mpb.SetVector("_WindVector", terrainSettings.windVector);
                            }
                        }
                    }
                }
                else
                {
                    if (!detailPrototype.useCustomMaterialForTextureDetail || (detailPrototype.useCustomMaterialForTextureDetail && detailPrototype.textureDetailCustomMaterial != null))
                    {
                        if (updateMeshes)
                        {
                            if (detailPrototype.useCrossQuads)
                            {
                                GPUInstancerPrototypeLOD lodBilboard = runtimeDataList[i].instanceLODs[runtimeDataList[i].instanceLODs.Count - 1];

                                if (runtimeDataList[i].instanceLODs.Count == 2 && runtimeDataList[i].instanceLODs[0].transformationMatrixAppendBuffer != null)
                                    runtimeDataList[i].instanceLODs[0].transformationMatrixAppendBuffer.Release();

                                runtimeDataList[i].instanceLODs.Clear();

                                runtimeDataList[i].AddLod(CreateCrossQuadsMeshForDetailGrass(1, 1, detailPrototype.prototypeTexture.name,
                                                                                      detailPrototype.quadCount), new List<Material> { lodBilboard.renderers[0].materials[0] }, new MaterialPropertyBlock(),
                                                                                      1); // not calling GetDistanceRelativeHeight since will be called below.

                                runtimeDataList[i].instanceLODs.Add(lodBilboard);
                                runtimeDataList[i].lodSizes.y = 0f;

                                if (runtimeDataList[i].argsBuffer != null)
                                    runtimeDataList[i].argsBuffer.Release();

                                runtimeDataList[i].argsBuffer = null;
                                InitializeGPUBuffer(runtimeDataList[i]);
                            }
                            else if (runtimeDataList[i].instanceLODs.Count == 2)
                            {
                                if (runtimeDataList[i].instanceLODs[0].transformationMatrixAppendBuffer != null)
                                    runtimeDataList[i].instanceLODs[0].transformationMatrixAppendBuffer.Release();
                                
                                runtimeDataList[i].instanceLODs.RemoveAt(0);

                                if (runtimeDataList[i].argsBuffer != null)
                                    runtimeDataList[i].argsBuffer.Release();

                                runtimeDataList[i].argsBuffer = null;
                                runtimeDataList[i].lodSizes.x = 0;
                                runtimeDataList[i].lodSizes.y = -1;
                                InitializeGPUBuffer(runtimeDataList[i]);
                            }
                        }

                        for (int lod = 0; lod < runtimeDataList[i].instanceLODs.Count; lod++)
                        {
                            MaterialPropertyBlock instanceMaterial = runtimeDataList[i].instanceLODs[lod].renderers[0].mpb;

                            instanceMaterial.SetTexture("_HealthyDryNoiseTexture", terrainSettings.healthyDryNoiseTexture);
                            instanceMaterial.SetTexture("_WindWaveNormalTexture", terrainSettings.windWaveNormalTexture);
                            instanceMaterial.SetVector("_WindVector", terrainSettings.windVector);

                            instanceMaterial.SetColor("_HealthyColor", detailPrototype.detailHealthyColor);
                            instanceMaterial.SetColor("_DryColor", detailPrototype.detailDryColor);
                            instanceMaterial.SetFloat("_NoiseSpread", detailPrototype.noiseSpread);
                            instanceMaterial.SetFloat("_AmbientOcclusion", detailPrototype.ambientOcclusion);
                            instanceMaterial.SetFloat("_GradientPower", detailPrototype.gradientPower);

                            instanceMaterial.SetColor("_WindWaveTintColor", detailPrototype.windWaveTintColor);
                            instanceMaterial.SetFloat("_WindIdleSway", detailPrototype.windIdleSway);
                            instanceMaterial.SetFloat("_WindWavesOn", detailPrototype.windWavesOn ? 1.0f : 0.0f);
                            instanceMaterial.SetFloat("_WindWaveSize", detailPrototype.windWaveSize);
                            instanceMaterial.SetFloat("_WindWaveTint", detailPrototype.windWaveTint);
                            instanceMaterial.SetFloat("_WindWaveSway", detailPrototype.windWaveSway);

                            instanceMaterial.SetFloat("_IsBillboard", detailPrototype.useCrossQuads && lod == 0 ? 0.0f : detailPrototype.isBillboard || detailPrototype.useCrossQuads ? 1.0f : 0.0f);
                        }
                    }

                    if (detailPrototype.useCrossQuads)
                    {
                        runtimeDataList[i].lodSizes[0] = GetDistanceRelativeHeight(detailPrototype);

                        if (detailPrototype.billboardDistanceDebug)
                        {
                            MaterialPropertyBlock instanceMaterial = runtimeDataList[i].instanceLODs[1].renderers[0].mpb;
                            instanceMaterial.SetColor("_HealthyColor", detailPrototype.billboardDistanceDebugColor);
                            instanceMaterial.SetColor("_DryColor", detailPrototype.billboardDistanceDebugColor);
                        }
                    }
                }


            }
        }

        public static float GetDistanceRelativeHeight(GPUInstancerDetailPrototype detailPrototype)
        {
            return (1 - detailPrototype.billboardDistance);
        }

        #endregion Create Prototypes

        #region Mesh Utility Methods

        public static Mesh CreateCrossQuadsMeshForDetailGrass(float width, float height, string name, int quality)
        {
            GameObject parent = new GameObject(name, typeof(MeshFilter));
            parent.transform.position = Vector3.zero;
            CombineInstance[] combinesInstances = new CombineInstance[quality];
            for (int i = 0; i < quality; i++)
            {
                GameObject child = new GameObject("quadToCombine_" + i, typeof(MeshFilter));

                Mesh mesh = GenerateQuadMesh(width, height, new Rect(0.0f, 0.0f, 1.0f, 1.0f), true);

                // modify normals fit for grass
                for (int j = 0; j < mesh.normals.Length; j++)
                    mesh.normals[i] = Vector3.up;

                child.GetComponent<MeshFilter>().sharedMesh = mesh;
                child.transform.parent = parent.transform;
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity * Quaternion.AngleAxis((180.0f / quality) * i, Vector3.up);
                child.transform.localScale = Vector3.one;

                combinesInstances[i] = new CombineInstance
                {
                    mesh = child.GetComponent<MeshFilter>().sharedMesh,
                    transform = child.transform.localToWorldMatrix
                };
            }
            parent.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            parent.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combinesInstances, true, true);
            Mesh result = parent.GetComponent<MeshFilter>().sharedMesh;
            result.name = name;

            GameObject.DestroyImmediate(parent);
            return result;
        }

        public static Mesh GenerateQuadMesh(float width, float height, Rect? uvRect = null, bool centerPivot = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = "QuadMesh";
            mesh.vertices = new Vector3[] { new Vector3(centerPivot ? - width/2 : 0, 0, 0),
            new Vector3(centerPivot ? - width / 2 : 0, height, 0),
            new Vector3(centerPivot ? width/2 : width, height, 0),
            new Vector3(centerPivot ? width/2 : width, 0, 0) };

            if (uvRect != null)
                mesh.uv = new Vector2[] {
                new Vector2(uvRect.Value.x, uvRect.Value.y),
                new Vector2(uvRect.Value.x, uvRect.Value.y + uvRect.Value.height),
                new Vector2(uvRect.Value.x + uvRect.Value.width, uvRect.Value.y + uvRect.Value.height),
                new Vector2(uvRect.Value.x + uvRect.Value.width, uvRect.Value.y)
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

            Color[] colors = new Color[mesh.vertices.Length];

            for (int i = 0; i < mesh.vertices.Length; i++)
                colors[i] = Color.Lerp(Color.clear, Color.red, mesh.vertices[i].y);

            mesh.colors = colors;

            return mesh;
        }

        #endregion Mesh Utility Methods

        #region Terrain Utility Methods

        public static List<int[]> GetDetailMapsFromTerrain(Terrain terrain, List<GPUInstancerPrototype> detailPrototypeList)
        {
            List<int[]> result = new List<int[]>();
            for (int i = 0; i < detailPrototypeList.Count; i++)
            {
                int[,] map = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailResolution, terrain.terrainData.detailResolution, ((GPUInstancerDetailPrototype)detailPrototypeList[i]).detailPrototypeIndex);
                result.Add(new int[map.GetLength(0) * map.GetLength(1)]);
                for (int y = 0; y < map.GetLength(0); y++)
                {
                    for (int x = 0; x < map.GetLength(1); x++)
                    {
                        result[i][x + y * map.GetLength(0)] = map[y, x];
                    }
                }
            }
            return result;
        }

        public static Bounds GenerateBoundsFromTerrainPositionAndSize(Vector3 position, Vector3 size)
        {
            return new Bounds(new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z + size.z / 2), size);
        }

        /// get height for specified coordinates
        public static float SampleTerrainHeight(float px, float py, float leftBottomH, float leftTopH, float rightBottomH, float rightTopH)
        {
            return Mathf.Lerp(Mathf.Lerp(leftBottomH, rightBottomH, px), Mathf.Lerp(leftTopH, rightTopH, px), py);
        }

        public static Vector3 ComputeTerrainNormal(float leftBottomH, float leftTopH, float rightBottomH, float scale)
        {
            Vector3 P = new Vector3(0, leftBottomH * scale, 0);
            Vector3 Q = new Vector3(0, leftTopH * scale, 1);
            Vector3 R = new Vector3(1, rightBottomH * scale, 0);

            return Vector3.Cross(Q - R, R - P).normalized;
        }

        #endregion Terrain Utility Methods

        #region Math Utility Methods

        public static int GCD(int[] numbers)
        {
            return numbers.Aggregate(GCD);
        }

        public static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        public static IEnumerable<int> GetDivisors(int n)
        {
            return from a in Enumerable.Range(2, n / 2)
                   where n % a == 0
                   select a;
        }

        #endregion Math Utility Methods

        #region ScriptableObject Utility Methods

        public static void RemoveAssetsOfType(UnityEngine.Object baseAsset, Type type)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string assetPath = AssetDatabase.GetAssetPath(baseAsset);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool requireImport = false;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset.GetType().Equals(type))
                    {
                        GameObject.DestroyImmediate(asset, true);
                        requireImport = true;
                    }
                }
                if (requireImport)
                    AssetDatabase.ImportAsset(assetPath);
            }
#endif
        }

        public static void RemoveUnusedAssets<T>(UnityEngine.Object baseAsset, List<T> prototypeList, Type prototypeType) where T : GPUInstancerPrototype
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string assetPath = AssetDatabase.GetAssetPath(baseAsset);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool requireImport = false;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset.GetType() == prototypeType && !prototypeList.Contains((T)asset))
                    {
                        GameObject.DestroyImmediate(asset, true);
                        requireImport = true;
                    }
                }
                if (requireImport)
                    AssetDatabase.ImportAsset(assetPath);
            }
#endif
        }

        public static void AddObjectToAsset(UnityEngine.Object baseAsset, UnityEngine.Object objectToAdd)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string assetPath = AssetDatabase.GetAssetPath(baseAsset);
                AssetDatabase.AddObjectToAsset(objectToAdd, assetPath);
                AssetDatabase.ImportAsset(assetPath);
            }
#endif
        }

        public static void SetPrototypeListFromAssets<T>(UnityEngine.Object baseAsset, List<T> prototypeList, Type prototypeType) where T : GPUInstancerPrototype
        {
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(baseAsset);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset.GetType() == prototypeType)
                    prototypeList.Add((T)asset);
            }
#endif
        }

        #endregion ScriptableObject Utility Methods

        #region Spatial Partitioning

        public static void CalculateSpatialPartitioningValuesFromTerrain(GPUInstancerSpatialPartitioningData<GPUInstancerCell> spData,
            Terrain terrain, float maxDetailDistance, float preferedCellSize = 0)
        {
            if (preferedCellSize == 0)
                preferedCellSize = maxDetailDistance / 2;

            float maxTerrainSize = Mathf.Max(terrain.terrainData.size.x, terrain.terrainData.size.z);
            spData.cellRowAndCollumnCountPerTerrain = Mathf.FloorToInt(maxTerrainSize / preferedCellSize);

            if (spData.cellRowAndCollumnCountPerTerrain == 0)
            {
                spData.cellRowAndCollumnCountPerTerrain = 1;
            }
            else
            {
                // fix cellRowAndCollumnCountPerTerrain
                if (terrain.terrainData.detailResolution % spData.cellRowAndCollumnCountPerTerrain != 0
                    || (terrain.terrainData.heightmapResolution - 1) % spData.cellRowAndCollumnCountPerTerrain != 0)
                {
                    int gcd = GCD(terrain.terrainData.detailResolution, terrain.terrainData.heightmapResolution - 1);
                    List<int> divisors = GetDivisors(gcd).ToList();
                    divisors.Add(gcd);
                    divisors.RemoveAll(d => d > spData.cellRowAndCollumnCountPerTerrain);
                    spData.cellRowAndCollumnCountPerTerrain = divisors.Last();
                }
            }

            float innerCellSizeX = terrain.terrainData.size.x / spData.cellRowAndCollumnCountPerTerrain;
            float innerCellSizeY = terrain.terrainData.size.y;
            float innerCellSizeZ = terrain.terrainData.size.z / spData.cellRowAndCollumnCountPerTerrain;
            float boundsAddition = maxDetailDistance * 2.5f;

            for (int z = 0; z < spData.cellRowAndCollumnCountPerTerrain; z++)
            {
                for (int x = 0; x < spData.cellRowAndCollumnCountPerTerrain; x++)
                {
                    GPUInstancerDetailCell cell = new GPUInstancerDetailCell(x, z);
                    cell.cellBounds = new Bounds(
                        new Vector3(
                            terrain.transform.position.x + (x * innerCellSizeX) + innerCellSizeX / 2,
                            terrain.transform.position.y + innerCellSizeY / 2,
                            terrain.transform.position.z + (z * innerCellSizeZ) + innerCellSizeZ / 2
                        ),
                        new Vector3(innerCellSizeX + boundsAddition, innerCellSizeY + boundsAddition, innerCellSizeZ + boundsAddition));

                    cell.instanceStartPosition = new Vector3(terrain.transform.position.x + x * innerCellSizeX, terrain.transform.position.y, terrain.transform.position.z + z * innerCellSizeZ);

                    spData.AddCell(cell);
                }
            }
        }

        public static void FillCellsDetailData(Terrain terrain, GPUInstancerSpatialPartitioningData<GPUInstancerCell> detailSPData)
        {
            int detailMapSize = terrain.terrainData.detailResolution / detailSPData.cellRowAndCollumnCountPerTerrain;
            int heightMapSize = ((terrain.terrainData.heightmapResolution - 1) / detailSPData.cellRowAndCollumnCountPerTerrain) + 1;
            //Debug.Log("Cell DetailMapSize: " + detailMapSize + " Cell HeightMapSize: " + heightMapSize);

            GPUInstancerCell cell = null;
            GPUInstancerDetailCell detailCell = null;
            for (int z = 0; z < detailSPData.cellRowAndCollumnCountPerTerrain; z++)
            {
                for (int x = 0; x < detailSPData.cellRowAndCollumnCountPerTerrain; x++)
                {
                    detailSPData.GetCell(GPUInstancerCell.CalculateHash(x, 0, z), out cell);

                    if (cell != null)
                        detailCell = (GPUInstancerDetailCell)cell;
                    else
                        continue;

                    detailCell.heightMapData = terrain.terrainData.GetHeights(detailCell.coordX * (heightMapSize - 1), detailCell.coordZ * (heightMapSize - 1),
                    heightMapSize, heightMapSize).MirrorAndFlatten();

                    detailCell.detailMapData = new List<int[]>();
                    detailCell.totalDetailCounts = new List<int>();

                    for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
                    {
                        int[] detailMapData = terrain.terrainData.GetDetailLayer(cell.coordX * detailMapSize, detailCell.coordZ * detailMapSize,
                            detailMapSize, detailMapSize, i).MirrorAndFlatten();
                        detailCell.detailMapData.Add(detailMapData);
                        int total = 0;
                        foreach (int num in detailMapData)
                            total += num;
                        detailCell.totalDetailCounts.Add(total);
                    }
                }
            }
        }

        #endregion Spatial Partitioning

        #region Shader Functions

        public static void GenerateInstancedShadersForGameObject(GameObject gameObject, GPUInstancerShaderBindings shaderBindings)
        {
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer mr in meshRenderers)
            {
                Material[] mats = mr.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (shaderBindings.IsShadersInstancedVersionExists(mats[i].shader.name))
                        continue;

                    if (!Application.isPlaying)
                    {
                        if (IsMaterialInstanced(mats[i]))
                        {
                            shaderBindings.AddShaderInstance(mats[i].shader.name, mats[i].shader);
                        }
                        else
                        {
                            Shader instancedShader = CreateInstancedShader(mats[i].shader, shaderBindings);
                            if (instancedShader != null)
                                shaderBindings.AddShaderInstance(mats[i].shader.name, instancedShader);
                            else
                                Debug.LogWarning("Can not create instanced version for shader: " + mats[i].shader.name + ". Standard Shader will be used instead.");
                        }
                    }
                }
            }
        }

        public static bool IsMaterialInstanced(Material material)
        {
#if UNITY_EDITOR
            string originalAssetPath = AssetDatabase.GetAssetPath(material.shader);
            string originalShaderText = "";
            try
            {
                originalShaderText = System.IO.File.ReadAllText(originalAssetPath);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(originalShaderText))
                return originalShaderText.Contains("GPUInstancerInclude.cginc");
#endif
            return false;
        }

        public static Shader CreateInstancedShader(Shader originalShader, GPUInstancerShaderBindings shaderBindings)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Creating Shader", "Creating instanced shader...", 0.1f);
            try
            {
                string originalAssetPath = AssetDatabase.GetAssetPath(originalShader);
                string originalShaderText = System.IO.File.ReadAllText(originalAssetPath);
                
                string newShaderName = "GPUInstancer/" + originalShader.name;
                string newShaderText = originalShaderText.Replace("\r\n", "\n").Replace(originalShader.name, newShaderName);

                string includePath = "Include/GPUInstancerInclude.cginc";
                string standardShaderPath = AssetDatabase.GetAssetPath(Shader.Find(GPUInstancerConstants.SHADER_GPUI_STANDARD));
                string[] oapSplit = originalAssetPath.Split('/');
                string[] sspSplit = standardShaderPath.Split('/');
                int startIndex = 0;
                for (int i = 0; i < oapSplit.Length - 1; i++)
                {
                    if (oapSplit[i] == sspSplit[i])
                        startIndex++;
                    else break;
                }
                for (int i = sspSplit.Length - 2; i >= startIndex; i--)
                {
                    includePath = sspSplit[i] + "/" + includePath;
                }
                //includePath = System.IO.Path.GetDirectoryName(standardShaderPath) + "/" + includePath;

                for (int i = startIndex; i < oapSplit.Length - 1; i++)
                {
                    includePath = "../" + includePath;
                }
                includePath = "./" + includePath;

                int lastIndex = 0;
                string searchStart = "CGPROGRAM";
                string additionTextStart = "\n#include \"UnityCG.cginc\"\n#include \"" + includePath + "\"\n#pragma instancing_options procedural:setupGPUI\n#pragma multi_compile_instancing";
                string searchEnd = "ENDCG";
                string additionTextEnd = "";//"#include \"" + includePath + "\"\n";

                int foundIndex = -1;
                while (true)
                {
                    foundIndex = newShaderText.IndexOf(searchStart, lastIndex);
                    if (foundIndex == -1)
                        break;
                    lastIndex = foundIndex + searchStart.Length + additionTextStart.Length + 1;

                    newShaderText = newShaderText.Substring(0, foundIndex + searchStart.Length) + additionTextStart + newShaderText.Substring(foundIndex + searchStart.Length, newShaderText.Length - foundIndex - searchStart.Length);

                    foundIndex = newShaderText.IndexOf(searchEnd, lastIndex);
                    lastIndex = foundIndex + searchStart.Length + additionTextEnd.Length + 1;
                    newShaderText = newShaderText.Substring(0, foundIndex) + additionTextEnd + newShaderText.Substring(foundIndex, newShaderText.Length - foundIndex);
                }

                string originalFileName = System.IO.Path.GetFileName(originalAssetPath);
                string newAssetPath = originalAssetPath.Replace(originalFileName, originalFileName.Replace(".shader", "_GPUI.shader"));

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(newShaderText);
                System.IO.FileStream fs = System.IO.File.Create(newAssetPath);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
                //System.IO.File.WriteAllText(newAssetPath, newShaderText);
                EditorUtility.DisplayProgressBar("Creating Shader", "Importing instanced shader...", 0.3f);
                AssetDatabase.Refresh();

                Debug.Log("Generated instanced version for shader: " + originalShader.name);
                EditorUtility.ClearProgressBar();

                return Shader.Find(newShaderName);
            }
            catch (Exception)
            {
                EditorUtility.ClearProgressBar();
            }
#endif
            return null;
        }

        #endregion Shader Functions

        #region Extensions

        public static T[] MirrorAndFlatten<T>(this T[,] array2D)
        {
            T[] resultArray1D = new T[array2D.GetLength(0) * array2D.GetLength(1)];

            for (int y = 0; y < array2D.GetLength(0); y++)
            {
                for (int x = 0; x < array2D.GetLength(1); x++)
                {
                    resultArray1D[x + y * array2D.GetLength(0)] = array2D[y, x];
                }
            }

            return resultArray1D;
        }

        /// <summary>
        ///   <para>Returns a random float number between and min [inclusive] and max [inclusive] (Read Only).</para>
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static float Range(this System.Random prng, float min, float max)
        {
            return (float)(min + (prng.NextDouble() * (max - min)));
        }

        public static void SetDataSingle(this ComputeBuffer computeBuffer, Matrix4x4[] data, int managedBufferStartIndex, int computeBufferStartIndex)
        {
#if UNITY_2017_1_OR_NEWER
            computeBuffer.SetData(data, managedBufferStartIndex, computeBufferStartIndex, 1);
#else
            if (GPUInstancerConstants.ComputeBufferSetDataPartial != null)
            {
                GPUInstancerConstants.ComputeBufferSetDataPartial.SetBuffer(GPUInstancerConstants.computeBufferSetDataSingleKernelId, GPUInstancerConstants.INSTANCE_DATA_BUFFER, computeBuffer);
                GPUInstancerConstants.ComputeBufferSetDataPartial.SetFloats(GPUInstancerConstants.BUFFER_PARAMETER_DATA_TO_SET,
                    data[managedBufferStartIndex].m00,
                    data[managedBufferStartIndex].m10,
                    data[managedBufferStartIndex].m20,
                    data[managedBufferStartIndex].m30,
                    data[managedBufferStartIndex].m01,
                    data[managedBufferStartIndex].m11,
                    data[managedBufferStartIndex].m21,
                    data[managedBufferStartIndex].m31,
                    data[managedBufferStartIndex].m02,
                    data[managedBufferStartIndex].m12,
                    data[managedBufferStartIndex].m22,
                    data[managedBufferStartIndex].m32,
                    data[managedBufferStartIndex].m03,
                    data[managedBufferStartIndex].m13,
                    data[managedBufferStartIndex].m23,
                    data[managedBufferStartIndex].m33
                    );
                GPUInstancerConstants.ComputeBufferSetDataPartial.SetInt(GPUInstancerConstants.BUFFER_PARAMETER_COMPUTE_BUFFER_START_INDEX, computeBufferStartIndex);
                GPUInstancerConstants.ComputeBufferSetDataPartial.Dispatch(GPUInstancerConstants.computeBufferSetDataSingleKernelId, 1, 1, 1);
            }
#endif
        }

        public static void SetDataPartial(this ComputeBuffer computeBuffer, Matrix4x4[] data, int managedBufferStartIndex, int computeBufferStartIndex, int count, ComputeBuffer managedBuffer = null, Matrix4x4[] managedData = null)
        {
            if (managedBufferStartIndex == 0 && computeBufferStartIndex == 0 && count == data.Length)
                computeBuffer.SetData(data);

#if UNITY_2017_1_OR_NEWER
            computeBuffer.SetData(data, managedBufferStartIndex, computeBufferStartIndex, count);
#else
            if(count == 1)
            {
                SetDataSingle(computeBuffer, data, managedBufferStartIndex, computeBufferStartIndex);
            }
            else
            {
                if (GPUInstancerConstants.ComputeBufferSetDataPartial != null)
                {
                    Array.Copy(data, managedBufferStartIndex, managedData, 0, count);
                    managedBuffer.SetData(managedData);

                    GPUInstancerConstants.ComputeBufferSetDataPartial.SetBuffer(GPUInstancerConstants.computeBufferSetDataPartialKernelId, GPUInstancerConstants.INSTANCE_DATA_BUFFER, computeBuffer);
                    GPUInstancerConstants.ComputeBufferSetDataPartial.SetBuffer(GPUInstancerConstants.computeBufferSetDataPartialKernelId, GPUInstancerConstants.BUFFER_PARAMETER_MANAGED_BUFFER_DATA, managedBuffer);
                    GPUInstancerConstants.ComputeBufferSetDataPartial.SetInt(GPUInstancerConstants.BUFFER_PARAMETER_COMPUTE_BUFFER_START_INDEX, computeBufferStartIndex);
                    GPUInstancerConstants.ComputeBufferSetDataPartial.SetInt(GPUInstancerConstants.BUFFER_PARAMETER_COUNT, count);
                    GPUInstancerConstants.ComputeBufferSetDataPartial.Dispatch(GPUInstancerConstants.computeBufferSetDataPartialKernelId, Mathf.CeilToInt(count / 1024f), 1, 1);
                }
            }
#endif
        }

        #endregion Extensions

        #region Event System

        private static Dictionary<GPUInstancerEventType, UnityEvent> _eventDictionary;

        public static void StartListening(GPUInstancerEventType eventType, UnityAction listener)
        {
            if (_eventDictionary == null)
                _eventDictionary = new Dictionary<GPUInstancerEventType, UnityEvent>();

            UnityEvent thisEvent = null;
            if (_eventDictionary.TryGetValue(eventType, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                _eventDictionary.Add(eventType, thisEvent);
            }
        }

        public static void StopListening(GPUInstancerEventType eventType, UnityAction listener)
        {
            if (_eventDictionary == null)
                return;

            UnityEvent thisEvent = null;
            if (_eventDictionary.TryGetValue(eventType, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(GPUInstancerEventType eventType)
        {
            if (_eventDictionary == null || !_eventDictionary.ContainsKey(eventType))
                return;

            UnityEvent thisEvent = null;
            if (_eventDictionary.TryGetValue(eventType, out thisEvent))
                thisEvent.Invoke();
        }

        #endregion Event System
    }

}


