using UnityEngine;
using System.Collections; // Potrzebne dla IEnumerator

[RequireComponent(typeof(Light))] // Automatycznie doda komponent Light, jeœli go nie ma, lub upewni siê, ¿e jest
public class BlinkingLight : MonoBehaviour
{
    [Tooltip("Minimalny czas, przez który œwiat³o bêdzie W£¥CZONE (w sekundach).")]
    public float minOnTime = 0.1f;
    [Tooltip("Maksymalny czas, przez który œwiat³o bêdzie W£¥CZONE (w sekundach).")]
    public float maxOnTime = 1.0f;

    [Tooltip("Minimalny czas, przez który œwiat³o bêdzie WY£¥CZONE (w sekundach).")]
    public float minOffTime = 0.1f;
    [Tooltip("Maksymalny czas, przez który œwiat³o bêdzie WY£¥CZONE (w sekundach).")]
    public float maxOffTime = 2.0f;

    private Light lightComponent;

    void Awake()
    {
        // Pobierz komponent Light z tego samego obiektu GameObject
        lightComponent = GetComponent<Light>();

        if (lightComponent == null)
        {
            Debug.LogError("BlinkingLight script requires a Light component on the same GameObject.", this);
            enabled = false; // Wy³¹cz ten skrypt, jeœli nie ma komponentu Light
            return;
        }
    }

    void Start()
    {
        // Rozpocznij korutynê migania
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        // Nieskoñczona pêtla, aby œwiat³o miga³o ci¹gle
        while (true)
        {
            // W³¹cz œwiat³o
            lightComponent.enabled = true;
            // Poczekaj losowy czas "w³¹czenia"
            float onDuration = Random.Range(minOnTime, maxOnTime);
            yield return new WaitForSeconds(onDuration);

            // Wy³¹cz œwiat³o
            lightComponent.enabled = false;
            // Poczekaj losowy czas "wy³¹czenia"
            float offDuration = Random.Range(minOffTime, maxOffTime);
            yield return new WaitForSeconds(offDuration);
        }
    }

    // Opcjonalnie, jeœli chcesz mieæ mo¿liwoœæ zatrzymania migania z innego skryptu
    public void StopBlinking()
    {
        StopAllCoroutines(); // Zatrzymuje wszystkie korutyny na tym MonoBehaviour
        if (lightComponent != null)
        {
            lightComponent.enabled = false; // Upewnij siê, ¿e œwiat³o jest wy³¹czone po zatrzymaniu
        }
    }

    // Opcjonalnie, jeœli chcesz mieæ mo¿liwoœæ ponownego uruchomienia migania
    public void StartBlinking()
    {
        if (lightComponent != null && lightComponent.isActiveAndEnabled) // SprawdŸ, czy komponent jest aktywny
        {
            StopAllCoroutines(); // Zatrzymaj istniej¹ce, zanim zaczniesz nowe
            StartCoroutine(BlinkRoutine());
        }
    }
}