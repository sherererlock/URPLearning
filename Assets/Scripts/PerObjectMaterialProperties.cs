using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int StenciId = Shader.PropertyToID("_ID");
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    //static int cutOffId = Shader.PropertyToID("_Cutoff");
    //static int metallicId = Shader.PropertyToID("_Metallic");
    //static int smoothnessId = Shader.PropertyToID("_Smoothness");
    //static int emissionColorId = Shader.PropertyToID("_EmissionColor");
 
    // Start is called before the first frame update

    [SerializeField]
    Color baseColor = Color.white;

    //[SerializeField, Range(0f, 1f)]
    //float cutoff = 0.5f, metallic = 0.5f, smoothness = 0.5f;

    [SerializeField]
    int MaskID = 0;

    static MaterialPropertyBlock block;

    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;

    void Awake()
    {
        OnValidate();
    }

    void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        //block.SetFloat(cutOffId, cutoff);
        block.SetColor(baseColorId, baseColor);
        //block.SetFloat(metallicId, metallic);
        //block.SetFloat(smoothnessId, smoothness);
        //block.SetColor(emissionColorId, emissionColor);
        block.SetInt(StenciId, MaskID);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Update()
    {
        block.SetColor(baseColorId, baseColor);
        //block.SetFloat(metallicId, metallic);
        //block.SetFloat(smoothnessId, smoothness);
        //block.SetColor(emissionColorId, emissionColor);
        block.SetInt(StenciId, MaskID);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
