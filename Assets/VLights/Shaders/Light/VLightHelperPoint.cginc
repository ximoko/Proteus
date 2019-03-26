float4 _FrustrumPlane0;
float4 _FrustrumPlane1;
float4 _FrustrumPlane2;
float4 _FrustrumPlane3;
float4 _FrustrumPlane4;
float4 _FrustrumPlane5;

inline fixed4 computeFragPoint (v2f i) 
{		
/*
	clip(dot(_FrustrumPlane0, i.positionV));
	clip(dot(_FrustrumPlane1, i.positionV));
	clip(dot(_FrustrumPlane2, i.positionV));
	clip(dot(_FrustrumPlane3, i.positionV));
	clip(dot(_FrustrumPlane4, i.positionV));
	clip(dot(_FrustrumPlane5, i.positionV));
*/	
	float _LightNearRange = _LightParams.x;
	float _LightFarRange = _LightParams.y;
	float _Range = _LightParams.z;
	float _Fov = _LightParams.w;	

	float3 direction = i.positionV.xyz;
	float3 rotatedDirection = mul(direction, (float3x3)_Rotation); 
	half noise = texCUBE(_NoiseTex, rotatedDirection).r;
	
	float dist = length(i.positionV.xyz) / (_LightFarRange * 2 - 0.01);
	float spotEffect = 1 - dist;
	float attenuation = 0.0f;

	spotEffect = pow((spotEffect), _SpotExp);	
	attenuation = spotEffect / (_ConstantAttn + _LinearAttn * dist + _QuadAttn * (dist * dist));	

	//shadow map test
	float shadowMapDepth = texCUBE(_ShadowTexture, direction.xyz).r; 
	int val = step(0, shadowMapDepth - dist);

	//final colour	
	float4 color = texCUBE(_LightColorEmission, mul(normalize(direction), (float3x3)_LocalRotation));	
	half3 Albedo = color * _Color.rgb;
	half Alpha = (noise * attenuation) * _Strength * _Color.a * val;

	//return half4(Albedo, Alpha);
	return fixed4(Albedo * Alpha, 1);
}