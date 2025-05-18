using UnityEngine;
using System.Collections; // Dla Coroutine (opcjonalnie, np. dla opÛünieÅE

[RequireComponent(typeof(Interactable))] // Wymagaj komponentu Interactable
public class ChargingStation : MonoBehaviour
{
    [Header("Wymagane Przedmioty")]
    [Tooltip("Dane InventoryItem reprezentujπce telefon.")]
    public InventoryItem requiredPhoneItemData;
    [Tooltip("Dane InventoryItem reprezentujπce ≥adowarkÅE")]
    public InventoryItem requiredChargerItemData;

    [Header("Referencje Stacji")]
    [Tooltip("GameObject reprezentujπcy model telefonu na stacji (wy≥πczony domyúlnie).")]
    public GameObject phoneModelVisual;
    [Tooltip("GameObject reprezentujπcy model ≥adowarki na stacji (wy≥πczony domyúlnie).")]
    public GameObject chargerModelVisual;
    [Tooltip("Komponent Interactable tej stacji (zostanie znaleziony automatycznie).")]
    private Interactable interactable;

    [Header("Ustawienia £adowania")]
    [Tooltip("SzybkoúÊ ≥adowania baterii na sekundÅE")]
    public float chargingRate = 5.0f; // 5 jednostek baterii na sekundÅE

    [Header("Teksty Interakcji")]
    [Tooltip("Tekst, gdy stacja jest pusta i moøna pod≥πczyÅEtelefon.")]
    public string promptWhenEmpty = "[F] Pod≥πcz telefon i ≥adowark";
    [Tooltip("Format tekstu, gdy telefon siÅE≥aduje. {0} = aktualna bateria, {1} = max bateria.")]
    public string promptFormatWhenCharging = "[F] £adowanie ({0:0}/{1:0}). Odbierz"; // Formatowanie do liczb ca≥kowitych
    [Tooltip("Tekst, gdy wystπpi≥ b≥πd (np. brak przedmiotÛw).")]
    public string promptError = "Potrzebujesz telefonu i ≥adowarki";
    [Tooltip("Czas wyúwietlania tekstu b≥Ídu (w sekundach).")]
    public float errorPromptDuration = 1.5f;

    [Header("DüwiÍki (Opcjonalne)")]
    public AudioSource audioSource;
    public AudioClip connectSound;
    public AudioClip disconnectSound;
    public AudioClip errorSound;
    public AudioClip chargingLoopSound; // DüwiÍk pÍtli podczas ≥adowania

    // --- Stan WewnÍtrzny ---
    private bool isCharging = false;
    private PhoneSystem connectedPhoneInstance = null; // Referencja do ≥adowanego telefonu
    private Coroutine errorPromptCoroutine = null; // Do zarzπdzania tymczasowym tekstem b≥Ídu

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            Debug.LogError($"ChargingStation na '{gameObject.name}' nie znalaz≥ komponentu Interactable!", this);
            enabled = false;
            return;
        }

        if (requiredPhoneItemData == null || requiredChargerItemData == null)
        {
            Debug.LogError($"ChargingStation na '{gameObject.name}': Musisz przypisaÅE'requiredPhoneItemData' i 'requiredChargerItemData'!", this);
            enabled = false;
            return;
        }

        // SprÛbuj znaleüÊ AudioSource, jeúli nie przypisano
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Opcjonalnie dodaj AudioSource jeúli go nie ma
                // audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Upewnij siÅE øe wizualizacje sπ wy≥πczone na starcie
        UpdateVisuals();
        // Ustaw poczπtkowy tekst interakcji
        UpdateInteractionPrompt(promptWhenEmpty);
    }

    // G£”WNA METODA WYWO£YWANA PRZEZ Interactable's onInteract EVENT
    public void Interact()
    {
        if (isCharging)
        {
            TryRetrieveItems();
        }
        else
        {
            TryStartCharging();
        }
    }

    private void TryStartCharging()
    {
        if (CarouselInventory.Instance == null)
        {
            Debug.LogError("CarouselInventory.Instance nie znaleziono!");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        // 1. SprawdÅE czy gracz ma WYBRANY telefon
        InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();
        if (selectedItem != requiredPhoneItemData)
        {
            Debug.Log("Gracz nie ma wybranego telefonu.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        // 2. SprawdÅE czy gracz ma w ekwipunku ≥adowarkÅE(gdziekolwiek)
        if (!CarouselInventory.Instance.HasItem(requiredChargerItemData))
        {
            Debug.Log("Gracz nie ma ≥adowarki w ekwipunku.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        // 3. ZnajdÅEinstancjÅEPhoneSystem powiπzanπ z wybranym telefonem
        //    Zak≥adamy, øe aktywny PhoneSystem ma ustawiony 'isSelected = true'
        //    To jest uproszczenie - lepszym rozwiπzaniem by≥oby, gdyby Inventory zwraca≥o GameObject
        PhoneSystem phoneToCharge = FindSelectedPhoneSystemInstance();

        if (phoneToCharge == null)
        {
            Debug.LogError($"Nie moøna znaleüÊ aktywnej instancji PhoneSystem dla {requiredPhoneItemData.itemName}");
            ShowErrorPrompt(); // Moøna dodaÅEinny b≥πd
            PlaySound(errorSound);
            return;
        }

        // 4. Wszystko siÅEzgadza - rozpocznij ≥adowanie
        Debug.Log($"Rozpoczynanie ≥adowania telefonu: {phoneToCharge.gameObject.name}");

        // UsuÅEprzedmioty z ekwipunku
        bool phoneRemoved = CarouselInventory.Instance.RemoveItem(requiredPhoneItemData);
        bool chargerRemoved = CarouselInventory.Instance.RemoveItem(requiredChargerItemData);

        if (!phoneRemoved || !chargerRemoved)
        {
            Debug.LogError("B≥πd podczas usuwania przedmiotÛw z ekwipunku! Odwracanie operacji.");
            // SprÛbuj dodaÅEprzedmioty z powrotem, jeúli coÅEposz≥o nie tak
            if (phoneRemoved) CarouselInventory.Instance.AddItem(requiredPhoneItemData);
            if (chargerRemoved) CarouselInventory.Instance.AddItem(requiredChargerItemData);
            ShowErrorPrompt(); // Pokaø b≥πd
            PlaySound(errorSound);
            return;
        }

        // Ustaw stan stacji
        isCharging = true;
        connectedPhoneInstance = phoneToCharge; // Zapisz referencjÅEdo telefonu

        // Zaktualizuj wyglπd i interakcjÅE
        UpdateVisuals();
        UpdateInteractionPrompt(); // Zaktualizuje tekst na format ≥adowania
        PlaySound(connectSound);
        PlayChargingLoop(true); // Zacznij odtwarzaÅEpÍtlÅEdüwiÍkowπ ≥adowania
    }

    // Prosta metoda do znalezienia PhoneSystem, ktÛry jest aktualnie zaznaczony
    // UWAGA: To moøe byÅEnieefektywne w duøych scenach.
    private PhoneSystem FindSelectedPhoneSystemInstance()
    {
        PhoneSystem[] allPhoneSystems = FindObjectsOfType<PhoneSystem>(); // ZnajdÅEwszystkie w scenie
        foreach (PhoneSystem ps in allPhoneSystems)
        {
            // SprawdÅEczy dane siÅEzgadzajπ ORAZ czy ten telefon uwaøa siÅEza wybrany
            // (pole 'isSelected' jest prywatne, ale [SerializeField] - nie moøemy go odczytaÅEbezpoúrednio)
            // Zamiast tego, polegamy na tym, øe CarouselInventory wie, ktÛry *itemData* jest wybrany.
            // Musimy znaleüÊ PhoneSystem pasujπcy do *itemData* telefonu, ktÛry chcemy na≥adowaÅE
            if (ps.phoneItemData == requiredPhoneItemData)
            {
                // Dodatkowo, moøemy sprawdziÅE czy ten telefon *myúli*, øe jest wybrany, jeúli mamy dostÍp
                // if(ps.IsSelected()) // Gdyby by≥a taka publiczna metoda w PhoneSystem
                return ps;
            }
        }
        // Jeúli nie znaleziono, zwrÛÊ null
        // Moøe to oznaczaÅE øe obiekt telefonu nie istnieje w scenie lub nie ma przypisanego phoneItemData
        return null;
    }

    private void TryRetrieveItems()
    {
        if (!isCharging || connectedPhoneInstance == null)
        {
            Debug.LogWarning("PrÛba odzyskania przedmiotÛw, gdy stacja nie ≥aduje lub brak referencji do telefonu.");
            return; // Nic do zrobienia
        }

        Debug.Log($"Odzyskiwanie telefonu '{connectedPhoneInstance.gameObject.name}' i ≥adowarki.");

        // Dodaj przedmioty z powrotem do ekwipunku
        CarouselInventory.Instance.AddItem(requiredPhoneItemData);
        CarouselInventory.Instance.AddItem(requiredChargerItemData);

        // Zresetuj stan stacji
        isCharging = false;
        connectedPhoneInstance = null;

        // Zaktualizuj wyglπd i interakcjÅE
        UpdateVisuals();
        UpdateInteractionPrompt(promptWhenEmpty); // WrÛÊ do domyúlnego tekstu
        PlaySound(disconnectSound);
        PlayChargingLoop(false); // Zatrzymaj pÍtlÅEdüwiÍkowπ ≥adowania
    }

    void Update()
    {
        // Jeúli ≥adujemy i mamy referencjÅEdo telefonu
        if (isCharging && connectedPhoneInstance != null)
        {
            // £aduj bateriÅE
            connectedPhoneInstance.ChargeBattery(chargingRate * Time.deltaTime);

            // Aktualizuj tekst interakcji, aby pokazaÅEpostÍp
            // Robimy to tutaj, a nie w ChargeBattery, bo tylko stacja wie, jak ma wyglπdaÅEjej prompt
            UpdateInteractionPrompt();

            // Opcjonalnie: Zatrzymaj ≥adowanie, gdy bateria jest pe≥na
            // if (connectedPhoneInstance.GetCurrentBattery() >= connectedPhoneInstance.GetMaxBattery())
            // {
            //     // Moøna tu np. zmieniÅEdüwiÍk lub lekko zmodyfikowaÅEprompt
            // }
        }
    }

    private void UpdateVisuals()
    {
        if (phoneModelVisual != null)
        {
            phoneModelVisual.SetActive(isCharging);
        }
        if (chargerModelVisual != null)
        {
            chargerModelVisual.SetActive(isCharging);
        }
    }

    // Aktualizuje tekst w komponencie Interactable
    private void UpdateInteractionPrompt(string customText = null)
    {
        if (interactable == null) return;

        // Jeúli podano konkretny tekst (np. b≥πd lub pusty), uøyj go
        if (!string.IsNullOrEmpty(customText))
        {
            interactable.interactionPrompt = customText;
        }
        // W przeciwnym razie, wybierz tekst na podstawie stanu
        else
        {
            if (isCharging && connectedPhoneInstance != null)
            {
                // Formatuj tekst ≥adowania z aktualnymi wartoúciami baterii
                float current = connectedPhoneInstance.GetCurrentBattery();
                float max = connectedPhoneInstance.GetMaxBattery();
                interactable.interactionPrompt = string.Format(promptFormatWhenCharging, current, max);
            }
            else
            {
                // Ustaw domyúlny tekst dla pustej stacji
                interactable.interactionPrompt = promptWhenEmpty;
            }
        }
    }

    // Wyúwietla tymczasowy tekst b≥Ídu
    private void ShowErrorPrompt()
    {
        if (errorPromptCoroutine != null)
        {
            StopCoroutine(errorPromptCoroutine); // Zatrzymaj poprzedni, jeúli dzia≥a≥
        }
        errorPromptCoroutine = StartCoroutine(ErrorPromptRoutine());
    }

    IEnumerator ErrorPromptRoutine()
    {
        string originalPrompt = interactable.interactionPrompt; // ZapamiÍtaj obecny tekst (prawdopodobnie promptWhenEmpty)
        UpdateInteractionPrompt(promptError); // Pokaø b≥πd
        yield return new WaitForSeconds(errorPromptDuration); // Poczekaj
        // PrzywrÛÊ tekst, ktÛry by≥ PRZED b≥Ídem, ale tylko jeúli stan siÅEnie zmieni≥ (nadal nie ≥adujemy)
        if (!isCharging)
        {
            UpdateInteractionPrompt(originalPrompt);
        }
        errorPromptCoroutine = null; // ZakoÒczono
    }


    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayChargingLoop(bool play)
    {
        if (audioSource != null && chargingLoopSound != null)
        {
            if (play && !audioSource.isPlaying)
            { // OdtwÛrz tylko jeúli nie gra juø czegoÅEinnego (np. connect sound)
                audioSource.clip = chargingLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!play && audioSource.clip == chargingLoopSound)
            { // Zatrzymaj tylko jeúli aktualnie gra pÍtla ≥adowania
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
            }
        }
    }
}