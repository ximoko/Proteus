using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{

    public abstract class GPUInstancerTerrainManager : GPUInstancerManager
    {
        [SerializeField]
        private Terrain _terrain;
        public Terrain terrain { get { return _terrain; } }
        public GPUInstancerTerrainSettings terrainSettings;
        protected bool replacingInstances;
        protected bool initalizingInstances;

        public override void OnDestroy()
        {
            base.OnDestroy();

            if(terrain != null && terrain.gameObject != null && terrain.GetComponent<GPUInstancerTerrainProxy>() != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(terrain.GetComponent<GPUInstancerTerrainProxy>());
                }
                else
                {
#if UNITY_EDITOR
                    Undo.RecordObject(terrain.gameObject, "Remove GPUInstancerTerrainProxy");
#endif
                    DestroyImmediate(terrain.GetComponent<GPUInstancerTerrainProxy>());
                }
            }
        }

        public virtual void SetupManagerWithTerrain(Terrain terrain)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Changed GPUInstancer Terrain Data for " + gameObject);
                if (_terrain != null && _terrain.GetComponent<GPUInstancerTerrainProxy>() != null)
                {
                    Undo.RecordObject(_terrain.gameObject, "Removed GPUInstancerTerrainProxy component");
                    DestroyImmediate(_terrain.GetComponent<GPUInstancerTerrainProxy>());
                }
            }
#endif

            _terrain = terrain;
            if (terrain != null)
            {
                if (terrainSettings != null)
                {
                    if (terrain.terrainData == terrainSettings.terrainData)
                        return;
                    else
                    {
                        prototypeList.Clear();
                        //RemoveTerrainSettings(terrainSettings);
                        terrainSettings = null;
                    }
                }
                terrainSettings = GenerateTerrainSettings(terrain, gameObject);
                GeneratePrototypes(false);
                if (!Application.isPlaying)
                    AddProxyToTerrain();
            }
            else
            {
                prototypeList.Clear();
                //RemoveTerrainSettings(terrainSettings);
                terrainSettings = null;
            }
        }

        public void AddProxyToTerrain()
        {
#if UNITY_EDITOR
            if (terrain != null)
            {
                GPUInstancerTerrainProxy terrainProxy = terrain.GetComponent<GPUInstancerTerrainProxy>();
                if (terrainProxy == null)
                {
                    Undo.RecordObject(terrain.gameObject, "Added GPUInstancerTerrainProxy component");
                    terrainProxy = terrain.gameObject.AddComponent<GPUInstancerTerrainProxy>();
                }
                if (this is GPUInstancerDetailManager && terrainProxy.detailManager != this)
                    terrainProxy.detailManager = (GPUInstancerDetailManager)this;
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(terrainProxy)) ;

            }
#endif
        }

        private GPUInstancerTerrainSettings GenerateTerrainSettings(Terrain terrain, GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string[] guids = AssetDatabase.FindAssets("t:GPUInstancerTerrainSettings");
                for (int i = 0; i < guids.Length; i++)
                {
                    GPUInstancerTerrainSettings ts = AssetDatabase.LoadAssetAtPath<GPUInstancerTerrainSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (ts != null && ts.terrainData == terrain.terrainData)
                    {
                        prototypeList.Clear();
                        if (this is GPUInstancerDetailManager)
                        {
                            GPUInstancerUtility.SetPrototypeListFromAssets(ts, prototypeList, typeof(GPUInstancerDetailPrototype));
                            prototypeList.Sort(SortDetailPrototypes);
                        }
                        return ts;
                    }
                }
            }
#endif

            GPUInstancerTerrainSettings terrainSettings = ScriptableObject.CreateInstance<GPUInstancerTerrainSettings>();
            terrainSettings.name = terrain.terrainData.name + "_" + terrain.terrainData.GetInstanceID();
            terrainSettings.terrainData = terrain.terrainData;
            terrainSettings.maxDetailDistance = terrain.detailObjectDistance;
            terrainSettings.healthyDryNoiseTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_HEALTHY_DRY_NOISE);
            terrainSettings.windWaveNormalTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_WIND_WAVE_NOISE);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string assetPath = GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_DETAIL_PATH + terrainSettings.name + ".asset";

                if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_DETAIL_PATH))
                {
                    System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_DETAIL_PATH);
                }

                AssetDatabase.CreateAsset(terrainSettings, assetPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
            return terrainSettings;
        }

        private int SortDetailPrototypes(GPUInstancerPrototype x, GPUInstancerPrototype y)
        {
            return ((GPUInstancerDetailPrototype)x).detailPrototypeIndex.CompareTo(((GPUInstancerDetailPrototype)y).detailPrototypeIndex);
        }

        private static void RemoveTerrainSettings(GPUInstancerTerrainSettings terrainSettings)
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(terrainSettings));
#endif
        }

#if UNITY_EDITOR
        public void ShowTerrainPicker()
        {
            EditorGUIUtility.ShowObjectPicker<Terrain>(null, true, null, pickerControlID);
        }

        public void AddTerrainPickerObject(UnityEngine.Object pickerObject)
        {
            if (pickerObject == null)
                return;

            if (pickerObject is GameObject)
            {
                GameObject go = (GameObject)pickerObject;
                if(go.GetComponent<Terrain>() != null)
                {
                    SetupManagerWithTerrain(go.GetComponent<Terrain>());
                }
            }
        }
#endif
    }
}