Shader "FX/StencilMask"
{
    Properties
    {
        _ID("Mask ID", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _SComp("Stencil Compare", Float ) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _SOp("Stencil Op", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            int _ID;
        CBUFFER_END

        ENDHLSL

        ColorMask 0
        ZWrite off
        Stencil
        {
            Ref [_ID]
            Comp [_SComp]
            Pass [_SOp]
        }

        Pass
        {
            Name "StencilMask"

            HLSLPROGRAM

            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varings
            {
                float4 positionCS : SV_POSITION;

            };

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS.xyz);
                return positionInputs.positionCS;
            }

            float4 frag(float4 positionCS : POSITION) :SV_Target
            {
                return float4(1, 0, 0, 1);
            }

            ENDHLSL
        }
    }
}
