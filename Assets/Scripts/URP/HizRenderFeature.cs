using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HizRenderFeature : ScriptableRendererFeature
{
    [SerializeField]
    [HideInInspector]
    [Reload("Assets/Shaders/Hiz/DepthMipGenerator.shader")]
    private Shader m_Shader;
    private string m_ShaderName = "Hidden/DepthMipmapGenerator";

    private Material m_Material;
    private HizPass m_HizPass;

    private GrassPass m_GrassPass;

    public ComputeShader cullingCompute;
    [SerializeField]
    private Mesh grassMesh;
    [SerializeField]
    private Material grassMaterial;

    public int HorizontalGrassCount = 300;
    public int VerticalGrassCount = 300;

    private bool GetMaterials()
    {
        if (m_Shader == null)
            m_Shader = Shader.Find(m_ShaderName);

        if (m_Material == null && m_Shader != null)
            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

        return m_Material != null;
    }

    // Start is called before the first frame update
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        bool shouldAdd = m_HizPass.Setup( ref renderer, ref m_Material);
        if (shouldAdd)
            renderer.EnqueuePass(m_HizPass);

        m_GrassPass.Setup(ref renderer, ref cullingCompute, ref grassMesh, ref grassMaterial, m_HizPass);
        renderer.EnqueuePass(m_GrassPass);
    }

    public override void Create()
    {
        if (m_HizPass == null)
            m_HizPass = new HizPass();

        if (m_GrassPass == null)
            m_GrassPass = new GrassPass();
    }

    protected override void Dispose(bool disposing)
    {
        m_HizPass?.Dispose();
        m_HizPass = null;
        m_GrassPass?.Dispose();
        m_GrassPass = null;

        CoreUtils.Destroy(m_Material);
    }

    private class HizPass : ScriptableRenderPass
    {
        internal string profilerTag = "Hiz";
        string profilerTag1 = "HizCopy";
        private ProfilingSampler m_ProfilingSampler;
        private ProfilingSampler m_ProfilingSampler1;

        private Material m_Material;
        private UniversalRenderer m_Renderer = null;

        int mipCount = 0;
        int _DepthMipmapID;
        RTHandle[] rthandles;
        int m_depthTextureSize = 0;
        const int k_maxMipLevel = 16;
        private RTHandle m_HizTexture;
        public static int[] _DepthMip;
        private RenderTextureDescriptor m_HizDescriptor;
        private const string m_HizName = "_DepthMipmap";
   
        internal HizPass()
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_ProfilingSampler1 = new ProfilingSampler(profilerTag1);

            _DepthMip = new int[k_maxMipLevel];
            rthandles = new RTHandle[k_maxMipLevel];

            _DepthMipmapID = Shader.PropertyToID(m_HizName);
            RTHandles.Alloc(m_HizTexture, name: m_HizName);

            for (int i = 0; i < k_maxMipLevel; i ++)
            {
                _DepthMip[i] = Shader.PropertyToID("_DepthMip" + i);
                rthandles[i] = RTHandles.Alloc(_DepthMip[i], name : "_DepthMip" + i);
            }
        }

        public Texture GetHizTexture()
        {
            return m_HizTexture.rt;
        }
        internal bool Setup(ref ScriptableRenderer renderer, ref Material material)
        {
            m_Material = material;
            m_Renderer = (UniversalRenderer)renderer;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            return m_Material != null;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_depthTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
            m_HizDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_HizDescriptor.msaaSamples = 1;
            m_HizDescriptor.height = m_HizDescriptor.width = m_depthTextureSize;
            m_HizDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
            m_HizDescriptor.depthStencilFormat = GraphicsFormat.None;
            m_HizDescriptor.depthBufferBits = 0;
            m_HizDescriptor.autoGenerateMips = false;
            m_HizDescriptor.useMipMap = true;

            RenderingUtils.ReAllocateIfNeeded(ref m_HizTexture, m_HizDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: m_HizName);

            int maxSize = m_depthTextureSize;
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            mipCount = Mathf.Clamp(iterations, 1, k_maxMipLevel);

            RenderTextureDescriptor desc = m_HizDescriptor;
            desc.autoGenerateMips = false;
            desc.useMipMap = false;

            for (int i = 0; i < mipCount; i ++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref rthandles[i], desc, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name : rthandles[i].name);

                desc.width = Mathf.Max(1, desc.width >> 1);
                desc.height = Mathf.Max(1, desc.height >> 1);
            }

            ConfigureTarget(m_Renderer.cameraColorTargetHandle, m_Renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat("{0}.Execute(): Missing material. Hiz pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                return;
            }

            if(m_Renderer.m_DepthTexture.rt == null)
            {
                Debug.LogErrorFormat("{0}.Execute(): Missing rt. Hiz pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                return;
            }

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                int w = m_HizDescriptor.width;

                Blitter.BlitCameraTexture(cmd, m_Renderer.m_DepthTexture, rthandles[0]);
                //Blitter.BlitTexture(cmd, m_Renderer.cameraDepthTargetHandle, rthandles[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 1);

                RTHandle preRenderTexture = rthandles[0];//上一层的mipmap，即mipmapLevel-1对应的mipmap
                for (int i = 1; i < mipCount; i ++ )
                {
                    cmd.SetGlobalVector("_BlitTextureSize", new Vector4(1.0f / w, 1.0f / w, w, w));

                    Blitter.BlitCameraTexture(cmd, preRenderTexture, rthandles[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 0);
                    // Blitter.BlitTexture(cmd, preRenderTexture, rthandles[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 0); // 不可用为甚？

                    preRenderTexture = rthandles[i];

                    w /= 2;
                }
            }

            using (new ProfilingScope(cmd, m_ProfilingSampler1))
            {
                for (int i = 0; i < mipCount; i++)
                {
                    cmd.CopyTexture(rthandles[i], 0, 0, m_HizTexture, 0, i);
                }
            }
        }

        internal void Dispose()
        {
            foreach (RTHandle handle in rthandles)
                handle?.Release();

            m_HizTexture?.Release();
        }
    }

    private class GrassPass : ScriptableRenderPass
    {
        internal string profilerTag = "GrassCull";
        private ProfilingSampler m_ProfilingSampler;

        int HorizontalGrassCount = 300;
        int VerticalGrassCount = 300;

        int kernel;
        ComputeShader cullingCompute;
        ComputeBuffer l2wMatrixBuffer;
        ComputeBuffer cullingResult;
        ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        Mesh grassMesh;
        int subMeshIndex;
        Material grassMaterial;
        List<Matrix4x4> l2wMatrix = new();

        HizPass hizpass;
        int depthMipmapID;

        private ScriptableRenderer m_Renderer = null;
        internal GrassPass()
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
        }

        internal void Setup(ref ScriptableRenderer renderer, ref ComputeShader cullingCompute, ref Mesh grassMesh, ref Material grassMaterial, HizPass hizpass)
        {
            this.cullingCompute = cullingCompute;
            this.grassMesh = grassMesh;
            this.grassMaterial = grassMaterial;
            this.hizpass = hizpass;
            m_Renderer = renderer;

            renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;

            kernel = cullingCompute.FindKernel("GrassCulling");
            depthMipmapID = Shader.PropertyToID("depthMipmap");

            UpdateBuffer();
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_Renderer.cameraColorTargetHandle, m_Renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cullingResult.SetCounterValue(0);
                cullingCompute.SetInt("instanceCount", l2wMatrix.Count);

                int vpMatrixId = Shader.PropertyToID("vpMatrix");

                cullingCompute.SetBuffer(kernel, "cullresults", cullingResult);
                cullingCompute.SetBuffer(kernel, "object2Worlds", l2wMatrixBuffer);
                cullingCompute.SetInt("depthTextureSize", hizpass.GetHizTexture().width);
                cullingCompute.SetMatrix(vpMatrixId, GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false) * Camera.main.worldToCameraMatrix);
                cullingCompute.SetTexture(kernel, depthMipmapID, hizpass.GetHizTexture());
                cmd.DispatchCompute(cullingCompute, kernel, 1 + (l2wMatrix.Count / 640), 1, 1);

                grassMaterial.SetBuffer("positionBuffer", cullingResult);
                cmd.CopyCounterValue(cullingResult, argsBuffer, sizeof(uint));
                cmd.DrawMeshInstancedIndirect(grassMesh, subMeshIndex, grassMaterial, 0, argsBuffer);
            }
        }

        void UpdateBuffer()
        {
            if (l2wMatrix.Count != 0)
                return;

            argsBuffer?.Release();
            argsBuffer = null;
            l2wMatrixBuffer?.Release();
            l2wMatrixBuffer = null;
            cullingResult?.Release();
            cullingResult = null;

            if (grassMesh != null)
                subMeshIndex = Mathf.Clamp(subMeshIndex, 0, grassMesh.subMeshCount - 1);

            float startX = -50.0f, startZ = -50.0f;
            float endX = 50.0f, endZ = 50.0f;

            float xDeltaDistance = 100.0f / HorizontalGrassCount;
            float zDeltaDistance = 100.0f / VerticalGrassCount;

            for (float x = startX, cx = 0; x < endX && cx < HorizontalGrassCount; x += xDeltaDistance, cx++)
            {
                for (float z = startZ, cz = 0; z < endZ && cz < VerticalGrassCount; z += zDeltaDistance, cz++)
                {
                    Vector3 position = new Vector3(x, 10.0f, z);
                    if (Physics.Raycast(position, Vector3.down, out RaycastHit hitInfo))
                    {
                        position.y = hitInfo.point.y;
                        l2wMatrix.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one));
                    }

                }
            }

            l2wMatrixBuffer = new ComputeBuffer(l2wMatrix.Count, 16 * sizeof(float));
            cullingResult = new ComputeBuffer(l2wMatrix.Count, sizeof(float) * 16, ComputeBufferType.Append);

            l2wMatrixBuffer.SetData(l2wMatrix);

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            if (grassMesh != null)
            {
                args[0] = (uint)grassMesh.GetIndexCount(subMeshIndex);
                args[1] = (uint)l2wMatrix.Count;
                args[2] = (uint)grassMesh.GetIndexStart(subMeshIndex);
                args[3] = (uint)grassMesh.GetBaseVertex(subMeshIndex);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }

            argsBuffer.SetData(args);
        }

        internal void Dispose()
        {
            l2wMatrixBuffer?.Release();
            l2wMatrixBuffer = null;

            cullingResult?.Release();
            cullingResult = null;

            argsBuffer?.Release();
            argsBuffer = null;
        }
    }
}
