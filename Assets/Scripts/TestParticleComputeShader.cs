using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestParticleComputeShader : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;

    ComputeBuffer particleBuffer;
    int kernelID;
    const int ParticleCount = 20000;

    struct ParticelData
    {
        Vector3 positon;
        Color color;
    }


    // Start is called before the first frame update
    void Start()
    {
        particleBuffer = new ComputeBuffer(ParticleCount, 28);
        ParticelData[] particels = new ParticelData[ParticleCount];
        particleBuffer.SetData(particels);
        kernelID = computeShader.FindKernel("ParticleMain");
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetBuffer(kernelID, "ParticleBuffer", particleBuffer);
        computeShader.SetFloat("Time", Time.time);
        computeShader.Dispatch(kernelID, ParticleCount / 1000, 1, 1);
        material.SetBuffer("_particleDataBuffer", particleBuffer);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, ParticleCount);
    }

    void OnDestroy()
    {
        particleBuffer.Release();
        particleBuffer.Dispose();
    }
}
