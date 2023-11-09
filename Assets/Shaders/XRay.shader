Shader "FX/XRay"
{
    Properties
    {
        _XRayColor ("XRay Color", Color) = (1,1,1,1)
    }

    SubShader
    {

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "XRay"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite off
            ZTest Greater
            Blend SrcAlpha One
            Cull  Back

            HLSLPROGRAM

            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _XRayColor;
            CBUFFER_END

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normal : NORMAL;
            };

            struct Varings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
 
            };

            Varings vert(Attributes IN)
            {
                Varings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;

                half3 viewDir = normalize(GetObjectSpaceNormalizeViewDir(IN.positionOS.xyz));
                float3 normal = normalize(IN.normal);
                float rim = 1 - dot(viewDir, normal);
                OUT.color = _XRayColor * rim;

                return OUT;
            }

            float4 frag(Varings IN) :SV_Target
            {
                return IN.color;
            }

            ENDHLSL
        }
    }
}
