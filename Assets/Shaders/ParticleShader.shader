Shader "Unlit/ParticleShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 color : COLOR0;
                float4 vertex : SV_POSITION;
            };

            struct ParticelData
            {
                float3 position;
                float4 color;
            };

            StructuredBuffer<ParticelData> _particleDataBuffer;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(_particleDataBuffer[id].position, 0));
                o.color = _particleDataBuffer[id].color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
