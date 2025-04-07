using UnityEngine;
using UnityEngine.Events; 

public class Interactable : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [Tooltip("Tekst, który pojawi siê, gdy gracz spojrzy na ten obiekt.")]
    public string interactionPrompt = "[F] Interact";

    [Tooltip("Akcje do wykonania po naciœniêciu klawisza interakcji.")]
    public UnityEvent onInteract;

    
    [Header("Dane Przedmiotu (jeœli podnoszony)")]
    [Tooltip("Dane przedmiotu do dodania do ekwipunku (pozostaw puste, jeœli nie jest podnoszony).")]
    public InventoryItem itemData;

    [Header("Dane Notatki (jeœli to notatka)")]
    [Tooltip("Dane notatki do dodania do notatnika (pozostaw puste, jeœli to nie notatka).")]
    public NoteData noteData;

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

    public void PickupNoteAndDestroy()
    {
        if (noteData != null)
        {
            CarouselInventory.Instance?.AddItem(itemData);
            NotebookManager.Instance?.AddNote(noteData); 
            Destroy(gameObject); 
        }
        else
        {
            Debug.LogWarning($"Próbowano podnieœæ {gameObject.name} jako notatkê, ale nie ma przypisanych danych (Note Data)!");
            // Destroy(gameObject); 
        }
    }
}