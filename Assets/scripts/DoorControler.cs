using UnityEngine;
using System.Collections; // Potrzebne dla Coroutine (opcjonalne opóŸnienie)

// Usuniêto: [RequireComponent(typeof(AudioSource))] // Ju¿ niepotrzebne
public class DoorController : MonoBehaviour
{
    [Header("Animacja")]
    [Tooltip("Referencja do komponentu Animator drzwi.")]
    public Animator doorAnimator;
    [Tooltip("Nazwa triggera w Animator Controller do otwierania drzwi.")]
    public string openTriggerName = "Open";
    [Tooltip("Nazwa triggera w Animator Controller do zamykania drzwi.")]
    public string closeTriggerName = "Close";
    [Tooltip("Nazwa STANU w Animatorze reprezentuj¹cego drzwi otwarte (np. 'DoorOpenState'). MUSI byæ DOK£ADN¥ nazw¹ stanu!")]
    public string openStateName = "Door_Open";
    [Tooltip("Nazwa STANU w Animatorze reprezentuj¹cego drzwi zamkniête (np. 'DoorClosedState'). MUSI byæ DOK£ADN¥ nazw¹ stanu!")]
    public string closedStateName = "Door_Close";

    [Header("Stan Drzwi")]
    [Tooltip("Czy drzwi maj¹ zaczynaæ w stanie otwartym? Ustaw te¿ odpowiednio obiekt w scenie.")]
    public bool startOpen = false;
    private bool isOpen = false;

    [Header("Wymagania")]
    [Tooltip("Czy drzwi wymagaj¹ przedmiotu do otwarcia?")]
    public bool requiresItem = false;
    [Tooltip("Dane wymaganego przedmiotu (ScriptableObject typu InventoryItem).")]
    public InventoryItem requiredItemData;
    [Tooltip("Czy przedmiot ma zostaæ zu¿yty (usuniêty z ekwipunku) po u¿yciu?")]
    public bool consumeItem = false;
    [Tooltip("Wiadomoœæ, która mo¿e siê pojawiæ, gdy brakuje przedmiotu (opcjonalne).")]
    public string lockedMessage = "Potrzebujesz klucza...";

    [Header("DŸwiêki (Opcjonalne)")]
    [Tooltip("DŸwiêk otwierania drzwi.")]
    public AudioClip openSound;
    [Tooltip("DŸwiêk zamykania drzwi.")]
    public AudioClip closeSound;
    [Tooltip("DŸwiêk zablokowanych drzwi (próba otwarcia bez przedmiotu).")]
    public AudioClip lockedSound;
    [Tooltip("G³oœnoœæ efektów dŸwiêkowych drzwi.")]
    [Range(0f, 1f)]
    public float doorSoundVolume = 1.0f; // Nowe pole dla g³oœnoœci

    // Usuniêto: private AudioSource audioSource; // Ju¿ niepotrzebne

    [Header("Ustawienia Tekstu Interakcji")]
    [Tooltip("Tekst wyœwietlany, gdy drzwi s¹ zamkniête i mo¿na je otworzyæ.")]
    public string promptWhenClosed = "[F] Open";
    [Tooltip("Tekst wyœwietlany, gdy drzwi s¹ otwarte i mo¿na je zamkn¹æ.")]
    public string promptWhenOpen = "[F] Close";
    [Tooltip("Tekst wyœwietlany tymczasowo PO interakcji.")]
    public string promptAfterInteraction = "Used...";
    [Tooltip("Czas (w sekundach), przez jaki wyœwietlany jest tymczasowy tekst.")]
    public float promptDisplayDuration = 0.2f;

    private Interactable doorInteractableTrigger;
    private Coroutine activePromptCoroutine = null;

    void Awake()
    {
        // Usuniêto: audioSource = GetComponent<AudioSource>(); // Ju¿ niepotrzebne

        if (doorInteractableTrigger == null)
        {
            doorInteractableTrigger = GetComponentInChildren<Interactable>();
        }
        if (doorInteractableTrigger == null)
        {
            Debug.LogError($"Nie znaleziono komponentu 'Interactable' na obiekcie {gameObject.name} ani na jego dzieciach!", this);
        }
        UpdateInteractionPrompt(isOpen ? promptWhenOpen : promptWhenClosed);

        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
            if (doorAnimator == null)
            {
                Debug.LogError($"Drzwi '{gameObject.name}' nie maj¹ przypisanego komponentu Animator!", this);
            }
        }

        isOpen = startOpen;
        Debug.Log($"Drzwi '{gameObject.name}' - Start(): Ustawiono isOpen na {isOpen} na podstawie startOpen.");

        if (doorAnimator != null)
        {
            string targetStateName = "";
            if (startOpen)
            {
                targetStateName = openStateName;
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu otwarcia (Open State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo¿na zainicjowaæ jako otwarte.", this);
                    isOpen = false;
                    targetStateName = closedStateName;
                    if (string.IsNullOrEmpty(targetStateName))
                    {
                        Debug.LogError($"Nazwa stanu zamkniêcia (Closed State Name) równie¿ nie jest ustawiona dla drzwi '{gameObject.name}'!", this);
                        return;
                    }
                }
            }
            else
            {
                targetStateName = closedStateName;
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu zamkniêcia (Closed State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo¿na zainicjowaæ jako zamkniête.", this);
                    return;
                }
            }
            doorAnimator.Play(targetStateName, 0, 1.0f);
            Debug.Log($"Drzwi '{gameObject.name}' - Start(): Animator ustawiony na stan '{targetStateName}' (koniec animacji).");
        }
        else
        {
            Debug.LogError($"Drzwi '{gameObject.name}' nie maj¹ przypisanego komponentu Animator w metodzie Start!", this);
        }
    }

    public void ToggleDoor()
    {
        if (requiresItem)
        {
            if (CarouselInventory.Instance == null)
            {
                Debug.LogError("Nie znaleziono instancji CarouselInventory!", this);
                PlayDoorSound(lockedSound); // Zmiana
                return;
            }

            InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();

            if (selectedItem == null || selectedItem != requiredItemData)
            {
                Debug.Log($"Próba otwarcia drzwi '{gameObject.name}', ale brakuje wymaganego przedmiotu: {requiredItemData?.itemName ?? "Nieokreœlony"}");
                PlayDoorSound(lockedSound); // Zmiana
                if (!string.IsNullOrEmpty(lockedMessage))
                {
                    Debug.Log(lockedMessage);
                }
                return;
            }

            if (consumeItem)
            {
                CarouselInventory.Instance.RemoveCurrentItem();
                requiresItem = false;
                Debug.Log($"Zu¿yto przedmiot '{selectedItem.itemName}' do otwarcia drzwi '{gameObject.name}'.");
            }
        }

        ShowTemporaryInteractionPrompt(promptAfterInteraction, !isOpen ? promptWhenOpen : promptWhenClosed);
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        if (!isOpen && doorAnimator != null)
        {
            isOpen = true;
            doorAnimator.SetTrigger(openTriggerName);
            PlayDoorSound(openSound); // Zmiana
            Debug.Log($"Drzwi '{gameObject.name}' otwarte.");
        }
    }

    private void Close()
    {
        if (isOpen && doorAnimator != null)
        {
            isOpen = false;
            doorAnimator.SetTrigger(closeTriggerName);
            PlayDoorSound(closeSound); // Zmiana
            Debug.Log($"Drzwi '{gameObject.name}' zamkniête.");
        }
    }

    // Nowa metoda do odtwarzania dŸwiêków przez SoundFXManager
    private void PlayDoorSound(AudioClip clip)
    {
        if (clip == null) return; // Nie próbuj odtwarzaæ, jeœli klip nie jest przypisany

        if (SoundFXManager.Instance != null)
        {
            // U¿ywamy transformu drzwi jako miejsca, z którego wydobywa siê dŸwiêk
            SoundFXManager.Instance.PlaySoundFXClip(clip, transform, doorSoundVolume);
        }
        else
        {
            Debug.LogWarning($"SoundFXManager.Instance nie jest dostêpny. DŸwiêk dla '{gameObject.name}' nie zostanie odtworzony.", this);
        }
    }

    private void UpdateInteractionPrompt(string newPrompt)
    {
        if (doorInteractableTrigger != null)
        {
            doorInteractableTrigger.interactionPrompt = newPrompt;
        }
        else
        {
            Debug.LogWarning($"Próba ustawienia tekstu interakcji, ale 'doorInteractableTrigger' nie jest ustawiony na {gameObject.name}.", this);
        }
    }

    private void ShowTemporaryInteractionPrompt(string temporaryPrompt, string eventualPrompt)
    {
        if (doorInteractableTrigger == null) return;

        if (activePromptCoroutine != null)
        {
            StopCoroutine(activePromptCoroutine);
        }
        activePromptCoroutine = StartCoroutine(TemporaryPromptCoroutine(temporaryPrompt, eventualPrompt, promptDisplayDuration));
    }

    IEnumerator TemporaryPromptCoroutine(string tempText, string finalText, float duration)
    {
        UpdateInteractionPrompt(tempText);
        yield return new WaitForSeconds(duration);
        UpdateInteractionPrompt(finalText);
        activePromptCoroutine = null;
    }
}