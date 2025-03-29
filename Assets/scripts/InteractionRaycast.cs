using UnityEngine;
using TMPro; // Pamiêtaj o dodaniu tej linii dla TextMeshPro

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
            playerCamera = Camera.main; // Spróbuj znaleŸæ g³ówn¹ kamerê
            if (playerCamera == null)
            {
                Debug.LogError("Nie znaleziono g³ównej kamery!");
                enabled = false; // Wy³¹cz skrypt, jeœli nie ma kamery
            }
        }

        // Weryfikacja przypisania tekstu UI
        if (interactionText == null)
        {
            Debug.LogError("Tekst interakcji (Interaction Text) nie jest przypisany w InteractionRaycast!");
            enabled = false; // Wy³¹cz skrypt, jeœli nie ma UI
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

        Interactable detectedInteractable = null; // Zmienna przechowuj¹ca wykryty Interactable w tej klatce

        // Rysuj promieñ w edytorze dla debugowania
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.yellow);

        // Wystrzel promieñ
        if (Physics.Raycast(ray, out hitInfo, interactionDistance))
        {
            // Spróbuj pobraæ komponent Interactable z trafionego obiektu
            detectedInteractable = hitInfo.collider.GetComponent<Interactable>();
        }

        // SprawdŸ, czy wykryty obiekt jest inny ni¿ ten, na który patrzyliœmy poprzednio
        if (currentInteractable != detectedInteractable)
        {
            if (detectedInteractable != null)
            {
                // Zaczêliœmy patrzeæ na nowy obiekt interaktywny
                currentInteractable = detectedInteractable;
                ShowInteractionPrompt(currentInteractable.interactionPrompt);
            }
            else
            {
                // Przestaliœmy patrzeæ na obiekt interaktywny
                currentInteractable = null;
                HideInteractionPrompt();
            }
        }
    }

    void HandleInteractionInput()
    {
        // Jeœli patrzymy na obiekt interaktywny i naciœniemy klawisz F
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.F))
        {
            Interactable interactedObject = currentInteractable;
            currentInteractable.Interact(); // Wywo³aj metodê Interact na tym obiekcie
            currentInteractable = null; // Przestañ œledziæ ten obiekt
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