using UnityEngine;
using TMPro;
using System.Collections;

public class IntroController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject introBlackScreenPanel;
    [SerializeField] private TextMeshProUGUI introDisplayText;

    [Header("Intro Content")]
    [TextArea(3, 10)]
    public string textToShowDuringIntro = "Witaj w grze...";
    public AudioClip introSoundClip;
    [Range(0f, 1f)]
    public float introSoundVolume = 0.7f;

    [Header("Settings")]
    public float introDuration = 10f;

    [Header("Player Control")]
    public FirstPersonMovement playerMovementScript;
    public FirstPersonLook playerLookScript;

    // Metoda Start jest wywo³ywana raz, gdy skrypt jest w³¹czany, po za³adowaniu wszystkich obiektów
    void Start()
    {
        Debug.Log("--- IntroController: Start() CALLED ---");

        // Sprawdzenia komponentów (wa¿ne, aby by³y wykonane przed uruchomieniem korutyny)
        bool canProceed = true;
        if (playerMovementScript == null)
        {
            Debug.LogError("IntroController ERROR: PlayerMovementScript nie jest przypisany!");
            canProceed = false;
        }
        if (playerLookScript == null)
        {
            Debug.LogError("IntroController ERROR: PlayerLookScript nie jest przypisany!");
            canProceed = false;
        }
        if (introDisplayText == null)
        {
            Debug.LogWarning("IntroController WARNING: IntroDisplayText (TextMeshProUGUI) nie jest przypisany. Tekst nie zostanie wyœwietlony.");
        }
        if (introBlackScreenPanel == null)
        {
            Debug.LogError("IntroController ERROR: IntroBlackScreenPanel nie jest przypisany!");
            canProceed = false;
        }
        if (SoundFXManager.Instance == null)
        {
            Debug.LogWarning("IntroController WARNING: SoundFXManager.Instance nie znaleziony! DŸwiêk intro nie zostanie odtworzony.");
        }
        if (introSoundClip == null)
        {
            Debug.LogWarning("IntroController WARNING: IntroSoundClip nie jest przypisany! DŸwiêk intro nie zostanie odtworzony.");
        }

        if (!canProceed)
        {
            Debug.LogError("IntroController: Intro sequence ABORTED due to missing critical components. Enabling player controls if possible.");
            if (introBlackScreenPanel != null) introBlackScreenPanel.SetActive(false);
            if (playerMovementScript != null) playerMovementScript.SetMovementEnabled(true);
            if (playerLookScript != null) playerLookScript.SetLookEnabled(true);
            return; // Nie uruchamiaj korutyny, jeœli brakuje krytycznych komponentów
        }

        Debug.Log("IntroController: All checks passed, starting IntroCoroutine from Start().");
        StartCoroutine(IntroCoroutine());
    }

    IEnumerator IntroCoroutine()
    {
        Debug.Log("--- IntroCoroutine STARTED ---");

        // 0. Odtwórz dŸwiêk
        if (SoundFXManager.Instance != null && introSoundClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(introSoundClip, transform, introSoundVolume);
            Debug.Log($"IntroCoroutine: Playing intro sound: {introSoundClip.name}");
        }
        else
        {
            Debug.Log("IntroCoroutine: SoundFXManager or introSoundClip not available, skipping sound.");
        }

        // 1. Zablokuj ruch gracza i rozgl¹danie siê
        Debug.Log("IntroCoroutine: Attempting to disable player controls...");
        if (playerMovementScript != null)
        {
            playerMovementScript.SetMovementEnabled(false);
            Debug.Log("IntroCoroutine: Called SetMovementEnabled(false) on playerMovementScript.");
        }
        else Debug.LogWarning("IntroCoroutine: playerMovementScript is NULL, cannot disable movement.");

        if (playerLookScript != null)
        {
            playerLookScript.SetLookEnabled(false);
            Debug.Log("IntroCoroutine: Called SetLookEnabled(false) on playerLookScript.");
        }
        else Debug.LogWarning("IntroCoroutine: playerLookScript is NULL, cannot disable look.");


        // 2. Poka¿ czarny ekran
        Debug.Log("IntroCoroutine: Attempting to show black screen panel...");
        if (introBlackScreenPanel != null)
        {
            introBlackScreenPanel.SetActive(true);
            Debug.Log($"IntroCoroutine: introBlackScreenPanel.SetActive(true) called. Panel activeSelf: {introBlackScreenPanel.activeSelf}");
        }
        else Debug.LogError("IntroCoroutine ERROR: introBlackScreenPanel is NULL at the point of showing!");


        // 3. Ustaw i poka¿ tekst
        Debug.Log("IntroCoroutine: Attempting to set and show text...");
        if (introDisplayText != null && introBlackScreenPanel != null && introBlackScreenPanel.activeSelf)
        {
            introDisplayText.text = textToShowDuringIntro;
            introDisplayText.gameObject.SetActive(true);
            Debug.Log($"IntroCoroutine: Displaying intro text: '{textToShowDuringIntro}'. Text object activeSelf: {introDisplayText.gameObject.activeSelf}");
        }
        else
        {
            if (introDisplayText == null) Debug.LogWarning("IntroCoroutine: introDisplayText is NULL, cannot show text.");
            else Debug.LogWarning($"IntroCoroutine: Cannot show text. introDisplayText assigned: {introDisplayText != null}, introBlackScreenPanel active: {introBlackScreenPanel?.activeSelf}");
        }

        // 4. Czekaj przez okreœlony czas
        Debug.Log($"IntroCoroutine: Waiting for {introDuration} seconds...");
        yield return new WaitForSeconds(introDuration);
        Debug.Log("IntroCoroutine: Wait finished.");

        // 5. Ukryj czarny ekran
        Debug.Log("IntroCoroutine: Attempting to hide black screen panel...");
        if (introBlackScreenPanel != null)
        {
            introBlackScreenPanel.SetActive(false);
            Debug.Log($"IntroCoroutine: introBlackScreenPanel.SetActive(false) called. Panel activeSelf: {introBlackScreenPanel.activeSelf}");
        }
        else Debug.LogError("IntroCoroutine ERROR: introBlackScreenPanel is NULL at the point of hiding!");


        // 6. Odblokuj ruch gracza i rozgl¹danie siê
        Debug.Log("IntroCoroutine: Attempting to enable player controls...");
        if (playerMovementScript != null)
        {
            playerMovementScript.SetMovementEnabled(true);
            Debug.Log("IntroCoroutine: Called SetMovementEnabled(true) on playerMovementScript.");
        }
        else Debug.LogWarning("IntroCoroutine: playerMovementScript is NULL, cannot enable movement.");

        if (playerLookScript != null)
        {
            playerLookScript.SetLookEnabled(true);
            Debug.Log("IntroCoroutine: Called SetLookEnabled(true) on playerLookScript.");
        }
        else Debug.LogWarning("IntroCoroutine: playerLookScript is NULL, cannot enable look.");

        Debug.Log("--- IntroCoroutine FINISHED ---");
    }
}