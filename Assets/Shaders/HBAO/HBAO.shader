Shader "Hidden/HBAO"
{
    Properties
    {
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        LOD 100

        Pass
        {
            Name "HBAOPass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment HBAO
            #pragma multi_compile_local_fragment _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS
            #include "HBAO.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HBAOBlurPass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment HBAOBlur

            #include "HBAO.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HBAOFinalblitPass"

            HLSLPROGRAM

            ENDHLSL
        }
    }
}
