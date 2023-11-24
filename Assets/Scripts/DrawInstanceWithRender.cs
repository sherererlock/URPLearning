using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawInstanceWithRender : MonoBehaviour
{
    public int instanceCount = 10000;

    public Material material;
    public Mesh mesh;

    private ComputeBuffer positionBuffer;

    private int cachedInstanceCount = -1;

    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;

    // Start is called before the first frame update
    void Start()
    {
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    // Update is called once per frame
    void Update()
    {
        if (cachedInstanceCount != instanceCount)
            UpdateBuffers();

        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();

        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)instanceCount;
        commandBuf.SetData(commandData);
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
    }

    void UpdateBuffers()
    {
        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();

        positionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        }
        positionBuffer.SetData(positions);
        material.SetBuffer("positionBuffer", positionBuffer);

        cachedInstanceCount = instanceCount;
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        positionBuffer = null;

        commandBuf?.Release();
        commandBuf = null;
    }
}
