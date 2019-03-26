using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{
    /// <summary>
    /// Add this to a Unity terrain for GPU Instancing details at runtime.
    /// </summary>
    [ExecuteInEditMode]
    public class GPUInstancerDetailManager : GPUInstancerTerrainManager
    {
        private static ComputeShader _grassInstantiationComputeShader;

        private ComputeBuffer _generatingVisibilityBuffer;
#if !UNITY_2017_1_OR_NEWER
        private ComputeBuffer _managedBuffer;
        private Matrix4x4[] _managedData;
#endif
        private bool _triggerEvent;

        #region MonoBehaviour Methods
        public override void Awake()
        {
            base.Awake();

            if (_grassInstantiationComputeShader == null)
                _grassInstantiationComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.GRASS_INSTANTIATION_RESOURCE_PATH);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (_generatingVisibilityBuffer != null)
            {
                _generatingVisibilityBuffer.Release();
            }
        }
        #endregion MonoBehaviour Methods

        #region Override Methods

        public override void ClearInstancingData()
        {
            base.ClearInstancingData();

            if (terrain != null && terrain.detailObjectDistance == 0)
            {
                terrain.detailObjectDistance = terrainSettings.maxDetailDistance > 250 ? 250 : terrainSettings.maxDetailDistance;
                terrain.Flush();
            }
            spData = null;

#if !UNITY_2017_1_OR_NEWER
            if (_managedBuffer != null)
                _managedBuffer.Release();
            _managedBuffer = null;
#endif
        }

        public override void GeneratePrototypes(bool forceNew = false)
        {
            base.GeneratePrototypes(forceNew);

            if (terrainSettings != null)
            {
                GPUInstancerUtility.SetDetailInstancePrototypes(gameObject, prototypeList, terrainSettings.terrainData.detailPrototypes, 2, shaderBindings, terrainSettings, forceNew);
            }
        }

#if UNITY_EDITOR
        public override void CheckPrototypeChanges()
        {
            base.CheckPrototypeChanges();

            if (!Application.isPlaying && terrainSettings != null)
            {
                if (prototypeList.Count != terrainSettings.terrainData.detailPrototypes.Length)
                {
                    GeneratePrototypes();
                }

                AddProxyToTerrain();
            }
        }

        public override void ShowObjectPicker()
        {
            base.ShowObjectPicker();

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Texture"), false, () => { EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", pickerControlID); });
            menu.AddItem(new GUIContent("Prefab (Grass)"), false, () => { pickerMode = 0; EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", pickerControlID); });
            menu.AddItem(new GUIContent("Prefab (Other)"), false, () => { pickerMode = 1; EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", pickerControlID); });

            // display the menu
            menu.ShowAsContext();
        }

        public override void AddPickerObject(UnityEngine.Object pickerObject)
        {
            base.AddPickerObject(pickerObject);

            if (pickerObject == null)
                return;

            Undo.RecordObject(this, "Add prototype");

            if (terrainSettings != null)
            {
                List<DetailPrototype> newDetailPrototypes = new List<DetailPrototype>(terrainSettings.terrainData.detailPrototypes);

                if (pickerObject is Texture2D)
                {
                    newDetailPrototypes.Add(new DetailPrototype()
                    {
                        usePrototypeMesh = false,
                        prototypeTexture = (Texture2D)pickerObject,
                        renderMode = DetailRenderMode.GrassBillboard
                    });

                    terrainSettings.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
                    terrainSettings.terrainData.RefreshPrototypes();
                    GeneratePrototypes();
                }
                else if (pickerObject is GameObject)
                {
                    if (((GameObject)pickerObject).GetComponentInChildren<MeshRenderer>() == null)
                        return;

                    // Determine terrainDetailPrototype color to get a similar look on Unity Terrain
                    Color dryColor = Color.white;
                    Color healthyColor = Color.white;

                    Renderer pickerObjectRenderer = ((GameObject)pickerObject).GetComponentInChildren<Renderer>();

                    if (pickerObjectRenderer.sharedMaterial.shader.name == GPUInstancerConstants.SHADER_GPUI_FOLIAGE)
                    {
                        dryColor = pickerObjectRenderer.sharedMaterial.GetColor("_DryColor");
                        healthyColor = pickerObjectRenderer.sharedMaterial.GetColor("_HealthyColor");
                    }
                    else
                    {
                        healthyColor = pickerObjectRenderer.sharedMaterial.color;
                        dryColor = pickerObjectRenderer.sharedMaterial.color;
                    }


                    DetailPrototype terrainDetailPrototype = new DetailPrototype()
                    {
                        usePrototypeMesh = true,
                        prototype = ((GameObject)pickerObject).GetComponentInChildren<MeshRenderer>().gameObject,
                        renderMode = pickerMode == 0 ? DetailRenderMode.Grass : DetailRenderMode.VertexLit,
                        healthyColor = healthyColor,
                        dryColor = dryColor
                    };
                    newDetailPrototypes.Add(terrainDetailPrototype);

                    terrainSettings.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
                    terrainSettings.terrainData.RefreshPrototypes();
                    GPUInstancerUtility.AddDetailInstancePrototypeFromTerrainPrototype(gameObject, prototypeList, terrainDetailPrototype, newDetailPrototypes.Count - 1, 1, shaderBindings, terrainSettings,
                        (GameObject)pickerObject);
                }
            }
        }

        public override void ApplyEditorDataChanges()
        {
            base.ApplyEditorDataChanges();

            if (terrainSettings.terrainData.detailPrototypes.Length != prototypeList.Count)
                return;

            // set detail prototypes
            DetailPrototype[] detailPrototypes = terrainSettings.terrainData.detailPrototypes;
            foreach (GPUInstancerDetailPrototype prototype in prototypeList)
            {
                GameObject prefab = null;
                if (prototype.prefabObject != null)
                {
                    prefab = ((GameObject)prototype.prefabObject).GetComponentInChildren<MeshRenderer>().gameObject;
                }

                DetailPrototype dp = detailPrototypes[prototype.detailPrototypeIndex];

                dp.renderMode = prototype.detailRenderMode;
                dp.usePrototypeMesh = prototype.usePrototypeMesh;
                dp.prototype = prefab;
                dp.prototypeTexture = prototype.prototypeTexture;
                dp.noiseSpread = prototype.noiseSpread;
                dp.minWidth = prototype.detailScale.x;
                dp.maxWidth = prototype.detailScale.y;
                dp.minHeight = prototype.detailScale.z;
                dp.maxHeight = prototype.detailScale.w;
                dp.healthyColor = prototype.detailHealthyColor;
                dp.dryColor = prototype.detailDryColor;

                // Update terrainDetailPrototype color form prototype material to get a similar look on Unity Terrain for Mesh type prototypes.
                if (prototype.usePrototypeMesh)
                {
                    if (prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.shader.name == GPUInstancerConstants.SHADER_GPUI_FOLIAGE)
                    {
                        dp.healthyColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_HealthyColor");
                        dp.dryColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_DryColor");
                    }
                    else
                    {
                        dp.healthyColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.color;
                        dp.dryColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.color;
                    }
                }

                if (prototype.useCustomMaterialForTextureDetail && prototype.textureDetailCustomMaterial != null)
                {
                    dp.healthyColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.color;
                    dp.dryColor = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial.color;
                }
            }

            terrainSettings.terrainData.detailPrototypes = detailPrototypes;
            editorDataChanged = false;
        }
#endif
        public override void InitializeRuntimeDataAndBuffers()
        {
            base.InitializeRuntimeDataAndBuffers();

            if (terrainSettings == null)
                return;

            replacingInstances = false;
            initalizingInstances = true;

            if (prototypeList != null && prototypeList.Count > 0)
            {
                GPUInstancerUtility.AddDetailInstanceRuntimeDataToList(runtimeDataList, prototypeList, shaderBindings, terrainSettings);
            }

            terrain.detailObjectDistance = 0;
            terrain.Flush();

            InitializeSpatialPartitioning();
        }

        public override void UpdateSpatialPartitioningCells()
        {
            base.UpdateSpatialPartitioningCells();

            if (terrainSettings == null || spData == null)
                return;

            if (!initalizingInstances && !replacingInstances && spData.IsActiveCellUpdateRequired(cameraData.mainCamera.transform.position))
            {
                replacingInstances = true;
                StartCoroutine(GenerateVisibilityBufferFromActiveCellsCoroutine());
            }
        }

        public override void DeletePrototype(GPUInstancerPrototype prototype)
        {
            base.DeletePrototype(prototype);

            if (terrainSettings != null)
            {
                GPUInstancerDetailPrototype detailPrototype = (GPUInstancerDetailPrototype)prototype;

                DetailPrototype[] detailPrototypes = terrainSettings.terrainData.detailPrototypes;
                List<DetailPrototype> newDetailPrototypes = new List<DetailPrototype>();
                List<int[,]> newDetailLayers = new List<int[,]>();

                for (int i = 0; i < detailPrototypes.Length; i++)
                {
                    if (i != detailPrototype.detailPrototypeIndex)
                    {
                        newDetailPrototypes.Add(detailPrototypes[i]);
                        newDetailLayers.Add(terrainSettings.terrainData.GetDetailLayer(0, 0, terrainSettings.terrainData.detailResolution, terrainSettings.terrainData.detailResolution, i));
                    }
                    terrainSettings.terrainData.SetDetailLayer(0, 0, i, new int[terrainSettings.terrainData.detailResolution, terrainSettings.terrainData.detailResolution]);
                }

                terrainSettings.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
                for (int i = 0; i < newDetailLayers.Count; i++)
                {
                    terrainSettings.terrainData.SetDetailLayer(0, 0, i, newDetailLayers[i]);
                }
                terrainSettings.terrainData.RefreshPrototypes();

                // fix detail prototype indexes
                for (int i = 0; i < prototypeList.Count; i++)
                {
                    ((GPUInstancerDetailPrototype)prototypeList[i]).detailPrototypeIndex = i;
                }

                GeneratePrototypes(false);
            }
        }

        #endregion Override Methods

        private static int FixBounds(int value, int max, int failValue)
        {
            if (value >= max)
                return failValue;
            return value;
        }

        public static Matrix4x4[] GetInstanceDataForDetailPrototype(GPUInstancerDetailPrototype detailPrototype, int[] detailMap, float[] heightMapData,
                                                                int detailMapSize, int heightMapSize,
                                                                int detailResolution, int heightResolution,
                                                                Vector3 startPosition, Vector3 terrainSize,
                                                                int instanceCount)
        {
            Matrix4x4[] result = new Matrix4x4[instanceCount];

            if (instanceCount == 0)
                return result;

            System.Random randomNumberGenerator = new System.Random();
            float detailHeightMapScale = (heightResolution - 1.0f) / detailResolution;
            int heightDataSize = heightMapSize * heightMapSize;
            float sizeDetailXScale = terrainSize.x / detailResolution;
            float sizeDetailZScale = terrainSize.z / detailResolution;
            float normalScale = heightResolution / (terrainSize.x / terrainSize.y);

            float px, py, leftBottomH, leftTopH, rightBottomH, rightTopH;
            int heightIndex;
            Vector3 position;

            int counter = 0;
            for (int y = 0; y < detailMapSize; y++)
            {
                for (int x = 0; x < detailMapSize; x++)
                {
                    for (int j = 0; j < detailMap[y * detailMapSize + x]; j++) // for the amount of detail at this point and for this prototype
                    {
                        position.x = x + randomNumberGenerator.Range(0f, 0.99f);
                        position.y = 0;
                        position.z = y + randomNumberGenerator.Range(0f, 0.99f);

                        // set height
                        px = position.x * detailHeightMapScale;
                        py = position.z * detailHeightMapScale;
                        heightIndex = Mathf.FloorToInt(px) + Mathf.FloorToInt(py) * heightMapSize;
                        leftBottomH = heightMapData[heightIndex];
                        leftTopH = heightMapData[FixBounds(heightIndex + heightMapSize, heightDataSize, heightIndex)];
                        rightBottomH = heightMapData[heightIndex + 1];
                        rightTopH = heightMapData[FixBounds(heightIndex + heightMapSize + 1, heightDataSize, heightIndex)];

                        position.x *= sizeDetailXScale;
                        position.y = GPUInstancerUtility.SampleTerrainHeight(px - Mathf.Floor(px), py - Mathf.Floor(py), leftBottomH, leftTopH, rightBottomH, rightTopH) * terrainSize.y;
                        position.z *= sizeDetailZScale;
                        position += startPosition;

                        // get normal
                        Vector3 terrainPointNormal = GPUInstancerUtility.ComputeTerrainNormal(leftBottomH, leftTopH, rightBottomH, normalScale);

                        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, terrainPointNormal);
                        rotation *= Quaternion.AngleAxis(randomNumberGenerator.Range(0.0f, 360.0f), Vector3.up);

                        float randomScale = randomNumberGenerator.Range(0.0f, 1.0f);

                        float xzScale = detailPrototype.detailScale.x + (detailPrototype.detailScale.y - detailPrototype.detailScale.x) * randomScale;
                        float yScale = detailPrototype.detailScale.z + (detailPrototype.detailScale.w - detailPrototype.detailScale.z) * randomScale;

                        Vector3 scale = new Vector3(xzScale, yScale, xzScale);

                        result[counter] = Matrix4x4.TRS(position, rotation, scale);
                        counter++;
                    }
                }
            }

            return result;
        }

        private static Matrix4x4[] GetInstanceDataForDetailPrototypeWithComputeShader(GPUInstancerDetailPrototype detailPrototype, int[] detailMap, float[] heightMapData,
                                                                int detailMapSize, int heightMapSize,
                                                                int detailResolution, int heightResolution,
                                                                Vector3 startPosition, Vector3 terrainSize,
                                                                int instanceCount,
                                                                ComputeShader grassInstantiationComputeShader, GPUInstancerTerrainSettings terrainSettings)
        {
            Matrix4x4[] result = new Matrix4x4[instanceCount];

            if (instanceCount == 0)
                return result;

            ComputeBuffer visibilityBuffer;

            // set compute shader
            int grassInstantiationComputeKernelId = grassInstantiationComputeShader.FindKernel(GPUInstancerConstants.GRASS_INSTANTIATION_KERNEL);

            ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMapData.Length, GPUInstancerConstants.STRIDE_SIZE_INT);
            heightMapBuffer.SetData(heightMapData);

            visibilityBuffer = new ComputeBuffer(instanceCount, GPUInstancerConstants.STRIDE_SIZE_MATRIX4X4);

            ComputeBuffer detailMapBuffer = new ComputeBuffer(Mathf.CeilToInt(detailMapSize * detailMapSize), GPUInstancerConstants.STRIDE_SIZE_INT);
            detailMapBuffer.SetData(detailMap);

            // dispatch compute shader
            DispatchDetailComputeShader(grassInstantiationComputeShader, grassInstantiationComputeKernelId,
                visibilityBuffer, detailMapBuffer, heightMapBuffer,
                new Vector4(detailMapSize, detailMapSize, heightMapSize, heightMapSize), startPosition, terrainSize, detailResolution, heightResolution, detailPrototype.detailScale, terrainSettings.healthyDryNoiseTexture, detailPrototype.noiseSpread, detailPrototype.GetInstanceID());

            detailMapBuffer.Release();

            visibilityBuffer.GetData(result);
            visibilityBuffer.Release();

            heightMapBuffer.Release();

            return result;
        }

        private static void DispatchDetailComputeShader(ComputeShader grassComputeShader, int grassInstantiationComputeKernelId,
            ComputeBuffer visibilityBuffer, ComputeBuffer detailMapBuffer, ComputeBuffer heightMapBuffer,
            Vector4 detailAndHeightMapSize, Vector3 startPosition, Vector3 terrainSize, int detailResolution, int heightResolution, Vector4 detailScale, Texture healthyDryNoiseTexture, float noiseSpread, int instanceID)
        {
            // setup compute shader
            grassComputeShader.SetBuffer(grassInstantiationComputeKernelId, GPUInstancerConstants.INSTANCE_DATA_BUFFER, visibilityBuffer);
            grassComputeShader.SetBuffer(grassInstantiationComputeKernelId, GPUInstancerConstants.DETAIL_MAP_DATA_BUFFER, detailMapBuffer);
            grassComputeShader.SetBuffer(grassInstantiationComputeKernelId, GPUInstancerConstants.HEIGHT_MAP_DATA_BUFFER, heightMapBuffer);
            grassComputeShader.SetInt(GPUInstancerConstants.TERRAIN_DETAIL_RESOLUTION_DATA, detailResolution);
            grassComputeShader.SetInt(GPUInstancerConstants.TERRAIN_HEIGHT_RESOLUTION_DATA, heightResolution);
            grassComputeShader.SetVector(GPUInstancerConstants.GRASS_START_POSITION_DATA, startPosition);
            grassComputeShader.SetVector(GPUInstancerConstants.TERRAIN_SIZE_DATA, terrainSize);
            grassComputeShader.SetVector(GPUInstancerConstants.DETAIL_SCALE_DATA, detailScale);
            grassComputeShader.SetVector(GPUInstancerConstants.DETAIL_AND_HEIGHT_MAP_SIZE_DATA, detailAndHeightMapSize);
            if (healthyDryNoiseTexture != null)
            {
                grassComputeShader.SetTexture(grassInstantiationComputeKernelId, GPUInstancerConstants.HEALTHY_DRY_NOISE_TEXTURE, healthyDryNoiseTexture);
                grassComputeShader.SetFloat(GPUInstancerConstants.NOISE_SPREAD, noiseSpread);
            }
            grassComputeShader.SetFloat(GPUInstancerConstants.DETAIL_UNIQUE_VALUE, instanceID / 1000f);

            // dispatch
            grassComputeShader.Dispatch(grassInstantiationComputeKernelId,
                Mathf.CeilToInt(detailAndHeightMapSize.x / GPUInstancerConstants.GRASS_SHADER_THREAD_COUNT.x),
                Mathf.CeilToInt(GPUInstancerConstants.GRASS_SHADER_THREAD_COUNT.y),
                Mathf.CeilToInt(detailAndHeightMapSize.y / GPUInstancerConstants.GRASS_SHADER_THREAD_COUNT.z));
        }

        #region Spatial Partitioning Cell Management
        public override void InitializeSpatialPartitioning()
        {
            base.InitializeSpatialPartitioning();

            spData = new GPUInstancerSpatialPartitioningData<GPUInstancerCell>();
            GPUInstancerUtility.CalculateSpatialPartitioningValuesFromTerrain(spData, terrain, terrainSettings.maxDetailDistance, terrainSettings.autoSPCellSize ? 0 : terrainSettings.preferedSPCellSize);

            // initialize cells
            GenerateCellsInstanceDataFromTerrain();
        }

        private IEnumerator GenerateVisibilityBufferFromActiveCellsCoroutine()
        {
            if (!initalizingInstances)
            {
#if !UNITY_2017_1_OR_NEWER
                if (_managedBuffer == null)
                    _managedBuffer = new ComputeBuffer(GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER, GPUInstancerConstants.STRIDE_SIZE_MATRIX4X4);
                if (_managedData == null)
                    _managedData = new Matrix4x4[GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER];
#endif
                List<GPUInstancerRuntimeData> runtimeDatas = new List<GPUInstancerRuntimeData>(runtimeDataList);

                int totalCount = 0;
                int lastbreak = 0;
                for (int r = 0; r < runtimeDatas.Count; r++)
                {
                    GPUInstancerRuntimeData rd = runtimeDatas[r];

                    if (spData.activeCellList != null && spData.activeCellList.Count > 0)
                    {
                        int totalInstanceCount = 0;

                        foreach (GPUInstancerDetailCell cell in spData.activeCellList)
                        {
                            if (cell != null && cell.detailInstanceList != null)
                            {
                                totalInstanceCount += cell.detailInstanceList[r].Length;
                            }
                        }

                        rd.bufferSize = totalInstanceCount;
                        rd.instanceCount = totalInstanceCount;

                        if (totalInstanceCount == 0)
                        {
                            if (rd.transformationMatrixVisibilityBuffer != null)
                                rd.transformationMatrixVisibilityBuffer.Release();
                            rd.transformationMatrixVisibilityBuffer = null;
                            continue;
                        }

                        _generatingVisibilityBuffer = new ComputeBuffer(totalInstanceCount, GPUInstancerConstants.STRIDE_SIZE_MATRIX4X4);

                        int startIndex = 0;
                        int cellStartIndex = 0;
                        int count = 0;
                        for (int c = 0; c < spData.activeCellList.Count; c++)
                        {
                            GPUInstancerDetailCell detailCell = (GPUInstancerDetailCell)spData.activeCellList[c];
                            cellStartIndex = 0;
                            for (int i = 0; i < Mathf.Ceil((float)detailCell.detailInstanceList[r].Length / (float)GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER); i++)
                            {
                                cellStartIndex = i * GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER;
                                count = GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER;
                                if (cellStartIndex + count > detailCell.detailInstanceList[r].Length)
                                    count = detailCell.detailInstanceList[r].Length - cellStartIndex;
#if UNITY_2017_1_OR_NEWER
                                _generatingVisibilityBuffer.SetDataPartial(detailCell.detailInstanceList[r], cellStartIndex, startIndex, count);
#else
                                _generatingVisibilityBuffer.SetDataPartial(detailCell.detailInstanceList[r], cellStartIndex, startIndex, count, _managedBuffer, _managedData);
#endif
                                startIndex += count;
                                totalCount += count;

                                if (count + cellStartIndex < detailCell.detailInstanceList[r].Length - 1 && totalCount - lastbreak > GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER)
                                {
                                    lastbreak = totalCount;
                                    yield return null;
                                }
                            }

                            if (initalizingInstances)
                                break;
                        }
                        if (initalizingInstances)
                            break;

                        cameraData.cameraChanged = true;

                        if (initalizingInstances)
                            break;

                        if (rd.transformationMatrixVisibilityBuffer != null)
                            rd.transformationMatrixVisibilityBuffer.Release();
                        rd.transformationMatrixVisibilityBuffer = _generatingVisibilityBuffer;
                    }
                    if (initalizingInstances)
                        break;

                    GPUInstancerUtility.InitializeGPUBuffer(rd);
                    lastbreak = totalCount;
                    yield return null;
                }
                if (initalizingInstances)
                {
                    if (_generatingVisibilityBuffer != null)
                        _generatingVisibilityBuffer.Release();
                    GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDatas);
                    GPUInstancerUtility.ClearInstanceData(runtimeDatas);
                }

                _generatingVisibilityBuffer = null;
                replacingInstances = false;

                if (!initalizingInstances)
                {
                    if (_triggerEvent)
                        GPUInstancerUtility.TriggerEvent(GPUInstancerEventType.DetailInitializationFinished);
                    _triggerEvent = false;
                }
            }
        }

        private void GenerateCellsInstanceDataFromTerrain()
        {
            GPUInstancerUtility.FillCellsDetailData(terrain, spData);

            int detailMapSize = terrain.terrainData.detailResolution / spData.cellRowAndCollumnCountPerTerrain;
            int heightMapSize = (terrain.terrainData.heightmapResolution - 1) / spData.cellRowAndCollumnCountPerTerrain + 1;
            int detailResolution = terrain.terrainData.detailResolution;
            int heightmapResolution = terrain.terrainData.heightmapResolution;
            Vector3 terrainSize = terrain.terrainData.size;

            StartCoroutine(SetInstanceDataForDetailCells(spData, prototypeList, detailMapSize, heightMapSize, detailResolution, heightmapResolution, terrainSize, terrainSettings, SetInstanceDataForDetailCellsCallback));
        }

        private void SetInstanceDataForDetailCellsCallback()
        {
            initalizingInstances = false;
            spData.activeCellList.Clear();
            _triggerEvent = true;
        }

        private static IEnumerator SetInstanceDataForDetailCells(GPUInstancerSpatialPartitioningData<GPUInstancerCell> spData, List<GPUInstancerPrototype> prototypeList,
            int detailMapSize, int heightMapSize, int detailResolution, int heightmapResolution, Vector3 terrainSize, GPUInstancerTerrainSettings terrainSettings, Action callback)
        {
            int totalCreated = 0;
            foreach (GPUInstancerDetailCell cell in spData.GetCellList())
            {
                if (cell.detailMapData == null)
                    continue;

                cell.detailInstanceList = new Dictionary<int, Matrix4x4[]>();
                for (int i = 0; i < prototypeList.Count; i++)
                {
                    totalCreated += cell.totalDetailCounts[i];
                    cell.detailInstanceList[i] = GetInstanceDataForDetailPrototypeWithComputeShader((GPUInstancerDetailPrototype)prototypeList[i], cell.detailMapData[i], cell.heightMapData,
                        detailMapSize, heightMapSize,
                        detailResolution, heightmapResolution,
                        cell.instanceStartPosition,
                        terrainSize, cell.totalDetailCounts[i], _grassInstantiationComputeShader, terrainSettings);
                    if (totalCreated >= GPUInstancerConstants.BUFFER_COROUTINE_STEP_NUMBER)
                    {
                        totalCreated = 0;
                        yield return null;
                    }
                }
            }
            callback();
        }

        #endregion Spatial Partitioning Cell Management
    }

}