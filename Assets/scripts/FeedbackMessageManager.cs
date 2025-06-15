// Plik: FeedbackMessageManager.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class FeedbackMessageManager : MonoBehaviour
{
    // --- Singleton ---
    public static FeedbackMessageManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }
    // -----------------

    [Header("Referencje")]
    [Tooltip("Obiekt TextMeshProUGUI do wyœwietlania komunikatów.")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Ustawienia")]
    [Tooltip("Czas w sekundach, po którym komunikat zniknie.")]
    [SerializeField] private float displayDuration = 3f;

    private Coroutine currentMessageCoroutine;

    void Start()
    {
        if (feedbackText == null)
        {
            Debug.LogError("FeedbackMessageManager: Nie przypisano obiektu 'feedbackText' w Inspektorze!", this);
            this.enabled = false;
            return;
        }
        // Upewnij siê, ¿e tekst jest ukryty na starcie
        feedbackText.gameObject.SetActive(false);
    }

    // Publiczna metoda do wywo³ania z innych skryptów
    public void ShowMessage(string message)
    {
        // Jeœli jakiœ komunikat jest ju¿ wyœwietlany, zatrzymaj go
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }
        // Uruchom now¹ korutynê, która poka¿e i ukryje komunikat
        currentMessageCoroutine = StartCoroutine(ShowAndHideMessage(message));
    }

    private IEnumerator ShowAndHideMessage(string message)
    {
        // Poka¿ tekst i ustaw jego treœæ
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        // Poczekaj zadan¹ iloœæ czasu
        yield return new WaitForSeconds(displayDuration);

        // Ukryj tekst
        feedbackText.gameObject.SetActive(false);
        currentMessageCoroutine = null;
    }
}