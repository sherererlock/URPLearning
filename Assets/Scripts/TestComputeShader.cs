using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TestComputeShader : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;

    RenderTexture mainTexture;

    int kernelIndex;
    // Start is called before the first frame update
    void Start()
    {
        mainTexture = new RenderTexture(256, 256, 0);
        mainTexture.enableRandomWrite = true;
        mainTexture.filterMode = FilterMode.Point;

        mainTexture.Create();
        
        kernelIndex = computeShader.FindKernel("CSMain");
    }

    // Update is called once per frame
    void Update()
    {
        if (computeShader == null || material == null)
            return;

        mainTexture.filterMode = FilterMode.Point;
        computeShader.SetTexture(kernelIndex, "Result", mainTexture);
        computeShader.Dispatch(kernelIndex, 32, 1, 1);

        material.SetTexture("_MainTex", mainTexture);
    }
}
