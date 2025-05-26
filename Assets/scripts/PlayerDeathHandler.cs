using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;


public class PlayerDeathHandler : MonoBehaviour
{
    public GameObject deathBackgroundObject;
    public TextMeshProUGUI deathText;

    public FirstPersonMovement playerMovementScript;

    public string mainMenuSceneName = "MainMenu";

    public float timeToWaitBeforeMenu = 5f;

    private bool isDying = false;

    void Start()
    {
        if (deathBackgroundObject != null)
        {
            deathBackgroundObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Obiekt tła ekranu śmierci (pole 'deathBackgroundObject') nie został przypisany w Inspektorze. Ekran śmierci może być niewidoczny.");
        }

        if (deathText != null)
        {
            deathText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Obiekt Text ekranu śmierci (pole 'deathText') nie został przypisany w Inspektorze. Ekran śmierci może być niewidoczny.");
        }

        if (playerMovementScript == null)
        {
            Debug.LogWarning("Skrypt ruchu gracza (pole 'playerMovementScript') typu FirstPersonMovement nie został przypisany w Inspektorze. Ruch gracza nie zostanie poprawnie zablokowany po śmierci.");
        }

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("Nazwa sceny menu głównego nie została ustawiona w Inspektorze!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !isDying)
        {
            Die();
        }
    }

    public void Die()
    {
        isDying = true;

        Debug.Log("Gracz umarł!");

        if (playerMovementScript != null)
        {
            playerMovementScript.SetMovementEnabled(false);
        }

        if (deathBackgroundObject != null)
        {
            deathBackgroundObject.SetActive(true);
        }
        if (deathText != null)
        {
            deathText.gameObject.SetActive(true);
        }

        StartCoroutine(WaitAndLoadMenu());
    }

    private IEnumerator WaitAndLoadMenu()
    {
        yield return new WaitForSeconds(timeToWaitBeforeMenu);

        if (!string.IsNullOrEmpty("menu"))
        {
            SceneManager.LoadScene("menu");
        }
        else
        {
            Debug.LogError("Nie można załadować sceny: Nazwa sceny menu głównego nie została ustawiona.");
        }
    }
}