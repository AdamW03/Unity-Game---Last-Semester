using UnityEngine;

public class GlitchController : MonoBehaviour
{
    [Header("Referencje")]
    public Material mat; // Materia³ z shaderem glitcha
    public Transform playerTransform; // Transform gracza (przypisz obiekt gracza w Inspektorze)
    public Transform enemyTransform; // Transform przeciwnika (przypisz obiekt przeciwnika w Inspektorze)

    [Header("Parametry Efektu")]
    [Tooltip("Poziomy promieñ wokó³ przeciwnika (w p³aszczyŸnie XZ), w którym aktywuje siê efekt glitcha.")]
    public float activationRadiusXZ = 30.0f;

    [Tooltip("Maksymalna dopuszczalna ró¿nica wysokoœci (oœ Y) miêdzy graczem a przeciwnikiem, aby efekt siê aktywowa³.")]
    public float maxYDifference = 5.0f; 

    [Header("Konfiguracja Si³y (_Strengh)")]
    [Tooltip("Minimalna wartoœæ si³y (_Strengh) na krawêdzi promienia aktywacji.")]
    public float minStrength = 0.1f;

    [Tooltip("Promieñ (w metrach), dla którego obliczana jest 'bazowa' krzywa wyk³adnicza. Wp³ywa na maksymaln¹ docelow¹ wartoœæ si³y.")]
    public float referenceRadiusForStrengthCalc = 10.0f;

    [Tooltip("Potêga krzywej narastania si³y. Wartoœci > 1 sprawi¹, ¿e si³a roœnie wolniej na pocz¹tku (dalej od przeciwnika) i szybciej blisko. Wartoœæ 1 to bardziej liniowe narastanie w wyk³adniku.")]
    public float strengthCurvePower = 2.5f; 

    [Tooltip("Opcjonalny limit maksymalnej wartoœci si³y (_Strengh).")]
    public float clampMaxStrength = 50.0f;

    private const float fixedNoiseAmount = 10.0f;
    private const float fixedGlitchStr = 10.0f;

    // --- Nazwy w³aœciwoœci w shaderze ---
    private const string noiseAmountShaderPropertyName = "_NoiseAmount";
    private const string glitchStrShaderPropertyName = "_GlitchStr";
    private const string strengthShaderPropertyName = "_Strengh"; 
    // ------------------------------------

    private float targetMaxStrength;

    void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false; 
            return;
        }

        targetMaxStrength = minStrength * Mathf.Pow(2.0f, referenceRadiusForStrengthCalc);
        Debug.Log($"GlitchController: Obliczono docelow¹ maksymaln¹ si³ê (_Strengh): {targetMaxStrength} (dla promienia referencyjnego {referenceRadiusForStrengthCalc}m)");

        SetInitialMaterialValues();
    }

    void Update()
    {
        // Sprawdzenie na wypadek, gdyby któryœ transform zosta³ zniszczony w trakcie gry
        if (playerTransform == null || enemyTransform == null || mat == null)
        {
            // Jeœli coœ jest nie tak, upewnij siê, ¿e efekt jest wy³¹czony
            if (mat != null)
            {
                SetMaterialValues(0f, 0f, 0f); // Wy³¹cz efekt
            }
            return;
        }

        // Pozycje gracza i przeciwnika
        Vector3 playerPos = playerTransform.position;
        Vector3 enemyPos = enemyTransform.position;

        // Oblicz ró¿nicê wysokoœci (oœ Y)
        float yDifference = Mathf.Abs(playerPos.y - enemyPos.y);

        // Oblicz dystans w p³aszczyŸnie XZ
        Vector2 playerPosXZ = new Vector2(playerPos.x, playerPos.z);
        Vector2 enemyPosXZ = new Vector2(enemyPos.x, enemyPos.z);
        float distanceXZ = Vector2.Distance(playerPosXZ, enemyPosXZ);

        float currentStrength = 0f;
        // U¿yj bezpiecznego promienia, aby unikn¹æ dzielenia przez zero
        float safeActivationRadiusXZ = Mathf.Max(0.001f, activationRadiusXZ);

        // SprawdŸ oba warunki: dystans XZ ORAZ ró¿nicê wysokoœci
        if (distanceXZ <= safeActivationRadiusXZ && yDifference <= maxYDifference)
        {
            // Gracz jest w zasiêgu poziomym i na odpowiedniej wysokoœci
            // Oblicz 'proportion' - jak blisko jest gracz do centrum promienia (0 na krawêdzi, 1 w centrum)
            float proportion = (safeActivationRadiusXZ - distanceXZ) / safeActivationRadiusXZ;
            proportion = Mathf.Clamp01(proportion); // Upewnij siê, ¿e jest w zakresie [0, 1]

            // Zastosuj potêgê do 'proportion', aby zmieniæ kszta³t krzywej narastania
            // Jeœli strengthCurvePower > 1, 'adjustedProportion' bêdzie ros³o wolniej na pocz¹tku
            float adjustedProportion = Mathf.Pow(proportion, Mathf.Max(0.001f, strengthCurvePower));

            // Oblicz wyk³adnik dla funkcji wyk³adniczej
            float exponent = referenceRadiusForStrengthCalc * adjustedProportion;
            currentStrength = minStrength * Mathf.Pow(2.0f, exponent);

            // Zastosuj opcjonalny clamp
            currentStrength = Mathf.Min(currentStrength, clampMaxStrength);

            // Ustawienie wartoœci w materiale
            SetMaterialValues(fixedNoiseAmount, fixedGlitchStr, currentStrength);
        }
        else
        {
            // Gracz jest poza zasiêgiem poziomym LUB zbyt du¿a ró¿nica wysokoœci
            SetMaterialValues(0f, 0f, 0f);
        }
    }

    // --- Funkcje Pomocnicze ---

    bool ValidateReferences()
    {
        if (mat == null)
        {
            Debug.LogError("GlitchController: Materia³ glitcha (mat) nie jest przypisany!", this);
            return false;
        }
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.LogWarning("GlitchController: Transform gracza nie by³ przypisany - znaleziono obiekt z tagiem 'Player'.", this);
            }
            else
            {
                Debug.LogError("GlitchController: Transform gracza (playerTransform) nie jest przypisany i nie znaleziono obiektu z tagiem 'Player'!", this);
                return false;
            }
        }
        if (enemyTransform == null)
        {
            Debug.LogError("GlitchController: Transform przeciwnika (enemyTransform) nie jest przypisany!", this);
            return false;
        }
        return true;
    }

    void SetInitialMaterialValues()
    {
        // SprawdŸ istnienie w³aœciwoœci przed ustawieniem wartoœci pocz¹tkowych
        CheckAndSet(noiseAmountShaderPropertyName, 0f, "NoiseAmount");
        CheckAndSet(glitchStrShaderPropertyName, 0f, "GlitchStr");
        CheckAndSet(strengthShaderPropertyName, 0f, "Strength (_Strengh)");
    }

    // Ustawia wszystkie 3 wartoœci w materiale
    void SetMaterialValues(float noise, float glitch, float strength)
    {
        SafeSetMaterialFloat(noiseAmountShaderPropertyName, noise);
        SafeSetMaterialFloat(glitchStrShaderPropertyName, glitch);
        SafeSetMaterialFloat(strengthShaderPropertyName, strength);
    }

    // Bezpieczne ustawianie wartoœci float w materiale (zak³adamy, ¿e w³aœciwoœæ istnieje po sprawdzeniu w Start)
    void SafeSetMaterialFloat(string propertyName, float value)
    {
        // Jeœli materia³ móg³by siê zmieniaæ w trakcie gry na taki bez tych w³aœciwoœci,
        // mo¿na by tu dodaæ ponowne mat.HasProperty(propertyName)
        mat.SetFloat(propertyName, value);
    }

    // Sprawdza i ustawia wartoœæ na starcie, loguje ostrze¿enie jeœli brakuje
    void CheckAndSet(string propertyName, float value, string readableName)
    {
        if (mat.HasProperty(propertyName))
        {
            mat.SetFloat(propertyName, value);
        }
        else
        {
            Debug.LogWarning($"GlitchController: Materia³ '{mat.name}' nie posiada w³aœciwoœci '{propertyName}'. Parametr '{readableName}' nie bêdzie dzia³a³.", this);
        }
    }

    // Wizualizacja promienia w edytorze
    void OnDrawGizmosSelected()
    {
        if (enemyTransform != null)
        {
            Gizmos.color = Color.red;
            // Rysuj okr¹g na poziomie przeciwnika
            DrawWireDisk(enemyTransform.position, activationRadiusXZ, Color.red);

            // Opcjonalnie: Rysuj linie pionowe i okrêgi na granicach wysokoœci
            if (maxYDifference > 0)
            {
                Vector3 topCenter = enemyTransform.position + Vector3.up * maxYDifference;
                Vector3 bottomCenter = enemyTransform.position - Vector3.up * maxYDifference;
                DrawWireDisk(topCenter, activationRadiusXZ, new Color(1f, 0.5f, 0.5f, 0.5f)); // Jaœniejszy czerwony
                DrawWireDisk(bottomCenter, activationRadiusXZ, new Color(1f, 0.5f, 0.5f, 0.5f));

                // Linie ³¹cz¹ce okrêgi (symulacja cylindra)
                int segments = 20;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * (360f / segments) * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * activationRadiusXZ, 0, Mathf.Sin(angle) * activationRadiusXZ);
                    Gizmos.DrawLine(bottomCenter + offset, topCenter + offset);
                }
            }
        }
    }

    // Funkcja pomocnicza do rysowania okrêgu w Gizmos
    public static void DrawWireDisk(Vector3 position, float radius, Color color, int segments = 20)
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = color;
        Vector3 oldPoint = position + new Vector3(radius, 0, 0); // Start from X-axis
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(oldPoint, newPoint);
            oldPoint = newPoint;
        }
        Gizmos.color = oldColor;
    }
}