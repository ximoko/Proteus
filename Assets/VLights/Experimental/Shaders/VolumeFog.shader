Shader "V-Light/Fog" {
Properties {
	_Color				("Color Tint", COLOR) = (1, 1, 1, 1)

	_LightColorEmission ("Light Color Emission Map", 2D) = "white" {}
	_NoiseTex			("Noise Map", 2D) = "white" {}
	_Offset				("Interleaved Offset", FLOAT) = 0

	_Strength			("Light Multiplier", FLOAT) = 1

	_SpotExp			("Spot Exponent", FLOAT) = 50.0
	_ConstantAttn		("Constant Attenuation ",FLOAT) = 0.05
	_LinearAttn			("Linear Attenuation (Distance)", FLOAT) = 0.25
	_QuadAttn 			("Quadratic Attenuation (Distance^2)", FLOAT) = 0.0125

	_PositionX			("Position X", 2D) = "white" {}
	_PositionY			("Position Y", 2D) = "white" {}
	_PositionZ			("Position Z", 2D) = "white" {}

	_Solidity			("Solidity", FLOAT) = 1
	_ExpStr				("Exp Str", FLOAT) = 1
}

CGINCLUDE
	#include "UnityCG.cginc"
	#pragma target 3.0

	struct v2f
	{
		float4 pos :SV_POSITION;
		float4 positionV :TEXCOORD0;
		float4 tcProjA :TEXCOORD1;
		float4 tcProjB :TEXCOORD2;
	};

	// x = near y = far z = far - near z = fov
	float4 _LightParams;
	float4 _minBounds;
	float4 _maxBounds;

	float4x4 _ViewWorldLight;
	float4x4 _ScrollA;
	float4x4 _ScrollB;

	float4x4 _Projection;

	// User
	sampler2D _NoiseTex;
	sampler2D _LightColorEmission;

	// Auto Set
	samplerCUBE _ShadowTexture;

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

		const float4x4 scale = float4x4(
			0.5f, 0.0f, 0.0f, 0.5f,
			0.0f, 0.5f, 0.0f, 0.5f,
			0.0f, 0.0f, 0.5f, 0.5f,
			0.0f, 0.0f, 0.0f, 1.0f);

		float4x4 viewWorldLightProj = mul(_Projection, _ViewWorldLight);

		o.tcProjA = mul(mul(scale, mul(_ScrollA, viewWorldLightProj)), pos);
		o.tcProjB = mul(mul(scale, mul(_ScrollB, viewWorldLightProj)), pos);

		return o;
	}

	#include "VLightHelperFog.cginc"

	half4 frag (v2f i) : COLOR
	{
		return computeFragPoint(i);
	}

ENDCG

Subshader
{
	Tags {"RenderType"="VLightFog" "Queue"="Transparent" "IgnoreProjector"="true"}

	Lod 200

	Pass {
		Fog { Mode Off }
		ZWrite off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag

		ENDCG
	}
}

Fallback Off
}

