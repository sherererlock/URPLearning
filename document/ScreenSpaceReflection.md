# ScreenSpaceReflection

## URP实现

### c#流程

```c#
UniversalRenderPipeline.Render(ScriptableRenderContext renderContext, List<Camera> cameras)
{
    SetHDRState(cameras);
    
    BeginContextRendering(renderContext, cameras); //调用回调函数
    
    SetupPerFrameShaderConstants();
    
    SortCameras(cameras);// by depth
    
    BeginCameraRendering(); // 调用回调函数
    
    RenderSingleCameraInternal();
    
    EndCameraRendering(renderContext, camera);
    
    EndContextRendering(renderContext, cameras);
}

UniversalRenderPipeline.RenderSingleCameraInternal(ScriptableRenderContext context, Camera camera)
{
    InitializeCameraData(camera, additionalCameraData, true, out var cameraData);
    {
        cameraData = new CameraData();
        InitializeStackedCameraData(camera, additionalCameraData, ref cameraData);
        {
            cameraData.targetTexture = baseCamera.targetTexture;
            cameraData.cameraType = baseCamera.cameraType;
            
            cameraData.isStopNaNEnabled = baseAdditionalCameraData.stopNaN && SystemInfo.graphicsShaderLevel >= 35;
            cameraData.isDitheringEnabled = baseAdditionalCameraData.dithering;
            cameraData.antialiasing = baseAdditionalCameraData.antialiasing;
            cameraData.antialiasingQuality = baseAdditionalCameraData.antialiasingQuality;
            cameraData.allowHDROutput = baseAdditionalCameraData.allowHDROutput;
            
            cameraData.isHdrEnabled = baseCamera.allowHDR && settings.supportsHDR;
            cameraData.pixelRect = baseCamera.pixelRect;
            cameraData.pixelWidth = baseCamera.pixelWidth;
            cameraData.pixelHeight = baseCamera.pixelHeight;
            cameraData.aspectRatio = (float)cameraData.pixelWidth / (float)cameraData.pixelHeight; 
        }
        
        // width, height, scale msaa sample
        cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData.renderScale,cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, msaaSamples, needsAlphaChannel, cameraData.requiresOpaqueTexture);  
    }
    
    // 后处理，单独的RT以及TAA数据的设置
    InitializeAdditionalCameraData(camera, additionalCameraData, true, ref cameraData);
    {
        cameraData.renderType = additionalCameraData.renderType;
        cameraData.clearDepth = (additionalCameraData.renderType != CameraRenderType.Base) ? additionalCameraData.clearDepth : true;
        cameraData.postProcessEnabled = additionalCameraData.renderPostProcessing;
        cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
        cameraData.requiresDepthTexture = additionalCameraData.requiresDepthTexture;
        cameraData.requiresOpaqueTexture = additionalCameraData.requiresColorTexture;
        cameraData.renderer = additionalCameraData.scriptableRenderer;
        cameraData.useScreenCoordOverride = additionalCameraData.useScreenCoordOverride;
        cameraData.screenSizeOverride = additionalCameraData.screenSizeOverride;
        cameraData.screenCoordScaleBias = additionalCameraData.screenCoordScaleBias; 
        
        if (additionalCameraData != null)
            UpdateTemporalAAData(ref cameraData, additionalCameraData);
        
        ApplyTaaRenderingDebugOverrides(ref cameraData.taaSettings);
        Matrix4x4 jitterMat = TemporalAA.CalculateJitterMatrix(ref cameraData);
        cameraData.SetViewProjectionAndJitterMatrix(camera.worldToCameraMatrix, projectionMatrix, jitterMat);  
    }
    
    RenderSingleCamera(context, ref cameraData, cameraData.postProcessEnabled);
}

UniversalRenderPipeline.RenderSingleCamera(ScriptableRenderContext context, ref CameraData cameraData, bool anyPostProcessingEnabled)
{
    ScriptableRenderer.current = renderer;
    CommandBuffer cmd = CommandBufferPool.Get();
    
    renderer.OnPreCullRenderPasses(in cameraData);
    renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);
    
    SetupPerCameraShaderConstants(cmd);
    
    additionalCameraData.motionVectorsPersistentData.Update(ref cameraData);
    UpdateTemporalAATargets(ref cameraData);
    
    var cullResults = context.Cull(ref cullingParameters);
    InitializeRenderingData(asset, ref cameraData, ref cullResults, anyPostProcessingEnabled, cmd, out var renderingData);
    
    renderer.AddRenderPasses(ref renderingData);
    {
        rendererFeatures[i].AddRenderPasses(this, ref renderingData);
        {
            m_SSRPass.Setup(ref settings, ref renderer, ref m_Material);
            {
                ConfigureInput(ScriptableRenderPassInput.Normal);
            }
            renderer.EnqueuePass(m_SSRPass);
        }
    }
    
    renderer.Setup(context, ref renderingData);
    renderer.Execute(context, ref renderingData);
}

UniversalRenderPipeline.InitializeRenderingData(UniversalRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults 		cullResults,bool anyPostProcessingEnabled, CommandBuffer cmd, out RenderingData renderingData)
{
    renderingData.cullResults = cullResults;
    renderingData.cameraData = cameraData;
    InitializeLightData(settings, visibleLights, mainLightIndex, out renderingData.lightData);
    InitializeShadowData(settings, visibleLights, mainLightCastShadows, additionalLightsCastShadows && 										!renderingData.lightData.shadeAdditionalLightsPerVertex, isForwardPlus, out renderingData.shadowData);
    InitializePostProcessingData(settings, out renderingData.postProcessingData);    
    renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount, isForwardPlus);
    
}

UniversalRenderer.Setup(ScriptableRenderContext context, ref RenderingData renderingData)
{
    RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);
    {
        bool needsNormals = (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
        inputSummary.requiresDepthPrepass |= needsNormals || needsDepth && eventBeforeMainRendering;
        inputSummary.requiresNormalsTexture |= needsNormals;
    }
    
    bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;
    
    bool requiresDepthPrepass = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && (!CanCopyDepth(ref renderingData.cameraData) || forcePrepass);
    requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
    requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;   
    
     ConfigureCameraTarget(m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);
    EnqueuePass(m_MainLightShadowCasterPass);
    EnqueuePass(m_AdditionalLightsShadowCasterPass);
    
    if ((this.renderingModeActual == RenderingMode.Deferred && !this.useRenderPassEnabled) || requiresDepthPrepass || requiresDepthCopyPass)
    {
        RenderingUtils.ReAllocateIfNeeded(ref m_DepthTexture, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthTexture");        
        
        cmd.SetGlobalTexture(m_DepthTexture.name, m_DepthTexture.nameID);
    }
    
    if (requiresDepthPrepass && renderPassInputs.requiresNormalsTexture)
    {
        RenderingUtils.ReAllocateIfNeeded(ref normalsTexture, normalDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraNormalsTexture");       
        cmd.SetGlobalTexture(normalsTexture.name, normalsTexture.nameID);
        
        m_DepthNormalPrepass.Setup(m_DepthTexture, m_NormalsTexture);
        EnqueuePass(m_DepthNormalPrepass);
    }
}

ScriptableRenderer.Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    ScriptableRenderer.SetupRenderPasses(in RenderingData renderingData);
    {
        rendererFeatures[i].SetupRenderPasses(this, in renderingData);
    }
    
    CommandBuffer cmd = CommandBufferPool.Get();
    
    InternalStartRendering(context, ref renderingData);
    {
        // 设置shader uniform变量
        // 分配Blit RT
        // 配置Target和ClearFlag
        ScreenSpaceReflectionPass.OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData);
    }
    
    SortStable(m_ActiveRenderPassQueue);
    
    ScriptableRenderPass.Configure(cmd, cameraData.cameraTargetDescriptor);
    
    // 设置camera相关的Shader全局变量
    SetPerCameraProperties(CommandBuffer cmd, ref CameraData cameraData, bool isTargetFlipped);
}

```



#### 疑问

1. Blit是否不会检查ConfigureTarget的配置，而是将内容直接渲染在Blit设置的RT上

2. Blit中的loadAction和StoreAction指的是作用于哪个buffer的Action？默认值是什么？

3. Execute中获取到renderingData中的cmd后还需要显示调用ExecuteCommandBuffer？

4. 打包到手机上为何无效果

   