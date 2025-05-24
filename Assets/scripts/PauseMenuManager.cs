using UnityEngine;
using UnityEngine.SceneManagement; // Do zarz¹dzania scenami

public class PauseMenuManager : MonoBehaviour
{
    // Statyczna zmienna, aby inne skrypty mog³y sprawdziæ, czy gra jest spauzowana
    public static bool GameIsPaused = false;

    [Header("UI Elements")]
    public GameObject pauseMenuUI; // Przeci¹gnij tutaj panel UI menu pauzy

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenuScene"; // Nazwa sceny menu g³ównego

    void Start()
    {
        // Upewnij siê, ¿e menu jest schowane na starcie i gra dzia³a
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

        // Ustawienia kursora na start gry (np. dla FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Nas³uchuj naciœniêcia klawisza Escape
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

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f; // Przywraca normalny up³yw czasu
        GameIsPaused = false;

        // Przywracamy kursor do trybu gry
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("PauseMenuManager: Gra wznowiona.");
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

        Debug.Log("PauseMenuManager: Gra spauzowana.");
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
        // Jeœli jesteœ w edytorze Unity, zatrzymaj odtwarzanie
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}