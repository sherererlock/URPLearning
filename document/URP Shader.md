# URP Shader

1. 采样深度贴图的方法

   ```c
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   
   float SampleDepth(float2 uv)
   {
       return SampleSceneDepth(uv.xy);
   }
   
   float GetLinearEyeDepth(float rawDepth)
   {
   #if defined(_ORTHOGRAPHIC)
       return LinearDepthToEyeDepth(rawDepth);
   #else
       return LinearEyeDepth(rawDepth, _ZBufferParams);
   #endif
   }
   
   float SampleAndGetLinearEyeDepth(float2 uv)
   {
       const float rawDepth = SampleDepth(uv);
       return GetLinearEyeDepth(rawDepth);
   }
   
   ```

2. 普通贴图的声明以及采样

   ```c
    [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
   
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;
   CBUFFER_END
       
   TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
   
   float2 uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
   float4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
   ```

3. RT贴图的声明以及采样

   ```
   TEXTURE2D_X(_BlitTexture);
   uniform float2 _BlitTextureSize;
   ```

   

4. 采样normal贴图

   ```c
   // 开启unity normal texture渲染时，需要在shader中加入LightMode为DepthNormals的pass
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
   float3 normal = SampleSceneNormals(IN.uv);
   ```

5. shader文件中不能有没有vert和fragment的空pass

6. 随机值

7. ![img](D:\Games\URPLearning\document\unity空间旋向.webp)

8. shader中的常用函数

   | 常用函数 | 作用                                           |
   | -------- | ---------------------------------------------- |
   | frac     | 返回传入参数的小数部分                         |
   | rsqrt    | 1除以开方后的参数                              |
   | round    | 返回最接近的整数                               |
   | exp      | 返回以e为底，以参数为指数的值，相当于pow(e, n) |
   | exp2     | 返回以2为底，以参数为指数的值，相当于pow(2, n) |

9. urp两个blit之间不能设置shader变量





