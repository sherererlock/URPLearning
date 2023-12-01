#ifndef UNIVERSAL_DEPTH_MIPMAP_INCLUDED
#define UNIVERSAL_DEPTH_MIPMAP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#define SAMPLE(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthAttachment, sampler_PointClamp, uv)
#define DEPTH_TEXTURE(name) TEXTURE2D_FLOAT(name)

DEPTH_TEXTURE(_CameraDepthAttachment);
SAMPLER(sampler_CameraDepthAttachment);

float frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float4 depth;
    float2 uv = input.texcoord.xy;

    depth.x = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
    depth.y = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv + ( _BlitTextureSize.x, 0), 0);
    depth.z = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv + (0, _BlitTextureSize.y), 0);
    depth.w = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv + (_BlitTextureSize.x, _BlitTextureSize.y), 0);

    //depth.x = _BlitTexture.SampleLevel(sampler_PointClamp, float3(uv, 0), 0);
    //depth.y = _BlitTexture.SampleLevel(sampler_PointClamp, float3(uv + ( _BlitTextureSize.x, 0), 0), 0);
    //depth.z = _BlitTexture.SampleLevel(sampler_PointClamp, float3(uv + (0, _BlitTextureSize.y), 0), 0);
    //depth.w = _BlitTexture.SampleLevel(sampler_PointClamp, float3(uv + (_BlitTextureSize.x, _BlitTextureSize.y), 0), 0);

#if UNITY_REVERSED_Z
    float mdepth = min(min(min(depth.x, depth.y), depth.z), depth.w);
#else
    float mdepth = max(max(max(depth.x, depth.y), depth.z), depth.w);
#endif

    return mdepth;
}

float SampleDepth(float2 uv)
{
    return SAMPLE(uv);
}

float CopyDepth(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return SampleDepth(input.texcoord);
}

#endif
