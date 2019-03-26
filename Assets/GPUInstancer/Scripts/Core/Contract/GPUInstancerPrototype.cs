using System;
using UnityEngine;

namespace GPUInstancer
{
    [Serializable]
    public abstract class GPUInstancerPrototype : ScriptableObject
    {
        public GameObject prefabObject;

        public bool isShadowCasting = true;
        public bool isFrustumCulling = true;
        [Range(0.0f, 0.5f)] public float frustumOffset = 0.2f;
        [Range(0f, 2500f)] public float maxDistance = 500;

        public override string ToString()
        {
            if (prefabObject != null)
                return prefabObject.name;
            return base.ToString();
        }
    }

}
