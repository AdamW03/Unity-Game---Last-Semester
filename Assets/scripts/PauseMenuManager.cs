using UnityEngine;
using UnityEngine.SceneManagement; // Do zarz¹dzania scenami
using UnityEngine.Rendering.PostProcessing; // Do efektów post-processingu

public class PauseMenuManager : MonoBehaviour
{
    // Statyczna zmienna, aby inne skrypty mog³y sprawdziæ, czy gra jest spauzowana
    public static bool GameIsPaused = false;

    [Header("UI Elements")]
    public GameObject pauseMenuUI; // Przeci¹gnij tutaj panel UI menu pauzy

    [Header("Post-Processing")]
    public PostProcessVolume postProcessVolume; // Przeci¹gnij tutaj obiekt z PostProcessVolume
    private DepthOfField depthOfField;          // Referencja do efektu Depth of Field

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenuScene"; // Nazwa sceny menu g³ównego

    void Start()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuUI nie jest przypisane w PauseMenuManager!");
        }

        Time.timeScale = 1f;
        GameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InitializeDepthOfField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void InitializeDepthOfField()
    {
        if (postProcessVolume != null)
        {
            if (postProcessVolume.profile != null)
            {
                if (postProcessVolume.profile.TryGetSettings(out depthOfField))
                {
                    Debug.Log("PauseMenuManager: Depth of Field effect FOUND in profile.");
                    // POPRAWKA TUTAJ:
                    depthOfField.active = false; // Domyœlnie wy³¹czamy efekt przy starcie gry
                }
                else
                {
                    Debug.LogError("PauseMenuManager: Depth of Field effect NOT FOUND in the assigned PostProcessProfile! Upewnij siê, ¿e efekt 'Depth of Field' jest dodany do profilu przypisanego do PostProcessVolume.");
                    depthOfField = null;
                }
            }
            else
            {
                Debug.LogWarning("PauseMenuManager: PostProcessProfile nie jest przypisany do PostProcessVolume!");
                depthOfField = null;
            }
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: PostProcessVolume nie jest przypisany w Inspektorze. Efekt rozmycia nie bêdzie dzia³a³.");
            depthOfField = null;
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        GameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (depthOfField != null)
        {
            // POPRAWKA TUTAJ:
            depthOfField.active = false;
            // POPRAWKA TUTAJ (w logu):
            Debug.Log("PauseMenuManager: Depth of Field Deactivated. Active state: " + depthOfField.active);
        }
    }

    void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        Time.timeScale = 0f;
        GameIsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (depthOfField != null)
        {
            // POPRAWKA TUTAJ:
            depthOfField.active = true;
            // POPRAWKA TUTAJ (w logu):
            Debug.Log("PauseMenuManager: Depth of Field Activated. Active state: " + depthOfField.active);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: Cannot activate Depth of Field, effect reference is null or not found.");
        }
    }

    public void LoadMenu()
    {
        Debug.Log("PauseMenuManager: £adowanie menu g³ównego: " + mainMenuSceneName);
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenSettings()
    {
        Debug.Log("PauseMenuManager: Otwieranie ustawieñ... (funkcjonalnoœæ do zaimplementowania)");
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