using UnityEngine;

namespace GPUInstancer
{

    public static class GPUInstancerConstants
    {
        // Compute buffer stride sizes
        public static readonly int STRIDE_SIZE_MATRIX4X4 = 4 * 4 * sizeof(float);  // = 4 * 4 * 4 = 64
        public static readonly int STRIDE_SIZE_BOOL = 4;
        public static readonly int STRIDE_SIZE_INT = 4;
        public static readonly int STRIDE_SIZE_FLOAT = 4;

        // Shader constants
        public static readonly string[] VISIBILITY_COMPUTE_KERNELS = new string[] { "CSInstancedRenderingVisibilityKernelLOD0", "CSInstancedRenderingVisibilityKernelLOD1", "CSInstancedRenderingVisibilityKernelLOD2", "CSInstancedRenderingVisibilityKernelLOD3", "CSInstancedRenderingVisibilityKernelLOD4",
                                                                                    "CSInstancedRenderingVisibilityKernelLOD0Shadow", "CSInstancedRenderingVisibilityKernelLOD1Shadow", "CSInstancedRenderingVisibilityKernelLOD2Shadow", "CSInstancedRenderingVisibilityKernelLOD3Shadow", "CSInstancedRenderingVisibilityKernelLOD4Shadow" };

        public static readonly string VISIBILITY_COMPUTE_RESOURCE_PATH = "Compute/CSInstancedRenderingVisibilityKernel";
        public static readonly int VISIBILITY_SHADER_THREAD_COUNT = 1024;
        public static readonly string TRANSFORMATION_MATRIX_APPEND_BUFFER = "gpuiTransformationMatrix";
        public static readonly string[] TRANSFORMATION_MATRIX_APPEND_LOD_BUFFERS = new string[] { "gpuiTransformationMatrix_LOD0", "gpuiTransformationMatrix_LOD1", "gpuiTransformationMatrix_LOD2", "gpuiTransformationMatrix_LOD3", "gpuiTransformationMatrix_LOD4" };
        public static readonly string INSTANCE_DATA_BUFFER = "gpuiInstanceData";
        public static readonly string RENDERER_TRANSFORM_OFFSET = "gpuiTransformOffset";
        public static readonly string BUFFER_PARAMETER_MVP_MATRIX = "mvpMartix";
        public static readonly string BUFFER_PARAMETER_BOUNDS_CENTER = "boundsCenter";
        public static readonly string BUFFER_PARAMETER_BOUNDS_EXTENTS = "boundsExtents";
        public static readonly string BUFFER_PARAMETER_FRUSTUM_CULL_SWITCH = "isFrustumCulling";
        public static readonly string BUFFER_PARAMETER_FRUSTUM_OFFSET = "frustumOffset";
        public static readonly string BUFFER_PARAMETER_MAX_VIEW_DISTANCE = "maxDistance";
        public static readonly string BUFFER_PARAMETER_CAMERA_POSITION = "camPos";
        public static readonly string BUFFER_PARAMETER_LOD_SIZES = "lodSizes";
        public static readonly string BUFFER_PARAMETER_FRUSTUM_HEIGHT = "frustumHeight";
        public static readonly int BUFFER_COROUTINE_STEP_NUMBER = 16384;
        public static readonly string TRANSFORMATION_MATRIX_APPEND_SHADOW_BUFFER = "gpuiTransformationMatrix_Shadow";
        public static readonly string BUFFER_PARAMETER_SHADOW_DISTANCE = "shadowDistance";
#if !UNITY_2017_1_OR_NEWER        
        // Compute Buffer Set Data Partial compute shader constants
        public static readonly string COMPUTE_SET_DATA_PARTIAL_RESOURCE_PATH = "Compute/CSInstancedComputeBufferSetDataPartialKernel";
        public static readonly string COMPUTE_SET_DATA_PARTIAL_KERNEL = "CSInstancedComputeBufferSetDataPartialKernel";
        public static readonly string COMPUTE_SET_DATA_SINGLE_KERNEL = "CSInstancedComputeBufferSetDataSingleKernel";
        public static readonly string BUFFER_PARAMETER_MANAGED_BUFFER_DATA = "gpuiManagedData";
        public static readonly string BUFFER_PARAMETER_COMPUTE_BUFFER_START_INDEX = "computeBufferStartIndex";
        public static readonly string BUFFER_PARAMETER_COUNT = "count";
        public static readonly string BUFFER_PARAMETER_DATA_TO_SET = "dataToSet";

        private static ComputeShader _computeBufferSetDataPartial;
        public static int computeBufferSetDataPartialKernelId;
        public static int computeBufferSetDataSingleKernelId;
        public static ComputeShader ComputeBufferSetDataPartial
        {
            get
            {
                if (_computeBufferSetDataPartial == null)
                {
                    _computeBufferSetDataPartial = Resources.Load<ComputeShader>(COMPUTE_SET_DATA_PARTIAL_RESOURCE_PATH);
                    if (_computeBufferSetDataPartial != null)
                    {
                        computeBufferSetDataPartialKernelId = _computeBufferSetDataPartial.FindKernel(COMPUTE_SET_DATA_PARTIAL_KERNEL);
                        computeBufferSetDataSingleKernelId = _computeBufferSetDataPartial.FindKernel(COMPUTE_SET_DATA_SINGLE_KERNEL);
                    }
                }
                return _computeBufferSetDataPartial;
            }
        }
#endif

        // Grass compute shader constants
        public static readonly string GRASS_INSTANTIATION_KERNEL = "CSInstancedRenderingGrassInstantiationKernel";
        public static readonly string GRASS_INSTANTIATION_RESOURCE_PATH = "Compute/CSInstancedRenderingGrassInstantiationKernel";
        public static readonly string DETAIL_MAP_DATA_BUFFER = "detailMapData";
        public static readonly string HEIGHT_MAP_DATA_BUFFER = "heightMapData";
        public static readonly string TERRAIN_DETAIL_RESOLUTION_DATA = "detailResolution";
        public static readonly string TERRAIN_HEIGHT_RESOLUTION_DATA = "heightResolution";
        public static readonly string GRASS_START_POSITION_DATA = "startPosition";
        public static readonly string TERRAIN_SIZE_DATA = "terrainSize";
        public static readonly string DETAIL_SCALE_DATA = "detailScale";
        public static readonly string DETAIL_AND_HEIGHT_MAP_SIZE_DATA = "detailAndHeightMapSize";
        public static readonly string HEALTHY_DRY_NOISE_TEXTURE = "healthyDryNoiseTexture";
        public static readonly string NOISE_SPREAD = "noiseSpread";
        public static readonly string DETAIL_UNIQUE_VALUE = "detailUniqueValue";
        public static readonly Vector3 GRASS_SHADER_THREAD_COUNT = new Vector3(32, 1, 32);

        // Unity Shader Names
        public static readonly string SHADER_UNITY_STANDARD = "Standard";
        public static readonly string SHADER_UNITY_STANDARD_SPECULAR = "Standard (Specular setup)";
        public static readonly string SHADER_UNITY_VERTEXLIT = "VertexLit";

        // Default GPU Instanced Shader Names
        public static readonly string SHADER_GPUI_STANDARD = "GPUInstancer/Standard";
        public static readonly string SHADER_GPUI_STANDARD_SPECULAR = "GPUInstancer/Standard (Specular setup)";
        public static readonly string SHADER_GPUI_VERTEXLIT = "GPUInstancer/VertexLit";
        public static readonly string SHADER_GPUI_FOLIAGE = "GPUInstancer/Foliage";
        public static readonly string SHADER_GPUI_SHADOWS_ONLY = "GPUInstancer/ShadowsOnly";

        // Debug
        public static readonly int DEBUG_INFO_SIZE = 105;

        // GPUInstancer Default Paths
        public static readonly string DEFAULT_PATH_GUID = "3ac41bd0ad94c784e83f5e717440e9ed";
        public static readonly string RESOURCES_PATH = "Resources/";
        public static readonly string SETTINGS_PATH = "Settings/";
        public static readonly string EDITOR_TEXTURES_PATH = "Textures/Editor/";
        public static readonly string NOISE_TEXTURES_PATH = "Textures/Noise/";
        public static readonly string SHADER_BINDINGS_DEFAULT_NAME = "GPUInstancerShaderBindings";
        public static readonly string PROTOTYPES_DETAIL_PATH = "PrototypeData/Detail/";
        public static readonly string PROTOTYPES_PREFAB_PATH = "PrototypeData/Prefab/";

        // Textures
        public static readonly string HELP_ICON = "help_gpui";
        public static readonly string HELP_ICON_ACTIVE = "help_gpui_active";
        public static readonly string DEFAULT_HEALTHY_DRY_NOISE = "Fractal_Simplex_grayscale";
        public static readonly string DEFAULT_WIND_WAVE_NOISE = "Fractal_Simplex_normal";



        private static string _defaultPath;
        public static string GetDefaultPath()
        {
            if (string.IsNullOrEmpty(_defaultPath))
            {
#if UNITY_EDITOR
                _defaultPath = UnityEditor.AssetDatabase.GUIDToAssetPath(DEFAULT_PATH_GUID);
                if (!string.IsNullOrEmpty(_defaultPath) && !_defaultPath.EndsWith("/"))
                    _defaultPath = _defaultPath + "/";
#endif
                if (string.IsNullOrEmpty(_defaultPath))
                    _defaultPath = "Assets/GPUInstancer/";
            }
            return _defaultPath;
        }
    }
}