#ifndef GRASS
#define GRASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
// Extended discussion on this function can be found at the following link:
// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
// Returns a number in the 0...1 range.
float rand(float3 co)
{
	return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

// Construct a rotation matrix that rotates around the provided axis, sourced from:
// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
float3x3 AngleAxis3x3(float angle, float3 axis)
{
	float c, s;
	sincos(angle, s, c);

	float t = 1 - c;
	float x = axis.x;
	float y = axis.y;
	float z = axis.z;

	return float3x3(
		t * x * x + c, t * x * y - s * z, t * x * z + s * y,
		t * x * y + s * z, t * y * y + c, t * y * z - s * x,
		t * x * z - s * y, t * y * z + s * x, t * z * z + c
		);
}

CBUFFER_START(UnityPerMaterial)
float4 _TopColor;
float4 _BottomColor;
float _BendRotation;
float _BladeWidth;
float _BladeWidthRandom;
float _BladeHeight;
float _BladeHeightRandom;
float _TessellationUniform;
float4 _WindFrequency;
float _WindStrength;
float _BladeForward;
float _BladeCurve;
float _Strength;
float _AmbientStrength;
CBUFFER_END

sampler2D _WindDistortionMap;
float4 _WindDistortionMap_ST;

float3 _PositionMoving;
float _Radius;

struct Attributes
{
	float4 positionOS : POSITION;
	float4 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 uv : TEXCOORD0;
};

struct Varyings
{
	float4 position : SV_POSITION;
	float3 normal : TEXCOORD3;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
};

Varyings grassVert(Attributes IN)
{
	Varyings OUT;

	OUT.position = IN.positionOS;
	OUT.normal = IN.normalOS;
	OUT.tangent = IN.tangentOS;

	OUT.uv = IN.uv;

	return OUT;
}

struct TessellationFactors
{
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

TessellationFactors patchConstantFunction(InputPatch<Attributes, 3> patch)
{
	TessellationFactors f;
	f.edge[0] = _TessellationUniform;
	f.edge[1] = _TessellationUniform;
	f.edge[2] = _TessellationUniform;
	f.inside = _TessellationUniform;
	return f;
}

[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[partitioning("integer")]
[patchconstantfunc("patchConstantFunction")]
Attributes hull(InputPatch<Attributes, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}

[domain("tri")]
Varyings domain(TessellationFactors factors, OutputPatch<Attributes, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
	Attributes v;

#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(positionOS)
	MY_DOMAIN_PROGRAM_INTERPOLATE(normalOS)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangentOS)

	return grassVert(v);
}

struct GeometryOutput
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	float3 worldPos : TEXCOORD3;
};

GeometryOutput VertexOutput(float3 pos, float3 normal, float2 uv)
{
	GeometryOutput o;

	o.pos = TransformObjectToHClip(pos);
	o.worldPos = TransformObjectToWorld(pos);
	o.normal = TransformObjectToWorldNormal(normal);
	o.uv = uv;

	return o;
}

#define SEGMENT_COUNT 5

[maxvertexcount(SEGMENT_COUNT * 2 + 1)]
void grassGeo(triangle Varyings i[3] : SV_POSITION, inout TriangleStream<GeometryOutput> stream)
{
	float3 pos = i[0].position;
	float3 normal = i[0].normal;
	float3 tangent = i[0].tangent;
	float3 binormal = cross(normal, tangent) * i[0].tangent.w;

	float3x3 tangentToLocal = float3x3(
		tangent.x, binormal.x, normal.x,
		tangent.y, binormal.y, normal.y,
		tangent.z, binormal.z, normal.z
		);

	float3x3 rotationYMatrix = AngleAxis3x3(rand(pos) * 2 * 3.141592654, float3(0, 0, 1));
	float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * 0.5 * 3.141592654 * _BendRotation, float3(-1, 0, 0));
	float3x3 rotationMatrix = mul(rotationYMatrix, bendRotationMatrix);
	float3x3 transformMatrix = mul(tangentToLocal, rotationMatrix);

	float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
	float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1) * _WindStrength;
	float3 wind = normalize(float3(windSample.x, windSample.y, 0));
	float3x3 windRotation = AngleAxis3x3(3.141592654 * windSample, wind);
	if(_WindStrength != 0.0)
		transformMatrix = mul(transformMatrix, windRotation);

	float3x3 transformMatrixFacing = mul(tangentToLocal, rotationYMatrix);

	float width = _BladeWidth + _BladeWidthRandom * (rand(pos.xzy) * 2 - 1);
	float height = _BladeHeight + _BladeHeightRandom * (rand(pos.zyx) * 2 - 1);

	width *= 10;
	height *= 10;

	float forward = rand(pos.yyz) * _BladeForward;

	float3 tangentNormal = float3(0, -1, 0);
	float3 localNormal = mul(tangentToLocal, tangentNormal);

	float3 worldPos = TransformObjectToWorld(pos);
	float dis = distance(_PositionMoving, worldPos);
	float3 radius = 1.0 - saturate(dis / _Radius);
	float3 sphereDis = worldPos - _PositionMoving;
	sphereDis *= radius;
	//sphereDis = clamp(sphereDis * _Strength, -0.8, 0.8);
	sphereDis *= 10;

	for (int i = 0; i < SEGMENT_COUNT; i++)
	{
		float t = i / (float)SEGMENT_COUNT;
		float w = width * (1 - t);
		float h = height * t;
		float3x3 mat = i == 0 ? transformMatrixFacing : transformMatrix;

		float f = pow(t, _BladeCurve) * forward;

		float3 offset1 = float3(w, f, h);
		float3 offset2 = float3(-w, f, h);

		float3 newpos = i == 0 ? pos : pos + sphereDis * t;

		stream.Append(VertexOutput(newpos + mul(mat, offset1), localNormal, float2(0, t)));
		stream.Append(VertexOutput(newpos + mul(mat, offset2), localNormal, float2(1, t)));
	}

	stream.Append(VertexOutput(pos + float3(sphereDis.x, sphereDis.y, sphereDis.z) + mul(transformMatrix, float3(0, forward, height)), localNormal, float2(0.5, 1)));
}

float4 grassFrag(GeometryOutput IN, float facing : VFACE) : SV_Target
{
	float3 normal = facing > 0 ? IN.normal : -IN.normal;

	float4 shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
#if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
	Light mainLight = GetMainLight(shadowCoord);
#else
	Light mainLight = GetMainLight();
#endif

	half shadow = mainLight.shadowAttenuation;

	float3 extraLights;
	int pixelLightCount = GetAdditionalLightsCount();
	for (int j = 0; j < pixelLightCount; ++j) 
	{
		Light light = GetAdditionalLight(j, IN.worldPos, half4(1, 1, 1, 1));
		float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
		extraLights += attenuatedLightColor;
	}

	float4 baseColor = lerp(_BottomColor, _TopColor, saturate(IN.uv.y));

	float4 litColor = (baseColor * float4(mainLight.color, 1));

	litColor += float4(extraLights, 1);

	float4 final = litColor * shadow;
	final += saturate((1 - shadow) * baseColor * 0.2);
	final += (unity_AmbientSky * _AmbientStrength);

	return final;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
	return 0;
}

#endif