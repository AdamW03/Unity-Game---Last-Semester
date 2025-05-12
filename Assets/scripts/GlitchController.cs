using UnityEngine;

public class GlitchController : MonoBehaviour
{
    [Header("Referencje")]
    public Material mat; 
    public Transform playerTransform; 
    public Transform enemyTransform; 

    [Header("Parametry Efektu")]
    [Tooltip("Promieñ wokó³ przeciwnika, w którym aktywuje siê efekt glitcha.")]
    public float activationRadius = 10.0f;

    [Header("Konfiguracja Si³y (_Strengh)")]
    [Tooltip("Minimalna wartoœæ si³y (_Strengh) na krawêdzi promienia aktywacji.")]
    public float minStrength = 0.1f;

    [Tooltip("Promieñ (w metrach), dla którego obliczana jest 'bazowa' krzywa wyk³adnicza (podwajanie co metr). Wp³ywa na maksymaln¹ docelow¹ wartoœæ si³y.")]
    public float referenceRadiusForStrengthCalc = 10.0f;

    [Tooltip("Opcjonalny limit maksymalnej wartoœci si³y (_Strengh), aby zapobiec ekstremalnym wartoœciom.")]
    public float clampMaxStrength = 200.0f; 

    private const float fixedNoiseAmount = 1000.0f;
    private const float fixedGlitchStr = 1000.0f;

    // --- Nazwy w³aœciwoœci w shaderze ---
    private const string noiseAmountShaderPropertyName = "_NoiseAmount";
    private const string glitchStrShaderPropertyName = "_GlitchStr";
    private const string strengthShaderPropertyName = "_Strengh"; 
    // ------------------------------------

    // Obliczona docelowa maksymalna si³a na podstawie promienia referencyjnego
    private float targetMaxStrength;

    void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        targetMaxStrength = minStrength * Mathf.Pow(2.0f, referenceRadiusForStrengthCalc);
        Debug.Log($"Obliczono docelow¹ maksymaln¹ si³ê (_Strengh): {targetMaxStrength} (dla promienia referencyjnego {referenceRadiusForStrengthCalc}m)");


        SetInitialMaterialValues();
    }

    void Update()
    {
        // SprawdŸ referencje na pocz¹tku
        if (playerTransform == null || enemyTransform == null || mat == null)
        {
            // Loguj jeœli brakuje referencji
            Debug.LogWarning($"GlitchController Update: Missing Reference! Player: {(playerTransform != null)}, Enemy: {(enemyTransform != null)}, Mat: {(mat != null)}");
            if (mat != null) { SetMaterialValues(0f, 0f, 0f); }
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, enemyTransform.position);

        float currentStrength = 0f;
        float safeActivationRadius = Mathf.Max(0.001f, activationRadius);

        if (distance <= safeActivationRadius)
        {
            Debug.Log($"GlitchController: INSIDE Radius (Distance: {distance})");

            float proportion = (safeActivationRadius - distance) / safeActivationRadius;
            proportion = Mathf.Clamp01(proportion);
            float exponent = referenceRadiusForStrengthCalc * proportion;
            currentStrength = minStrength * Mathf.Pow(2.0f, exponent);
            currentStrength = Mathf.Min(currentStrength, clampMaxStrength);

            Debug.Log($"GlitchController: Setting ACTIVE values -> Noise={fixedNoiseAmount}, Glitch={fixedGlitchStr}, Strength={currentStrength}");
            SetMaterialValues(fixedNoiseAmount, fixedGlitchStr, currentStrength);
        }
        else
        {
            SetMaterialValues(0f, 0f, 0f);
        }
    }

    // --- Funkcje Pomocnicze ---

    bool ValidateReferences()
    {
        if (mat == null) { Debug.LogError("Materia³ glitcha (mat) nie jest przypisany!", this); return false; }
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) { playerTransform = playerObject.transform; Debug.LogWarning("Transform gracza nie by³ przypisany - znaleziono 'Player'.", this); }
            else { Debug.LogError("Transform gracza (playerTransform) nie jest przypisany i nie znaleziono 'Player'!", this); return false; }
        }
        if (enemyTransform == null) { Debug.LogError("Transform przeciwnika (enemyTransform) nie jest przypisany!", this); return false; }
        return true;
    }

    void SetInitialMaterialValues()
    {
        CheckAndSet(noiseAmountShaderPropertyName, 0f, "NoiseAmount");
        CheckAndSet(glitchStrShaderPropertyName, 0f, "GlitchStr");
        CheckAndSet(strengthShaderPropertyName, 0f, "Strength (_Strengh)");
    }

    void SetMaterialValues(float noise, float glitch, float strength)
    {
        SafeSetMaterialFloat(noiseAmountShaderPropertyName, noise);
        SafeSetMaterialFloat(glitchStrShaderPropertyName, glitch);
        SafeSetMaterialFloat(strengthShaderPropertyName, strength);
    }

    void SafeSetMaterialFloat(string propertyName, float value)
    {
        mat.SetFloat(propertyName, value);
    }

    void CheckAndSet(string propertyName, float value, string readableName)
    {
        if (mat.HasProperty(propertyName))
        {
            mat.SetFloat(propertyName, value);
        }
        else
        {
            Debug.LogWarning($"Materia³ '{mat.name}' nie posiada w³aœciwoœci '{propertyName}'. Parametr '{readableName}' nie bêdzie dzia³a³.", this);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (enemyTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemyTransform.position, activationRadius);
        }
    }
}