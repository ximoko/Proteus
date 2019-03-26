Shader "GPUInstancer/ColorVariationShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setupGPUI
		#pragma surface surf Standard addshadow fullforwardshadows

		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			StructuredBuffer<float4> colorBuffer;
		#endif

		#include "./../../../Resources/Shaders/Include/GPUInstancerInclude.cginc"

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float4 col = 1.0f;

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
					uint index = gpuiTransformationMatrix[unity_InstanceID];
					col = colorBuffer[index];
			#else
					col = _Color;
			#endif

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * col;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
