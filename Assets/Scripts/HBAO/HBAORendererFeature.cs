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

    [System.Serializable]
    public class AOSettings
    {
        public float AORadius;
        public float RadiusMaxPixel;
        public float MaxDistance;
        public float DistanceFallOff;
        public float Intensity;
        public float AngleBias;
        public bool aoOnly;

        [Space(10), Range(0, 16)]
        public float sharpness;
        public AOSettings()
        {
            AORadius = 0.8f;
            RadiusMaxPixel = 128f;
            MaxDistance = 150f;
            DistanceFallOff = 50f;
            aoOnly = false;
            sharpness = 1.0f;
            Intensity = 1.0f;
            AngleBias = 0.05f;
        }
    }

    public AOSettings aOSettings = new AOSettings();

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
            hbaoPass = new HBAORenderPass(aOSettings);
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
        AOSettings aoSettings;

        string profilerTag = "HBAO";
        private ProfilingSampler m_ProfilingSampler;

        RTHandle[] HBAOTextures = new RTHandle[4];

        const string k_SourceDepth = "_SOURCE_DEPTH";
        const string k_SourceDepthNormal = "_SOURCE_DEPTH_NORMALS";
        const string k_SourceDepthLowKeyword = "_SOURCE_DEPTH_LOW";
        const string k_SourceDepthMediumKeyword = "_SOURCE_DEPTH_MEDIUM";
        const string k_SourceDepthHighKeyword = "_SOURCE_DEPTH_HIGH";
        const string k_AOOnly = "_DEBUG_AO_ONLY";

        public HBAORenderPass(AOSettings aoSettings)
        {
            this.aoSettings = aoSettings;
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
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthLowKeyword, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthMediumKeyword, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthHighKeyword, true);

                    break;

                case DepthSource.DepthNormal:
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepth, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthNormal, true);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthLowKeyword, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthMediumKeyword, false);
                    CoreUtils.SetKeyword(aoMaterial, k_SourceDepthHighKeyword, false);
                    break;
            }

            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();
            view.SetColumn(3, new Vector4(0, 0, 0, 1.0f));
            Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix();
            Matrix4x4 vp = proj * view;

            Vector4 cameraLeftTop = vp.inverse.MultiplyPoint(new Vector4(-1, 1, -1, 1));
            Vector4 cameraRightTop = vp.inverse.MultiplyPoint(new Vector4(1, 1, -1, 1));
            Vector4 cameraLeftBottom = vp.inverse.MultiplyPoint(new Vector4(-1, -1, -1, 1));

            Vector4 cameraXExtent = cameraRightTop - cameraLeftTop;
            Vector4 cameraYExtent = cameraLeftBottom - cameraLeftTop;

            aoMaterial.SetVector("_CameraLT", cameraLeftTop);
            aoMaterial.SetVector("_CameraXExtent", cameraXExtent);
            aoMaterial.SetVector("_CameraYExtent", cameraYExtent);

            CoreUtils.SetKeyword(aoMaterial, k_AOOnly, aoSettings.aoOnly);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float tanfov = Mathf.Tan(0.5f * renderingData.cameraData.camera.fieldOfView * Mathf.Deg2Rad);
            float maxRadInPixels = Mathf.Max(16, aoSettings.RadiusMaxPixel * Mathf.Sqrt((screenWidth * screenHeight) / (1080.0f * 1920.0f)));
            float radius = screenHeight / (tanfov * 2.0f);

            aoMaterial.SetFloat("_Radius", radius);
            aoMaterial.SetFloat("_BlurSharpness", aoSettings.sharpness);
            aoMaterial.SetFloat("_NegInvRadius2", -1.0f / (aoSettings.AORadius * aoSettings.AORadius));
            aoMaterial.SetFloat("_MaxDistance", aoSettings.MaxDistance);
            aoMaterial.SetFloat("_DistanceFallOff", aoSettings.DistanceFallOff);
            aoMaterial.SetFloat("_RadiusMaxPixel", maxRadInPixels);
            aoMaterial.SetFloat("_Intensity", aoSettings.Intensity);
            aoMaterial.SetFloat("_AngleBias", aoSettings.AngleBias);
            aoMaterial.SetVector("_ProjectionParams2", new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            aoMaterial.SetVector("_deltaUV", new Vector4(1.0f / screenWidth, 1.0f / screenHeight, 0.0f, 0.0f));

            RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            PostProcessUtils.SetSourceSize(cmd, renderTextureDescriptor);

            renderTextureDescriptor.msaaSamples = 1;
            renderTextureDescriptor.depthBufferBits = 0;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[0], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "aotexture");

            RenderingUtils.ReAllocateIfNeeded(ref HBAOTextures[1], renderTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "copyRT");
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

            RTHandle colorMap = renderer.cameraColorTargetHandle;
            RTHandle depthMap = renderer.cameraDepthTargetHandle;

            var cmd = renderingData.commandBuffer;
            using(new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.SetGlobalTexture("_AOTexture", HBAOTextures[1]);

                // copy camera rt
                if (colorMap.rt == null)
                {
                    CoreUtils.SetRenderTarget(cmd, HBAOTextures[0]);
                    Blitter.BlitTexture(cmd, colorMap.nameID, Vector2.one, Blitter.GetBlitMaterial(TextureXR.dimension), 1);
                }
                else
                {
                    Blitter.BlitCameraTexture(cmd, colorMap, HBAOTextures[0]);
                }

                // calc ao
                if(depthMap.rt == null)
                {
                    CoreUtils.SetRenderTarget(cmd, HBAOTextures[1]);
                    Blitter.BlitTexture(cmd, depthMap.nameID, Vector2.one, aoMaterial, 0);
                }
                else
                {
                    Blitter.BlitCameraTexture(cmd, depthMap, HBAOTextures[1], aoMaterial, 0);
                }

                // blur
                Blitter.BlitCameraTexture(cmd, HBAOTextures[1], HBAOTextures[2], aoMaterial, 1);
                Blitter.BlitCameraTexture(cmd, HBAOTextures[2], HBAOTextures[1], aoMaterial, 2);

                Blitter.BlitCameraTexture(cmd, HBAOTextures[0], renderer.cameraColorTargetHandle, aoMaterial, 3);
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
