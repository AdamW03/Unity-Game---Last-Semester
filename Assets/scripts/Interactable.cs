using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    [Tooltip("DŸwiêk odtwarzany przy podniesieniu tego przedmiotu.")]
    [SerializeField] private AudioClip itemPickupSoundClip; // <-- NOWE POLE NA DWIÊK PODNIESIENIA PRZEDMIOTU

    [Header("Dane Notatki (jeœli to notatka)")]
    [Tooltip("Dane notatki do dodania do notatnika (pozostaw puste, jeœli to nie notatka).")]
    public NoteData noteData;
    [Tooltip("DŸwiêk odtwarzany przy podniesieniu tej notatki.")]
    [SerializeField] private AudioClip notePickupSoundClip; // <-- NOWE POLE NA DWIÊK PODNIESIENIA NOTATKI


    public virtual void Interact()
    {
        Debug.Log($"Wykonano interakcjê z: {gameObject.name}");
        onInteract.Invoke();
    }

    public void PickupItemAndDestroy()
    {
        if (itemData != null)
        {
            // Odtwórz dŸwiêk podniesienia przedmiotu, jeœli jest przypisany
            if (SoundFXManager.Instance != null && itemPickupSoundClip != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(itemPickupSoundClip, transform, 1f); // U¿ywamy transformu tego obiektu i domyœlnej g³oœnoœci 1f
            }
            else if (itemPickupSoundClip == null)
            {
                Debug.LogWarning($"Brak przypisanego dŸwiêku podniesienia (itemPickupSoundClip) dla {gameObject.name}, ale przedmiot zostanie podniesiony.");
            }
            else if (SoundFXManager.Instance == null)
            {
                Debug.LogWarning("SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku podniesienia.");
            }

            CarouselInventory.Instance?.AddItem(itemData);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"Próbowano podnieœæ {gameObject.name}, ale nie ma przypisanych danych przedmiotu (Item Data)!");
        }
    }

    public void PickupNoteAndDestroy()
    {
        if (noteData != null)
        {
            // Odtwórz dŸwiêk podniesienia notatki, jeœli jest przypisany
            if (SoundFXManager.Instance != null && notePickupSoundClip != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(notePickupSoundClip, transform, 1f); // U¿ywamy transformu tego obiektu i domyœlnej g³oœnoœci 1f
            }
            else if (notePickupSoundClip == null)
            {
                Debug.LogWarning($"Brak przypisanego dŸwiêku podniesienia (notePickupSoundClip) dla notatki {gameObject.name}, ale notatka zostanie podniesiona.");
            }
            else if (SoundFXManager.Instance == null)
            {
                Debug.LogWarning("SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku podniesienia notatki.");
            }

            // Poni¿sza linia by³a w Twoim oryginalnym kodzie. Jeœli notatka ma równie¿ dodawaæ itemData, odkomentuj j¹.
            // Zazwyczaj podniesienie notatki dodaje j¹ tylko do NotebookManager.
            // CarouselInventory.Instance?.AddItem(itemData); 

            NotebookManager.Instance?.AddNote(noteData);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"Próbowano podnieœæ {gameObject.name} jako notatkê, ale nie ma przypisanych danych (Note Data)!");
        }
    }
    public void ChangeToNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);       
    }

}