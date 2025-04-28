using UnityEngine;
using System.Collections; // Dla Coroutine (opcjonalnie, np. dla opóŸnieñ)

[RequireComponent(typeof(Interactable))] // Wymagaj komponentu Interactable
public class ChargingStation : MonoBehaviour
{
    [Header("Wymagane Przedmioty")]
    [Tooltip("Dane InventoryItem reprezentuj¹ce telefon.")]
    public InventoryItem requiredPhoneItemData;
    [Tooltip("Dane InventoryItem reprezentuj¹ce ³adowarkê.")]
    public InventoryItem requiredChargerItemData;

    [Header("Referencje Stacji")]
    [Tooltip("GameObject reprezentuj¹cy model telefonu na stacji (wy³¹czony domyœlnie).")]
    public GameObject phoneModelVisual;
    [Tooltip("GameObject reprezentuj¹cy model ³adowarki na stacji (wy³¹czony domyœlnie).")]
    public GameObject chargerModelVisual;
    [Tooltip("Komponent Interactable tej stacji (zostanie znaleziony automatycznie).")]
    private Interactable interactable;

    [Header("Ustawienia £adowania")]
    [Tooltip("Szybkoœæ ³adowania baterii na sekundê.")]
    public float chargingRate = 5.0f; // 5 jednostek baterii na sekundê

    [Header("Teksty Interakcji")]
    [Tooltip("Tekst, gdy stacja jest pusta i mo¿na pod³¹czyæ telefon.")]
    public string promptWhenEmpty = "[F] Pod³¹cz telefon i ³adowarkê";
    [Tooltip("Format tekstu, gdy telefon siê ³aduje. {0} = aktualna bateria, {1} = max bateria.")]
    public string promptFormatWhenCharging = "[F] £adowanie ({0:0}/{1:0}). Odbierz"; // Formatowanie do liczb ca³kowitych
    [Tooltip("Tekst, gdy wyst¹pi³ b³¹d (np. brak przedmiotów).")]
    public string promptError = "Potrzebujesz telefonu i ³adowarki";
    [Tooltip("Czas wyœwietlania tekstu b³êdu (w sekundach).")]
    public float errorPromptDuration = 1.5f;

    [Header("DŸwiêki (Opcjonalne)")]
    public AudioSource audioSource;
    public AudioClip connectSound;
    public AudioClip disconnectSound;
    public AudioClip errorSound;
    public AudioClip chargingLoopSound; // DŸwiêk pêtli podczas ³adowania

    // --- Stan Wewnêtrzny ---
    private bool isCharging = false;
    private PhoneSystem connectedPhoneInstance = null; // Referencja do ³adowanego telefonu
    private Coroutine errorPromptCoroutine = null; // Do zarz¹dzania tymczasowym tekstem b³êdu

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            Debug.LogError($"ChargingStation na '{gameObject.name}' nie znalaz³ komponentu Interactable!", this);
            enabled = false;
            return;
        }

        if (requiredPhoneItemData == null || requiredChargerItemData == null)
        {
            Debug.LogError($"ChargingStation na '{gameObject.name}': Musisz przypisaæ 'requiredPhoneItemData' i 'requiredChargerItemData'!", this);
            enabled = false;
            return;
        }

        // Spróbuj znaleŸæ AudioSource, jeœli nie przypisano
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Opcjonalnie dodaj AudioSource jeœli go nie ma
                // audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Upewnij siê, ¿e wizualizacje s¹ wy³¹czone na starcie
        UpdateVisuals();
        // Ustaw pocz¹tkowy tekst interakcji
        UpdateInteractionPrompt(promptWhenEmpty);
    }

    // G£ÓWNA METODA WYWO£YWANA PRZEZ Interactable's onInteract EVENT
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

        // 1. SprawdŸ, czy gracz ma WYBRANY telefon
        InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();
        if (selectedItem != requiredPhoneItemData)
        {
            Debug.Log("Gracz nie ma wybranego telefonu.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        // 2. SprawdŸ, czy gracz ma w ekwipunku ³adowarkê (gdziekolwiek)
        if (!CarouselInventory.Instance.HasItem(requiredChargerItemData))
        {
            Debug.Log("Gracz nie ma ³adowarki w ekwipunku.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        // 3. ZnajdŸ instancjê PhoneSystem powi¹zan¹ z wybranym telefonem
        //    Zak³adamy, ¿e aktywny PhoneSystem ma ustawiony 'isSelected = true'
        //    To jest uproszczenie - lepszym rozwi¹zaniem by³oby, gdyby Inventory zwraca³o GameObject
        PhoneSystem phoneToCharge = FindSelectedPhoneSystemInstance();

        if (phoneToCharge == null)
        {
            Debug.LogError($"Nie mo¿na znaleŸæ aktywnej instancji PhoneSystem dla {requiredPhoneItemData.itemName}");
            ShowErrorPrompt(); // Mo¿na dodaæ inny b³¹d
            PlaySound(errorSound);
            return;
        }

        // 4. Wszystko siê zgadza - rozpocznij ³adowanie
        Debug.Log($"Rozpoczynanie ³adowania telefonu: {phoneToCharge.gameObject.name}");

        // Usuñ przedmioty z ekwipunku
        bool phoneRemoved = CarouselInventory.Instance.RemoveItem(requiredPhoneItemData);
        bool chargerRemoved = CarouselInventory.Instance.RemoveItem(requiredChargerItemData);

        if (!phoneRemoved || !chargerRemoved)
        {
            Debug.LogError("B³¹d podczas usuwania przedmiotów z ekwipunku! Odwracanie operacji.");
            // Spróbuj dodaæ przedmioty z powrotem, jeœli coœ posz³o nie tak
            if (phoneRemoved) CarouselInventory.Instance.AddItem(requiredPhoneItemData);
            if (chargerRemoved) CarouselInventory.Instance.AddItem(requiredChargerItemData);
            ShowErrorPrompt(); // Poka¿ b³¹d
            PlaySound(errorSound);
            return;
        }

        // Ustaw stan stacji
        isCharging = true;
        connectedPhoneInstance = phoneToCharge; // Zapisz referencjê do telefonu

        // Zaktualizuj wygl¹d i interakcjê
        UpdateVisuals();
        UpdateInteractionPrompt(); // Zaktualizuje tekst na format ³adowania
        PlaySound(connectSound);
        PlayChargingLoop(true); // Zacznij odtwarzaæ pêtlê dŸwiêkow¹ ³adowania
    }

    // Prosta metoda do znalezienia PhoneSystem, który jest aktualnie zaznaczony
    // UWAGA: To mo¿e byæ nieefektywne w du¿ych scenach.
    private PhoneSystem FindSelectedPhoneSystemInstance()
    {
        PhoneSystem[] allPhoneSystems = FindObjectsOfType<PhoneSystem>(); // ZnajdŸ wszystkie w scenie
        foreach (PhoneSystem ps in allPhoneSystems)
        {
            // SprawdŸ czy dane siê zgadzaj¹ ORAZ czy ten telefon uwa¿a siê za wybrany
            // (pole 'isSelected' jest prywatne, ale [SerializeField] - nie mo¿emy go odczytaæ bezpoœrednio)
            // Zamiast tego, polegamy na tym, ¿e CarouselInventory wie, który *itemData* jest wybrany.
            // Musimy znaleŸæ PhoneSystem pasuj¹cy do *itemData* telefonu, który chcemy na³adowaæ.
            if (ps.phoneItemData == requiredPhoneItemData)
            {
                // Dodatkowo, mo¿emy sprawdziæ, czy ten telefon *myœli*, ¿e jest wybrany, jeœli mamy dostêp
                // if(ps.IsSelected()) // Gdyby by³a taka publiczna metoda w PhoneSystem
                return ps;
            }
        }
        // Jeœli nie znaleziono, zwróæ null
        // Mo¿e to oznaczaæ, ¿e obiekt telefonu nie istnieje w scenie lub nie ma przypisanego phoneItemData
        return null;
    }

    private void TryRetrieveItems()
    {
        if (!isCharging || connectedPhoneInstance == null)
        {
            Debug.LogWarning("Próba odzyskania przedmiotów, gdy stacja nie ³aduje lub brak referencji do telefonu.");
            return; // Nic do zrobienia
        }

        Debug.Log($"Odzyskiwanie telefonu '{connectedPhoneInstance.gameObject.name}' i ³adowarki.");

        // Dodaj przedmioty z powrotem do ekwipunku
        CarouselInventory.Instance.AddItem(requiredPhoneItemData);
        CarouselInventory.Instance.AddItem(requiredChargerItemData);

        // Zresetuj stan stacji
        isCharging = false;
        connectedPhoneInstance = null;

        // Zaktualizuj wygl¹d i interakcjê
        UpdateVisuals();
        UpdateInteractionPrompt(promptWhenEmpty); // Wróæ do domyœlnego tekstu
        PlaySound(disconnectSound);
        PlayChargingLoop(false); // Zatrzymaj pêtlê dŸwiêkow¹ ³adowania
    }

    void Update()
    {
        // Jeœli ³adujemy i mamy referencjê do telefonu
        if (isCharging && connectedPhoneInstance != null)
        {
            // £aduj bateriê
            connectedPhoneInstance.ChargeBattery(chargingRate * Time.deltaTime);

            // Aktualizuj tekst interakcji, aby pokazaæ postêp
            // Robimy to tutaj, a nie w ChargeBattery, bo tylko stacja wie, jak ma wygl¹daæ jej prompt
            UpdateInteractionPrompt();

            // Opcjonalnie: Zatrzymaj ³adowanie, gdy bateria jest pe³na
            // if (connectedPhoneInstance.GetCurrentBattery() >= connectedPhoneInstance.GetMaxBattery())
            // {
            //     // Mo¿na tu np. zmieniæ dŸwiêk lub lekko zmodyfikowaæ prompt
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

        // Jeœli podano konkretny tekst (np. b³¹d lub pusty), u¿yj go
        if (!string.IsNullOrEmpty(customText))
        {
            interactable.interactionPrompt = customText;
        }
        // W przeciwnym razie, wybierz tekst na podstawie stanu
        else
        {
            if (isCharging && connectedPhoneInstance != null)
            {
                // Formatuj tekst ³adowania z aktualnymi wartoœciami baterii
                float current = connectedPhoneInstance.GetCurrentBattery();
                float max = connectedPhoneInstance.GetMaxBattery();
                interactable.interactionPrompt = string.Format(promptFormatWhenCharging, current, max);
            }
            else
            {
                // Ustaw domyœlny tekst dla pustej stacji
                interactable.interactionPrompt = promptWhenEmpty;
            }
        }
    }

    // Wyœwietla tymczasowy tekst b³êdu
    private void ShowErrorPrompt()
    {
        if (errorPromptCoroutine != null)
        {
            StopCoroutine(errorPromptCoroutine); // Zatrzymaj poprzedni, jeœli dzia³a³
        }
        errorPromptCoroutine = StartCoroutine(ErrorPromptRoutine());
    }

    IEnumerator ErrorPromptRoutine()
    {
        string originalPrompt = interactable.interactionPrompt; // Zapamiêtaj obecny tekst (prawdopodobnie promptWhenEmpty)
        UpdateInteractionPrompt(promptError); // Poka¿ b³¹d
        yield return new WaitForSeconds(errorPromptDuration); // Poczekaj
        // Przywróæ tekst, który by³ PRZED b³êdem, ale tylko jeœli stan siê nie zmieni³ (nadal nie ³adujemy)
        if (!isCharging)
        {
            UpdateInteractionPrompt(originalPrompt);
        }
        errorPromptCoroutine = null; // Zakoñczono
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
            { // Odtwórz tylko jeœli nie gra ju¿ czegoœ innego (np. connect sound)
                audioSource.clip = chargingLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!play && audioSource.clip == chargingLoopSound)
            { // Zatrzymaj tylko jeœli aktualnie gra pêtla ³adowania
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
            }
        }
    }
}