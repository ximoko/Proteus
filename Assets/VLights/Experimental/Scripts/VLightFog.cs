//#define DEBUG_MODE

/*
 * VLight Fog
 * Copyright Brian Su 2011
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class VLightFog : MonoBehaviour
{
    public enum ShadowMode
    {
        None,
        Realtime,
        Baked
    }

    public float fogRadius = 1;
    public Vector3 noiseSpeed;
    public ShadowMode shadowMode;
    public float aspectRatio = 1.0f;
    public int slices = 30;
    public int minSlices = 5;
    public bool dynamicLevelOfDetail = false;
    public Shader renderDepthShader;

    // Materials
    public Material fogMaterial;
    private Material _prevMaterialFog;
    public Material _instancedFogMaterial;

    [HideInInspector]
    public bool lockTransforms = false;

    public Mesh meshContainer;

    private int _maxSlices;
    public int MaxSlices
    {
        get { return _maxSlices; }
        set { _maxSlices = value; }
    }

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
    private Matrix4x4 _scrollA;
    private Matrix4x4 _scrollB;
    private Matrix4x4 _viewWorldLight;

    private Vector3[] _frustrumPoints;
    private Vector3 _angle = Vector3.zero;

    private Vector3 _minBounds, _maxBounds;
    private bool _cameraHasBeenUpdated = false;
    private MeshFilter _meshFilter;
    private RenderTexture _depthTexture;
    //Cache it for speed
    private const int VERT_COUNT = 65000;
    private const int TRI_COUNT = VERT_COUNT * 3;
    private const System.StringComparison STR_CMP_TYPE = System.StringComparison.OrdinalIgnoreCase;

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
        SafeDestroy(_positionX);
        SafeDestroy(_positionY);
        SafeDestroy(_positionZ);
    }

    private void Start()
    {
#if DEBUG_MODE
        Debug.Log("Start V-light");
#endif
        CreateMaterials();
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
        bool createdMaterial = false;
        createdMaterial |= GenerateNewMaterial(fogMaterial, ref _instancedFogMaterial);
        if (createdMaterial)
        {
            GetComponent<Renderer>().sharedMaterial = _instancedFogMaterial;
        }
    }

    private void CleanMaterials()
    {
        SafeDestroy(_instancedFogMaterial);
        SafeDestroy(GetComponent<Renderer>().sharedMaterial);
        SafeDestroy(meshContainer);

        _prevMaterialFog = null;
        _instancedFogMaterial = null;
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

        Gizmos.color = new Color(1, 1, 0, 1);
        Gizmos.DrawWireCube(GetComponent<Renderer>().bounds.center, GetComponent<Renderer>().bounds.size);
    }

    private void CalculateMinMax(out Vector3 min, out Vector3 max, bool forceFrustrumUpdate)
    {
        if (_frustrumPoints == null || forceFrustrumUpdate)
        {
            VLightGeometryUtil.RecalculateFrustrumPoints(GetComponent<Camera>(), aspectRatio, out _frustrumPoints);
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
            projectionMatrix = Matrix4x4.Perspective(GetComponent<Camera>().fieldOfView, aspectRatio, GetComponent<Camera>().nearClipPlane, GetComponent<Camera>().farClipPlane);
        }
        else
        {
            projectionMatrix = Matrix4x4.Ortho(-GetComponent<Camera>().orthographicSize * 0.5f * aspectRatio, GetComponent<Camera>().orthographicSize * 0.5f * aspectRatio, -GetComponent<Camera>().orthographicSize * 0.5f, GetComponent<Camera>().orthographicSize * 0.5f, GetComponent<Camera>().farClipPlane, GetComponent<Camera>().nearClipPlane);
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

                        CreateDepthTexture();

                        GetComponent<Camera>().targetTexture = null;
                        GetComponent<Camera>().projectionMatrix = Matrix4x4.Perspective(90, aspectRatio, 0.01f, GetComponent<Camera>().farClipPlane);
                        GetComponent<Camera>().SetReplacementShader(renderDepthShader, "RenderType");
                        GetComponent<Camera>().RenderToCubemap(_depthTexture, 63);
                        GetComponent<Camera>().ResetReplacementShader();
                        break;
                    }
                }
                break;
        }
    }

    private void CreateDepthTexture()
    {
        if (_depthTexture == null)
        {
#if DEBUG_MODE
            Debug.Log("Creating new depth texture");
#endif
            _depthTexture = new RenderTexture(256, 256, 24);
            _depthTexture.hideFlags = HideFlags.HideAndDontSave;
            _depthTexture.isPowerOfTwo = true;
            _depthTexture.isCubemap = true;
        }
        else if (!_depthTexture.isCubemap && _depthTexture.IsCreated())
        {
#if DEBUG_MODE
            Debug.Log("Swapping to cubemap depth texture");
#endif
            SafeDestroy(_depthTexture);
            _depthTexture = new RenderTexture(256, 256, 24);
            _depthTexture.hideFlags = HideFlags.HideAndDontSave;
            _depthTexture.isPowerOfTwo = true;
            _depthTexture.isCubemap = true;
        }
    }

    private void Update()
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

        // Render any shadow maps
        RenderShadowMap();
    }

#if DEBUG_MODE
    private bool _hasCalledUpdate = false;
#endif

    //Main loop to render everything
    private void OnWillRenderObject()
    {
        if (fogMaterial == null || _instancedFogMaterial == null)
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

            SetShaderProperties();
        }
    }

    private bool CameraHasBeenUpdated()
    {
        bool hasBeenUpdated = false;
        hasBeenUpdated |= _meshFilter == null || _meshFilter.sharedMesh == null;
        hasBeenUpdated |= GetComponent<Camera>().farClipPlane != _prevFar;
        hasBeenUpdated |= GetComponent<Camera>().nearClipPlane != _prevNear;
        hasBeenUpdated |= GetComponent<Camera>().fieldOfView != _prevFov;
        hasBeenUpdated |= GetComponent<Camera>().orthographicSize != _prevOrthoSize;
        hasBeenUpdated |= GetComponent<Camera>().orthographic != _prevIsOrtho;
        hasBeenUpdated |= fogRadius != _prevPointLightRadius;
        hasBeenUpdated |= fogMaterial != _prevMaterialFog;
        hasBeenUpdated |= _prevSlices != slices;
        hasBeenUpdated |= _prevShadowMode != shadowMode;
        return hasBeenUpdated;
    }

    public void UpdateSettings()
    {
        _cameraHasBeenUpdated = CameraHasBeenUpdated();
        if (_cameraHasBeenUpdated)
        {
            GetComponent<Renderer>().sharedMaterial = _instancedFogMaterial;
            GetComponent<Camera>().orthographic = true;
            GetComponent<Camera>().nearClipPlane = -fogRadius;
            GetComponent<Camera>().farClipPlane = fogRadius;
            GetComponent<Camera>().orthographicSize = fogRadius * 2.0f;

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
        _prevMaterialFog = fogMaterial;
        _prevShadowMode = shadowMode;
        _prevPointLightRadius = fogRadius;
    }

    public void UpdateLightMatrices()
    {
        _localToWorldMatrix = transform.localToWorldMatrix;
        _worldToCamera = GetComponent<Camera>().worldToCameraMatrix;

        _scrollA = Matrix4x4.TRS(_angle, Quaternion.Euler(90, 0, 0), Vector3.one);
        _scrollB = Matrix4x4.TRS(-_angle, Quaternion.Euler(0, 0, 0), Vector3.one);
        _angle += noiseSpeed * Time.deltaTime;

        RebuildMesh();
    }

    public void UpdateViewMatrices(Camera targetCamera)
    {
        _viewWorldToCameraMatrixCached = targetCamera.worldToCameraMatrix;
        _viewCameraToWorldMatrixCached = targetCamera.cameraToWorldMatrix;

        Matrix4x4 origin = Matrix4x4.TRS(-transform.position, Quaternion.identity, Vector3.one);
        _viewWorldLight = origin * _viewCameraToWorldMatrixCached;
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

    private Texture2D _positionX;
    private Texture2D _positionY;
    private Texture2D _positionZ;
    public Transform[] positions;

    public void OnGUI()
    {
        if (_positionX != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 20), _positionX);
        }
    }

    public void GenerateTexture(ref Texture2D tex)
    {
        if (tex == null)
        {
            tex = new Texture2D(24, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
        }
    }

    Color[] _colorX = new Color[24];
    Color[] _colorY = new Color[24];
    Color[] _colorZ = new Color[24];

    public void SetShaderProperties()
    {
        Material material = GetComponent<Renderer>().sharedMaterial;
        material.SetVector("_minBounds", _minBounds);
        material.SetVector("_maxBounds", _maxBounds);
        material.SetMatrix("_Projection", _projectionMatrixCached);
        material.SetMatrix("_ViewWorldLight", _viewWorldLight);

        material.SetMatrix("_ScrollA", _scrollA);
        material.SetMatrix("_ScrollB", _scrollB);

        Plane[] frustrumPLanes = GeometryUtility.CalculateFrustumPlanes(_projectionMatrixCached);
        for (int i = 0; i < frustrumPLanes.Length; i++)
        {
            Vector3 planeNormal = transform.TransformDirection(frustrumPLanes[i].normal);
            float distance = frustrumPLanes[i].distance;
            material.SetVector("_FrustrumPlane" + i, new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, distance));
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

        //Generate position textures
        GenerateTexture(ref _positionX);
        GenerateTexture(ref _positionY);
        GenerateTexture(ref _positionZ);

        //int max = Mathf.Min(particles.Length, 24);
        for (int i = 0; i < 24; i++)
        {
            Color zero = VLightGeometryUtil.FloatToRGBA(0.5f);
            _colorX[i] = zero;
            _colorY[i] = zero;
            _colorZ[i] = zero;
        }
        //for (int i = 0; i < max; i++)
        //{
        //    Vector3 viewPos = _worldToCamera.MultiplyPoint(particles[i].position);
        //    _positionX.SetPixel(i, 0, VLightGeometryUtil.FloatToRGBA((viewPos.x + 20.0f) / 40.0f));
        //    _positionY.SetPixel(i, 0, VLightGeometryUtil.FloatToRGBA((viewPos.y + 20.0f) / 40.0f));
        //    _positionZ.SetPixel(i, 0, VLightGeometryUtil.FloatToRGBA((viewPos.z + 20.0f) / 40.0f));
        //}
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 viewPos = _worldToCamera.MultiplyPoint(positions[i].position);
            _colorX[i] = VLightGeometryUtil.FloatToRGBA((viewPos.x + 20.0f) / 40.0f);
            _colorY[i] = VLightGeometryUtil.FloatToRGBA((viewPos.y + 20.0f) / 40.0f);
            _colorZ[i] = VLightGeometryUtil.FloatToRGBA((viewPos.z + 20.0f) / 40.0f);
        }

        _positionX.SetPixels(_colorX);
        _positionY.SetPixels(_colorY);
        _positionZ.SetPixels(_colorZ);
        _positionX.Apply(false);
        _positionY.Apply(false);
        _positionZ.Apply(false);

        //Color packedCol = VLightGeometryUtil.FloatToRGBA(_worldToCamera.MultiplyPoint(positions[0].position).x);
        //Debug.Log(Vector4.Dot(new Vector4(1.0f, 1f/255.0f, 1f/65025.0f, 1f/160581375.0f), new Vector4(packedCol.r, packedCol.g, packedCol.b, packedCol.a)));

        material.SetFloat("_Points", positions.Length);
        material.SetTexture("_PositionX", _positionX);
        material.SetTexture("_PositionY", _positionY);
        material.SetTexture("_PositionZ", _positionZ);

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

