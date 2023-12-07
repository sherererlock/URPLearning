using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HBAORendererFeature : ScriptableRendererFeature
{
    public enum DepthSource
    {
        Depth,
        DepthNormal
    }

    Material aoMaterial;
    public DepthSource depthSource;

    HBAORenderPass hbaoPass;
    Material GetMaterial()
    {
        if (aoMaterial == null)
            aoMaterial = new Material(Shader.Find("Hidden/HBAO"));

        return aoMaterial;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (GetMaterial() == null)
            return;

        hbaoPass.Setup(aoMaterial, depthSource, renderer, ref renderingData);
        renderer.EnqueuePass(hbaoPass);
    }

    public override void Create()
    {
        if (hbaoPass == null)
            hbaoPass = new HBAORenderPass();
    }

    protected override void Dispose(bool disposing)
    {
        hbaoPass?.Dispose();
        hbaoPass = null;
        CoreUtils.Destroy(aoMaterial);
    }

    class HBAORenderPass : ScriptableRenderPass
    {
        ScriptableRenderer renderer;
        Material aoMaterial;
        DepthSource depthSource;
        string profilerTag = "HBAO";
        private ProfilingSampler m_ProfilingSampler;

        RTHandle[] HBAOTextures = new RTHandle[4];

        const string k_SourceDepth = "_SOURCE_DEPTH";
        const string k_SourceDepthNormal = "_SOURCE_DEPTH_NORMALS";

        public HBAORenderPass()
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
        }

        public void Setup(Material material, DepthSource source, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            aoMaterial = material;
            depthSource = source;
            this.renderer = renderer;
            switch (depthSource)
            {
                case DepthSource.Depth:
                    ConfigureInput(ScriptableRenderPassInput.Depth);
                    break;

                case DepthSource.DepthNormal:
                    ConfigureInput(ScriptableRenderPassInput.Normal);
                    break;
            }

            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            switch (depthSource)
            {
                case DepthSource.Depth:
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepth, true);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthNormal, false);
                    break;

                case DepthSource.DepthNormal:
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepth, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthNormal, true);
                    break;
            }

            RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            renderTextureDescriptor.msaaSamples = 1;
            renderTextureDescriptor.depthBufferBits = 0;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[0], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "aotexture");
            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[1], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "aoblurhtexture");
            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[2], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "aoblurvtexture");
            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[2], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "texture");

            ConfigureTarget(renderer.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);
              
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(aoMaterial == null)
            {
                Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);

                return;
            }

            var cmd = renderingData.commandBuffer;
            using(new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, renderer.cameraColorTargetHandle, HBAOTextures[0], aoMaterial, 0);
                Blitter.BlitCameraTexture(cmd, HBAOTextures[0], renderer.cameraColorTargetHandle);
            }
        }

        public void Dispose()
        {
            HBAOTextures[0]?.Release();
            HBAOTextures[1]?.Release();
            HBAOTextures[2]?.Release();
            HBAOTextures[3]?.Release();
        }
    }
}
