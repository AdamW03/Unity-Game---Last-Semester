using UnityEngine;

public class GlitchController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material mat;

    public float noiseAmount;
    public float glitchStr;
    public float Str;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mat.SetFloat("_NoiseAmount", noiseAmount);
        mat.SetFloat("_GlitchStr", glitchStr);

    }
}
