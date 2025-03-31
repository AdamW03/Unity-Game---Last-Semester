using UnityEngine;
using System.Collections; // Potrzebne dla Coroutine (opcjonalne op�nienie)

// Opcjonalnie: Wymu� posiadanie komponentu AudioSource, je�li planujesz d�wi�ki
[RequireComponent(typeof(AudioSource))]
public class DoorController : MonoBehaviour
{
    [Header("Animacja")]
    [Tooltip("Referencja do komponentu Animator drzwi.")]
    public Animator doorAnimator;
    [Tooltip("Nazwa triggera w Animator Controller do otwierania drzwi.")]
    public string openTriggerName = "Open";
    [Tooltip("Nazwa triggera w Animator Controller do zamykania drzwi.")]
    public string closeTriggerName = "Close";
    [Tooltip("Nazwa STANU w Animatorze reprezentuj�cego drzwi otwarte (np. 'DoorOpenState'). MUSI by� DOK�ADN� nazw� stanu!")]
    public string openStateName = "Door_Open";
    [Tooltip("Nazwa STANU w Animatorze reprezentuj�cego drzwi zamkni�te (np. 'DoorClosedState'). MUSI by� DOK�ADN� nazw� stanu!")]
    public string closedStateName = "Door_Close";

    [Header("Stan Drzwi")]
    [Tooltip("Czy drzwi maj� zaczyna� w stanie otwartym? Ustaw te� odpowiednio obiekt w scenie.")]
    public bool startOpen = false; // Ta zmienna kontroluje stan pocz�tkowy
    private bool isOpen = false;

    [Header("Wymagania")]
    [Tooltip("Czy drzwi wymagaj� przedmiotu do otwarcia?")]
    public bool requiresItem = false;
    [Tooltip("Dane wymaganego przedmiotu (ScriptableObject typu InventoryItem).")]
    public InventoryItem requiredItemData; // U�ywamy typu z Twojego ekwipunku
    [Tooltip("Czy przedmiot ma zosta� zu�yty (usuni�ty z ekwipunku) po u�yciu?")]
    public bool consumeItem = false;
    [Tooltip("Wiadomo��, kt�ra mo�e si� pojawi�, gdy brakuje przedmiotu (opcjonalne).")]
    public string lockedMessage = "Potrzebujesz klucza...";

    [Header("D�wi�ki (Opcjonalne)")]
    [Tooltip("D�wi�k otwierania drzwi.")]
    public AudioClip openSound;
    [Tooltip("D�wi�k zamykania drzwi.")]
    public AudioClip closeSound;
    [Tooltip("D�wi�k zablokowanych drzwi (pr�ba otwarcia bez przedmiotu).")]
    public AudioClip lockedSound;

    private AudioSource audioSource;

    [Header("Ustawienia Tekstu Interakcji")]
    [Tooltip("Tekst wy�wietlany, gdy drzwi s� zamkni�te i mo�na je otworzy�.")]
    public string promptWhenClosed = "[F] Otw�rz";
    [Tooltip("Tekst wy�wietlany, gdy drzwi s� otwarte i mo�na je zamkn��.")]
    public string promptWhenOpen = "[F] Zamknij";
    [Tooltip("Tekst wy�wietlany tymczasowo PO interakcji.")]
    public string promptAfterInteraction = "U�yto...";
    [Tooltip("Czas (w sekundach), przez jaki wy�wietlany jest tymczasowy tekst.")]
    public float promptDisplayDuration = 0.2f;

    // Prywatne pole do przechowywania referencji do Interactable (znajdowane automatycznie)
    private Interactable doorInteractableTrigger;
    // Prywatna zmienna do �ledzenia aktywnej Coroutine zmiany tekstu
    private Coroutine activePromptCoroutine = null;

    void Awake()
    {
        // Pobierz referencj� do AudioSource przy starcie
        audioSource = GetComponent<AudioSource>();

        // Je�li nie przypisano w Inspektorze, spr�buj znale�� automatycznie
        if (doorInteractableTrigger == null)
        {
            doorInteractableTrigger = GetComponentInChildren<Interactable>();
        }
        if (doorInteractableTrigger == null)
        {
            Debug.LogError($"Nie znaleziono komponentu 'Interactable' na obiekcie {gameObject.name} ani na jego dzieciach!", this);
        }
        UpdateInteractionPrompt(isOpen ? promptWhenOpen : promptWhenClosed);

        // Upewnij si�, �e Animator jest przypisany, je�li nie, spr�buj go znale�� na tym samym obiekcie
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
            if (doorAnimator == null)
            {
                Debug.LogError($"Drzwi '{gameObject.name}' nie maj� przypisanego komponentu Animator!", this);
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
                // Sprawd�, czy nazwa stanu otwartego jest ustawiona
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu otwarcia (Open State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo�na zainicjowa� jako otwarte.", this);
                    isOpen = false; // Wr�� do stanu zamkni�tego, bo nie mo�emy poprawnie zainicjowa�
                    targetStateName = closedStateName; // Spr�buj ustawi� stan zamkni�ty
                    if (string.IsNullOrEmpty(targetStateName))
                    {
                        Debug.LogError($"Nazwa stanu zamkni�cia (Closed State Name) r�wnie� nie jest ustawiona dla drzwi '{gameObject.name}'!", this);
                        return; // Zako�cz, je�li obie nazwy s� puste
                    }
                }
            }
            else // Je�li startOpen jest false
            {
                targetStateName = closedStateName;
                // Sprawd�, czy nazwa stanu zamkni�tego jest ustawiona
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu zamkni�cia (Closed State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo�na zainicjowa� jako zamkni�te.", this);
                    return; // Zako�cz, je�li nazwa stanu zamkni�tego jest pusta
                }
            }

            // Natychmiast przejd� do KO�CA animacji w odpowiednim stanie (otwartym lub zamkni�tym)
            // Play(nazwaStanu, indeksWarstwy, znormalizowanyCzas)
            // znormalizowanyCzas = 1.0f oznacza koniec animacji w danym stanie.
            doorAnimator.Play(targetStateName, 0, 1.0f);
            Debug.Log($"Drzwi '{gameObject.name}' - Start(): Animator ustawiony na stan '{targetStateName}' (koniec animacji).");
        }
        else
        {
            Debug.LogError($"Drzwi '{gameObject.name}' nie maj� przypisanego komponentu Animator w metodzie Start!", this);
        }
    }

    // --- G��WNA METODA WYWO�YWANA PRZEZ INTERAKCJ� ---
    // T� metod� podepniesz pod UnityEvent 'onInteract' w komponencie Interactable
    public void ToggleDoor()
    {
        // 1. Sprawd� wymagania
        if (requiresItem)
        {
            // Sprawd�, czy ekwipunek istnieje i czy gracz ma wybrany przedmiot
            if (CarouselInventory.Instance == null)
            {
                Debug.LogError("Nie znaleziono instancji CarouselInventory!", this);
                PlaySound(lockedSound); // Odtw�rz d�wi�k b��du/blokady
                // Mo�esz te� wy�wietli� UI z informacj� o b��dzie
                return; // Przerwij dzia�anie
            }

            InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem(); // Potrzebujesz metody GetSelectedItem() w CarouselInventory

            // Sprawd�, czy wybrany przedmiot pasuje do wymaganego
            if (selectedItem == null || selectedItem != requiredItemData) // Por�wnujemy referencje do ScriptableObject
            {
                Debug.Log($"Pr�ba otwarcia drzwi '{gameObject.name}', ale brakuje wymaganego przedmiotu: {requiredItemData?.itemName ?? "Nieokre�lony"}");
                
                PlaySound(lockedSound);
                // Opcjonalnie: Wy�wietl graczowi wiadomo�� (np. u�ywaj�c systemu UI)
                if (!string.IsNullOrEmpty(lockedMessage))
                {
                    // Tutaj kod do wy�wietlenia lockedMessage na ekranie
                    Debug.Log(lockedMessage); // Tymczasowy log
                }
                return; // Przerwij dzia�anie, drzwi si� nie otworz�
            }

            // Przedmiot jest poprawny, kontynuuj. Sprawd�, czy zu�y�.
            if (consumeItem)
            {
                // Potrzebujesz metody do usuwania przedmiotu w CarouselInventory
                CarouselInventory.Instance.RemoveCurrentItem();
                requiresItem = false;
                Debug.Log($"Zu�yto przedmiot '{selectedItem.itemName}' do otwarcia drzwi '{gameObject.name}'.");
            }
        }

        // 2. Zmie� stan drzwi i uruchom animacj�/d�wi�k
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
            PlaySound(openSound);
            Debug.Log($"Drzwi '{gameObject.name}' otwarte.");
        }
    }

    private void Close()
    {
        if (isOpen && doorAnimator != null)
        {
            isOpen = false;
            doorAnimator.SetTrigger(closeTriggerName);
            PlaySound(closeSound);
            Debug.Log($"Drzwi '{gameObject.name}' zamkni�te.");
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
            Debug.LogWarning($"Pr�ba ustawienia tekstu interakcji, ale 'doorInteractableTrigger' nie jest ustawiony na {gameObject.name}.", this);
        }
    }

    private void ShowTemporaryInteractionPrompt(string temporaryPrompt, string eventualPrompt)
    {
        if (doorInteractableTrigger == null) return; // Sprawdzenie bezpiecze�stwa

        // Je�li poprzednia Coroutine zmiany tekstu jeszcze dzia�a, zatrzymaj j�
        if (activePromptCoroutine != null)
        {
            StopCoroutine(activePromptCoroutine);
        }

        // Uruchom now� Coroutine i zapisz do niej referencj�
        activePromptCoroutine = StartCoroutine(TemporaryPromptCoroutine(temporaryPrompt, eventualPrompt, promptDisplayDuration));
    }

    // Coroutine obs�uguj�ca tymczasow� zmian� tekstu
    IEnumerator TemporaryPromptCoroutine(string tempText, string finalText, float duration)
    {
        // 1. Ustaw tekst tymczasowy
        UpdateInteractionPrompt(tempText);

        // 2. Poczekaj okre�lony czas
        yield return new WaitForSeconds(duration);

        // 3. Sprawd�, czy TA Coroutine nadal powinna ustawia� tekst
        //    (Mo�liwe, �e inna interakcja uruchomi�a now� Coroutine w mi�dzyczasie)
        //    Por�wnujemy referencje - je�li activePromptCoroutine wskazuje na INN�
        //    coroutine lub null, to znaczy, �e ta ju� nie jest "aktualna".
        //    Jednak prostsze mo�e by� po prostu ustawienie tekstu ko�cowego.
        //    Je�li inna coroutine zosta�a uruchomiona, to ona ustawi sw�j tekst tymczasowy,
        //    a potem sw�j ko�cowy, nadpisuj�c ten. To zazwyczaj po��dane zachowanie.

        // Ustaw tekst ko�cowy (ten, kt�ry ma by� po odczekaniu)
        UpdateInteractionPrompt(finalText);

        // 4. Zako�czono, wyczy�� referencj�
        activePromptCoroutine = null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}