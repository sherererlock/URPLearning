Shader "Hidden/DepthMipmapGenerator"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "Depth Mipmap Generator"
            ZTest Always ZWrite Off ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Assets/Shaders/Hiz/DepthMipmapGenerator.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Copy Depth"
            ZTest Always ZWrite Off ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CopyDepth

            #include "Assets/Shaders/Hiz/DepthMipmapGenerator.hlsl"

            ENDHLSL
        }

    }
}
