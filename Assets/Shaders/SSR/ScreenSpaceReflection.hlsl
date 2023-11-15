#ifndef SCREENSPACEREFLECTION
#define SCREENSPACEREFLECTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#define MAX_DISTANCE 1000
#define STEP_COUNT 50
#define MIN_THINCKNESS 0.01
#define MAX_THINCKNESS 0.1
#define STEP_SIZE 0.1

TEXTURE2D(_SSRTexture);
SAMPLER(sampler_BlitTexture);

float4x4 _CameraViewProjection;
float4 _SourceSize;
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
		if (any(uv2 > 1) || any(uv2 < 0))
			return GetSource(uv);

		float stepRawDepth = SampleSceneDepth(uv2);
		float stepViewDepth = LinearEyeDepth(stepRawDepth, _ZBufferParams);

		if (stepViewDepth < stepDepth && stepDepth - stepViewDepth < MIN_THINCKNESS)
			return GetSource(uv2);
	}

	return GetSource(uv);
}

void ComputeReflectDirAndMaxDistance(float3 viewPos, float3 normal, out float3 startTS, out float3 rDirTS, out float maxDistance)
{
	float3 wpos = viewPos + _WorldSpaceCameraPos;
	float3 viewDir = normalize(viewPos);
	float3 reflectDir = reflect(viewDir, normal);

	float3 startView = TransformWorldToView(wpos); // ?
	float3 reflectDirView = TransformWorldToViewDir(reflectDir, true);

	float magnitude = MAX_DISTANCE;
	float end = startView.z + reflectDirView.z * magnitude;
	if(end > -_ProjectionParams.y)
		magnitude = (-_ProjectionParams.y - startView.z) / reflectDirView.z;

	float3 endView = startView + reflectDirView * magnitude;

	float4 startClip = TransformWViewToHClip(startView);
	float4 endClip = TransformWViewToHClip(endView);

	startClip /= startClip.w;
	endClip /= endClip.w;

	float3 rDirClip = normalize((endClip - startClip).xyz);

	startClip.xy *= float2(0.5, -0.5);
	startClip.xy += float2(0.5, 0.5);

	rDirClip.xy *= float2(0.5, -0.5);

	startTS = startClip;
	rDirTS = rDirClip;

	float maxDistanceX = abs(rDirTS.x < 0 ? -startTS.x / rDirTS.x : (1.0 - startTS.x) / rDirTS.x);
	float maxDistanceY = abs(rDirTS.y < 0 ? -startTS.y / rDirTS.y : (1.0 - startTS.y) / rDirTS.y);
	float maxDistanceZ = abs(rDirTS.z < 0 ? (1.0 - startTS.z / rDirTS.z): (startTS.z) / rDirTS.z);

	maxDistance = min(min(maxDistanceX, maxDistanceY), maxDistanceZ);
}

float4 RayMarchingInTextureSpace(float2 uv)
{
	float3 normal = normalize(SampleSceneNormals(uv));

	float up = dot(normal, float3(0, 1, 0));
	if (up < 0.5 || length(normal) == 0)
		return GetSource(uv);

	float rawDepth = SampleSceneDepth(uv);
	float linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
	float3 viewPos = ReconstructViewPos(uv, linearEyeDepth);

	float3 startTS;
	float3 rDirTS;
	float maxDistance;
	ComputeReflectDirAndMaxDistance(viewPos, normal, startTS, rDirTS, maxDistance);
	
	float3 endTS = startTS + rDirTS * maxDistance;

	float3 distanceUV = endTS - startTS;

	int2 startPixel = (int2)(startTS.xy * _SourceSize.xy);
	int2 endPixel = (int2)(endTS.xy * _SourceSize.xy);

	int maxDis = max(abs(endPixel.x - startPixel.x), abs(endPixel.y - startPixel.y));

	float3 deltaUV = distanceUV / (float) maxDis;

	float3 start = startTS + deltaUV;
	float3 cur = start;

	int hitIndex = -1;

#define AAA

	UNITY_LOOP
#ifdef AAA
	for (int i = 0; i < maxDis; i += 4)
	{
		float3 pos0 = cur + deltaUV * 0;
		float3 pos1 = cur + deltaUV * 1;
		float3 pos2 = cur + deltaUV * 2;
		float3 pos3 = cur + deltaUV * 3;

		float thinckness = 0.0;

		float depth0 = SampleSceneDepth(pos0.xy);
		float depth1 = SampleSceneDepth(pos1.xy);
		float depth2 = SampleSceneDepth(pos2.xy);
		float depth3 = SampleSceneDepth(pos3.xy);

		{
			float linearEyeDepth = LinearEyeDepth(depth3, _ZBufferParams);
			float linearEyeDepth3 = LinearEyeDepth(pos3.z, _ZBufferParams);

			thinckness = linearEyeDepth3 - linearEyeDepth;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 3 : hitIndex;
		}

		{
			float linearEyeDepth = LinearEyeDepth(depth2, _ZBufferParams);
			float linearEyeDepth2 = LinearEyeDepth(pos2.z, _ZBufferParams);

			thinckness = linearEyeDepth2 - linearEyeDepth;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 2 : hitIndex;
		}

		{
			float linearEyeDepth = LinearEyeDepth(depth1, _ZBufferParams);
			float linearEyeDepth1 = LinearEyeDepth(pos1.z, _ZBufferParams);

			thinckness = linearEyeDepth1 - linearEyeDepth;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 1 : hitIndex;
		}

		{
			float linearEyeDepth = LinearEyeDepth(depth0, _ZBufferParams);
			float linearEyeDepth0 = LinearEyeDepth(pos0.z, _ZBufferParams);

			thinckness = linearEyeDepth0 - linearEyeDepth;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 0 : hitIndex;
		}

		if (hitIndex != -1) break;

		cur = pos3 + deltaUV;
	}
#endif

#ifdef BBB

	for (int i = 0; i < maxDis; i += 4)
	{
		float3 pos0 = cur + deltaUV * 0;
		float3 pos1 = cur + deltaUV * 1;
		float3 pos2 = cur + deltaUV * 2;
		float3 pos3 = cur + deltaUV * 3;

		thinckness = 0.0;

		float depth0 = SampleSceneDepth(pos0.xy);
		float depth1 = SampleSceneDepth(pos1.xy);
		float depth2 = SampleSceneDepth(pos2.xy);
		float depth3 = SampleSceneDepth(pos3.xy);

		{
			thinckness = depth3 - pos3.z;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 3 : hitIndex;
		}

		{
			thinckness = depth2 - pos2.z;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 2 : hitIndex;
		}

		{
			thinckness = depth1 - pos1.z;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 1 : hitIndex;
		}

		{
			thinckness = depth0 - pos0.z;
			hitIndex = thinckness >= MIN_THINCKNESS && thinckness < MAX_THINCKNESS ? i + 0 : hitIndex;
		}

		if (hitIndex != -1) break;

		cur = pos3 + deltaUV;
	}

#endif

	float3 intesection = start + hitIndex * deltaUV;

	half4 color = GetSource(uv);
	
	return hitIndex == -1 ? color : color + GetSource(intesection.xy);
}

half4 SSR(Varyings input) : SV_Target
{
	return RayMarchingInTextureSpace(input.texcoord);
}

half4 Copy(Varyings input) : SV_Target
{
	return GetSource(input.texcoord);
}

#endif