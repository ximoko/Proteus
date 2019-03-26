using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    /// <summary>
    /// Simulate GPU Instancing while game is not running
    /// </summary>
    public class GPUInstancerSimulator
    {
        public GPUInstancerDetailManager detailManager;
        public Editor editor;
        public bool simulateAtEditor;
        public bool initializingInstances;

        private bool initializationFinished;
        private GPUInstancerCameraData cameraData = new GPUInstancerCameraData(null);
        private List<GPUInstancerRuntimeData> runtimeDataList;
        private Bounds instancingBounds;
        private DateTime timeSimulationUpdateRequested;

        private static ComputeShader _visibilityComputeShader;
        private static int[] _instanceVisibilityComputeKernelIDs;

        public GPUInstancerSimulator(GPUInstancerDetailManager detailManager, Editor editor)
        {
            this.detailManager = detailManager;
            this.editor = editor;
        }

        public void StartSimulation()
        {
            if (Application.isPlaying || detailManager == null)
                return;

            if (_visibilityComputeShader == null)
                _visibilityComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.VISIBILITY_COMPUTE_RESOURCE_PATH);
            if (_instanceVisibilityComputeKernelIDs == null)
                _instanceVisibilityComputeKernelIDs = new int[GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS.Length];
            for (int i = 0; i < _instanceVisibilityComputeKernelIDs.Length; i++)
                _instanceVisibilityComputeKernelIDs[i] = _visibilityComputeShader.FindKernel(GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS[i]);

            InitializeRuntimeDataAndBuffers();

            simulateAtEditor = true;
            cameraData.mainCamera = null;
            timeSimulationUpdateRequested = DateTime.Now;
            EditorApplication.update += EditorUpdate;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#else
            EditorApplication.playmodeStateChanged = HandlePlayModeStateChanged;
#endif
        }

        public void StopSimulation()
        {
            if (runtimeDataList != null)
            {
                GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
                runtimeDataList.Clear();
            }
            if (!Application.isPlaying)
            {
                detailManager.terrain.detailObjectDistance = detailManager.terrainSettings.maxDetailDistance > 250 ? 250 : detailManager.terrainSettings.maxDetailDistance;
            }

            simulateAtEditor = false;
            EditorApplication.update -= EditorUpdate;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#else
            EditorApplication.playmodeStateChanged = null;
#endif
        }

        public void UpdateSimulation()
        {
            EditorApplication.update -= EditorUpdate;
            if (!simulateAtEditor)
                return;
            timeSimulationUpdateRequested = DateTime.Now;
            EditorApplication.update += EditorUpdate;
        }

        private void EditorUpdate()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= EditorUpdate;
                return;
            }

            if (initializingInstances)
            {
                if (initializationFinished)
                    InstanceInitializationThreadFinished();
                return;
            }

            if (runtimeDataList == null || runtimeDataList.Count == 0)
                return;

            if (cameraData.mainCamera == null || cameraData.mainCamera.name != "SceneCamera")
            {
                Camera currentCam = Camera.current;
                if (currentCam != null && currentCam.name == "SceneCamera")
                    cameraData.mainCamera = currentCam;
                cameraData.cameraChanged = true;
            }

            // If can not find camera
            if (cameraData.mainCamera == null)
                return;

            // wait before update
            if (DateTime.Now.Subtract(timeSimulationUpdateRequested).Milliseconds < 500)
                return;

            UpdateBuffers();
            EditorApplication.update -= EditorUpdate;
        }

        private void UpdateBuffers()
        {
            cameraData.CalculateCameraData();
            instancingBounds.center = cameraData.mainCamera.transform.position;
            GPUInstancerUtility.UpdateGPUBuffers(_visibilityComputeShader, _instanceVisibilityComputeKernelIDs, runtimeDataList,
                cameraData, instancingBounds);
        }

        private void InitializeRuntimeDataAndBuffers()
        {
            instancingBounds = new Bounds(detailManager.terrain.terrainData.size / 2, detailManager.terrain.terrainData.size);

            GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
            if (runtimeDataList != null)
                runtimeDataList.Clear();
            else
                runtimeDataList = new List<GPUInstancerRuntimeData>();
            
            if (detailManager.prototypeList != null && detailManager.prototypeList.Count > 0)
            {
                GPUInstancerUtility.AddDetailInstanceRuntimeDataToList(runtimeDataList, detailManager.prototypeList, detailManager.shaderBindings, detailManager.terrainSettings);
                initializingInstances = true;
                initializationFinished = false;

                float[] heightMap = detailManager.terrain.terrainData.GetHeights(0, 0, detailManager.terrain.terrainData.heightmapResolution, detailManager.terrain.terrainData.heightmapResolution).MirrorAndFlatten();
                List<int[]> detailMaps = new List<int[]>();
                foreach (GPUInstancerRuntimeData rd in runtimeDataList)
                {
                    detailMaps.Add(detailManager.terrain.terrainData.GetDetailLayer(0, 0, detailManager.terrain.terrainData.detailResolution, detailManager.terrain.terrainData.detailResolution, ((GPUInstancerDetailPrototype)rd.prototype).detailPrototypeIndex).MirrorAndFlatten());
                }
                int detailResolution = detailManager.terrain.terrainData.detailResolution;
                int heightmapResolution = detailManager.terrain.terrainData.heightmapResolution;
                Vector3 terrainPosition = detailManager.terrain.GetPosition();
                Vector3 terrainSize = detailManager.terrain.terrainData.size;

                ThreadStart threadStart = delegate
                {
                    for (int i = 0; i < runtimeDataList.Count; i++)
                    {
                        int instanceCount = detailMaps[i].Sum();
                        runtimeDataList[i].instanceDataArray = GPUInstancerDetailManager.GetInstanceDataForDetailPrototype(
                                (GPUInstancerDetailPrototype)runtimeDataList[i].prototype,
                                detailMaps[i],
                                heightMap,
                                detailResolution,
                                heightmapResolution,
                                detailResolution,
                                heightmapResolution,
                                terrainPosition,
                                terrainSize,
                                instanceCount
                            );
                        runtimeDataList[i].instanceCount = instanceCount;
                        runtimeDataList[i].bufferSize = instanceCount;
                    }
                    initializationFinished = true;
                };
                new Thread(threadStart).Start();
            }
        }

        private void InstanceInitializationThreadFinished()
        {
            if (Application.isPlaying)
            {
                initializingInstances = false;
                return;
            }
            GPUInstancerUtility.InitializeGPUBuffers(runtimeDataList);
            detailManager.terrain.detailObjectDistance = 0;
            initializingInstances = false;
            editor.Repaint();
        }

#if UNITY_2017_2_OR_NEWER        
        public void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            StopSimulation();
        }
#else
        public void HandlePlayModeStateChanged()
        {
            StopSimulation();
        }
#endif

        public List<GPUInstancerRuntimeData> GetRuntimeDataList()
        {
            return runtimeDataList;
        }
    }
}