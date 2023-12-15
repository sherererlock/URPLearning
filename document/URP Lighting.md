# URP Lighting

```mermaid
classDiagram
class Attributes{
	float4 positionOS;
	float3 normalOS;
	float4 tangentOS;
	float2 texcoord;
	float2 staticLightmapUV;
	float2 dynamicLightmapUV;
}

class Varyings{
	float2 uv;
	float3 positionWS;
	float3 normalWS;
	half4 tangentWS;
	float4 shadowCoord;
	float4 positionCS;
}

class VertexPositionInputs{
	float3 positionWS;
    float3 positionVS;
    float4 positionCS;
    float4 positionNDC
}

class VertexNormalInputs{
    real3 tangentWS;
    real3 bitangentWS;
    float3 normalWS;
}
```

```mermaid
classDiagram
class SurfaceData{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;
}

class InputData{
	float3  positionWS;
    float4  positionCS;
    float3  normalWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
    half    fogCoord;
    half3   vertexLighting;
    half3   bakedGI;
    float2  normalizedScreenSpaceUV;
    half4   shadowMask;
    half3x3 tangentToWorld;
}

class BRDFData{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;
    half normalizationTerm; 
    half roughness2MinusOne; 
}
```

```mermaid
classDiagram
class Light{
    half3   direction;
    half3   color;
    float   distanceAttenuation;
    half    shadowAttenuation;
    uint    layerMask;
}

```



```c
Varyings LitPassVertex(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz); // 转换空间，ndc坐标并未执行透视除法
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.normalWS = normalInput.normalWS;
    output.positionWS = vertexInput.positionWS;
    
    // 获取shadodwCoord
    // cascade index计算
    // 获取Matrix
    // 执行转换
    output.shadowCoord = GetShadowCoord(vertexInput); 
    
    output.positionCS = vertexInput.positionCS;
}

```

```c

void LitPassFragment(Varyings input, out half4 outColor : SV_Target0)
{
    // 采样贴图得到各种表面得属性
    // albedo
    // metallic, smooth
    // normal
    // occlusion
    // emission
    // detailUv
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    
   	// 在两个lod之间平滑过渡
    LODFadeCrossFade(input.positionCS);
    
    // 基本上是把Varyings的值拷贝到InputData中
    // 计算了tangentToWorld矩阵
    // 为每个fragment计算shadowCoord
    //  采样贴图得到shadowmask,bakedGI
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData); 
    
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    outColor = color;
}
```

```c
inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic); // 物体不反射的属性
    half reflectivity = half(1.0) - oneMinusReflectivity; // 反射的属性
    
    half3 brdfDiffuse = albedo * oneMinusReflectivity; 
    half3 brdfSpecular = lerp(kDieletricSpec.rgb, albedo, metallic); //F0
}

half3 SubtractDirectMainLightFromLightmap(Light mainLight, half3 normalWS, half3 bakedGI)
{
    half shadowStrength = GetMainLightShadowStrength(); // 光的阴影强度属性
    half contributionTerm = saturate(dot(mainLight.direction, normalWS));
    half3 lambert = mainLight.color * contributionTerm;
    half3 estimatedLightContributionMaskedByInverseOfShadow = lambert * (1.0 - mainLight.shadowAttenuation);
    half3 subtractedLightmap = bakedGI - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    half3 realtimeShadow = max(subtractedLightmap, _SubtractiveShadowColor.xyz);
    realtimeShadow = lerp(bakedGI, realtimeShadow, shadowStrength);

    // 3) Pick darkest color
    return min(bakedGI, realtimeShadow);
}

void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout half3 bakedGI)
{
    bakedGI = SubtractDirectMainLightFromLightmap(light, normalWS, bakedGI);
}

half4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)
{
    // 初始化brdf
    // albedo、brdf的漫反射项diffuse, brdf的镜面反射项specular(F0)
    // 物体的反射率reflectivity
    // 从smoothness计算出roughness
    // grazingTerm
    InitializeBRDFData(surfaceData, brdfData);
    
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    
    half4 shadowMask = CalculateShadowMask(inputData);
    
    // 获取方向光信息
    // direction和color
    //
    //
    //
 
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    {
        // realtime shadow 采样阴影贴图(软硬阴影)
        // bakedShadow 
        // 
        light.shadowAttenuation = MainLightShadow(shadowCoord, positionWS, shadowMask, _MainLightOcclusionProbes);
    }
}


```

