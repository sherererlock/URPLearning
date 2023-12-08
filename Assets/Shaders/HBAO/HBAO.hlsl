#ifndef HBAOINCLUDE
#define HBAOINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

half4 _HBAOParams
float4 _CameraLT;
float4 _CameraXExtent;
float4 _CameraYExtent;
float4 _SourceSize;
float4 _ProjectionParams2;

#define DIRECTIONS_NUM 4
#define STEP_NUM 5
#define FALLOFF _HBAOParams.w

static const half  HALF_TWO_PI = half(6.28318530717958647693);
static const float SKY_DEPTH_VALUE = 0.00001;
static const half  HALF_ZERO = half(0.0);
static const half  HALF_HALF = half(0.5);

half4 PackAONormal(half ao, half3 n)
{
	n *= HALF_HALF;
	n += HALF_HALF;
	return half4(ao, n);
}

float GetLinearEyeDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
	return LinearDepthToEyeDepth(rawDepth);
#else
	return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float3 ReconstructNormal(float2 uv, float linearDepth, float3 vpos)
{
#if defined(_SOURCE_DEPTH_LOW)
	return half3(normalize(cross(ddy(vpos), ddx(vpos))));
#else
    float2 delta = float2(_SourceSize.zw * 2.0);

    float2 pixelDensity = float2(1, 1);

    // Sample the neighbour fragments
    float2 lUV = float2(-delta.x, 0.0) * pixelDensity;
    float2 rUV = float2(delta.x, 0.0) * pixelDensity;
    float2 uUV = float2(0.0, delta.y) * pixelDensity;
    float2 dUV = float2(0.0, -delta.y) * pixelDensity;

    float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearEyeDepth(l1.xy); // Left1
    float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearEyeDepth(r1.xy); // Right1
    float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearEyeDepth(u1.xy); // Up1
    float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearEyeDepth(d1.xy); // Down1

    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
#if defined(_SOURCE_DEPTH_MEDIUM)
    uint closest_horizontal = l1.z > r1.z ? 0 : 1;
    uint closest_vertical = d1.z > u1.z ? 0 : 1;
#else
    float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearEyeDepth(l2.xy); // Left2
    float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearEyeDepth(r2.xy); // Right2
    float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearEyeDepth(u2.xy); // Up2
    float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearEyeDepth(d2.xy); // Down2

    const uint closest_horizontal = abs((2.0 * l1.z - l2.z) - linearDepth) < abs((2.0 * r1.z - r2.z) - linearDepth) ? 0 : 1;
    const uint closest_vertical = abs((2.0 * d1.z - d2.z) - linearDepth) < abs((2.0 * u1.z - u2.z) - linearDepth) ? 0 : 1;
#endif

    // Calculate the triangle, in a counter-clockwize order, to
    // use based on the closest horizontal and vertical depths.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // Calculate the view space positions for the three points...
    half3 P1;
    half3 P2;
    if (closest_vertical == 0)
    {
        P1 = half3(closest_horizontal == 0 ? l1 : d1);
        P2 = half3(closest_horizontal == 0 ? d1 : r1);
    }
    else
    {
        P1 = half3(closest_horizontal == 0 ? u1 : r1);
        P2 = half3(closest_horizontal == 0 ? l1 : u1);
    }

    // Use the cross product to calculate the normal...
    return half3(normalize(cross(ReconstructViewPos(P2.xy, P2.z) - vpos, ReconstructViewPos(P1.xy, P1.z) - vpos)));
#endif
}

half3 ReconstructViewPos(float2 uv, float linearDepth)
{
	uv.y = 1 - uv.y;

	float scale = linearDepth * _ProjectionParams2.x;
	float3 npos = (_CameraLT + uv.x * _CameraXExtent + uv.y * _CameraYExtent).xyz;

	return half3(npos * scale);
}

float3 GetNormal(float2 uv, float linearDepth)
{
#if defined (_SOURCE_DEPTH_NORMALS)
	return SampleSceneNormals(uv);
#else
    float3 vpos = ReconstructViewPos(uv, linearDepth);
	return ReconstructNormal(uv, linearDepth, vpos);
#endif
}

float4 HBAO(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float2 uv = input.texcoord;
	float depth = SampleSceneDepth(uv);
	if (depth < SKY_DEPTH_VALUE)
		return PackAONormal(HALF_ZERO, HALF_ZERO);

	float linearDepth_o = GetLinearEyeDepth(rawDepth_o);
	half halfLinearDepth_o = half(linearDepth_o);
	if (halfLinearDepth_o > FALLOFF)
		return PackAONormal(HALF_ZERO, HALF_ZERO);

	float3 normal = GetNormal(uv, linearDepth_o);
	float3 position = ReconstructViewPos(uv, linearDepth_o);

	float deltaAngle = HALF_TWO_PI / DIRECTION_NUM
	float angle = 0.0;
	for (int i = 0; i < DIRECTIONS_NUM; i++)
	{
		angle += deltaAngle * i;
		float2 direction = float2(cos(angle), sin(angle));
		for (int j = 0; j < STEP_NUM; j++)
		{

		}
	}

#if defined (_SOURCE_DEPTH_NORMALS)
	return float4(1, 0, 0, 1);
#else 
	return float4(0, 1, 0, 1);
#endif
	
}

float4 HBAOBlur(Varyings input) : SV_Target
{
	return float4(1, 0, 0, 1);
}

#endif // !HBAOINCLUDE
