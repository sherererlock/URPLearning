using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GrassRendering : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;

    public int HorizontalGrassCount = 300;
    public int VerticalGrassCount = 300;

    public ComputeShader cullingCompute;
    ComputeBuffer l2wMatrixBuffer;
    ComputeBuffer cullingResult;

    ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    List<Matrix4x4> l2wMatrix = new();

    int kernel;

    int subMeshIndex;
    int depthMipmapIDc;
    int depthMipmapIDs;
    // Start is called before the first frame update
    void Start()
    {
        kernel = cullingCompute.FindKernel("GrassCulling");
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
 
        depthMipmapIDc = Shader.PropertyToID("depthMipmap");

        UpdateBuffer();
    }

    // Update is called once per frame
    void Update()
    {
        int count = HorizontalGrassCount * VerticalGrassCount;
        cullingResult.SetCounterValue(0);
        cullingCompute.SetInt("instanceCount", count);

        int vpMatrixId = Shader.PropertyToID("vpMatrix");

        cullingCompute.SetBuffer(kernel, "cullresults", cullingResult);
        cullingCompute.SetBuffer(kernel, "object2Worlds", l2wMatrixBuffer);

        cullingCompute.SetMatrix(vpMatrixId, GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false) * Camera.main.worldToCameraMatrix);
        depthMipmapIDs = Shader.PropertyToID("_DepthMipmap");
        cullingCompute.SetTextureFromGlobal(kernel, depthMipmapIDc, depthMipmapIDs);

        cullingCompute.Dispatch(kernel, 1 + (count / 640), 1, 1);

        grassMaterial.SetBuffer("positionBuffer", cullingResult);
        ComputeBuffer.CopyCount(cullingResult, argsBuffer, sizeof(uint));

        Vector3 bounds = Vector3.one * 1000;
        Graphics.DrawMeshInstancedIndirect(grassMesh, subMeshIndex, grassMaterial, new Bounds(Vector3.zero, bounds), argsBuffer);
    }


    void OnDisable()
    {
        l2wMatrixBuffer?.Release();
        l2wMatrixBuffer = null;

        cullingResult?.Release();
        cullingResult = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }

    void UpdateBuffer()
    {
        if (grassMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, grassMesh.subMeshCount - 1);

        if (l2wMatrixBuffer != null)
            l2wMatrixBuffer.Release();

        int count = HorizontalGrassCount * VerticalGrassCount;

        cullingResult = new ComputeBuffer(count, sizeof(float) * 16, ComputeBufferType.Append);

        l2wMatrixBuffer = new ComputeBuffer(count, 16 * sizeof(float));
        float startX = -50.0f, startZ = -50.0f;
        float endX = 50.0f, endZ = 50.0f;

        float xDeltaDistance = 100.0f / HorizontalGrassCount;
        float zDeltaDistance = 100.0f / VerticalGrassCount;

        l2wMatrixBuffer = new ComputeBuffer(count, 16 * sizeof(float));
        l2wMatrix.Clear();
        for (float x = startX, cx = 0; x < endX && cx < HorizontalGrassCount; x += xDeltaDistance, cx ++)
        {
            for(float z = startZ, cz = 0; z < endZ && cz < VerticalGrassCount; z += zDeltaDistance, cz ++)
            {
                Vector3 position = new Vector3(x, 10.0f, z);
                bool hit = Physics.Raycast(position, Vector3.down, out RaycastHit hitInfo);
                position.y = hit ? hitInfo.point.y : 0f;

                l2wMatrix.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one));
            }
        }

        l2wMatrixBuffer.SetData(l2wMatrix);

        // Indirect args
        if (grassMesh != null)
        {
            args[0] = (uint)grassMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)count;
            args[2] = (uint)grassMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)grassMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        argsBuffer.SetData(args);
    }
}
