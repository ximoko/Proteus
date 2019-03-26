#define DEBUG_MODE_OFF

/*
 * VLight
 * Copyright Brian Su 2011
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using VLights;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class VLight : MonoBehaviour
{
    public enum ShadowMode
    {
        None,
        Realtime,
        Baked
    }

    public enum LightTypes
    {
        Spot,
        Point
    }
	
	[SerializeField][HideInInspector]
    private Material spotMaterial;
	[SerializeField][HideInInspector]
    private Material pointMaterial;
	[SerializeField][HideInInspector]
    private Shader renderDepthShader;	
	
    [HideInInspector]
    public bool lockTransforms = false;	
	[HideInInspector]
	public bool renderWireFrame = true;
	//
    public LightTypes lightType;
    public float pointLightRadius = 1;	
	public float spotRange = 1;
	public float spotNear = 1;
	public float spotAngle = 45;
    public ShadowMode shadowMode;
    public int slices = 30;

	[SerializeField]
	private Color colorTint = Color.white;
	[SerializeField]
	private float lightMultiplier = 1;
	[SerializeField]
	private float spotExponent = 1;
	[SerializeField]
	private float constantAttenuation = 1;
	[SerializeField]
	private float linearAttenuation = 10;
	[SerializeField]
	private float quadraticAttenuation = 100;
	[SerializeField]
    private Vector3 noiseSpeed;	
	//
	[SerializeField][HideInInspector]
	private Texture spotEmission;
	[SerializeField][HideInInspector]
	private Texture spotNoise;
	[SerializeField][HideInInspector]
	private Texture spotShadow;
	[SerializeField][HideInInspector]
	private Cubemap pointEmission;
	[SerializeField][HideInInspector]
	private Cubemap pointNoise;
	[SerializeField][HideInInspector]
	private Cubemap pointShadow;
	[HideInInspector][SerializeField]
    private Mesh meshContainer;
	[HideInInspector][SerializeField]
    private Material _prevMaterialSpot;
	[HideInInspector][SerializeField]
    private Material _prevMaterialPoint;
	[HideInInspector][SerializeField]
    public Material _instancedSpotMaterial;
	[HideInInspector][SerializeField]
    public Material _instancedPointMaterial;
	
	private MaterialPropertyBlock _propertyBlock;
	private int _idColorTint = 0;
	private int _idLightMultiplier = 0;
	private int _idSpotExponent = 0;
	private int _idConstantAttenuation = 0;
	private int _idLinearAttenuation = 0;
	private int _idQuadraticAttenuation = 0;
	private int _idLightParams = 0;
	private int _idMinBounds = 0;
	private int _idMaxBounds = 0;
	private int _idViewWorldLight = 0;
	private int _idRotation = 0;
	private int _idLocalRotation = 0;
	private int _idProjection = 0;
    private LightTypes _prevLightType;
    private ShadowMode _prevShadowMode;

    private int _prevSlices;
    private bool _frustrumSwitch;
    private bool _prevIsOrtho;

    private float _prevNear;
    private float _prevFar;
    private float _prevFov;
    private float _prevOrthoSize;
    private float _prevPointLightRadius;

    private Matrix4x4 _worldToCamera;
    private Matrix4x4 _projectionMatrixCached;
    private Matrix4x4 _viewWorldToCameraMatrixCached;
    private Matrix4x4 _viewCameraToWorldMatrixCached;
    private Matrix4x4 _localToWorldMatrix;
    private Matrix4x4 _rotation;
    private Matrix4x4 _localRotation;
    private Matrix4x4 _viewWorldLight;

    private Vector3[] _frustrumPoints;
    private Vector3 _angle = Vector3.zero;

    private Vector3 _minBounds, _maxBounds;
    private bool _cameraHasBeenUpdated = false;
    private MeshFilter _meshFilter;
    private RenderTexture _depthTexture;
 
    private const int VERT_COUNT = 65000;
    private const int TRI_COUNT = VERT_COUNT * 3;
    private const System.StringComparison STR_CMP_TYPE = System.StringComparison.OrdinalIgnoreCase;

    private int _maxSlices;
    public int MaxSlices
    {
        get { return _maxSlices; }
        set { _maxSlices = value; }
    }	

    public void OnEnable()
    {
#if DEBUG_MODE
        Debug.Log("Enable V-light");
#endif
        _maxSlices = slices;

        int layer = LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME);
        if (layer != -1)
        {
            gameObject.layer = layer;
        }

        GetComponent<Camera>().enabled = false;
        GetComponent<Camera>().cullingMask &= ~(1 << gameObject.layer);


        CreateMaterials();
    }

    private void OnApplicationQuit()
    {
#if DEBUG_MODE
        Debug.Log("App Quit V-light");
#endif
    }

    private void OnDestroy()
    {
#if DEBUG_MODE
        Debug.Log("Destroy V-light");
#endif
        CleanMaterials();
        SafeDestroy(_depthTexture);
        SafeDestroy(meshContainer);
    }

    private void Start()
    {
#if DEBUG_MODE
        Debug.Log("Start V-light");
#endif
        CreateMaterials();

		spotNear = GetComponent<Camera>().nearClipPlane;
		spotRange = GetComponent<Camera>().farClipPlane;
		spotAngle = GetComponent<Camera>().fieldOfView;
    }

    public void Reset()
    {
#if DEBUG_MODE
        Debug.Log("Reset V-light");
#endif
        CleanMaterials();
        SafeDestroy(_depthTexture);
        SafeDestroy(meshContainer);
    }

    public bool GenerateNewMaterial(Material originalMaterial, ref Material instancedMaterial)
    {
        string id = GetInstanceID().ToString();

        if (originalMaterial != null && (
            instancedMaterial == null ||
            instancedMaterial.name.IndexOf(id, STR_CMP_TYPE) < 0 ||
            instancedMaterial.name.IndexOf(originalMaterial.name, STR_CMP_TYPE) < 0))
        {
            if (!originalMaterial.shader.isSupported)
            {
                Debug.LogError("Volumetric light shader not supported");
                enabled = false;
                return false;

            }

            Material sourceMaterial = originalMaterial;
            //We are cloning the material from another light
            if (instancedMaterial != null && instancedMaterial.name.IndexOf(originalMaterial.name, STR_CMP_TYPE) > 0)
            {
#if DEBUG_MODE
                Debug.Log("Create clone of material ");
#endif
                sourceMaterial = instancedMaterial;
            }
            else
            {
#if DEBUG_MODE
                Debug.Log("Create new point V-light mat ");
#endif
            }

            instancedMaterial = new Material(sourceMaterial);
            instancedMaterial.name = id + " " + originalMaterial.name;
        }

        return true;
    }

    public void CreateMaterials()
    {
		_propertyBlock = new MaterialPropertyBlock();


		_idColorTint = Shader.PropertyToID("_Color");
		_idLightMultiplier = Shader.PropertyToID("_Strength");
		_idSpotExponent = Shader.PropertyToID("_SpotExp");
		_idConstantAttenuation = Shader.PropertyToID("_ConstantAttn");
		_idLinearAttenuation = Shader.PropertyToID("_LinearAttn");
		_idQuadraticAttenuation = Shader.PropertyToID("_QuadAttn");
		_idLightParams = Shader.PropertyToID("_LightParams");
		_idMinBounds = Shader.PropertyToID("_minBounds");
		_idMaxBounds = Shader.PropertyToID("_maxBounds");
		_idViewWorldLight = Shader.PropertyToID("_ViewWorldLight");
		_idLocalRotation = Shader.PropertyToID("_LocalRotation");
		_idRotation = Shader.PropertyToID("_Rotation");
		_idProjection = Shader.PropertyToID("_Projection");

		Material mat = (lightType == LightTypes.Spot) ? _instancedSpotMaterial : _instancedPointMaterial;
		if(mat == null)
		{
			mat = (lightType == LightTypes.Spot) ? spotMaterial : pointMaterial;
		}
		
#if UNITY_EDITOR
		if(mat != null && (mat.shader.name == "V-Light/Spot" || mat.shader.name == "V-Light/Point"))
		{
			colorTint = mat.GetColor("_Color");
			spotExponent = mat.GetFloat("_SpotExp");
			constantAttenuation = mat.GetFloat("_ConstantAttn");
			linearAttenuation = mat.GetFloat("_LinearAttn");
			quadraticAttenuation = mat.GetFloat("_QuadAttn");
			
			if(mat.shader.name == "V-Light/Spot")
			{
				mat.shader = Shader.Find("V-Light/Spot Version 2");
			}
			
			if(mat.shader.name == "V-Light/Point")
			{
				mat.shader = Shader.Find("V-Light/Point Version 2");
			}
		}
#endif

		if(_instancedSpotMaterial != null)
		{
			spotEmission = _instancedSpotMaterial.GetTexture("_LightColorEmission");
			spotNoise = _instancedSpotMaterial.GetTexture("_NoiseTex");
			spotShadow = _instancedSpotMaterial.GetTexture("_ShadowTexture");
		}

		if(_instancedPointMaterial != null)
		{
			pointEmission = _instancedPointMaterial.GetTexture("_LightColorEmission") as Cubemap;
			pointNoise = _instancedPointMaterial.GetTexture("_NoiseTex") as Cubemap;
			pointShadow = _instancedPointMaterial.GetTexture("_ShadowTexture") as Cubemap;
		}

        bool createdMaterial = false;
        createdMaterial |= GenerateNewMaterial(pointMaterial, ref _instancedPointMaterial);
        createdMaterial |= GenerateNewMaterial(spotMaterial, ref _instancedSpotMaterial);
        if (createdMaterial)
        {
            switch (lightType)
            {
                case LightTypes.Point:
                    GetComponent<Renderer>().sharedMaterial = _instancedPointMaterial;
					if(pointEmission != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_LightColorEmission", pointEmission);
					}
					if(pointNoise != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_NoiseTex", pointNoise);
					}
					if(pointShadow != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_ShadowTexture", pointShadow);
					}
                    break;
                case LightTypes.Spot:
                    GetComponent<Renderer>().sharedMaterial = _instancedSpotMaterial;
					if(spotEmission != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_LightColorEmission", spotEmission);
					}
					if(spotNoise != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_NoiseTex", spotNoise);
					}
					if(spotShadow != null)
					{
						GetComponent<Renderer>().sharedMaterial.SetTexture("_ShadowTexture", spotShadow);
					}
                    break;
            }

		}
    }

    private void CleanMaterials()
    {
        SafeDestroy(_instancedSpotMaterial);
        SafeDestroy(_instancedPointMaterial);
        SafeDestroy(GetComponent<Renderer>().sharedMaterial);
        SafeDestroy(meshContainer);

        _prevMaterialPoint = null;
        _prevMaterialSpot = null;
        _instancedSpotMaterial = null;
        _instancedPointMaterial = null;
        meshContainer = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_frustrumPoints == null)
        {
            return;
        }

        Gizmos.color = new Color(0, 1, 0, 0.2f);

        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[0]), transform.TransformPoint(_frustrumPoints[1]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[2]), transform.TransformPoint(_frustrumPoints[3]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[4]), transform.TransformPoint(_frustrumPoints[5]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[6]), transform.TransformPoint(_frustrumPoints[7]));

        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[1]), transform.TransformPoint(_frustrumPoints[3]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[3]), transform.TransformPoint(_frustrumPoints[7]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[7]), transform.TransformPoint(_frustrumPoints[5]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[5]), transform.TransformPoint(_frustrumPoints[1]));

        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[0]), transform.TransformPoint(_frustrumPoints[2]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[2]), transform.TransformPoint(_frustrumPoints[6]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[6]), transform.TransformPoint(_frustrumPoints[4]));
        Gizmos.DrawLine(transform.TransformPoint(_frustrumPoints[4]), transform.TransformPoint(_frustrumPoints[0]));
    }

    private void CalculateMinMax(out Vector3 min, out Vector3 max, bool forceFrustrumUpdate)
    {
        if (_frustrumPoints == null || forceFrustrumUpdate)
        {
            VLightGeometryUtil.RecalculateFrustrumPoints(GetComponent<Camera>(), 1.0f, out _frustrumPoints);
        }

        Vector3[] pointsViewSpace = new Vector3[8];
        Vector3 vecMinBounds = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        Vector3 vecMaxBounds = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Matrix4x4 m = _viewWorldToCameraMatrixCached * _localToWorldMatrix;
        for (int i = 0; i < _frustrumPoints.Length; i++)
        {
            pointsViewSpace[i] = m.MultiplyPoint((_frustrumPoints[i]));

            vecMinBounds.x = (vecMinBounds.x > pointsViewSpace[i].x) ? vecMinBounds.x : pointsViewSpace[i].x;
            vecMinBounds.y = (vecMinBounds.y > pointsViewSpace[i].y) ? vecMinBounds.y : pointsViewSpace[i].y;
            vecMinBounds.z = (vecMinBounds.z > pointsViewSpace[i].z) ? vecMinBounds.z : pointsViewSpace[i].z;

            vecMaxBounds.x = (vecMaxBounds.x <= pointsViewSpace[i].x) ? vecMaxBounds.x : pointsViewSpace[i].x;
            vecMaxBounds.y = (vecMaxBounds.y <= pointsViewSpace[i].y) ? vecMaxBounds.y : pointsViewSpace[i].y;
            vecMaxBounds.z = (vecMaxBounds.z <= pointsViewSpace[i].z) ? vecMaxBounds.z : pointsViewSpace[i].z;
        }

        min = vecMinBounds;
        max = vecMaxBounds;
    }

    private Matrix4x4 CalculateProjectionMatrix()
    {
        Matrix4x4 projectionMatrix;
        if (!GetComponent<Camera>().orthographic)
        {
            projectionMatrix = Matrix4x4.Perspective(GetComponent<Camera>().fieldOfView, 1.0f, GetComponent<Camera>().nearClipPlane, GetComponent<Camera>().farClipPlane);
        }
        else
        {
			float halfOrtho = GetComponent<Camera>().orthographicSize * 0.5f;
            projectionMatrix = Matrix4x4.Ortho(
				-halfOrtho,
				 halfOrtho,
				-halfOrtho,
				 halfOrtho,
				 GetComponent<Camera>().farClipPlane, GetComponent<Camera>().nearClipPlane);
        }
        return projectionMatrix;
    }

    private void BuildMesh(bool manualPositioning, int planeCount, Vector3 minBounds, Vector3 maxBounds)
    {
        if (meshContainer == null || meshContainer.name.IndexOf(GetInstanceID().ToString(), System.StringComparison.OrdinalIgnoreCase) != 0)
        {
#if DEBUG_MODE
            Debug.Log("Creating new mesh container");
#endif
            meshContainer = new Mesh();
            meshContainer.hideFlags = HideFlags.HideAndDontSave;
            meshContainer.name = GetInstanceID().ToString();
        }

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        Vector3[] vertBucket = new Vector3[VERT_COUNT];
        int[] triBucket = new int[TRI_COUNT];
        int vertBucketCount = 0;
        int triBucketCount = 0;

        float depthOffset = 1.0f / (float)(planeCount - 1);
        float depth = (manualPositioning) ? 1f : 0f;
        float xLeft = 0f;
        float xRight = 1f;
        float xBottom = 0f;
        float xTop = 1f;

        int vertOffset = 0;
        for (int i = 0; i < planeCount; i++)
        {
            Vector3[] verts = new Vector3[4];
            Vector3[] results;

            if (manualPositioning)
            {
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_projectionMatrixCached * GetComponent<Camera>().worldToCameraMatrix);

                for (int j = 0; j < planes.Length; j++)
                {
                    Vector3 centre = planes[j].normal * -planes[j].distance;
                    planes[j] = new Plane(_viewWorldToCameraMatrixCached.MultiplyVector(planes[j].normal), _viewWorldToCameraMatrixCached.MultiplyPoint3x4(centre));
                }

                verts[0] = CalculateTriLerp(new Vector3(xLeft, xBottom, depth), minBounds, maxBounds);
                verts[1] = CalculateTriLerp(new Vector3(xLeft, xTop, depth), minBounds, maxBounds);
                verts[2] = CalculateTriLerp(new Vector3(xRight, xTop, depth), minBounds, maxBounds);
                verts[3] = CalculateTriLerp(new Vector3(xRight, xBottom, depth), minBounds, maxBounds);
                results = VLightGeometryUtil.ClipPolygonAgainstPlane(verts, planes);
            }
            else
            {
                verts[0] = new Vector3(xLeft, xBottom, depth);
                verts[1] = new Vector3(xLeft, xTop, depth);
                verts[2] = new Vector3(xRight, xTop, depth);
                verts[3] = new Vector3(xRight, xBottom, depth);
                results = verts;
            }
            depth += (manualPositioning) ? -depthOffset : depthOffset;

            if (results.Length > 2)
            {
                Array.Copy(results, 0, vertBucket, vertBucketCount, results.Length);
                vertBucketCount += results.Length;

                int[] tris = new int[(results.Length - 2) * 3];
                int vertOff = 0;
                for (int j = 0; j < tris.Length; j += 3)
                {
                    tris[j + 0] = vertOffset + 0;
                    tris[j + 1] = vertOffset + (vertOff + 1);
                    tris[j + 2] = vertOffset + (vertOff + 2);
                    vertOff++;
#if DEBUG_MODE
                    Color lightBlue = new Color(0, 0, 1, 0.05f);
                    Matrix4x4 cameraToWorld = _viewCameraToWorldMatrixCached;
                    Debug.DrawLine(cameraToWorld.MultiplyPoint(vertBucket[tris[j + 0]]), cameraToWorld.MultiplyPoint(vertBucket[tris[j + 1]]), lightBlue);
                    Debug.DrawLine(cameraToWorld.MultiplyPoint(vertBucket[tris[j + 1]]), cameraToWorld.MultiplyPoint(vertBucket[tris[j + 2]]), lightBlue);
                    Debug.DrawLine(cameraToWorld.MultiplyPoint(vertBucket[tris[j + 2]]), cameraToWorld.MultiplyPoint(vertBucket[tris[j + 0]]), lightBlue);
#endif
                }
                vertOffset += results.Length;
                Array.Copy(tris, 0, triBucket, triBucketCount, tris.Length);
                triBucketCount += tris.Length;
            }
        }
        meshContainer.Clear();

        Vector3[] newVerts = new Vector3[vertBucketCount];
        Array.Copy(vertBucket, newVerts, vertBucketCount);
        meshContainer.vertices = newVerts;

        int[] newTris = new int[triBucketCount];
        Array.Copy(triBucket, newTris, triBucketCount);
        meshContainer.triangles = newTris;
        meshContainer.normals = new Vector3[vertBucketCount];
        meshContainer.uv = new Vector2[vertBucketCount];

        Vector3 centrePT = Vector3.zero;
        foreach (var vert in _frustrumPoints)
        {
            centrePT += vert;
        }
        centrePT /= _frustrumPoints.Length;

        Bounds localBounds = new Bounds(centrePT, Vector3.zero);
        foreach (var vert in _frustrumPoints)
        {
            localBounds.Encapsulate(vert);
        }

        _meshFilter.sharedMesh = meshContainer;
        _meshFilter.sharedMesh.bounds = localBounds;
    }

    private Vector3 CalculateTriLerp(Vector3 vertex, Vector3 minBounds, Vector3 maxBounds)
    {
        Vector3 triLerp = new Vector3(1, 1, 1) - vertex;
        Vector3 result =
            new Vector3(minBounds.x * vertex.x, minBounds.y * vertex.y, maxBounds.z * vertex.z) +
            new Vector3(maxBounds.x * triLerp.x, maxBounds.y * triLerp.y, minBounds.z * triLerp.z);
        return result;
    }

    private void RenderShadowMap()
    {
        switch (shadowMode)
        {
            case ShadowMode.None:
                break;
            case ShadowMode.Baked:
                break;
            case ShadowMode.Realtime:
                if (SystemInfo.supportsImageEffects)
                {
                    int layer = LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME);
                    if (layer != -1)
                    {
                        gameObject.layer = layer;
                        GetComponent<Camera>().backgroundColor = Color.red;
                        GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                        GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;
                        GetComponent<Camera>().renderingPath = RenderingPath.VertexLit;

                        CreateDepthTexture(lightType);

						if(renderDepthShader == null)
						{
							renderDepthShader = Shader.Find(VLightShaderUtil.DEPTH_SHADER_NAME);
						}

                        switch (lightType)
                        {
                            case LightTypes.Spot:
                                GetComponent<Camera>().targetTexture = _depthTexture;
                                GetComponent<Camera>().projectionMatrix = CalculateProjectionMatrix();
                                GetComponent<Camera>().RenderWithShader(renderDepthShader, "RenderType");
                                break;
                            case LightTypes.Point:
                              	GetComponent<Camera>().projectionMatrix = Matrix4x4.Perspective(90, 1.0f, 0.1f, GetComponent<Camera>().farClipPlane);
                                GetComponent<Camera>().SetReplacementShader(renderDepthShader, "RenderType");
                                GetComponent<Camera>().RenderToCubemap(_depthTexture, 63);
                                GetComponent<Camera>().ResetReplacementShader();
                                break;
                        }
                    }
                }
                break;
        }
    }

    private RenderTexture GenerateShadowMap(int resX, int resY)
    {
#if UNITY_3_4
        return new RenderTexture(256, 256, 1);
#else
        return new RenderTexture(256, 256, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
#endif
    }

    private void CreateDepthTexture(LightTypes type)
    {
        if (_depthTexture == null)
        {
#if DEBUG_MODE
            Debug.Log("Creating new depth texture");
#endif
            _depthTexture = GenerateShadowMap(256, 256);
            _depthTexture.hideFlags = HideFlags.HideAndDontSave;
            _depthTexture.isPowerOfTwo = true;
            switch (type)
            {
                case LightTypes.Point:
                    _depthTexture.isCubemap = true;
                    break;
            }
        }
        else if (type == LightTypes.Point && !_depthTexture.isCubemap && _depthTexture.IsCreated())
        {
#if DEBUG_MODE
            Debug.Log("Swapping to cubemap depth texture");
#endif
            SafeDestroy(_depthTexture);
            _depthTexture = GenerateShadowMap(256, 256);

            _depthTexture.hideFlags = HideFlags.HideAndDontSave;
            _depthTexture.isPowerOfTwo = true;
            _depthTexture.isCubemap = true;
        }
        else if (type == LightTypes.Spot && _depthTexture.isCubemap && _depthTexture.IsCreated())
        {
#if DEBUG_MODE
            Debug.Log("Swapping to non cubemap depth texture");
#endif
            SafeDestroy(_depthTexture);
            _depthTexture = GenerateShadowMap(512, 512);
            _depthTexture.hideFlags = HideFlags.HideAndDontSave;
            _depthTexture.isPowerOfTwo = true;
            _depthTexture.isCubemap = false;
        }
    }

    private void LateUpdate()
    {
        // Playing use custom or main camera
        if (Application.isPlaying)
        {
            if (VLightManager.Instance.targetCamera != null)
            {
                UpdateViewMatrices(VLightManager.Instance.targetCamera);
            }
            else
            {
				if(Camera.main != null)
				{
                	UpdateViewMatrices(Camera.main);
				}
            }
        }
        else
        {
            // Use editor only
            if (Camera.current != null)
            {
                UpdateViewMatrices(Camera.current);
            }
        }

        // Render any shadow maps
        RenderShadowMap();
    }

#if DEBUG_MODE
    private bool _hasCalledUpdate = false;
#endif

    //Main loop to render everything
    private void OnWillRenderObject()
    {
        if (spotMaterial == null || pointMaterial == null || _instancedSpotMaterial == null || _instancedPointMaterial == null)
        {
            Debug.Log("Materials not initialized");
            CreateMaterials();
            return;
        }
		
		

#if DEBUG_MODE
        if (!_hasCalledUpdate)
        {
            _hasCalledUpdate = true;
            Debug.Log("Will Render");
        }
#endif
        if (!lockTransforms)
        {
            UpdateSettings();
            UpdateLightMatrices();

            // Playing use custom or main camera
            if (Application.isPlaying)
            {
                if (VLightManager.Instance.targetCamera != null)
                {
                    UpdateViewMatrices(VLightManager.Instance.targetCamera);
                }
                else
                {
                    UpdateViewMatrices(Camera.main);
                }
            }
            else
            {
                // Use editor only
                if (Camera.current != null)
                {
                    UpdateViewMatrices(Camera.current);
                }
            }

            //SetShaderProperties();
			SetShaderPropertiesBlock();
        }
    }

    private bool CameraHasBeenUpdated()
    {
        bool hasBeenUpdated = false;
        hasBeenUpdated |= _meshFilter == null || _meshFilter.sharedMesh == null;
        hasBeenUpdated |= spotRange != _prevFar;
        hasBeenUpdated |= spotNear != _prevNear;
        hasBeenUpdated |= spotAngle != _prevFov;
        hasBeenUpdated |= GetComponent<Camera>().orthographicSize != _prevOrthoSize;
        hasBeenUpdated |= GetComponent<Camera>().orthographic != _prevIsOrtho;
        hasBeenUpdated |= pointLightRadius != _prevPointLightRadius;

        hasBeenUpdated |= spotMaterial != _prevMaterialSpot;
        hasBeenUpdated |= pointMaterial != _prevMaterialPoint;

        hasBeenUpdated |= _prevSlices != slices;
        hasBeenUpdated |= _prevShadowMode != shadowMode;
        hasBeenUpdated |= _prevLightType != lightType;
        return hasBeenUpdated;
    }

    public void UpdateSettings()
    {
        _cameraHasBeenUpdated = CameraHasBeenUpdated();
        if (_cameraHasBeenUpdated)
        {
            switch (lightType)
            {
                case LightTypes.Point:
                    GetComponent<Renderer>().sharedMaterial = _instancedPointMaterial;
                    GetComponent<Camera>().orthographic = true;
                    GetComponent<Camera>().nearClipPlane = -pointLightRadius;
                    GetComponent<Camera>().farClipPlane = pointLightRadius;
                    GetComponent<Camera>().orthographicSize = pointLightRadius * 2.0f;
                    break;
                case LightTypes.Spot:
                    GetComponent<Renderer>().sharedMaterial = _instancedSpotMaterial;
					GetComponent<Camera>().farClipPlane = spotRange;
					GetComponent<Camera>().nearClipPlane = spotNear;
					GetComponent<Camera>().fieldOfView = spotAngle;
                    GetComponent<Camera>().orthographic = false;
                    break;
            }

            if (shadowMode == ShadowMode.None || shadowMode == ShadowMode.Baked)
            {
                if (_depthTexture != null)
                {
                    SafeDestroy(_depthTexture);
                }
            }
        }

        _prevSlices = slices;
        _prevFov = GetComponent<Camera>().fieldOfView;
        _prevNear = GetComponent<Camera>().nearClipPlane;
        _prevFar = GetComponent<Camera>().farClipPlane;
        _prevIsOrtho = GetComponent<Camera>().orthographic;
        _prevOrthoSize = GetComponent<Camera>().orthographicSize;
        _prevMaterialSpot = spotMaterial;
        _prevMaterialPoint = pointMaterial;
        _prevShadowMode = shadowMode;
        _prevLightType = lightType;
        _prevPointLightRadius = pointLightRadius;
    }

    public void UpdateLightMatrices()
    {
        _localToWorldMatrix = transform.localToWorldMatrix;
        _worldToCamera = GetComponent<Camera>().worldToCameraMatrix;

        _rotation = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(_angle.x, _angle.y, _angle.z), Vector3.one);
        _angle += noiseSpeed * Time.deltaTime;

        RebuildMesh();
    }

    public void UpdateViewMatrices(Camera targetCamera)
    {
        _viewWorldToCameraMatrixCached = targetCamera.worldToCameraMatrix;
        _viewCameraToWorldMatrixCached = targetCamera.cameraToWorldMatrix;

        switch (lightType)
        {
            case LightTypes.Spot:
                _viewWorldLight = _worldToCamera * _viewCameraToWorldMatrixCached;
                break;
            case LightTypes.Point:
                Matrix4x4 origin = Matrix4x4.TRS(-transform.position, Quaternion.identity, Vector3.one);
                _localRotation = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);
                _viewWorldLight = origin * _viewCameraToWorldMatrixCached;
                break;
        }
    }

    public void SetInterleavedOffset(float value)
    {
        GetComponent<Renderer>().sharedMaterial.SetFloat("_Offset", value);
    }

    public void RebuildMesh()
    {
        CalculateMinMax(out _minBounds, out _maxBounds, _cameraHasBeenUpdated);

        // Build the mesh if we have modified the parameters
        if (_cameraHasBeenUpdated)
        {
            _projectionMatrixCached = CalculateProjectionMatrix();
            CreateMaterials();
            BuildMesh(false, slices, _minBounds, _maxBounds);
        }
    }

    public void SetShaderPropertiesBlock()
    {
		MaterialPropertyBlock material = _propertyBlock;
        material.SetVector(_idMinBounds, _minBounds);
        material.SetVector(_idMaxBounds, _maxBounds);
        material.SetMatrix(_idProjection, _projectionMatrixCached);
        material.SetMatrix(_idViewWorldLight, _viewWorldLight);
        material.SetMatrix(_idLocalRotation, _localRotation);
        material.SetMatrix(_idRotation, _rotation);
		material.SetColor(_idColorTint, colorTint);
		material.SetFloat(_idSpotExponent, spotExponent);
		material.SetFloat(_idConstantAttenuation, constantAttenuation);
		material.SetFloat(_idLinearAttenuation, linearAttenuation);
		material.SetFloat(_idQuadraticAttenuation, quadraticAttenuation);
		material.SetFloat(_idLightMultiplier, lightMultiplier);
		
//        Plane[] frustrumPLanes = GeometryUtility.CalculateFrustumPlanes(_projectionMatrixCached);
//        switch (lightType)
//        {
//            case LightTypes.Point:
//                for (int i = 0; i < frustrumPLanes.Length; i++)
//                {
//                    Vector3 planeNormal = transform.TransformDirection(frustrumPLanes[i].normal);
//                    float distance = frustrumPLanes[i].distance;
//                    material.AddVector("_FrustrumPlane" + i, new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, distance));
//                }
//                break;
//            case LightTypes.Spot:
//                for (int i = 0; i < frustrumPLanes.Length; i++)
//                {
//                    Vector3 planeNormal = frustrumPLanes[i].normal;
//                    float distance = frustrumPLanes[i].distance;
//                    material.AddVector("_FrustrumPlane" + i, new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, distance));
//                }
//                break;
//        }

        switch (shadowMode)
        {
            case ShadowMode.Realtime:
                GetComponent<Renderer>().sharedMaterial.SetTexture("_ShadowTexture", _depthTexture);
                break;
            case ShadowMode.Baked:
                break;
            case ShadowMode.None:
                GetComponent<Renderer>().sharedMaterial.SetTexture("_ShadowTexture", null);
                break;
        }

        float far = GetComponent<Camera>().farClipPlane;
        float near = GetComponent<Camera>().nearClipPlane;
        //float zBounds = maxBounds.z - minBounds.z;
        //float samplingDelta = 0.01f;
        //int shellsToDraw = (int)((zBounds / samplingDelta) + 0.5f);
        material.SetVector(_idLightParams, new Vector4(near, far, far - near, (GetComponent<Camera>().orthographic) ? Mathf.PI : GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad));

		GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
    }

    public void SetShaderPropertiesMaterials()
    {
        Material material = GetComponent<Renderer>().sharedMaterial;
        material.SetVector("_minBounds", _minBounds);
        material.SetVector("_maxBounds", _maxBounds);
        material.SetMatrix("_Projection", _projectionMatrixCached);
        material.SetMatrix("_ViewWorldLight", _viewWorldLight);
        material.SetMatrix("_LocalRotation", _localRotation);
        material.SetMatrix("_Rotation", _rotation);

        Plane[] frustrumPLanes = GeometryUtility.CalculateFrustumPlanes(_projectionMatrixCached);
        switch (lightType)
        {
            case LightTypes.Point:
                for (int i = 0; i < frustrumPLanes.Length; i++)
                {
                    Vector3 planeNormal = transform.TransformDirection(frustrumPLanes[i].normal);
                    float distance = frustrumPLanes[i].distance;
                    material.SetVector("_FrustrumPlane" + i, new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, distance));
                }
                break;
            case LightTypes.Spot:
                for (int i = 0; i < frustrumPLanes.Length; i++)
                {
                    Vector3 planeNormal = frustrumPLanes[i].normal;
                    float distance = frustrumPLanes[i].distance;
                    material.SetVector("_FrustrumPlane" + i, new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, distance));
                }
                break;
        }

        switch (shadowMode)
        {
            case ShadowMode.Realtime:
                material.SetTexture("_ShadowTexture", _depthTexture);
                break;
            case ShadowMode.Baked:
                break;
            case ShadowMode.None:
                material.SetTexture("_ShadowTexture", null);
                break;
        }

        float far = GetComponent<Camera>().farClipPlane;
        float near = GetComponent<Camera>().nearClipPlane;
        //float zBounds = maxBounds.z - minBounds.z;
        //float samplingDelta = 0.01f;
        //int shellsToDraw = (int)((zBounds / samplingDelta) + 0.5f);
        material.SetVector("_LightParams", new Vector4(near, far, far - near, (GetComponent<Camera>().orthographic) ? Mathf.PI : GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad));
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj != null)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj, true);
            }
        }
        obj = null;
    }
}

