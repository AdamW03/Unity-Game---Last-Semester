using UnityEngine;
using System.Collections; // Potrzebne dla Coroutine (opcjonalne opóŸnienie)

// Opcjonalnie: Wymuœ posiadanie komponentu AudioSource, jeœli planujesz dŸwiêki
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
    [Tooltip("Nazwa STANU w Animatorze reprezentuj¹cego drzwi otwarte (np. 'DoorOpenState'). MUSI byæ DOK£ADN¥ nazw¹ stanu!")]
    public string openStateName = "Door_Open";
    [Tooltip("Nazwa STANU w Animatorze reprezentuj¹cego drzwi zamkniête (np. 'DoorClosedState'). MUSI byæ DOK£ADN¥ nazw¹ stanu!")]
    public string closedStateName = "Door_Close";

    [Header("Stan Drzwi")]
    [Tooltip("Czy drzwi maj¹ zaczynaæ w stanie otwartym? Ustaw te¿ odpowiednio obiekt w scenie.")]
    public bool startOpen = false; // Ta zmienna kontroluje stan pocz¹tkowy
    private bool isOpen = false;

    [Header("Wymagania")]
    [Tooltip("Czy drzwi wymagaj¹ przedmiotu do otwarcia?")]
    public bool requiresItem = false;
    [Tooltip("Dane wymaganego przedmiotu (ScriptableObject typu InventoryItem).")]
    public InventoryItem requiredItemData; // U¿ywamy typu z Twojego ekwipunku
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

    private AudioSource audioSource;

    void Awake()
    {
        // Pobierz referencjê do AudioSource przy starcie
        audioSource = GetComponent<AudioSource>();

        // Upewnij siê, ¿e Animator jest przypisany, jeœli nie, spróbuj go znaleŸæ na tym samym obiekcie
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
                // SprawdŸ, czy nazwa stanu otwartego jest ustawiona
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu otwarcia (Open State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo¿na zainicjowaæ jako otwarte.", this);
                    isOpen = false; // Wróæ do stanu zamkniêtego, bo nie mo¿emy poprawnie zainicjowaæ
                    targetStateName = closedStateName; // Spróbuj ustawiæ stan zamkniêty
                    if (string.IsNullOrEmpty(targetStateName))
                    {
                        Debug.LogError($"Nazwa stanu zamkniêcia (Closed State Name) równie¿ nie jest ustawiona dla drzwi '{gameObject.name}'!", this);
                        return; // Zakoñcz, jeœli obie nazwy s¹ puste
                    }
                }
            }
            else // Jeœli startOpen jest false
            {
                targetStateName = closedStateName;
                // SprawdŸ, czy nazwa stanu zamkniêtego jest ustawiona
                if (string.IsNullOrEmpty(targetStateName))
                {
                    Debug.LogError($"Nazwa stanu zamkniêcia (Closed State Name) nie jest ustawiona w inspektorze dla drzwi '{gameObject.name}'! Nie mo¿na zainicjowaæ jako zamkniête.", this);
                    return; // Zakoñcz, jeœli nazwa stanu zamkniêtego jest pusta
                }
            }

            // Natychmiast przejdŸ do KOÑCA animacji w odpowiednim stanie (otwartym lub zamkniêtym)
            // Play(nazwaStanu, indeksWarstwy, znormalizowanyCzas)
            // znormalizowanyCzas = 1.0f oznacza koniec animacji w danym stanie.
            doorAnimator.Play(targetStateName, 0, 1.0f);
            Debug.Log($"Drzwi '{gameObject.name}' - Start(): Animator ustawiony na stan '{targetStateName}' (koniec animacji).");
        }
        else
        {
            Debug.LogError($"Drzwi '{gameObject.name}' nie maj¹ przypisanego komponentu Animator w metodzie Start!", this);
        }
    }

    // --- G£ÓWNA METODA WYWO£YWANA PRZEZ INTERAKCJÊ ---
    // Tê metodê podepniesz pod UnityEvent 'onInteract' w komponencie Interactable
    public void ToggleDoor()
    {
        // 1. SprawdŸ wymagania
        if (requiresItem)
        {
            // SprawdŸ, czy ekwipunek istnieje i czy gracz ma wybrany przedmiot
            if (CarouselInventory.Instance == null)
            {
                Debug.LogError("Nie znaleziono instancji CarouselInventory!", this);
                PlaySound(lockedSound); // Odtwórz dŸwiêk b³êdu/blokady
                // Mo¿esz te¿ wyœwietliæ UI z informacj¹ o b³êdzie
                return; // Przerwij dzia³anie
            }

            InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem(); // Potrzebujesz metody GetSelectedItem() w CarouselInventory

            // SprawdŸ, czy wybrany przedmiot pasuje do wymaganego
            if (selectedItem == null || selectedItem != requiredItemData) // Porównujemy referencje do ScriptableObject
            {
                Debug.Log($"Próba otwarcia drzwi '{gameObject.name}', ale brakuje wymaganego przedmiotu: {requiredItemData?.itemName ?? "Nieokreœlony"}");
                PlaySound(lockedSound);
                // Opcjonalnie: Wyœwietl graczowi wiadomoœæ (np. u¿ywaj¹c systemu UI)
                if (!string.IsNullOrEmpty(lockedMessage))
                {
                    // Tutaj kod do wyœwietlenia lockedMessage na ekranie
                    Debug.Log(lockedMessage); // Tymczasowy log
                }
                return; // Przerwij dzia³anie, drzwi siê nie otworz¹
            }

            // Przedmiot jest poprawny, kontynuuj. SprawdŸ, czy zu¿yæ.
            if (consumeItem)
            {
                // Potrzebujesz metody do usuwania przedmiotu w CarouselInventory
                CarouselInventory.Instance.RemoveCurrentItem();
                requiresItem = false;
                Debug.Log($"Zu¿yto przedmiot '{selectedItem.itemName}' do otwarcia drzwi '{gameObject.name}'.");
            }
        }

        // 2. Zmieñ stan drzwi i uruchom animacjê/dŸwiêk
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
            Debug.Log($"Drzwi '{gameObject.name}' zamkniête.");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}