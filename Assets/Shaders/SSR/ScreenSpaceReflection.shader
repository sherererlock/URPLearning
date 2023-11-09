Shader "Hidden/ScreenSpaceReflection"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        Cull off ZWrite off ZTest Always

        Pass
        {
            Name "ScreenSpaceReflection"
            ZTest Always
            ZWrite off
            Cull off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment SSR
            #include "ScreenSpaceReflection.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Copy"
            ZTest Always
            ZWrite off
            Cull off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Copy
            #include "ScreenSpaceReflection.hlsl"
            ENDHLSL
        }
    }
}
