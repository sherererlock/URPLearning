#ifndef SCREENSPACEREFLECTION
#define SCREENSPACEREFLECTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#define MAX_DISTANCE 15
#define STEP_COUNT 50
#define THINCKNESS 0.3
#define STEP_SIZE 0.1

TEXTURE2D(_SSRTexture);
SAMPLER(sampler_BlitTexture);

float4x4 _CameraViewProjection;
float4 _ProjectionParams2;
float4 _CameraTopLeftCorner;
float4 _CameraViewXExtent;
float4 _CameraViewYExtent;

float3 ReconstructViewPos(float2 uv, float linearDepth)
{
	uv.y = _ProjectionParams.x == -1 ? 1.0 - uv.y : uv.y;

	float zScale = _ProjectionParams2.x * linearDepth;
	float3 viewPos = _CameraTopLeftCorner.xyz + _CameraViewXExtent.xyz * uv.x + _CameraViewYExtent.xyz * uv.y;
	viewPos *= zScale;

	return viewPos;
}

void RestructUVAndViewDepth(float3 worldPos, out float2 uv, out float depth)
{
	float4 clipPos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
	uv = float2(clipPos.x, clipPos.y * _ProjectionParams.x) / clipPos.w * 0.5 + 0.5;
	depth = clipPos.w;
}

half4 GetSource(half2 uv) {
	return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, uv, _BlitMipLevel);
}

half4 RayMarchingInWorldSpace(float2 uv)
{
	float rawDepth = SampleSceneDepth(uv);
	float linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
	float3 viewPos = ReconstructViewPos(uv, linearEyeDepth);
	float3 normal = normalize(SampleSceneNormals(uv));

	float up = dot(normal, float3(0, 1, 0));
	if (up < 0.5)
		return GetSource(uv);

	float3 wpos = viewPos + _WorldSpaceCameraPos;
	float3 viewDir = normalize(viewPos);
	float3 reflectDir = normalize(reflect(viewDir, normal));

	float2 uv2;
	float stepDepth = 0;

	for (int i = 1; i < STEP_COUNT; i++)
	{
		float3 pos = wpos + reflectDir * STEP_SIZE * i;
		RestructUVAndViewDepth(pos, uv2, stepDepth);
		float stepRawDepth = SampleSceneDepth(uv2);
		float stepViewDepth = LinearEyeDepth(stepRawDepth, _ZBufferParams);

		if (stepViewDepth < stepDepth && stepDepth - stepViewDepth < THINCKNESS)
		{
			return GetSource(uv2);
		}
	}

	return GetSource(uv);
}

half4 RayMarchingInTextureSpace(float2 uv)
{
	return 0;
}

half4 SSR(Varyings input) : SV_Target
{
	return RayMarchingInWorldSpace(input.texcoord);
}

half4 Copy(Varyings input) : SV_Target
{
	return GetSource(input.texcoord);
}

#endif