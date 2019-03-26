Shader "V-Light/Volumetric Light Depth" {

Subshader
{
	Tags {"RenderType"="VLight"}

	Pass {
		Fog { Mode Off }
		AlphaTest Greater 0
		ZWrite off
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend One One

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"

struct v2f
{
	float4 pos :SV_POSITION;
	float4 tcProj : TEXCOORD0;
	float4 tcProjScroll : TEXCOORD1;
	float4 positionV : TEXCOORD2;
	float4 screenPos : TEXCOORD3;
};

// x = near y = far z = far - near z = fov
float4 _LightParams;
float4 _minBounds;
float4 _maxBounds;
float4x4 _ViewWorldLight;
float4x4 _Projection;
float4x4 _Rotation;
float4x4 _LocalRotation;

// User
sampler2D _NoiseTex;
sampler2D _LightColorEmission;

// Auto Set
sampler2D _ShadowTexture;

// Attenuation values
float _SpotExp;
float _ConstantAttn;
float _LinearAttn;
float _QuadAttn;

// Light settings
float _Strength;
float _Offset;
float4 _Color;

v2f vert (appdata_full v) {
	v2f o;

	v.vertex -= float4(0, 0, _Offset, _Offset);

	const float4x4 scale = float4x4(
		0.5f, 0.0f, 0.0f, 0.5f,
		0.0f, 0.5f, 0.0f, 0.5f,
		0.0f, 0.0f, 0.5f, 0.5f,
		0.0f, 0.0f, 0.0f, 1.0f);

	float4x4 viewWorldLightProj = mul(_Projection, _ViewWorldLight);
	float4x4 lightProjection = mul(scale, viewWorldLightProj);
	float4x4 lightProjectionNoise = mul(scale, mul(_Rotation, viewWorldLightProj));

	float4 pos = _minBounds * v.vertex + _maxBounds * (1  - v.vertex);
	pos.w = 1;

	o.tcProj = mul(lightProjection, pos);
	o.tcProjScroll = mul(lightProjectionNoise, pos);

	o.pos = mul(UNITY_MATRIX_P, pos);
	o.positionV = mul(_ViewWorldLight, pos);
	o.positionV.w = -pos.z * _ProjectionParams.w;

	o.screenPos = ComputeScreenPos(o.pos);
	return o;
}

#include "VLightHelper.cginc"

sampler2D _CameraDepthNormalsTexture;
half4 frag (v2f i) : COLOR
{
	float partZ = i.positionV.w;
	half sceneZ = UNITY_SAMPLE_DEPTH(DecodeFloatRG(tex2Dproj(_CameraDepthNormalsTexture, UNITY_PROJ_COORD(i.screenPos)).zw));
	clip(sceneZ - partZ);
	return computeFragSpot(i);
}

		ENDCG
	}
}


Subshader
{
	Tags {"RenderType"="VLightPoint"}

	Pass {
		Fog { Mode Off }
		AlphaTest Greater 0
		ZWrite off
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend One One

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f
{
	float4 pos :SV_POSITION;
	float4 positionV : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
};

// x = near y = far z = far - near z = fov
float4 _LightParams;
float4 _minBounds;
float4 _maxBounds;
float4x4 _ViewWorldLight;
float4x4 _Rotation;
float4x4 _LocalRotation;

// User
samplerCUBE _NoiseTex;
samplerCUBE _LightColorEmission;

// Auto Set
samplerCUBE _ShadowTexture;
sampler2D _CameraDepthTexture;

// Attenuation values
float _SpotExp;
float _ConstantAttn;
float _LinearAttn;
float _QuadAttn;

// Light settings
float _Strength;
float _Offset;
float4 _Color;

v2f vert (appdata_full v) {
	v2f o;
	v.vertex -= float4(0, 0, _Offset, 0);

	float4 pos = _minBounds * v.vertex + _maxBounds * (1  - v.vertex);
	pos.w = 1;

	o.pos = mul(UNITY_MATRIX_P, pos);
	o.positionV = mul(_ViewWorldLight, pos);
	o.positionV.w = -pos.z * _ProjectionParams.w;

	o.screenPos = ComputeScreenPos(o.pos);
	return o;
}

#include "VLightHelperPoint.cginc"

sampler2D _CameraDepthNormalsTexture;
half4 frag (v2f i) : COLOR
{
	float partZ = i.positionV.w;
	half sceneZ = UNITY_SAMPLE_DEPTH(DecodeFloatRG(tex2Dproj(_CameraDepthNormalsTexture, UNITY_PROJ_COORD(i.screenPos)).zw));
	clip(sceneZ - partZ);
	return computeFragPoint(i);
}



		ENDCG
	}
}

Fallback Off
}

