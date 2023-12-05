Shader "Custom/Grass"
{
    Properties
    {   
        _TopColor("Top Color", Color) = (0,1,0,1)
        _BottomColor("Bottom Color", Color) = (0,0,0,1)
        _BendRotation("Bend Rotaion Random", Range(0, 1)) = 0.2

        _BladeWidth("Blade width", Float) = 1
        _BladeWidthRandom("Blade width random", Range(0, 1)) = 0.2
        _BladeHeight("Blade Height", Float) = 1
        _BladeHeightRandom("Blade height random", Range(0, 1)) = 0.25

        _TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1

        _WindStrength("Wind Strength", Float) = 1
        _WindDistortionMap("Wind Texture", 2D) = "white" {}
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)

        _BladeForward("Blade Forward", Float) = 0.38
        _BladeCurve("Blade Curve", Range(1, 4)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        Pass
        {
            Name "URP Grass SimpleLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 4.6

            #pragma vertex grassVert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry grassGeo
            #pragma fragment grassFrag

            #include "grass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull off

            HLSLPROGRAM
            #pragma target 4.6

            // -------------------------------------
            // Shader Stages
            #pragma vertex grassVert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry grassGeo
            #pragma fragment ShadowPassFragment

            // Includes
            #include "grass.hlsl"

            ENDHLSL
        }
    }
}
