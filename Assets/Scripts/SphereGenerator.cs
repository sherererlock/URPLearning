using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SphereGenerator : MonoBehaviour
{
    public GameObject spherePrefab; // 预制体，用于生成Sphere
    public int numberOfSpheres = 1000; // 想要生成的Sphere数量
    public float sphereRadius = 50; // 球的半径

    void Start()
    {
        UniversalRenderPipeline urp;
        UniversalRenderPipelineAsset asset;
        GenerateSpheres();
    }

    //[ContextMenu("Generate Sphere")]
    void GenerateSpheres()
    {
        for (int i = 0; i < numberOfSpheres; i++)
        {
            // 随机生成Sphere在球内的位置
            Vector3 randomPosition = Random.insideUnitSphere * sphereRadius;

            // 创建Sphere实例
            GameObject newSphere = Instantiate(spherePrefab, transform.position + randomPosition, Quaternion.identity);

            // 确保生成的Sphere位于球内
            if (randomPosition.magnitude <= sphereRadius)
            {
                // 如果需要的话，您可以在这里对新生成的Sphere进行额外的设置
            }
        }
    }
}
