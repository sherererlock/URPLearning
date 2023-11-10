using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ScreenSpaceReflectionSettings
{

}

[DisallowMultipleRendererFeature("Screen Space Reflection")]
public class ScreenSpaceReflection : ScriptableRendererFeature
{
    [SerializeField]
    private ScreenSpaceReflectionSettings m_Settings = new ScreenSpaceReflectionSettings();

    [SerializeField]
    [HideInInspector]
    [Reload("Assets/Shaders/ScreenSpaceReflection.shader")]
    private Shader m_Shader;
    private string m_ShaderName = "Hidden/ScreenSpaceReflection";

    private Material m_Material;
    private ScreenSpaceReflectionPass m_SSRPass = null;

    internal ref ScreenSpaceReflectionSettings settings => ref m_Settings;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        bool shouldAdd = m_SSRPass.Setup(ref settings, ref renderer, ref m_Material);
        if (shouldAdd)
            renderer.EnqueuePass(m_SSRPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_SSRPass?.Dispose();
        m_SSRPass = null;
        CoreUtils.Destroy(m_Material);
    }

    public override void Create()
    {
        if(m_SSRPass == null)
            m_SSRPass = new ScreenSpaceReflectionPass();
    }

    private bool GetMaterials()
    {
        if(m_Shader == null)
            m_Shader = Shader.Find(m_ShaderName);

        if (m_Material == null && m_Shader != null)
            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

        return m_Material != null;
    }

    private class ScreenSpaceReflectionPass : ScriptableRenderPass
    {
        internal string profilerTag = "ScreenSpaceReflection";

        private Material m_Material;

        private Matrix4x4 m_CameraViewProjection;
        private Vector4 m_CameraTopLeftCorner = new Vector4();
        private Vector4 m_CameraXExtent = new Vector4();
        private Vector4 m_CameraYExtent = new Vector4();

        private ProfilingSampler m_ProfilingSampler;

        private ScriptableRenderer m_Renderer = null;

        private ScreenSpaceReflectionSettings m_CurrentSettings;

        private const string m_SSRTextureName = "_SSRTexture";

        private RenderTextureDescriptor m_SSRDescriptor;

        private RTHandle m_SSRTexture;

        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionID = Shader.PropertyToID("_CameraViewProjection");
        private static readonly int s_CameraTopLeftCornerID = Shader.PropertyToID("_CameraTopLeftCorner");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");

        internal ScreenSpaceReflectionPass()
        {
            m_CurrentSettings = new();
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
        }

        internal bool Setup(ref ScreenSpaceReflectionSettings settings, ref ScriptableRenderer renderer, ref Material material)
        {
            m_Material = material;
            m_CurrentSettings = settings;
            m_Renderer = renderer;

            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            ConfigureInput(ScriptableRenderPassInput.Normal);

            return m_Material != null;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();
            Matrix4x4 projection = renderingData.cameraData.GetProjectionMatrix();
            m_CameraViewProjection = projection * view;

            Matrix4x4 cview = view;
            cview.SetColumn(3, new Vector4(0, 0, 0, 1));

            Matrix4x4 cviewproj = projection * cview;
            Matrix4x4 cviewProjInv = cviewproj.inverse;

            m_CameraTopLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
            Vector4 topRight = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
            Vector4 bottomLeft = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));

            m_CameraXExtent = topRight - m_CameraTopLeftCorner;
            m_CameraYExtent = bottomLeft - m_CameraTopLeftCorner;

            m_Material.SetMatrix(s_CameraViewProjectionID, m_CameraViewProjection);
            m_Material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0, 0, 0));
            m_Material.SetVector(s_CameraTopLeftCornerID, m_CameraTopLeftCorner);
            m_Material.SetVector(s_CameraViewXExtentID, m_CameraXExtent);
            m_Material.SetVector(s_CameraViewYExtentID, m_CameraYExtent);

            m_SSRDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_SSRDescriptor.msaaSamples = 1;
            m_SSRDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_SSRTexture, m_SSRDescriptor, name: m_SSRTextureName);

            ConfigureTarget(m_Renderer.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceReflection pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                return;
            }

            var cmd = renderingData.commandBuffer;

            RTHandle source = m_Renderer.cameraColorTargetHandle;
            RTHandle target = m_Renderer.cameraColorTargetHandle;

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, source, m_SSRTexture, m_Material, 0);
                Blitter.BlitCameraTexture(cmd, m_SSRTexture, target, m_Material, 1);
            }
        }

        internal void Dispose()
        {
            m_SSRTexture?.Release();
        }
    }
}
