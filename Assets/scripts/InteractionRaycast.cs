using UnityEngine;
using TMPro; // Pami�taj o dodaniu tej linii dla TextMeshPro

public class InteractionRaycast : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 2f; // Dystans interakcji
    [SerializeField] private Camera playerCamera;           // Kamera gracza

    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI interactionText; // Tekst podpowiedzi (TMP)

    private Interactable currentInteractable; // Aktualnie namierzony obiekt Interactable

    void Start()
    {
        // Weryfikacja przypisania kamery
        if (playerCamera == null)
        {
            Debug.LogError("Kamera gracza (Player Camera) nie jest przypisana w InteractionRaycast!");
            playerCamera = Camera.main; // Spr�buj znale�� g��wn� kamer�
            if (playerCamera == null)
            {
                Debug.LogError("Nie znaleziono g��wnej kamery!");
                enabled = false; // Wy��cz skrypt, je�li nie ma kamery
            }
        }

        // Weryfikacja przypisania tekstu UI
        if (interactionText == null)
        {
            Debug.LogError("Tekst interakcji (Interaction Text) nie jest przypisany w InteractionRaycast!");
            enabled = false; // Wy��cz skrypt, je�li nie ma UI
            return;
        }

        // Ukryj tekst na starcie
        interactionText.gameObject.SetActive(false);
    }

    void Update()
    {
        PerformRaycast();
        HandleInteractionInput();
    }

    void PerformRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hitInfo;

        Interactable detectedInteractable = null; // Zmienna przechowuj�ca wykryty Interactable w tej klatce

        // Rysuj promie� w edytorze dla debugowania
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.yellow);

        // Wystrzel promie�
        if (Physics.Raycast(ray, out hitInfo, interactionDistance))
        {
            // Spr�buj pobra� komponent Interactable z trafionego obiektu
            detectedInteractable = hitInfo.collider.GetComponent<Interactable>();
        }

        // Sprawd�, czy wykryty obiekt jest inny ni� ten, na kt�ry patrzyli�my poprzednio
        if (currentInteractable != detectedInteractable)
        {
            if (detectedInteractable != null)
            {
                // Zacz�li�my patrze� na nowy obiekt interaktywny
                currentInteractable = detectedInteractable;
                ShowInteractionPrompt(currentInteractable.interactionPrompt);
            }
            else
            {
                // Przestali�my patrze� na obiekt interaktywny
                currentInteractable = null;
                HideInteractionPrompt();
            }
        }
    }

    void HandleInteractionInput()
    {
        // Je�li patrzymy na obiekt interaktywny i naci�niemy klawisz F
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.F))
        {
            Interactable interactedObject = currentInteractable;
            currentInteractable.Interact(); // Wywo�aj metod� Interact na tym obiekcie
            currentInteractable = null; // Przesta� �ledzi� ten obiekt
            HideInteractionPrompt();
        }
    }

    void ShowInteractionPrompt(string prompt)
    {
        interactionText.text = prompt;
        interactionText.gameObject.SetActive(true);
    }

    void HideInteractionPrompt()
    {
        interactionText.gameObject.SetActive(false);
    }
}