using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI Elements - Main Pause")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;

    [Header("UI Elements - Settings Panel")]
    public Scrollbar sensitivityScrollbar;
    public Scrollbar fovScrollbar;

    [Header("Player Components References")]
    public FirstPersonLook playerLookComponent;
    public Camera playerCameraComponent;

    [Header("Settings Ranges")]
    public float minSensitivity = 0.5f;
    public float maxSensitivity = 5f;
    public float defaultSensitivity = 2f;

    public float minFOV = 60f;
    public float maxFOV = 90f;
    public float defaultFOV = 75f;

    [Header("Audio")] // <-- NOWA SEKCJA
    [Tooltip("DŸwiêk odtwarzany przy klikniêciu dowolnego przycisku w menu pauzy.")]
    [SerializeField] private AudioClip buttonClickSoundClip; // <-- NOWE POLE NA DWIÊK KLIKNIÊCIA

    [Header("Scene Management")]
    public string mainMenuSceneName = "menu";

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        else Debug.LogError("PauseMenuUI nie jest przypisane!");

        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        else Debug.LogError("SettingsMenuUI nie jest przypisane!");

        Time.timeScale = 1f;
        GameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InitializeSettings();
    }

    void InitializeSettings()
    {
        if (sensitivityScrollbar != null && playerLookComponent != null)
        {
            float currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
            playerLookComponent.sensitivity = currentSensitivity;
            sensitivityScrollbar.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, currentSensitivity);
            sensitivityScrollbar.onValueChanged.RemoveAllListeners();
            sensitivityScrollbar.onValueChanged.AddListener(OnSensitivityScrollbarChanged);
        }
        else
        {
            Debug.LogWarning("Sensitivity Scrollbar lub PlayerLookComponent nie jest przypisany.");
        }

        if (fovScrollbar != null && playerCameraComponent != null)
        {
            float currentFOV = PlayerPrefs.GetFloat("CameraFOV", defaultFOV);
            playerCameraComponent.fieldOfView = currentFOV;
            fovScrollbar.value = Mathf.InverseLerp(minFOV, maxFOV, currentFOV);
            fovScrollbar.onValueChanged.RemoveAllListeners();
            fovScrollbar.onValueChanged.AddListener(OnFOVScrollbarChanged);
        }
        else
        {
            Debug.LogWarning("FOV Scrollbar lub PlayerCameraComponent nie jest przypisany.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                if (settingsMenuUI != null && settingsMenuUI.activeSelf)
                {
                    CloseSettingsPanel(); // DŸwiêk zostanie odtworzony wewn¹trz tej metody
                }
                else
                {
                    Resume(); // DŸwiêk zostanie odtworzony wewn¹trz tej metody
                }
            }
            else
            {
                Pause(); // DŸwiêk otwarcia menu pauzy (jeœli chcesz) mo¿na dodaæ tutaj lub zostawiæ bez
            }
        }
    }

    // --- NOWA METODA DO ODTWARZANIA DWIÊKU KLIKNIÊCIA ---
    private void PlayButtonClickSound()
    {
        if (SoundFXManager.Instance != null && buttonClickSoundClip != null)
        {
            // Odtwarzamy dŸwiêk w pozycji kamery gracza lub tego obiektu
            // Dla UI czêsto wystarczy pozycja tego obiektu lub kamery UI
            SoundFXManager.Instance.PlaySoundFXClip(buttonClickSoundClip, transform, 1f);
        }
        else if (buttonClickSoundClip == null)
        {
            // Ten log mo¿e byæ zbyt czêsty, jeœli nie przypiszesz dŸwiêku, wiêc mo¿na go zakomentowaæ
            // Debug.LogWarning("PauseMenuManager: Brak przypisanego dŸwiêku klikniêcia przycisku (buttonClickSoundClip).");
        }
        else if (SoundFXManager.Instance == null)
        {
            Debug.LogWarning("PauseMenuManager: SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku klikniêcia.");
        }
    }
    // ---------------------------------------------------------

    public void Resume()
    {
        PlayButtonClickSound(); // <-- ODTWÓRZ DWIÊK
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);

        Time.timeScale = 1f;
        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("PauseMenuManager: Gra wznowiona.");
    }

    void Pause()
    {
        // DŸwiêk otwarcia menu pauzy (jeœli chcesz inny ni¿ klikniêcie) mo¿na dodaæ tutaj
        // np. PlaySound(openMenuSoundClip, "otwarcia menu pauzy");
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);

        Time.timeScale = 0f;
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("PauseMenuManager: Gra spauzowana, g³ówne menu pauzy aktywne.");
    }

    public void LoadMenu()
    {
        PlayButtonClickSound(); // <-- ODTWÓRZ DWIÊK
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenSettingsPanel()
    {
        PlayButtonClickSound(); // <-- ODTWÓRZ DWIÊK
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null)
        {
            settingsMenuUI.SetActive(true);
            if (playerLookComponent != null && sensitivityScrollbar != null)
            {
                sensitivityScrollbar.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, playerLookComponent.sensitivity);
            }
            if (playerCameraComponent != null && fovScrollbar != null)
            {
                fovScrollbar.value = Mathf.InverseLerp(minFOV, maxFOV, playerCameraComponent.fieldOfView);
            }
            Debug.Log("PauseMenuManager: Otwarto panel ustawieñ.");
        }
    }

    public void CloseSettingsPanel()
    {
        PlayButtonClickSound(); // <-- ODTWÓRZ DWIÊK
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Debug.Log("PauseMenuManager: Zamkniêto panel ustawieñ, powrót do menu pauzy.");
    }

    public void OnSensitivityScrollbarChanged(float scrollbarValue)
    {
        // Zazwyczaj nie odtwarzamy dŸwiêku klikniêcia przy ka¿dej zmianie wartoœci suwaka/scrollbara,
        // bo by³oby to zbyt czêste. DŸwiêk jest bardziej odpowiedni dla akcji "jednorazowych".
        if (playerLookComponent != null)
        {
            float actualSensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, scrollbarValue);
            playerLookComponent.sensitivity = actualSensitivity;
            PlayerPrefs.SetFloat("MouseSensitivity", actualSensitivity);
        }
    }

    public void OnFOVScrollbarChanged(float scrollbarValue)
    {
        // Podobnie jak wy¿ej, brak dŸwiêku klikniêcia.
        if (playerCameraComponent != null)
        {
            float actualFOV = Mathf.Lerp(minFOV, maxFOV, scrollbarValue);
            playerCameraComponent.fieldOfView = actualFOV;
            PlayerPrefs.SetFloat("CameraFOV", actualFOV);
        }
    }

    public void QuitGame()
    {
        PlayButtonClickSound(); // <-- ODTWÓRZ DWIÊK
        Debug.Log("PauseMenuManager: Zamykanie gry...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}