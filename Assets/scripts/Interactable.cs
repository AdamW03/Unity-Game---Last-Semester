using UnityEngine;
using UnityEngine.Events; // Potrzebne do u�ywania UnityEvent

public class Interactable : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [Tooltip("Tekst, kt�ry pojawi si�, gdy gracz spojrzy na ten obiekt.")]
    public string interactionPrompt = "[F] Interact";

    [Tooltip("Akcje do wykonania po naci�ni�ciu klawisza interakcji.")]
    public UnityEvent onInteract;

    // --- NOWE POLE ---
    [Header("Dane Przedmiotu (je�li podnoszony)")]
    [Tooltip("Dane przedmiotu do dodania do ekwipunku (pozostaw puste, je�li nie jest podnoszony).")]
    public InventoryItem itemData;
    // ---------------

    public virtual void Interact()
    {
        Debug.Log($"Wykonano interakcj� z: {gameObject.name}");
        onInteract.Invoke();
    }

    // --- NOWA METODA POMOCNICZA ---
    // Metoda, kt�r� wywo�amy przez UnityEvent, aby doda� przedmiot i zniszczy� obiekt
    public void PickupItemAndDestroy()
    {
        if (itemData != null)
        {
            // Zak�adamy, �e CarouselInventory ma statyczn� instancj� (Singleton)
            CarouselInventory.Instance?.AddItem(itemData); // Dodaj przedmiot do ekwipunku
            Destroy(gameObject); // Zniszcz obiekt w scenie
        }
        else
        {
            Debug.LogWarning($"Pr�bowano podnie�� {gameObject.name}, ale nie ma przypisanych danych przedmiotu (Item Data)!");
            // Opcjonalnie zniszcz mimo wszystko lub zostaw
            // Destroy(gameObject);
        }
    }
}