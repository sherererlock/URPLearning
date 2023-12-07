#ifndef HBAOINCLUDE
#define HBAOINCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

float4 HBAO(Varyings input) : SV_Target
{
	return float4(1, 0, 0, 1);
}

float4 HBAOBlur(Varyings input) : SV_Target
{
	return float4(1, 0, 0, 1);
}

#endif // !HBAOINCLUDE
