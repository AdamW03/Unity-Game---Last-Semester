using UnityEngine;
using UnityEngine.Events; // Potrzebne do u¿ywania UnityEvent

public class Interactable : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [Tooltip("Tekst, który pojawi siê, gdy gracz spojrzy na ten obiekt.")]
    public string interactionPrompt = "[F] Interact";

    [Tooltip("Akcje do wykonania po naciœniêciu klawisza interakcji.")]
    public UnityEvent onInteract;

    // --- NOWE POLE ---
    [Header("Dane Przedmiotu (jeœli podnoszony)")]
    [Tooltip("Dane przedmiotu do dodania do ekwipunku (pozostaw puste, jeœli nie jest podnoszony).")]
    public InventoryItem itemData;
    // ---------------

    public virtual void Interact()
    {
        Debug.Log($"Wykonano interakcjê z: {gameObject.name}");
        onInteract.Invoke();
    }

    // --- NOWA METODA POMOCNICZA ---
    // Metoda, któr¹ wywo³amy przez UnityEvent, aby dodaæ przedmiot i zniszczyæ obiekt
    public void PickupItemAndDestroy()
    {
        if (itemData != null)
        {
            // Zak³adamy, ¿e CarouselInventory ma statyczn¹ instancjê (Singleton)
            CarouselInventory.Instance?.AddItem(itemData); // Dodaj przedmiot do ekwipunku
            Destroy(gameObject); // Zniszcz obiekt w scenie
        }
        else
        {
            Debug.LogWarning($"Próbowano podnieœæ {gameObject.name}, ale nie ma przypisanych danych przedmiotu (Item Data)!");
            // Opcjonalnie zniszcz mimo wszystko lub zostaw
            // Destroy(gameObject);
        }
    }
}