#ifndef HBAOBLURINCLUDE
#define HBAOBLURINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

#define KERNEL_RADIUS 2

float _BlurSharpness;
float4 _deltaUV;

inline float CrossBilateralWeight(float distance, float depth, float depth0)
{
	const float BlurSigma = (float)KERNEL_RADIUS * 0.5;
	const float BlurFalloff = 1.0 / (2.0 * BlurSigma * BlurSigma);

	float dz = (depth - depth0) * _ProjectionParams.z * _BlurSharpness;
	
	return exp2(-distance * distance * BlurFalloff - dz * dz);
}

inline void ProcessSample(float ao, float depth, float depth0, float distance, inout float weight, inout float totalAO)
{
	float w = CrossBilateralWeight(distance, depth, depth0);
	weight += w;
	totalAO += w * ao;
}

float4 Blur(float2 uv, float2 deltauv)
{
	float depth = SampleSceneDepth(uv);

	float totalAO = 0.0;
	float totalWeight = 0.0;

	for (int i = -KERNEL_RADIUS; i < KERNEL_RADIUS; i++)
	{
		float2 offsetuv = uv + deltauv.xy * i;
		float depth0 = SampleSceneDepth(offsetuv);
		float ao = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, offsetuv, 0).a;
		ProcessSample(ao, depth, depth0, abs(i), totalWeight, totalAO);
	}

	totalAO /= totalWeight;

	return float4(totalAO, totalAO, totalAO, totalAO);
}

float4 HBAOBlurH(Varyings input) : SV_Target
{
	float2 uv = input.texcoord.xy;
	
	return Blur(uv, float2(_deltaUV.x, 0.0f));
}

float4 HBAOBlurV(Varyings input) : SV_Target
{
	float2 uv = input.texcoord.xy;
	return Blur(uv, float2(0.0, _deltaUV.y));
}

#endif // !HBAOINCLUDE
