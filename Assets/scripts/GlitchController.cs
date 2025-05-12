using UnityEngine;

public class GlitchController : MonoBehaviour
{
    [Header("Referencje")]
    [Tooltip("Materia³, którego w³aœciwoœæ _Strengh bêdzie modyfikowana.")]
    public Material mat; // Materia³ do modyfikacji

    [Tooltip("Transform obiektu gracza (wokó³ którego liczony jest dystans).")]
    public Transform playerTransform; // Referencja do gracza

    [Tooltip("Transform obiektu przeciwnika (do którego liczony jest dystans).")]
    public Transform opponentTransform; // Referencja do przeciwnika

    [Header("Parametry Glitcha")]
    [Tooltip("Promieñ wokó³ gracza, w którym efekt glitcha siê nasila.")]
    public float activationRadius = 15.0f; // Promieñ aktywacji

    [Tooltip("Minimalna si³a glitcha (gdy przeciwnik jest na krawêdzi promienia lub dalej). Musi byæ wiêksza od 0.")]
    public float minStrength = 0.1f;

    [Tooltip("Maksymalna si³a glitcha (gdy przeciwnik jest tu¿ obok gracza).")]
    public float maxStrength = 6.0f;

    // Aktualnie obliczona si³a glitcha (mo¿na obserwowaæ w Inspektorze)
    [SerializeField] // Pokazuje prywatne pole w Inspektorze do debugowania
    [Tooltip("Aktualna si³a efektu glitch. Zmienia siê dynamicznie.")]
    private float currentStrength;

    // Usuniête nieu¿ywane zmienne: noiseAmount, glitchStr

    void Start()
    {
        // --- Walidacja pocz¹tkowa ---
        if (mat == null)
        {
            Debug.LogError("GlitchController: Materia³ nie zosta³ przypisany!", this);
            enabled = false; // Wy³¹cz skrypt, jeœli brakuje materia³u
            return;
        }
        // SprawdŸ, czy referencje do gracza i przeciwnika zosta³y przypisane
        if (playerTransform == null)
        {
            Debug.LogError("GlitchController: Transform gracza (Player Transform) nie zosta³ przypisany w Inspektorze!", this);
            enabled = false;
            return;
        }
        if (opponentTransform == null)
        {
            Debug.LogError("GlitchController: Transform przeciwnika (Opponent Transform) nie zosta³ przypisany w Inspektorze!", this);
            enabled = false;
            return;
        }

        // Walidacja parametrów (promieñ, si³a)
        if (activationRadius <= 0)
        {
            Debug.LogWarning("GlitchController: activationRadius powinien byæ dodatni. Ustawiam na 0.01.", this);
            activationRadius = 0.01f; // Zapobiega dzieleniu przez zero
        }
        if (minStrength <= 0)
        {
            Debug.LogWarning("GlitchController: minStrength musi byæ wiêksze od 0. Ustawiam na 0.001.", this);
            minStrength = 0.001f; // Minimalna wartoœæ musi byæ > 0
        }
        if (maxStrength < minStrength)
        {
            Debug.LogWarning("GlitchController: maxStrength nie mo¿e byæ mniejsze ni¿ minStrength. Zamieniam wartoœci.", this);
            float temp = maxStrength;
            maxStrength = minStrength;
            minStrength = temp;
        }

        // --- Ustaw pocz¹tkow¹ si³ê ---
        // Oblicz si³ê na starcie, zak³adaj¹c, ¿e oba Transformy s¹ dostêpne
        UpdateStrength();
        mat.SetFloat("_Strengh", currentStrength);
    }

    void Update()
    {
        // --- SprawdŸ, czy referencje nadal s¹ wa¿ne ---
        // (Obiekty mog³y zostaæ zniszczone w trakcie gry)
        if (playerTransform == null || opponentTransform == null)
        {
            // Jeœli brakuje gracza lub przeciwnika, ustaw minimaln¹ si³ê
            // i przestañ aktualizowaæ, aby unikn¹æ b³êdów NullReferenceException.
            if (currentStrength != minStrength)
            {
                currentStrength = minStrength;
                // SprawdŸ czy materia³ nadal istnieje (na wszelki wypadek)
                if (mat != null)
                {
                    mat.SetFloat("_Strengh", currentStrength);
                }
            }
            // Mo¿na te¿ wy³¹czyæ komponent, jeœli obiekty zniknê³y na sta³e
            // enabled = false;
            return; // Zakoñcz Update, jeœli brakuje któregoœ obiektu
        }

        // --- Oblicz i zastosuj si³ê ---
        UpdateStrength();
        mat.SetFloat("_Strengh", currentStrength);
    }

    // Funkcja obliczaj¹ca si³ê glitcha na podstawie dystansu
    void UpdateStrength()
    {
        // Sprawdzenie null na wszelki wypadek, chocia¿ Update() ju¿ to robi
        if (playerTransform == null || opponentTransform == null) return;

        // Oblicz dystans miêdzy graczem a przeciwnikiem
        float distance = Vector3.Distance(playerTransform.position, opponentTransform.position);

        // SprawdŸ, czy przeciwnik jest w zasiêgu promienia aktywacji wokó³ gracza
        if (distance <= activationRadius)
        {
            // Oblicz znormalizowany dystans: 0 = tu¿ obok, 1 = na krawêdzi promienia
            float normalizedDistance = Mathf.Clamp01(distance / activationRadius);

            // Interpoluj si³ê liniowo (Lerp)
            // Kiedy normalizedDistance = 0 (blisko), si³a = maxStrength
            // Kiedy normalizedDistance = 1 (na krawêdzi), si³a = minStrength
            currentStrength = Mathf.Lerp(maxStrength, minStrength, normalizedDistance);
        }
        else
        {
            // Jeœli przeciwnik jest poza promieniem, ustaw minimaln¹ si³ê
            currentStrength = minStrength;
        }

        // Opcjonalnie: Dodatkowe zabezpieczenie Clamp
        // currentStrength = Mathf.Clamp(currentStrength, minStrength, maxStrength);
    }

    // Opcjonalnie: Rysuj Gizmo w edytorze, aby zwizualizowaæ promieñ wokó³ gracza
    void OnDrawGizmosSelected()
    {
        // Rysuj sferê wokó³ pozycji gracza, jeœli zosta³ przypisany
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, activationRadius);
        }
        // Mo¿na te¿ narysowaæ liniê miêdzy graczem a przeciwnikiem dla lepszej wizualizacji
        if (playerTransform != null && opponentTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerTransform.position, opponentTransform.position);
        }
    }
}