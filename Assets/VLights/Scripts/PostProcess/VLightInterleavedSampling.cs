using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VLights;

/*
 * VLight
 * Copyright Brian Su 2011-2014
*/

[ExecuteInEditMode()]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("V-Lights/VLight Image Effects")]
public class VLightInterleavedSampling : MonoBehaviour
{
	[SerializeField]
	private bool useInterleavedSampling = true;
	[SerializeField]
    private float ditherOffset = 0.02f;
	[SerializeField]
    private float blurRadius = 1.5f;
	[SerializeField]
    private int blurIterations = 1;
	[SerializeField]
    private int downSample = 4;
	[SerializeField]
	private Shader postEffectShader;
	[SerializeField]
	private Shader volumeLightShader;
	//
    private GameObject _ppCameraGO = null;
    private LayerMask _volumeLightLayer;
    private VLight[] _vLights;
	private Material _postMaterial;
	private Material PostMaterial
	{
		get
		{
			if(_postMaterial == null)
			{
				_postMaterial = new Material(postEffectShader);
			}
			return _postMaterial;
		}
	}

    private void OnEnable()
    {
        _vLights = null;
        Init();
    }

    private void OnDisable()
    {
        if (_vLights != null)
        {
            foreach (var vLight in _vLights)
            {
                if (vLight == null)
                {
                    continue;
                }
                vLight.SetInterleavedOffset(0.0f);
                vLight.lockTransforms = false;
            }
        }
        _vLights = null;
        CleanUp();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_vLights == null)
        {
            _vLights = GameObject.FindObjectsOfType(typeof(VLight)) as VLight[];
        }

        int downsampleFactor = Mathf.Clamp(downSample, 1, 20);
        blurIterations = Mathf.Clamp(blurIterations, 0, 20);

        // 4 samples for the interleaved buffer
        RenderTexture bufferA = RenderTexture.GetTemporary(Screen.width / downsampleFactor, Screen.height / downsampleFactor, 1);
        RenderTexture bufferB = RenderTexture.GetTemporary(Screen.width / downsampleFactor, Screen.height / downsampleFactor, 1);
        RenderTexture bufferC = RenderTexture.GetTemporary(Screen.width / downsampleFactor, Screen.height / downsampleFactor, 1);
        RenderTexture bufferD = RenderTexture.GetTemporary(Screen.width / downsampleFactor, Screen.height / downsampleFactor, 1);
        RenderTexture interleavedBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);

        Camera ppCamera = GetPPCamera();
        ppCamera.CopyFrom(GetComponent<Camera>());
        ppCamera.enabled = false;
		ppCamera.depthTextureMode = DepthTextureMode.None;
		ppCamera.clearFlags = CameraClearFlags.SolidColor;
        ppCamera.cullingMask = _volumeLightLayer;
        ppCamera.backgroundColor = Color.clear;
		ppCamera.renderingPath = RenderingPath.VertexLit;

		foreach (var vLight in _vLights)
        {
            if (vLight == null)
            {
                continue;
            }
            vLight.lockTransforms = true;
            vLight.UpdateSettings();
            vLight.UpdateLightMatrices();
            vLight.UpdateViewMatrices(Camera.current);
			vLight.SetShaderPropertiesBlock();
        }

		if(useInterleavedSampling)
		{
	        // Render the interleaved samples
	        float offset = 0.0f;
	        RenderSample(offset, ppCamera, bufferA);
	        offset += ditherOffset;
	        RenderSample(offset, ppCamera, bufferB);
	        offset += ditherOffset;
	        RenderSample(offset, ppCamera, bufferC);
	        offset += ditherOffset;
	        RenderSample(offset, ppCamera, bufferD);

	        //Combine the 4 samples to make an interleaved image and the edge border
	        PostMaterial.SetTexture("_MainTexA", bufferA);
	        PostMaterial.SetTexture("_MainTexB", bufferB);
	        PostMaterial.SetTexture("_MainTexC", bufferC);
	        PostMaterial.SetTexture("_MainTexD", bufferD);
	        Graphics.Blit(null, interleavedBuffer, PostMaterial, 0);
		}
		else
		{
			RenderSample(0, ppCamera, bufferA);
			Graphics.Blit(bufferA, interleavedBuffer);
		}

 		foreach (var vLight in _vLights)
        {
            if (vLight == null)
            {
                continue;
            }
            vLight.lockTransforms = false;
        }

		//Blur the result
		RenderTexture pingPong = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        PostMaterial.SetFloat("_BlurSize", blurRadius);
        for (int i = 0; i < blurIterations; i++)
        {
            Graphics.Blit(interleavedBuffer, pingPong, PostMaterial, 1);
			Graphics.Blit(pingPong, interleavedBuffer, PostMaterial, 2);
        }
		RenderTexture.ReleaseTemporary(pingPong);


        //Combine back with the scene
        PostMaterial.SetTexture("_MainTexBlurred", interleavedBuffer);
        Graphics.Blit(source, destination, PostMaterial, 3);

        RenderTexture.ReleaseTemporary(bufferA);
        RenderTexture.ReleaseTemporary(bufferB);
        RenderTexture.ReleaseTemporary(bufferC);
        RenderTexture.ReleaseTemporary(bufferD);
        RenderTexture.ReleaseTemporary(interleavedBuffer);
    }

    private void RenderSample(float offset, Camera ppCamera, RenderTexture buffer)
    {
        foreach (var vLight in _vLights)
        {
            if (vLight == null)
            {
                continue;
            }
            vLight.SetInterleavedOffset(offset);
        }
		ppCamera.projectionMatrix = GetComponent<Camera>().projectionMatrix;
        ppCamera.targetTexture = buffer;
        ppCamera.RenderWithShader(volumeLightShader, "RenderType");
    }

    private void Init()
    {
        if (LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME) == -1 || !SystemInfo.supportsImageEffects)
        {
            Debug.LogWarning(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME + " layer does not exist! Cannot use interleaved sampling please add this layer.");
            return;
        }
        _volumeLightLayer = 1 << LayerMask.NameToLayer(VLightManager.VOLUMETRIC_LIGHT_LAYER_NAME);

        GetComponent<Camera>().cullingMask &= ~_volumeLightLayer;
		GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;

		if(postEffectShader == null)
		{
			postEffectShader = Shader.Find(VLightShaderUtil.POST_SHADER_NAME);
		}

		if(volumeLightShader == null)
		{
			volumeLightShader = Shader.Find(VLightShaderUtil.INTERLEAVED_SHADER_NAME);
		}
    }

    private void CleanUp()
    {
        GetComponent<Camera>().cullingMask |= _volumeLightLayer;
        if (Application.isEditor)
        {
            DestroyImmediate(_postMaterial);
        }
        else
        {
            Destroy(_postMaterial);
        }
    }

    private Camera GetPPCamera()
    {
        if (_ppCameraGO == null)
        {
            _ppCameraGO = new GameObject("Post Processing Camera", typeof(Camera));
            _ppCameraGO.GetComponent<Camera>().enabled = false;
            _ppCameraGO.hideFlags = HideFlags.HideAndDontSave;
        }
        return _ppCameraGO.GetComponent<Camera>();
    }
}

