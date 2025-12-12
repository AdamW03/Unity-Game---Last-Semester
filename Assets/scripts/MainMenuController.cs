using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuController : MonoBehaviour
{

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;
    }
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

public void OptionMenu()
    {
        SceneManager.LoadScene("options");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
