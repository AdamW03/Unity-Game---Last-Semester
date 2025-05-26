using UnityEngine;
using UnityEngine.SceneManagement; // Potrzebne do zarz¹dzania scenami
using System.Collections;          // Potrzebne do Coroutines (opóŸnienia)

public class EscapeBuilding : MonoBehaviour
{
    [Header("Konfiguracja DŸwiêku")]
    [Tooltip("Klip dŸwiêkowy odtwarzany podczas ucieczki.")]
    public AudioClip escapeSoundClip;
    [Tooltip("G³oœnoœæ dŸwiêku ucieczki.")]
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    [Header("Konfiguracja Sceny")]
    [Tooltip("Nazwa sceny, która ma zostaæ za³adowana.")]
    public string sceneNameToLoad;
    [Tooltip("OpóŸnienie w sekundach po odtworzeniu dŸwiêku, zanim scena zostanie zmieniona. Ustaw 0 dla natychmiastowej zmiany.")]
    public float delayBeforeSceneChange = 0.5f; // Mo¿esz dostosowaæ to opóŸnienie

    private bool escapeTriggered = false; // Flaga zapobiegaj¹ca wielokrotnemu wywo³aniu

    // Metodê tê mo¿na wywo³aæ z zewn¹trz, np. przez trigger, przycisk UI, lub inny skrypt
    public void TriggerEscape()
    {
        if (escapeTriggered)
        {
            Debug.LogWarning("Próba ponownego wywo³ania ucieczki, która ju¿ zosta³a zainicjowana.", this);
            return; // Ju¿ uciekamy, nie rób nic wiêcej
        }

        if (string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.LogError("Nazwa sceny do za³adowania (Scene Name To Load) nie jest ustawiona w inspektorze!", this);
            escapeTriggered = false; // Zresetuj flagê, bo nic siê nie sta³o
            return;
        }

        escapeTriggered = true; // Ustaw flagê, ¿e proces siê rozpocz¹³

        // 1. Odtwórz dŸwiêk u¿ywaj¹c SoundFXManager
        PlayEscapeSound();

        // 2. Rozpocznij proces zmiany sceny z opóŸnieniem
        StartCoroutine(LoadSceneAfterDelay());
    }

    private void PlayEscapeSound()
    {
        if (SoundFXManager.Instance != null && escapeSoundClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(escapeSoundClip, transform, soundVolume);
        }
        else
        {
            if (SoundFXManager.Instance == null)
            {
                Debug.LogWarning("SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku ucieczki.", this);
            }
            if (escapeSoundClip == null)
            {
                Debug.LogWarning("Brak przypisanego klipu dŸwiêkowego (Escape Sound Clip) dla ucieczki.", this);
            }
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        float actualDelay = delayBeforeSceneChange;
        if (actualDelay < 0) actualDelay = 0; 

        if (escapeSoundClip != null && actualDelay < escapeSoundClip.length)
        {
             actualDelay = escapeSoundClip.length;
        }

        yield return new WaitForSeconds(actualDelay);

        // 3. Zmieñ scenê
        Debug.Log($"£adowanie sceny: {sceneNameToLoad}");
        SceneManager.LoadScene(sceneNameToLoad);
    }

}