using UnityEngine;
using UnityEngine.Rendering; 
using URPGlitch;

public class ProximityGlitchController : MonoBehaviour
{
    [Header("Referencje")]
    public Volume globalVolume;         
    public Transform playerTransform;   
    public Transform enemyTransform;    

    [Header("Ustawienia Walca Detekcji")]
    [Tooltip("Promieñ walca, w którym efekt mo¿e siê aktywowaæ.")]
    public float detectionCylinderRadius = 25f;
    [Tooltip("Po³owa wysokoœci walca (od œrodka przeciwnika w górê i w dó³), w którym efekt mo¿e siê aktywowaæ.")]
    public float detectionCylinderHalfHeight = 2f; 

    [Header("Ustawienia Intensywnoœci Efektu w Walcu")]
    [Tooltip("Promieñ (na p³aszczyŸnie XZ) wewn¹trz walca, przy którym efekt osi¹ga maksymaln¹ intensywnoœæ. Powinien byæ <= detectionCylinderRadius.")]
    public float fullEffectRadiusXZ = 2f;

    [Header("Maksymalne Wartoœci Glitcha")]
    public float maxDigitalIntensity = 1.0f;
    public float maxAnalogParamValue = 0.1f;

    [Header("Charakterystyka Narastania Efektu")]
    [Tooltip("Wyk³adnik potêgi dla proximityFactor. Wartoœci > 1.0 (np. 2.0, 3.0) sprawi¹, ¿e efekt bêdzie narasta³ wolniej na pocz¹tku i gwa³towniej blisko przeciwnika.")]
    public float proximityCurveExponent = 2.0f;

    private DigitalGlitchVolume digitalGlitchEffect;
    private AnalogGlitchVolume analogGlitchEffect;

    private bool effectsInitialized = false;

    void Start()
    {
        // Sprawdzenie podstawowych referencji
        if (globalVolume == null) { Debug.LogError("Global Volume nie jest przypisany!", this); enabled = false; return; }
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else { Debug.LogError("Player Transform nie jest przypisany i nie znaleziono 'Player'!", this); enabled = false; return; }
        }
        if (enemyTransform == null) { Debug.LogError("Enemy Transform nie jest przypisany!", this); enabled = false; return; }

        // Upewnij siê, ¿e fullEffectRadiusXZ nie jest wiêkszy ni¿ detectionCylinderRadius
        if (fullEffectRadiusXZ > detectionCylinderRadius)
        {
            Debug.LogWarning("fullEffectRadiusXZ by³ wiêkszy ni¿ detectionCylinderRadius. Zosta³ ograniczony.", this);
            fullEffectRadiusXZ = detectionCylinderRadius;
        }


        // Pobierz komponenty efektów z profilu Volume
        if (globalVolume.profile.TryGet(out digitalGlitchEffect) &&
            globalVolume.profile.TryGet(out analogGlitchEffect))
        {
            effectsInitialized = true;
            Debug.Log("Efekty glitch pomyœlnie zainicjalizowane.");

            if (digitalGlitchEffect != null) digitalGlitchEffect.intensity.overrideState = true;
            if (analogGlitchEffect != null)
            {
                analogGlitchEffect.scanLineJitter.overrideState = true;
                analogGlitchEffect.verticalJump.overrideState = true;
                analogGlitchEffect.horizontalShake.overrideState = true;
                analogGlitchEffect.colorDrift.overrideState = true;
            }
        }
        else
        {
            if (!globalVolume.profile.TryGet(out digitalGlitchEffect)) Debug.LogError("Nie mo¿na znaleŸæ DigitalGlitchVolume w profilu Volume.", this);
            if (!globalVolume.profile.TryGet(out analogGlitchEffect)) Debug.LogError("Nie mo¿na znaleŸæ AnalogGlitchVolume w profilu Volume.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (!effectsInitialized || playerTransform == null || enemyTransform == null)
        {
            return;
        }

        Vector3 playerPos = playerTransform.position;
        Vector3 enemyPos = enemyTransform.position;

        Vector2 playerPosXZ = new Vector2(playerPos.x, playerPos.z);
        Vector2 enemyPosXZ = new Vector2(enemyPos.x, enemyPos.z);
        float horizontalDistance = Vector2.Distance(playerPosXZ, enemyPosXZ);

        float verticalDifference = Mathf.Abs(playerPos.y - enemyPos.y);

        if (horizontalDistance <= detectionCylinderRadius && verticalDifference <= detectionCylinderHalfHeight)
        {
            float baseProximityFactor = Mathf.InverseLerp(detectionCylinderRadius, fullEffectRadiusXZ, horizontalDistance);
            baseProximityFactor = Mathf.Clamp01(baseProximityFactor);

            float curvedProximityFactor = Mathf.Pow(baseProximityFactor, Mathf.Max(1.0f, proximityCurveExponent));

            ApplyGlitchIntensity(curvedProximityFactor);
        }
        else
        {
            ApplyGlitchIntensity(0f);
        }
    }

    void ApplyGlitchIntensity(float intensityFactor)
    {
        if (digitalGlitchEffect != null)
        {
            digitalGlitchEffect.intensity.value = intensityFactor * maxDigitalIntensity;
        }

        if (analogGlitchEffect != null)
        {
            float targetAnalogValue = intensityFactor * maxAnalogParamValue;
            analogGlitchEffect.scanLineJitter.value = targetAnalogValue;
            analogGlitchEffect.verticalJump.value = targetAnalogValue;
            analogGlitchEffect.horizontalShake.value = targetAnalogValue;
            analogGlitchEffect.colorDrift.value = targetAnalogValue;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (enemyTransform == null) return;

        Vector3 enemyPos = enemyTransform.position;
        Color outerCylinderColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.3f);
        Color innerCylinderColor = new Color(Color.red.r, Color.red.g, Color.red.b, 0.3f);

        // Rysuj zewnêtrzny walec detekcji
        DrawWireCylinder(enemyPos, detectionCylinderRadius, detectionCylinderHalfHeight, outerCylinderColor);

        // Rysuj wewnêtrzny walec pe³nego efektu (jeœli jego promieñ jest > 0)
        if (fullEffectRadiusXZ > 0.01f) // Ma³y próg, aby unikn¹æ rysowania, gdy jest praktycznie zerowy
        {
            DrawWireCylinder(enemyPos, fullEffectRadiusXZ, detectionCylinderHalfHeight, innerCylinderColor);
        }


        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerTransform.position, enemyTransform.position);
        }
    }

    // Helper do rysowania walca w Gizmos
    void DrawWireCylinder(Vector3 position, float radius, float halfHeight, Color color, int segments = 32)
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = color;

        Vector3 topCenter = position + Vector3.up * halfHeight;
        Vector3 bottomCenter = position - Vector3.up * halfHeight;

        // Rysuj górny i dolny okr¹g
        DrawWireDisk(topCenter, radius, color, segments);
        DrawWireDisk(bottomCenter, radius, color, segments);

        // Rysuj linie pionowe ³¹cz¹ce okrêgi
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * 360f * Mathf.Deg2Rad;
            float nextAngle = (i + 1) / (float)segments * 360f * Mathf.Deg2Rad; // Dla lepszego wygl¹du, ale mo¿na uproœciæ

            Vector3 topPoint = topCenter + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Vector3 bottomPoint = bottomCenter + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            // Rysuj tylko kilka pionowych linii dla czytelnoœci, np. 4 lub 8
            if (i % (segments / 4) == 0) // Rysuj co 1/4 segmentów
            {
                 Gizmos.DrawLine(topPoint, bottomPoint);
            }
        }
        Gizmos.color = oldColor;
    }

    // Helper do rysowania okrêgu w Gizmos (u¿ywany przez DrawWireCylinder)
    void DrawWireDisk(Vector3 position, float radius, Color color, int segments = 32)
    {
        Color oldColor = Gizmos.color; // Zachowaj oryginalny kolor Gizmos
        Gizmos.color = color;          // Ustaw kolor dla tego rysowania

        Vector3 oldPoint = position + new Vector3(radius, 0, 0); // Pierwszy punkt na okrêgu
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * 2f * Mathf.PI; // K¹t w radianach
            Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(oldPoint, newPoint);
            oldPoint = newPoint;
        }
        Gizmos.color = oldColor; // Przywróæ oryginalny kolor Gizmos
    }
}