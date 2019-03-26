// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/V-Light/Post" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_MainTexA ("Sample A", 2D) = "" {}
		_MainTexB ("Sample B", 2D) = "" {}
		_MainTexC ("Sample C", 2D) = "" {}
		_MainTexD ("Sample D", 2D) = "" {}
		_MainTexHighRes ("High Res Edge", 2D) = "" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc" 
	#define DEBUG_INTERLEAVED 0

	struct v2fInterleaved
	{
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
	};

	struct v2fBlur
	{
		float4 pos : POSITION;
		float2 uv[6] : TEXCOORD0;
		float4 screenPos : TEXCOORD2;
	};

	struct v2fComposite
	{
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
	};

	sampler2D _MainTex;
	sampler2D _MainTexBlurred;

	sampler2D _MainTexA;
	sampler2D _MainTexB;
	sampler2D _MainTexC;
	sampler2D _MainTexD;
	//
	float4 samplingOffset;
	float _BlurSize;
	//
	float4 _MainTexBlurred_TexelSize;
	float4 _MainTex_TexelSize;

	v2fInterleaved vertInterleaved( appdata_img v )
	{
		v2fInterleaved o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.screenPos = ComputeScreenPos(v.vertex);

		return o;
	}
	

	v2fBlur vertBlurHorz( appdata_img v ) {
		v2fBlur o;
		o.pos = UnityObjectToClipPos(v.vertex);

 		float3 off = float3(_MainTex_TexelSize.x, -_MainTex_TexelSize.x, 0) * _BlurSize;

		o.uv[0] = v.texcoord.xy + off.xz;
		o.uv[1] = v.texcoord.xy + off.yz;
		o.uv[2] = v.texcoord.xy + off.xz * 2;
		o.uv[3] = v.texcoord.xy + off.yz * 2;
		o.uv[4] = v.texcoord.xy + off.xz * 3;
		o.uv[5] = v.texcoord.xy + off.yz * 3;
		return o;
	}

	v2fBlur vertBlurVert( appdata_img v ) {
		v2fBlur o;
		o.pos = UnityObjectToClipPos(v.vertex);

 		float3 off = float3(_MainTex_TexelSize.y, -_MainTex_TexelSize.y, 0) * _BlurSize;

		o.uv[0] = v.texcoord.xy + off.zx;
		o.uv[1] = v.texcoord.xy + off.zy;
		o.uv[2] = v.texcoord.xy + off.zx * 2;
		o.uv[3] = v.texcoord.xy + off.zy * 2;
		o.uv[4] = v.texcoord.xy + off.zx * 3;
		o.uv[5] = v.texcoord.xy + off.zy * 3;
		return o;
	} 
	

	v2fComposite vert( appdata_img v ) {
		v2fComposite o;
		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv[0] = v.texcoord.xy;
		o.uv[1] = v.texcoord.xy;
		#if SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			o.uv[0].y = 1-o.uv[0].y;
		#endif
		return o;
	}

	fixed4 frag(v2fInterleaved i) : COLOR
	{
		int2 screenPos = i.screenPos.xy  * _ScreenParams.xy;

		// Based on the screen (x,y), determine whether the pixel is even or odd
		float2 vEvenOdd = (float2) floor(fmod(screenPos.xy + 0.5, 2));
		float index = abs(3 * (float)vEvenOdd.x - 2 * (float)vEvenOdd.y);

		half4 color = 0;

#if DEBUG_INTERLEAVED
		color += tex2D (_MainTexA, i.uv * 2 - float2(1.0, 1.0));
		color += tex2D (_MainTexB, i.uv * 2 - float2(0.0, 1.0));
		color += tex2D (_MainTexC, i.uv * 2 - float2(1.0, 0.0));
		color += tex2D (_MainTexD, i.uv * 2 - float2(0.0, 0.0));
#else
		color += tex2D (_MainTexA, i.uv) * (index == 0);
		color += tex2D (_MainTexB, i.uv) * (index == 1);
		color += tex2D (_MainTexC, i.uv) * (index == 2);
		color += tex2D (_MainTexD, i.uv) * (index == 3);
#endif
		return color;
	}

	fixed4 fragBlur(v2fBlur i) : COLOR
	{
		half4 color = tex2D(_MainTex, i.uv[0]);
		color += tex2D(_MainTex, i.uv[1]);
		color += tex2D(_MainTex, i.uv[2]);
		color += tex2D(_MainTex, i.uv[3]);
		color += tex2D(_MainTex, i.uv[4]);
		color += tex2D(_MainTex, i.uv[5]);
		color.a = 1;

		return (color / 6);
	}

	fixed4 fragComposite(v2fComposite i) : COLOR
	{
		fixed4 textureBlurred = tex2D(_MainTexBlurred, i.uv[0]);
		fixed4 sourceTexture = tex2D(_MainTex, i.uv[1]);

		return textureBlurred + sourceTexture;
	}

	ENDCG

Subshader
{
	Pass
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma vertex vertInterleaved
		#pragma fragment frag
		ENDCG
	}

	Pass
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma vertex vertBlurHorz
		#pragma fragment fragBlur
		ENDCG
	}

	Pass
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma vertex vertBlurVert
		#pragma fragment fragBlur
		ENDCG
	}

	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma vertex vert
		#pragma fragment fragComposite
		ENDCG
	}

}

Fallback off

} // shader