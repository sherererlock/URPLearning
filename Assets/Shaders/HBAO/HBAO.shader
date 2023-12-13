Shader "Hidden/HBAO"
{

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

            #pragma enable_d3d11_debug_symbols
            #pragma vertex Vert
            #pragma fragment HBAO
            #pragma multi_compile_local_fragment _SOURCE_DEPTH_LOW _SOURCE_DEPTH_MEDIUM _SOURCE_DEPTH_HIGH _SOURCE_DEPTH_NORMALS
            #include "HBAO.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HBAOBlurHPass"

            HLSLPROGRAM

            #pragma enable_d3d11_debug_symbols
            #pragma vertex Vert
            #pragma fragment HBAOBlurH

            #include "HBAOBlur.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HBAOBlurVPass"

            HLSLPROGRAM

            #pragma enable_d3d11_debug_symbols
            #pragma vertex Vert
            #pragma fragment HBAOBlurV

            #include "HBAOBlur.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HBAOCompositePass"

            HLSLPROGRAM

            #pragma enable_d3d11_debug_symbols
            #pragma vertex Vert
            #pragma fragment HBAOComposite
            #pragma multi_compile_local __ _DEBUG_AO_ONLY

            #include "HBAO.hlsl"

            ENDHLSL
        }
    }
}
