using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerTerrainSettings : ScriptableObject
    {
        public TerrainData terrainData;
        [Range(0f, 500f)]
        public float maxDetailDistance = 500F;
        public Vector2 windVector = new Vector2(0.4f, 0.8f);
        public Texture2D healthyDryNoiseTexture;
        public Texture2D windWaveNormalTexture;
        public bool autoSPCellSize = true;
        [Range(25, 500)]
        public int preferedSPCellSize = 125;
    }
}