float4 _FrustrumPlane0;
float4 _FrustrumPlane1;
float4 _FrustrumPlane2;
float4 _FrustrumPlane3;
float4 _FrustrumPlane4;
float4 _FrustrumPlane5;

inline half4 computeFragSpot (v2f i) 
{	
/*
	clip(dot(_FrustrumPlane0, i.positionV));
	clip(dot(_FrustrumPlane1, i.positionV));
	clip(dot(_FrustrumPlane2, i.positionV));
	clip(dot(_FrustrumPlane3, i.positionV));
*/	
	float _LightNearRange = _LightParams.x;
	float _LightFarRange = _LightParams.y;
	float _Range = _LightParams.z;
	float _Fov = _LightParams.w;

	half noise = tex2Dproj(_NoiseTex, i.tcProjScroll).r;
		
	float3 lightDir = float3(0.0f, 0.0f, -1.0);
	float spotEffect = dot(lightDir, normalize(i.positionV.xyz));	
	float attenuation = 0.0f;

	float dist = (-i.positionV.z - _LightNearRange) / _Range;
	float distShadow = -i.positionV.z / _LightFarRange;

	//clip if greater than FOV	
	clip(_Fov < acos(spotEffect) ? -1 : 1);

	spotEffect = pow(spotEffect, _SpotExp);	
	attenuation = spotEffect / (_ConstantAttn + _LinearAttn * dist + _QuadAttn * (dist * dist));	

	//shadow map test
	float shadowMapDepth = tex2Dproj(_ShadowTexture, i.tcProj).r; 
	clip(shadowMapDepth - distShadow);

	//final colour	
	float4 color = tex2Dproj(_LightColorEmission, i.tcProj);	
	half3 Albedo = color * _Color.rgb;
	half Alpha = (noise * attenuation) * _Strength * _Color.a;

	return half4(Albedo * Alpha, 1);
}