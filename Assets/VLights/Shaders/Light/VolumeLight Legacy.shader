Shader "V-Light/Spot" {
Properties {

	_LightColorEmission ("Light Color Emission Map", 2D) = "white" {}
	_NoiseTex			("Noise Map (r)", 2D) = "clear" {}
	_ShadowTexture		("Shadow Map", 2D) = "red" {}

	_Color				("Unused", COLOR) = (1, 1, 1, 1)
	_Strength			("Unused", FLOAT) = 1
	_SpotExp			("Unused", FLOAT) = 50.0
	_ConstantAttn		("Unused", FLOAT) = 0.05
	_LinearAttn			("Unused", FLOAT) = 0.25
	_QuadAttn 			("Unused", FLOAT) = 0.0125
}

CGINCLUDE
	#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos :SV_POSITION;
		float4 tcProj : TEXCOORD0;
		float4 tcProjScroll : TEXCOORD1;
		float4 positionV : TEXCOORD2;
		float3 viewDir : TEXCOORD3;
	};

	// x = near y = far z = far - near z = fov
	float4 _LightParams;
	float4 _minBounds;
	float4 _maxBounds;
	float4x4 _ViewWorldLight;
	float4x4 _Rotation;
	float4x4 _Projection;

	// User
	sampler2D _NoiseTex;
	sampler2D _LightColorEmission;

	// Auto Set
	sampler2D _ShadowTexture;
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
		return o;
	}

	#include "VLightHelper.cginc"

	half4 frag (v2f i) : COLOR
	{
		return computeFragSpot(i);
	}

ENDCG

Subshader
{
	Tags {"RenderType"="VLight" "Queue"="Transparent" "IgnoreProjector"="true"}

	Lod 200

	Pass {
		Fog { Mode Off }
		AlphaTest Greater 0
		ZWrite off
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend One One

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag

		ENDCG
	}
}

Fallback Off
}

