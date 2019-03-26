Shader "V-Light/Point" {
Properties {

	_LightColorEmission ("Light Color Emission Map (Cube)", Cube) = "white" {}
	_NoiseTex			("Noise Map (Cube)", Cube) = "white" {}
	_ShadowTexture		("Shadow Map (Cube)", Cube) = "white" {}
	
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
		float4 positionV :TEXCOORD0;
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
		return o;
	}

	#include "VLightHelperPoint.cginc"

	fixed4 frag (v2f i) : COLOR
	{
		return computeFragPoint(i);
	}

ENDCG

Subshader
{
	Tags {"RenderType"="VLightPoint" "Queue"="Transparent" "IgnoreProjector"="true"}

	Lod 200

	Pass {
		Fog { Mode Off }
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

