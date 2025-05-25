using UnityEngine;
using System.Linq; // Potrzebne dla Where, jeœli bêdziemy mieli wiêcej dŸwiêków ruchu

public class EnemyFootstepAudio : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("G³ówny transform przeciwnika, którego ruch bêdziemy œledziæ.")]
    public Transform enemyTransform; // Przypisz g³ówny obiekt przeciwnika
    [Tooltip("Komponent GroundCheck na obiekcie-dziecku (np. na stopach).")]
    public GroundCheck groundCheck;  // Przypisz obiekt z GroundCheck

    [Header("Footstep Sound")]
    [Tooltip("Komponent AudioSource dla dŸwiêków kroków.")]
    public AudioSource stepAudio;    // Mo¿esz przypisaæ istniej¹cy lub skrypt go stworzy
    [Tooltip("Klip dŸwiêkowy dla kroków przeciwnika. Powinien byæ zapêtlony, jeœli jest d³ugi.")]
    public AudioClip stepSoundClip;  // Przypisz swój klip audio
    [Tooltip("Minimalna prêdkoœæ (jednostki na sekundê), aby odtwarzaæ dŸwiêk kroków.")]
    public float velocityThreshold = 0.1f;

    // Zmienne wewnêtrzne do œledzenia pozycji
    private Vector3 lastPosition; // Zmieniono na Vector3 dla pe³nego œledzenia
    private Vector3 CurrentPosition => enemyTransform != null ? enemyTransform.position : Vector3.zero;

    void Reset()
    {
        // Spróbuj automatycznie znaleŸæ referencje, jeœli nie s¹ przypisane
        if (enemyTransform == null)
        {
            enemyTransform = transform; // Zak³adamy, ¿e skrypt jest na g³ównym obiekcie przeciwnika
            if (enemyTransform.parent != null && enemyTransform.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                // Jeœli skrypt jest na dziecku, a rodzic ma NavMeshAgent, u¿yj rodzica
                enemyTransform = transform.parent;
            }
        }

        if (groundCheck == null && enemyTransform != null)
        {
            // Spróbuj znaleŸæ GroundCheck jako dziecko enemyTransform
            groundCheck = enemyTransform.GetComponentInChildren<GroundCheck>();
        }

        // Stwórz AudioSource dla kroków, jeœli nie istnieje
        stepAudio = GetOrCreateAudioSource("Enemy Step Audio", enemyTransform != null ? enemyTransform : transform);

        // Sprawdzenia
        if (enemyTransform == null) Debug.LogError("EnemyFootstepAudio: 'Enemy Transform' nie jest przypisany i nie mo¿na go znaleŸæ!", this);
        if (groundCheck == null) Debug.LogError("EnemyFootstepAudio: 'Ground Check' nie jest przypisany i nie mo¿na go znaleŸæ jako dziecka Enemy Transform!", this);
    }

    void Start()
    {
        // Inicjalizacja pozycji
        if (enemyTransform != null)
        {
            lastPosition = CurrentPosition;
        }

        // Upewnij siê, ¿e AudioSource ma przypisany klip i jest ustawiony na pêtlê
        if (stepAudio != null && stepSoundClip != null)
        {
            stepAudio.clip = stepSoundClip;
            stepAudio.loop = true; // WA¯NE dla d³ugich klipów chodzenia
        }
        else if (stepAudio != null && stepSoundClip == null)
        {
            Debug.LogWarning($"EnemyFootstepAudio na '{gameObject.name}': Brak przypisanego 'Step Sound Clip' do 'Step Audio'. DŸwiêk kroków nie bêdzie odtwarzany.", this);
        }
    }

    void Update() // Zmieniono z FixedUpdate na Update dla p³ynniejszego wykrywania ruchu wizualnego
    {
        if (enemyTransform == null || groundCheck == null || stepAudio == null)
        {
            // Jeœli brakuje kluczowych referencji, nie rób nic
            return;
        }

        // Oblicz prêdkoœæ na podstawie zmiany pozycji
        // U¿ywamy Vector3.Distance dla pe³nej prêdkoœci, nie tylko w p³aszczyŸnie XZ
        float distanceMoved = Vector3.Distance(CurrentPosition, lastPosition);
        float currentSpeed = (Time.deltaTime > 0) ? distanceMoved / Time.deltaTime : 0; // Prêdkoœæ na sekundê

        // SprawdŸ, czy przeciwnik siê porusza i jest na ziemi
        bool isMovingAndGrounded = currentSpeed >= velocityThreshold && groundCheck.isGrounded;

        if (isMovingAndGrounded)
        {
            // Jeœli dŸwiêk kroków nie gra, uruchom go
            if (!stepAudio.isPlaying)
            {
                stepAudio.Play();
            }
        }
        else
        {
            // Jeœli przeciwnik siê nie rusza lub nie jest na ziemi, a dŸwiêk gra, zatrzymaj go
            if (stepAudio.isPlaying)
            {
                stepAudio.Pause(); // U¿yj Pause(), aby wznowiæ z tego samego miejsca, jeœli chcesz
                                   // lub stepAudio.Stop() jeœli chcesz, aby zaczyna³ od pocz¹tku przy nastêpnym ruchu
            }
        }

        // Zapamiêtaj aktualn¹ pozycjê na nastêpn¹ klatkê
        lastPosition = CurrentPosition;
    }

    /// <summary>
    /// Pobiera istniej¹cy AudioSource o podanej nazwie jako dziecko parentTransform
    /// lub tworzy nowy, jeœli nie zosta³ znaleziony.
    /// </summary>
    AudioSource GetOrCreateAudioSource(string name, Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogError("GetOrCreateAudioSource: parentTransform jest null!", this);
            parentTransform = transform; // Awaryjnie u¿yj transformu tego obiektu
        }

        // Spróbuj znaleŸæ AudioSource jako dziecko parentTransform
        AudioSource result = parentTransform.GetComponentsInChildren<AudioSource>()
                                          .FirstOrDefault(a => a.gameObject.name == name);
        if (result)
            return result;

        // AudioSource nie istnieje, stwórz go
        GameObject audioGameObject = new GameObject(name);
        result = audioGameObject.AddComponent<AudioSource>();
        result.spatialBlend = 1;    // DŸwiêk 3D
        result.playOnAwake = false;
        result.transform.SetParent(parentTransform, false); // Ustaw jako dziecko przeciwnika
        result.transform.localPosition = Vector3.zero;    // Wyœrodkuj w rodzicu
        return result;
    }
}