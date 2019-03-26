using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{

    public abstract class GPUInstancerManager : MonoBehaviour
    {
        public GPUInstancerShaderBindings shaderBindings;

        public List<GPUInstancerPrototype> prototypeList;

        public bool autoSelectCamera = true;
        public GPUInstancerCameraData cameraData = new GPUInstancerCameraData(null);

        public List<GPUInstancerRuntimeData> runtimeDataList;
        public Bounds instancingBounds;

        protected GPUInstancerSpatialPartitioningData<GPUInstancerCell> spData;

        public static List<GPUInstancerManager> activeManagerList;
        public static bool showRenderedAmount;

        protected static ComputeShader _visibilityComputeShader;
        protected static int[] _instanceVisibilityComputeKernelIDs;

#if UNITY_EDITOR
        [HideInInspector]
        public GPUInstancerPrototype selectedPrototype;
        [HideInInspector]
        public int pickerControlID = -1;
        [HideInInspector]
        public bool editorDataChanged = false;
        [HideInInspector]
        public int pickerMode = 0;
#endif

        protected static readonly Queue<Action> threadQueue = new Queue<Action>();

        #region MonoBehaviour Methods

        public virtual void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                CheckPrototypeChanges();
#endif
            if (activeManagerList == null)
                activeManagerList = new List<GPUInstancerManager>();
            
            if (_visibilityComputeShader == null)
            { 
                _visibilityComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.VISIBILITY_COMPUTE_RESOURCE_PATH);
                _instanceVisibilityComputeKernelIDs = new int[GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS.Length];
                for (int i = 0; i < _instanceVisibilityComputeKernelIDs.Length; i++)
                    _instanceVisibilityComputeKernelIDs[i] = _visibilityComputeShader.FindKernel(GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS[i]);
            }

            showRenderedAmount = false;

            InitializeCameraData();

            SetDefaultGPUInstancerShaderBindings();
        }

        public virtual void OnEnable()
        {
            if (activeManagerList != null && !activeManagerList.Contains(this))
                activeManagerList.Add(this);

            if (shaderBindings == null)
                Debug.LogWarning("No shader bindings file was supplied. Instancing will terminate!");

            if (Application.isPlaying && (runtimeDataList == null || runtimeDataList.Count == 0))
                InitializeRuntimeDataAndBuffers();
        }

        public virtual void Update()
        {
            if (threadQueue.Count > 0)
            {
                Action action = threadQueue.Dequeue();
                if (action != null)
                    action.Invoke();
            }
        }

        public virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                CheckPrototypeChanges();
            else
#endif
                UpdateBuffers();
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void Reset()
        {
            SetDefaultGPUInstancerShaderBindings();
#if UNITY_EDITOR
            CheckPrototypeChanges();
#endif
        }

        public virtual void OnDisable() // could also be OnDestroy, but OnDestroy seems to be too late to prevent buffer leaks.
        {
            if (activeManagerList != null)
                activeManagerList.Remove(this);

            ClearInstancingData();
        }

        // Remove comment-out status to see partitioning bound gizmos:

        //public void OnDrawGizmos()
        //{
        //    if (spData != null && spData.activeCellList != null)
        //    {
        //        Color oldColor = Gizmos.color;
        //        Gizmos.color = Color.blue;
        //        foreach (GPUInstancerCell cell in spData.activeCellList)
        //        {
        //            if (cell != null)
        //                Gizmos.DrawWireCube(cell.cellBounds.center, cell.cellBounds.size);
        //        }
        //        Gizmos.color = oldColor;
        //    }
        //}

        public void SetDefaultGPUInstancerShaderBindings()
        {
            if (shaderBindings == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RecordObject(this, "GPUInstancerShaderBindings instance generated");
#endif
                shaderBindings = Resources.Load<GPUInstancerShaderBindings>(GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.SHADER_BINDINGS_DEFAULT_NAME);

                if (shaderBindings == null)
                {
                    shaderBindings = ScriptableObject.CreateInstance<GPUInstancerShaderBindings>();
                    shaderBindings.ResetShaderInstances();
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH))
                        {
                            System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH);
                        }

                        AssetDatabase.CreateAsset(shaderBindings, GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.SHADER_BINDINGS_DEFAULT_NAME + ".asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
#endif
                }
            }
        }

        #endregion MonoBehaviour Methods

        #region Virtual Methods

        public virtual void ClearInstancingData()
        {
            GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
            if (runtimeDataList != null)
                runtimeDataList.Clear();
            spData = null;
            threadQueue.Clear();
        }

        public virtual void GeneratePrototypes(bool forceNew = false)
        {
            ClearInstancingData();

            if (forceNew || prototypeList == null)
                prototypeList = new List<GPUInstancerPrototype>();
            else
                prototypeList.RemoveAll(p => p == null);

            SetDefaultGPUInstancerShaderBindings();
        }

#if UNITY_EDITOR
        public virtual void CheckPrototypeChanges()
        {
            if (prototypeList == null)
                GeneratePrototypes();
            else
                prototypeList.RemoveAll(p => p == null);

            if (shaderBindings != null)
            {
                shaderBindings.ClearEmptyShaderInstances();
                foreach (GPUInstancerPrototype prototype in prototypeList)
                {
                    if (prototype.prefabObject != null)
                        GPUInstancerUtility.GenerateInstancedShadersForGameObject(prototype.prefabObject, shaderBindings);
                }
            }
        }

        public virtual void ShowObjectPicker()
        {

        }

        public virtual void AddPickerObject(UnityEngine.Object pickerObject)
        {

        }

        public virtual void OnEditorDataChanged()
        {
            editorDataChanged = true;
        }

        public virtual void ApplyEditorDataChanges()
        {

        }
#endif
        public virtual void InitializeRuntimeDataAndBuffers()
        {
            instancingBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

            GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
            if (runtimeDataList != null)
                runtimeDataList.Clear();
            else
                runtimeDataList = new List<GPUInstancerRuntimeData>();
        }

        public virtual void InitializeSpatialPartitioning()
        {

        }

        public virtual void UpdateSpatialPartitioningCells()
        {

        }

        public virtual void DeletePrototype(GPUInstancerPrototype prototype)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Delete prototype");
#endif
            prototypeList.Remove(prototype);
        }
        #endregion Virtual Methods

        #region Private Methods

        private void UpdateBuffers()
        {
            if (cameraData != null && cameraData.mainCamera != null)
            {
                cameraData.CalculateCameraData();

                instancingBounds.center = cameraData.mainCamera.transform.position;

                UpdateSpatialPartitioningCells();

                GPUInstancerUtility.UpdateGPUBuffers(_visibilityComputeShader, _instanceVisibilityComputeKernelIDs, runtimeDataList,
                    cameraData, instancingBounds, showRenderedAmount);

                if (cameraData.cameraChanged)
                    cameraData.cameraChanged = false;
            }
        }

        private void InitializeCameraData()
        {
            if (autoSelectCamera || cameraData.mainCamera == null)
            {
                cameraData.mainCamera = Camera.main;
                cameraData.cameraChanged = true;
            }
        }

        #endregion Private Methods

        #region Public Methods

        public void SetCamera(Camera camera)
        {
            if (cameraData == null)
                cameraData = new GPUInstancerCameraData(camera);
            else
                cameraData.mainCamera = camera;
            cameraData.cameraChanged = true;
        }
        #endregion Public Methods
    }

}