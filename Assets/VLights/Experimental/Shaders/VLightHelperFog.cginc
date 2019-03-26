float4 _FrustrumPlane0;
float4 _FrustrumPlane1;
float4 _FrustrumPlane2;
float4 _FrustrumPlane3;
float4 _FrustrumPlane4;
float4 _FrustrumPlane5;

sampler2D _PositionX;
sampler2D _PositionY;
sampler2D _PositionZ;

float _Points;

float _Solidity;
float _ExpStr;

float unpackFloat(const float4 rgba)
{
	return dot( rgba, float4(1.0, 1/255.0, 1/65025.0, 1/160581375.0));
}

inline half4 computeFragPoint (v2f i)
{
	clip(dot(_FrustrumPlane0, i.positionV));
	clip(dot(_FrustrumPlane1, i.positionV));
	clip(dot(_FrustrumPlane2, i.positionV));
	clip(dot(_FrustrumPlane3, i.positionV));
	clip(dot(_FrustrumPlane4, i.positionV));
	clip(dot(_FrustrumPlane5, i.positionV));

	float _LightNearRange = _LightParams.x;
	float _LightFarRange = _LightParams.y;
	float _Range = _LightParams.z;

	half noise = tex2Dproj(_NoiseTex, i.tcProjA).r;
	noise += tex2Dproj(_NoiseTex, i.tcProjB);
	noise *= 0.5;

	float range = 0;
	float light = 0;
	for(int k = 0; k < 16; k++)
	{
		float positionX = unpackFloat(tex2D(_PositionX, float2(k / 32.0, 0))) * 40 - 20;
		float positionY = unpackFloat(tex2D(_PositionY, float2(k / 32.0, 0))) * 40 - 20;
		float positionZ = unpackFloat(tex2D(_PositionZ, float2(k / 32.0, 0))) * 40 - 20;
		float3 dir = i.positionV.xyz - float3(positionX, positionY, -positionZ);
		light += dot(i.positionV.xyz, float3(0, -1, 1)) * 0.001;
		range += _ExpStr/length(dir) * step(k, _Points - 1);
	}

	range = log10(range);
	return half4(_Color.rgb + light, range * _Solidity * noise);////half4(Albedo, Alpha);
}