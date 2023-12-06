#ifndef WATER
#define WATER

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _SurfaceNoiseMap_ST;
float4 _ShallowColor;
float4 _DeepColor;
float _MaxWaterDepth;
float _SurfaceNoiseCutoff;
float _FoamMinDistance;
float _FoamMaxDistance;
float2 _SurfaceNoiseScroll;
CBUFFER_END

TEXTURE2D(_SurfaceNoiseMap);
SAMPLER(sampler_SurfaceNoiseMap);

struct Attributes
{
	float4 positionOS : POSITION;
	float4 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 uv : TEXCOORD0;
};

struct Varyings
{
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD2;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	float4 screenPos : TEXCOORD3;
};

float SampleDepth(float2 uv)
{
	return SampleSceneDepth(uv.xy);
}

float GetLinearEyeDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
	return LinearDepthToEyeDepth(rawDepth);
#else
	return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
	const float rawDepth = SampleDepth(uv);
	return GetLinearEyeDepth(rawDepth);
}

Varyings waterVert(Attributes IN)
{
	Varyings OUT;

	OUT.position = TransformObjectToHClip(IN.positionOS);
	OUT.screenPos = OUT.position;

#if UNITY_UV_STARTS_AT_TOP
	OUT.screenPos.y = -OUT.screenPos.y;
#endif

	OUT.uv = IN.uv;
	OUT.tangent = IN.tangentOS;
	OUT.normal = TransformObjectToWorldNormal(IN.normalOS);

	return OUT;
}

float4 waterFrag(Varyings IN) : SV_Target
{
	IN.screenPos.xyz *= rcp(IN.screenPos.w);
	IN.screenPos.xy = IN.screenPos.xy * 0.5 + 0.5;

	float linearDepthInTex = SampleAndGetLinearEyeDepth(IN.screenPos.xy);
	float warterDepth = IN.screenPos.w;
	float depthDifference = linearDepthInTex - warterDepth;

	float waterDepthDiffrence = saturate(depthDifference / _MaxWaterDepth);

	float3 normal = SampleSceneNormals(IN.uv);
	float normaldot = saturate(dot(normal, IN.normal));
	float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normaldot);
	float foamDepthDifference = saturate(depthDifference / foamDistance);
	float surfaceNoiseCutoff = foamDepthDifference * _SurfaceNoiseCutoff;
	float2 uv = IN.uv * _SurfaceNoiseMap_ST.xy + _SurfaceNoiseMap_ST.zw;
	float2 noiseUV = float2(uv.x + _Time.y * _SurfaceNoiseScroll.x, uv.y + _Time.y * _SurfaceNoiseScroll.y);
	float noiseSample = SAMPLE_TEXTURE2D(_SurfaceNoiseMap, sampler_SurfaceNoiseMap, noiseUV).r;
	float noise = noiseSample > surfaceNoiseCutoff ? 1 : 0.0;


	float4 waterColor = lerp(_ShallowColor, _DeepColor, waterDepthDiffrence);

	return waterColor + noise;
}

#endif