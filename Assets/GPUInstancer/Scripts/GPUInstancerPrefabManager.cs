using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{
    [ExecuteInEditMode]
    public class GPUInstancerPrefabManager : GPUInstancerManager
    {
        [SerializeField]
        public List<RegisteredPrefabsData> registeredPrefabs = new List<RegisteredPrefabsData>();
        [SerializeField]
        public List<GameObject> prefabList;

        private List<GPUInstancerModificationCollider> _modificationColliders;
        private Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>> _registeredPrefabsRuntimeData;
        private List<IPrefabVariationData> _variationDataList;

        #region MonoBehavior Methods

        public override void Awake()
        {
            base.Awake();
        }

        public override void Reset()
        {
            base.Reset();

            RegisterPrefabsInScene();
        }

        #endregion MonoBehavior Methods

        public override void ClearInstancingData()
        {
            base.ClearInstancingData();
            
            if (Application.isPlaying && _registeredPrefabsRuntimeData != null)
            {
                foreach (GPUInstancerPrototype p in _registeredPrefabsRuntimeData.Keys)
                {
                    foreach (GPUInstancerPrefab prefabInstance in _registeredPrefabsRuntimeData[p])
                    {
                        if (!prefabInstance)
                            continue;

                        SetRenderersEnabled(prefabInstance, true);
                    }
                }
            }

            if(_variationDataList != null)
            {
                foreach (IPrefabVariationData pvd in _variationDataList)
                    pvd.ReleaseBuffer();
            }
        }

        public override void GeneratePrototypes(bool forceNew = false)
        {
            base.GeneratePrototypes();

            GeneratePrefabPrototypes(forceNew);
        }

#if UNITY_EDITOR
        public override void CheckPrototypeChanges()
        {
            base.CheckPrototypeChanges();

            if (prefabList == null)
                prefabList = new List<GameObject>();

            prefabList.RemoveAll(p => p == null);
            prefabList.RemoveAll(p => p.GetComponent<GPUInstancerPrefab>() == null);
            prototypeList.RemoveAll(p => p == null);
            prototypeList.RemoveAll(p => !prefabList.Contains(p.prefabObject));

            if (prefabList.Count != prototypeList.Count)
                GeneratePrototypes();

            registeredPrefabs.RemoveAll(rpd => !prototypeList.Contains(rpd.prefabPrototype));
            foreach (GPUInstancerPrefabPrototype prototype in prototypeList)
            {
                if (!registeredPrefabs.Exists(rpd => rpd.prefabPrototype == prototype))
                    registeredPrefabs.Add(new RegisteredPrefabsData(prototype));
            }
        }

        public override void ShowObjectPicker()
        {
            base.ShowObjectPicker();

            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", pickerControlID);
        }

        public override void AddPickerObject(UnityEngine.Object pickerObject)
        {
            base.AddPickerObject(pickerObject);

            if (pickerObject == null)
                return;

            if (!(pickerObject is GameObject) || PrefabUtility.GetPrefabType(pickerObject) != PrefabType.Prefab)
            {
                Debug.LogWarning("GPU Instancer Prefab manager only accepts user created prefabs. Cannot add selected object.");
                return;
            }

            GameObject prefabObject = (GameObject)pickerObject;

            GPUInstancerPrefab prefabScript = prefabObject.GetComponent<GPUInstancerPrefab>();
            if (prefabScript == null)
                prefabScript = prefabObject.AddComponent<GPUInstancerPrefab>();
            if (prefabScript == null)
                return;

            Undo.RecordObject(this, "Add prototype");

            if (!prefabList.Contains(prefabObject))
            {
                prefabList.Add(prefabObject);
                GeneratePrototypes();
            }

            if(prefabScript.prefabPrototype != null)
            {
                if (registeredPrefabs == null)
                    registeredPrefabs = new List<RegisteredPrefabsData>();

                RegisteredPrefabsData data = registeredPrefabs.Find(d => d.prefabPrototype == prefabScript.prefabPrototype);
                if (data == null)
                {
                    data = new RegisteredPrefabsData(prefabScript.prefabPrototype);
                    registeredPrefabs.Add(data);
                }

                GPUInstancerPrefab[] scenePrefabInstances = FindObjectsOfType<GPUInstancerPrefab>();
                foreach (GPUInstancerPrefab prefabInstance in scenePrefabInstances)
                    if(prefabInstance.prefabPrototype == prefabScript.prefabPrototype)
                        data.registeredPrefabs.Add(prefabInstance);
            }
        }
#endif
        public override void InitializeRuntimeDataAndBuffers()
        {
            base.InitializeRuntimeDataAndBuffers();

            if(registeredPrefabs != null && registeredPrefabs.Count > 0)
            {
                if (_registeredPrefabsRuntimeData == null)
                    _registeredPrefabsRuntimeData = new Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>>();

                foreach (RegisteredPrefabsData rpd in registeredPrefabs)
                {
                    if (!_registeredPrefabsRuntimeData.ContainsKey(rpd.prefabPrototype))
                        _registeredPrefabsRuntimeData.Add(rpd.prefabPrototype, rpd.registeredPrefabs);
                    else
                    {
                        _registeredPrefabsRuntimeData[rpd.prefabPrototype].AddRange(rpd.registeredPrefabs);
                        _registeredPrefabsRuntimeData[rpd.prefabPrototype] = new List<GPUInstancerPrefab>(_registeredPrefabsRuntimeData[rpd.prefabPrototype].Distinct());
                    }
                }
                registeredPrefabs.Clear();
            }

            if (_registeredPrefabsRuntimeData != null && _registeredPrefabsRuntimeData.Count > 0)
            {
                InitializeRuntimeDataRegisteredPrefabs();
                GPUInstancerUtility.InitializeGPUBuffers(runtimeDataList);
            }
        }

        public override void DeletePrototype(GPUInstancerPrototype prototype)
        {
            base.DeletePrototype(prototype);
            
            prefabList.Remove(prototype.prefabObject);
            DestroyImmediate(prototype.prefabObject.GetComponent<GPUInstancerPrefab>(), true);
#if UNITY_EDITOR
            EditorUtility.SetDirty(prototype.prefabObject);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prototype));
#endif
            GeneratePrototypes(false);
        }

        public void GeneratePrefabPrototypes(bool forceNew)
        {
            base.GeneratePrototypes();

            if (prefabList == null)
                return;

#if UNITY_EDITOR
            bool changed = false;
            if (forceNew)
            {
                foreach(GPUInstancerPrefabPrototype prototype in prototypeList)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prototype));
                    changed = true;
                }
            }
            else
            {
                foreach (GPUInstancerPrefabPrototype prototype in prototypeList)
                {
                    if (!prefabList.Contains(prototype.prefabObject))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prototype));
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif

            foreach (GameObject go in prefabList)
            {
                if (!forceNew && prototypeList.Exists(p => p.prefabObject == go))
                    continue;

                GPUInstancerPrefab prefabScript = go.GetComponent<GPUInstancerPrefab>();
                if (prefabScript == null)
                    prefabScript = go.AddComponent<GPUInstancerPrefab>();
                if(prefabScript == null)
                    continue;
                if (prefabScript.prefabPrototype == null)
                {
                    GPUInstancerPrefabPrototype prototype = ScriptableObject.CreateInstance<GPUInstancerPrefabPrototype>();
                    prefabScript.prefabPrototype = prototype;
                    prototype.prefabObject = go;
                    prototype.name = go.name + "_Prototype_" + go.GetInstanceID();

                    GPUInstancerUtility.GenerateInstancedShadersForGameObject(go, shaderBindings);

#if UNITY_EDITOR
                    EditorUtility.SetDirty(go);
                    string assetPath = GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_PREFAB_PATH + prototype.name + ".asset";

                    if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_PREFAB_PATH))
                    {
                        System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_PREFAB_PATH);
                    }

                    AssetDatabase.CreateAsset(prototype, assetPath);
#endif
                }

                prototypeList.Add(prefabScript.prefabPrototype);
            }

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isPlaying)
            {
                GPUInstancerPrefab[] prefabInstances = FindObjectsOfType<GPUInstancerPrefab>();
                for (int i = 0; i < prefabInstances.Length; i++)
                {
                    UnityEngine.Object prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstances[i].gameObject);
                    if (prefabRoot != null && ((GameObject)prefabRoot).GetComponent<GPUInstancerPrefab>() != null && prefabInstances[i].prefabPrototype != ((GameObject)prefabRoot).GetComponent<GPUInstancerPrefab>().prefabPrototype)
                    {
                        Undo.RecordObject(prefabInstances[i], "Changed GPUInstancer Prefab Prototype " + prefabInstances[i].gameObject + i);
                        prefabInstances[i].prefabPrototype = ((GameObject)prefabRoot).GetComponent<GPUInstancerPrefab>().prefabPrototype;
                    }
                }
            }
#endif
        }

        private void InitializeRuntimeDataRegisteredPrefabs()
        {
            if(runtimeDataList == null)
                runtimeDataList = new List<GPUInstancerRuntimeData>();
            else
                GPUInstancerUtility.ClearInstanceData(runtimeDataList);

            foreach (GPUInstancerPrefabPrototype p in prototypeList)
            {
                GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == p);
                if (runtimeData == null)
                {
                    runtimeData = new GPUInstancerRuntimeData(p);
                    runtimeData.CreateRenderersFromGameObject(p.prefabObject, shaderBindings);
                    runtimeDataList.Add(runtimeData);
                    if (p.isShadowCasting)
                    {
                        runtimeData.shadowCasterMaterial = new Material(Shader.Find(GPUInstancerConstants.SHADER_GPUI_SHADOWS_ONLY));
                        runtimeData.shadowCasterMPB = new MaterialPropertyBlock();
                    }   
                }

                int instanceCount = 0;
                List<GPUInstancerPrefab> registeredPrefabsList;
                if(_registeredPrefabsRuntimeData.TryGetValue(p, out registeredPrefabsList))
                {
                    if(registeredPrefabsList.Count > 0)
                    {
                        runtimeData.instanceDataArray = new Matrix4x4[registeredPrefabsList.Count + (p.enableRuntimeModifications && p.addRemoveInstancesAtRuntime ? p.extraBufferSize : 0)];
                        runtimeData.bufferSize = runtimeData.instanceDataArray.Length;

                        Matrix4x4 instanceData;
                        foreach (GPUInstancerPrefab prefabInstance in registeredPrefabsList)
                        {
                            if (!prefabInstance)
                                continue;

                            instanceData = prefabInstance.transform.localToWorldMatrix;
                            prefabInstance.state = PrefabInstancingState.Instanced;

                            bool disableRenderers = true;

                            if (prefabInstance.prefabPrototype.enableRuntimeModifications)
                            {
                                if (_modificationColliders != null && _modificationColliders.Count > 0)
                                {
                                    bool isInsideCollider = false;
                                    foreach (GPUInstancerModificationCollider mc in _modificationColliders)
                                    {
                                        if (mc.IsInsideCollider(prefabInstance))
                                        {
                                            isInsideCollider = true;
                                            mc.AddEnteredInstance(prefabInstance);
                                            instanceData = Matrix4x4.zero;
                                            prefabInstance.state = PrefabInstancingState.Disabled;
                                            disableRenderers = false;
                                            break;
                                        }
                                    }
                                    if (!isInsideCollider)
                                    {
                                        if (prefabInstance.prefabPrototype.startWithRigidBody && prefabInstance.GetComponent<Rigidbody>() != null)
                                        {
                                            isInsideCollider = true;
                                            _modificationColliders[0].AddEnteredInstance(prefabInstance);
                                            instanceData = Matrix4x4.zero;
                                            prefabInstance.state = PrefabInstancingState.Disabled;
                                            disableRenderers = false;
                                        }
                                    }
                                }
                            }

                            if (disableRenderers)
                                SetRenderersEnabled(prefabInstance, false);

                            runtimeData.instanceDataArray[instanceCount] = instanceData;
                            instanceCount++;
                            prefabInstance.gpuInstancerID = instanceCount;
                        }
                    }
                }

                // set instanceCount
                runtimeData.instanceCount = instanceCount;
                
                // variations
                if (_variationDataList != null)
                {
                    foreach (IPrefabVariationData pvd in _variationDataList)
                    {
                        if (pvd.GetPrototype() == p)
                        {
                            pvd.InitializeBufferAndArray(runtimeData.instanceDataArray.Length);
                            if (registeredPrefabsList != null)
                            {
                                foreach (GPUInstancerPrefab prefabInstance in registeredPrefabsList)
                                {
                                    pvd.SetInstanceData(prefabInstance);
                                }
                            }
                            pvd.SetBufferData(0, 0, runtimeData.instanceDataArray.Length);

                            for (int i = 0; i < runtimeData.instanceLODs.Count; i++)
                            {
                                for (int j = 0; j < runtimeData.instanceLODs[i].renderers.Count; j++)
                                {
                                    pvd.SetVariation(runtimeData.instanceLODs[i].renderers[j].mpb);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SetRenderersEnabled(GPUInstancerPrefab prefabInstance, bool enabled)
        {
            MeshRenderer[] meshRenderers = prefabInstance.GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers != null && meshRenderers.Length > 0)
                for (int mr = 0; mr < meshRenderers.Length; mr++)
                    meshRenderers[mr].enabled = enabled;
                        
            if (prefabInstance.GetComponent<LODGroup>() != null)
                prefabInstance.GetComponent<LODGroup>().enabled = enabled;

            Rigidbody rigidbody = prefabInstance.GetComponent<Rigidbody>();

            if (enabled)
            {
                Rigidbody prototypeRb = prefabInstance.prefabPrototype.prefabObject.GetComponent<Rigidbody>();
                if (prototypeRb != null)
                {
                    if (rigidbody == null)
                    {
                        rigidbody = prefabInstance.gameObject.AddComponent<Rigidbody>();
                        rigidbody.useGravity = prototypeRb.useGravity;
                        rigidbody.angularDrag = prototypeRb.angularDrag;
                        rigidbody.mass = prototypeRb.mass;
                        rigidbody.constraints = prototypeRb.constraints;
                        rigidbody.detectCollisions = true;
                        rigidbody.drag = prototypeRb.drag;
                        rigidbody.isKinematic = prototypeRb.isKinematic;
                        rigidbody.interpolation = prototypeRb.interpolation;
                    }
                }
            }
            else if (rigidbody != null)
                Destroy(rigidbody);
        }

        #region API Methods

        public void DisableIntancingForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if(runtimeData != null)
            {
                prefabInstance.state = PrefabInstancingState.Disabled;
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = Matrix4x4.zero;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                SetRenderersEnabled(prefabInstance, true);

                runtimeData.modified = true;
            }
        }

        public void EnableInstancingForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                prefabInstance.state = PrefabInstancingState.Instanced;
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = prefabInstance.transform.localToWorldMatrix;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                SetRenderersEnabled(prefabInstance, false);

                runtimeData.modified = true;
            }
        }

        public void UpdateTransformDataForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = prefabInstance.transform.localToWorldMatrix;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);

                runtimeData.modified = true;
            }
        }

        public void AddPrefabInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                if(runtimeData.instanceDataArray.Length == runtimeData.instanceCount)
                {
                    Debug.LogWarning("Can not add instance. Buffer is full.");
                    return;
                }
                prefabInstance.state = PrefabInstancingState.Instanced;
                runtimeData.instanceDataArray[runtimeData.instanceCount] = prefabInstance.transform.localToWorldMatrix;
                runtimeData.instanceCount++;
                prefabInstance.gpuInstancerID = runtimeData.instanceCount;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);

                SetRenderersEnabled(prefabInstance, false);
                runtimeData.modified = true;

                if (!_registeredPrefabsRuntimeData.ContainsKey(prefabInstance.prefabPrototype))
                    _registeredPrefabsRuntimeData.Add(prefabInstance.prefabPrototype, new List<GPUInstancerPrefab>());
                _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Add(prefabInstance);

                // variations
                if (_variationDataList != null)
                {
                    foreach (IPrefabVariationData pvd in _variationDataList)
                    {
                        if (pvd.GetPrototype() == prefabInstance.prefabPrototype)
                        {
                            pvd.SetInstanceData(prefabInstance);
                            pvd.SetBufferData(prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                        }
                    }
                }
            }
        }

        public void RemovePrefabInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                if (prefabInstance.gpuInstancerID > runtimeData.instanceDataArray.Length)
                {
                    Debug.LogWarning("Instance can not be removed.");
                    return;
                }
                List<GPUInstancerPrefab> prefabInstanceList = _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype];
                GPUInstancerPrefab lastIndexPrefabInstance = prefabInstanceList.Find(pi => pi.gpuInstancerID == runtimeData.instanceCount);

                prefabInstance.state = PrefabInstancingState.None;

                // exchange last index with this one
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = runtimeData.instanceDataArray[lastIndexPrefabInstance.gpuInstancerID - 1];
                // set last index data to Matrix4x4.zero
                runtimeData.instanceDataArray[lastIndexPrefabInstance.gpuInstancerID - 1] = Matrix4x4.zero;
                runtimeData.instanceCount--;

                prefabInstanceList.Remove(prefabInstance);

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, lastIndexPrefabInstance.gpuInstancerID - 1, lastIndexPrefabInstance.gpuInstancerID - 1, 1);

                lastIndexPrefabInstance.gpuInstancerID = prefabInstance.gpuInstancerID;

                SetRenderersEnabled(prefabInstance, true);
                runtimeData.modified = true;
                Destroy(prefabInstance);

                // variations
                if (_variationDataList != null)
                {
                    foreach (IPrefabVariationData pvd in _variationDataList)
                    {
                        if (pvd.GetPrototype() == lastIndexPrefabInstance.prefabPrototype)
                        {
                            pvd.SetInstanceData(lastIndexPrefabInstance);
                            pvd.SetBufferData(lastIndexPrefabInstance.gpuInstancerID - 1, lastIndexPrefabInstance.gpuInstancerID - 1, 1);
                        }
                    }
                }
            }
        }

        public void RegisterPrefabsInScene()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Registered prefabs changed");
#endif
            registeredPrefabs.Clear();
            foreach (GPUInstancerPrefabPrototype pp in prototypeList)
                registeredPrefabs.Add(new RegisteredPrefabsData(pp));

            GPUInstancerPrefab[] scenePrefabInstances = FindObjectsOfType<GPUInstancerPrefab>();
            foreach (GPUInstancerPrefab prefabInstance in scenePrefabInstances)
                AddRegisteredPrefab(prefabInstance);
        }

        public void RegisterPrefabInstanceList(List<GPUInstancerPrefab> prefabInstanceList)
        {
            foreach(GPUInstancerPrefab prefabInstance in prefabInstanceList)
            {
                _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Add(prefabInstance);
            }
        }

        public void DefinePrototypeVariationBuffer<T>(GPUInstancerPrefabPrototype prototype, string bufferName)
        {
            if (_variationDataList == null)
                _variationDataList = new List<IPrefabVariationData>();
            _variationDataList.Add(new PrefabVariationData<T>(prototype, bufferName));
        }
        
        #endregion API Methods

        public void AddRegisteredPrefab(GPUInstancerPrefab prefabInstance)
        {
            RegisteredPrefabsData data = registeredPrefabs.Find(rpd => rpd.prefabPrototype == prefabInstance.prefabPrototype);
            if (data != null)
                data.registeredPrefabs.Add(prefabInstance);
        }

        public void AddRuntimeRegisteredPrefab(GPUInstancerPrefab prefabInstance)
        {
            List<GPUInstancerPrefab> list;
            if (_registeredPrefabsRuntimeData.ContainsKey(prefabInstance.prefabPrototype))
                list = _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype];
            else
            {
                list = new List<GPUInstancerPrefab>();
                _registeredPrefabsRuntimeData.Add(prefabInstance.prefabPrototype, list);
            }

            if (!list.Contains(prefabInstance))
                list.Add(prefabInstance);
        }

        public void AddModificationCollider(GPUInstancerModificationCollider modificationCollider)
        {
            if (_modificationColliders == null)
                _modificationColliders = new List<GPUInstancerModificationCollider>();

            _modificationColliders.Add(modificationCollider);
        }

        public int GetEnabledPrefabCount()
        {
            int sum = 0;
            if(_modificationColliders != null)
            {
                for (int i = 0; i < _modificationColliders.Count; i++)
                    sum += _modificationColliders[i].GetEnteredInstanceCount();
            }
            return sum;
        }

        public Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>> GetRegisteredPrefabsRuntimeData()
        {
            return _registeredPrefabsRuntimeData;
        }
    }

    [Serializable]
    public class RegisteredPrefabsData
    {
        public GPUInstancerPrototype prefabPrototype;
        public List<GPUInstancerPrefab> registeredPrefabs;

        public RegisteredPrefabsData(GPUInstancerPrototype prefabPrototype)
        {
            this.prefabPrototype = prefabPrototype;
            registeredPrefabs = new List<GPUInstancerPrefab>();
        }
    }

    public interface IPrefabVariationData
    {
        void InitializeBufferAndArray(int count);
        void SetInstanceData(GPUInstancerPrefab prefabInstance);
        void SetBufferData(int managedBufferStartIndex, int computeBufferStartIndex, int count);
        void SetVariation(MaterialPropertyBlock mpb);
        GPUInstancerPrefabPrototype GetPrototype();
        void ReleaseBuffer();
    }

    public class PrefabVariationData<T> : IPrefabVariationData
    {
        public GPUInstancerPrefabPrototype prototype;
        public string bufferName;
        public ComputeBuffer variationBuffer;
        public T[] dataArray;
        public T defaultValue;

        public PrefabVariationData(GPUInstancerPrefabPrototype prototype, string bufferName, T defaultValue = default(T))
        {
            this.prototype = prototype;
            this.bufferName = bufferName;
            this.defaultValue = defaultValue;
        }

        public void InitializeBufferAndArray(int count)
        {
            dataArray = new T[count];
            for (int i = 0; i < count; i++)
            {
                dataArray[i] = defaultValue;
            }
            if (variationBuffer != null)
                variationBuffer.Release();
            variationBuffer = new ComputeBuffer(count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        }

        public void SetInstanceData(GPUInstancerPrefab prefabInstance)
        {
            if(prefabInstance.variationDataList != null && dataArray != null && prefabInstance.variationDataList.ContainsKey(bufferName) && dataArray.Length > prefabInstance.gpuInstancerID - 1)
                dataArray[prefabInstance.gpuInstancerID - 1] = (T)prefabInstance.variationDataList[bufferName];
        }

        public void SetBufferData(int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (variationBuffer != null)
            {
#if UNITY_2017_1_OR_NEWER
                variationBuffer.SetData(dataArray, managedBufferStartIndex, computeBufferStartIndex, count);
#else
                variationBuffer.SetData(dataArray);
#endif
            }
        }

        public void SetVariation(MaterialPropertyBlock mpb)
        {
            if(variationBuffer != null)
                mpb.SetBuffer(bufferName, variationBuffer);
        }

        public GPUInstancerPrefabPrototype GetPrototype()
        {
            return prototype;
        }

        public void ReleaseBuffer()
        {
            if (variationBuffer != null)
                variationBuffer.Release();
            variationBuffer = null;
            dataArray = null;
        }
    }
}