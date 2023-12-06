using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GrassInactive : MonoBehaviour
{
    public float radius = 5f;
    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalVector("_PositionMoving", transform.position);
        Shader.SetGlobalFloat("_Radius", radius);
    }
}
