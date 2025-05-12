using UnityEngine;

public class GlitchController : MonoBehaviour
{
    public Material mat;
    public float noiseAmount;
    public float glitchStr;
    public float Str;

    void Start()
    {
        noiseAmount = 1000.0f;
        glitchStr = 10000.0f;
    }

    void Update()
    {
        mat.SetFloat("_Strengh", Str);

    }
}