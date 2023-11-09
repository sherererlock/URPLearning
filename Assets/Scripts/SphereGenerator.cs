using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SphereGenerator : MonoBehaviour
{
    public GameObject spherePrefab; // Ԥ���壬��������Sphere
    public int numberOfSpheres = 1000; // ��Ҫ���ɵ�Sphere����
    public float sphereRadius = 50; // ��İ뾶

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
            // �������Sphere�����ڵ�λ��
            Vector3 randomPosition = Random.insideUnitSphere * sphereRadius;

            // ����Sphereʵ��
            GameObject newSphere = Instantiate(spherePrefab, transform.position + randomPosition, Quaternion.identity);

            // ȷ�����ɵ�Sphereλ������
            if (randomPosition.magnitude <= sphereRadius)
            {
                // �����Ҫ�Ļ���������������������ɵ�Sphere���ж��������
            }
        }
    }
}
