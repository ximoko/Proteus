using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    /// <summary>
    /// Add this to the prefabs of GameObjects you want to GPU Instance at runtime.
    /// </summary>
    public class GPUInstancerPrefab : MonoBehaviour
    {
        [HideInInspector]
        public GPUInstancerPrefabPrototype prefabPrototype;
        [HideInInspector]
        public int gpuInstancerID;
        [HideInInspector]
        public PrefabInstancingState state = PrefabInstancingState.None;
        public Dictionary<string, object> variationDataList;

        public void AddVariation<T>(string bufferName, T value)
        {
            if (variationDataList == null)
                variationDataList = new Dictionary<string, object>();
            if (variationDataList.ContainsKey(bufferName))
                variationDataList[bufferName] = value;
            else
                variationDataList.Add(bufferName, value);
        }
    }

    public enum PrefabInstancingState
    {
        None,
        Disabled,
        Instanced
    }
}
