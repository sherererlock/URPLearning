#ifndef SCREENSPACEREFLECTION
#define SCREENSPACEREFLECTION

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

TEXTURE2D(_SSRTexture);
SAMPLER(sampler_BlitTexture);

half4 GetSource(half2 uv) {
	return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, uv, _BlitMipLevel);
}

half4 SSR(Varyings input) : SV_Target
{
	float2 uv = input.texcoord;
	float3 normal = normalize(SampleSceneNormals(uv));

	return half4(color, 1);
}

half4 Copy(Varyings input) : SV_Target
{
	return GetSource(input.texcoord);
}

#endif