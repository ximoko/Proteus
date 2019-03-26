using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class ColorVariations : MonoBehaviour
    {
        public GPUInstancerPrefab prefab;
        public GPUInstancerPrefabManager prefabManager;
        public int instances = 1000;

        private string bufferName = "colorBuffer";

        void Start()
        {
            List<GPUInstancerPrefab> goList = new List<GPUInstancerPrefab>();

            if (prefabManager != null && prefabManager.isActiveAndEnabled)
            {
                GPUInstancerAPI.DefinePrototypeVariationBuffer<Vector4>(prefabManager, prefab.prefabPrototype, bufferName);
            }

            for (int i = 0; i < instances; i++)
            {
                GPUInstancerPrefab prefabInstance = Instantiate(prefab);
                prefabInstance.transform.localPosition = Random.insideUnitSphere * 20;
                prefabInstance.transform.SetParent(transform);
                goList.Add(prefabInstance);

                prefabInstance.AddVariation(bufferName, (Vector4)Random.ColorHSV());
            }

            if (prefabManager != null && prefabManager.isActiveAndEnabled)
            {
                GPUInstancerAPI.RegisterPrefabInstanceList(prefabManager, goList);
                GPUInstancerAPI.InitializeGPUInstancer(prefabManager);
            }
        }
    }
}


