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
        // --- Czu³oœæ ---
        if (sensitivityScrollbar != null && playerLookComponent != null)
        {
            float currentSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
            playerLookComponent.sensitivity = currentSensitivity;
            sensitivityScrollbar.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, currentSensitivity);
            // Usuniêto wywo³anie UpdateSensitivityText

            sensitivityScrollbar.onValueChanged.RemoveAllListeners();
            sensitivityScrollbar.onValueChanged.AddListener(OnSensitivityScrollbarChanged);
        }
        else
        {
            Debug.LogWarning("Sensitivity Scrollbar lub PlayerLookComponent nie jest przypisany.");
        }

        // --- FOV ---
        if (fovScrollbar != null && playerCameraComponent != null)
        {
            float currentFOV = PlayerPrefs.GetFloat("CameraFOV", defaultFOV);
            playerCameraComponent.fieldOfView = currentFOV;
            fovScrollbar.value = Mathf.InverseLerp(minFOV, maxFOV, currentFOV);
            // Usuniêto wywo³anie UpdateFOVText

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
                    CloseSettingsPanel();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
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
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenSettingsPanel()
    {
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
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Debug.Log("PauseMenuManager: Zamkniêto panel ustawieñ, powrót do menu pauzy.");
    }

    public void OnSensitivityScrollbarChanged(float scrollbarValue)
    {
        if (playerLookComponent != null)
        {
            float actualSensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, scrollbarValue);
            playerLookComponent.sensitivity = actualSensitivity;
            PlayerPrefs.SetFloat("MouseSensitivity", actualSensitivity);
        }
    }

    public void OnFOVScrollbarChanged(float scrollbarValue)
    {
        if (playerCameraComponent != null)
        {
            float actualFOV = Mathf.Lerp(minFOV, maxFOV, scrollbarValue);
            playerCameraComponent.fieldOfView = actualFOV;
            PlayerPrefs.SetFloat("CameraFOV", actualFOV);
        }
    }


    public void QuitGame()
    {
        Debug.Log("PauseMenuManager: Zamykanie gry...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}