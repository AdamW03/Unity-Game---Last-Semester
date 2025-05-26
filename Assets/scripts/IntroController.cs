using UnityEngine;
using TMPro;
using System.Collections;

public class IntroController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject introBlackScreenPanel;
    [SerializeField] private CanvasGroup introPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI introDisplayText;

    [Header("Intro Content")]
    [TextArea(3, 10)]
    public string textToShowDuringIntro = "Witaj w grze...";
    public AudioClip introSoundClip;
    [Range(0f, 1f)]
    public float introSoundVolume = 0.7f;

    [Header("Settings")]
    public float introDuration = 10f;
    public float fadeOutDuration = 1.5f;

    [Header("Player Control")]
    public FirstPersonMovement playerMovementScript;
    public FirstPersonLook playerLookScript;

    void Start()
    {
        Debug.Log("--- IntroController: Start() CALLED ---");

        if (introBlackScreenPanel != null && introPanelCanvasGroup == null)
        {
            introPanelCanvasGroup = introBlackScreenPanel.GetComponent<CanvasGroup>();
            if (introPanelCanvasGroup == null)
            {
                Debug.LogWarning("IntroController WARNING: introBlackScreenPanel nie ma komponentu CanvasGroup. Dodajê go automatycznie.");
                introPanelCanvasGroup = introBlackScreenPanel.AddComponent<CanvasGroup>();
            }
        }

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
        if (introPanelCanvasGroup == null)
        {
            Debug.LogError("IntroController ERROR: introPanelCanvasGroup jest NULL! Fade out nie bêdzie mo¿liwy.");
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
            return;
        }

        if (introBlackScreenPanel != null)
        {
            introBlackScreenPanel.SetActive(false);
            if (introPanelCanvasGroup != null) introPanelCanvasGroup.alpha = 0f;
        }

        Debug.Log("IntroController: All checks passed, starting IntroCoroutine from Start().");
        StartCoroutine(IntroCoroutine());
    }

    IEnumerator IntroCoroutine()
    {
        Debug.Log("--- IntroCoroutine STARTED ---");

        if (SoundFXManager.Instance != null && introSoundClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(introSoundClip, transform, introSoundVolume);
            Debug.Log($"IntroCoroutine: Playing intro sound: {introSoundClip.name}");
        }
        else
        {
            Debug.Log("IntroCoroutine: SoundFXManager or introSoundClip not available, skipping sound.");
        }

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

        Debug.Log("IntroCoroutine: Attempting to show black screen panel...");
        if (introBlackScreenPanel != null && introPanelCanvasGroup != null)
        {
            introPanelCanvasGroup.alpha = 1f;
            introBlackScreenPanel.SetActive(true);
            Debug.Log($"IntroCoroutine: introBlackScreenPanel.SetActive(true) called. Panel activeSelf: {introBlackScreenPanel.activeSelf}, CanvasGroup Alpha: {introPanelCanvasGroup.alpha}");
        }
        else
        {
            Debug.LogError("IntroCoroutine ERROR: introBlackScreenPanel or introPanelCanvasGroup is NULL at the point of showing! Cannot proceed with intro visuals.");
            yield return new WaitForSeconds(introDuration > 0 ? introDuration : 1f);
        }

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

        Debug.Log($"IntroCoroutine: Waiting for {introDuration} seconds (panel visible)...");
        yield return new WaitForSeconds(introDuration);
        Debug.Log("IntroCoroutine: Panel visible wait finished.");

        Debug.Log("IntroCoroutine: Starting fade out sequence...");
        if (introPanelCanvasGroup != null && introBlackScreenPanel != null && introBlackScreenPanel.activeSelf)
        {
            yield return StartCoroutine(FadeOutPanelCoroutine(introPanelCanvasGroup, fadeOutDuration));
        }
        else
        {
            Debug.LogError("IntroCoroutine ERROR: introPanelCanvasGroup is NULL or panel is not active, cannot start fade out!");
        }

        if (introBlackScreenPanel != null)
        {
            introBlackScreenPanel.SetActive(false);
            Debug.Log($"IntroCoroutine: introBlackScreenPanel.SetActive(false) called after fade attempt. Panel activeSelf: {introBlackScreenPanel.activeSelf}");
        }

        // ----- POCZ¥TEK KLUCZOWEJ ZMIANY -----
        Debug.Log("IntroCoroutine: Waiting one frame before enabling player controls to ensure all states are updated...");
        yield return null; // Poczekaj na nastêpn¹ klatkê przed odblokowaniem kontroli
        // ----- KONIEC KLUCZOWEJ ZMIANY -----

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

    IEnumerator FadeOutPanelCoroutine(CanvasGroup canvasGroup, float duration)
    {
        float currentTime = 0f;
        float startAlpha = 1f;

        Debug.Log($"FadeOutPanelCoroutine: Starting fade. Duration: {duration:F3}, Start Alpha: {startAlpha:F3}, Time.timeScale: {Time.timeScale}");

        if (duration <= 0f)
        {
            Debug.LogWarning("FadeOutPanelCoroutine: Duration is 0 or negative. Setting alpha to 0 directly.");
            canvasGroup.alpha = 0f;
            yield break;
        }

        if (Time.timeScale == 0f)
        {
            Debug.LogError("FadeOutPanelCoroutine: Time.timeScale is 0! Fade out will not work correctly with Time.deltaTime. Alpha will be set to 0 at the end.");
        }

        int loopCount = 0;

        while (currentTime < duration)
        {
            loopCount++;
            if (Time.timeScale == 0f && duration > 0)
            {
                Debug.LogWarning($"FadeOutPanelCoroutine LOOP {loopCount}: Breaking loop because Time.timeScale is 0.");
                break;
            }

            currentTime += Time.deltaTime;
            float ratio = Mathf.Clamp01(currentTime / duration);
            float newAlpha = Mathf.Lerp(startAlpha, 0f, ratio);
            canvasGroup.alpha = newAlpha;
            // Odkomentuj poni¿szy log tylko jeœli potrzebujesz bardzo szczegó³owej diagnostyki pêtli zanikania
            // Debug.Log($"FadeOutPanelCoroutine LOOP {loopCount}: currentTime: {currentTime:F3}, deltaTime: {Time.deltaTime:F3}, ratio: {ratio:F3}, newAlpha: {newAlpha:F3}");
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Debug.Log($"FadeOutPanelCoroutine: Fade complete after {loopCount} loop iterations. Alpha set to 0.");
    }
}