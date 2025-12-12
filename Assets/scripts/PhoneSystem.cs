using UnityEngine;
using UnityEngine.Events; // Potrzebne dla UnityEvent (opcjonalne, np. do UI)

public class PhoneSystem : MonoBehaviour
{
    [Header("Referencje")]
    [Tooltip("Obiekt GameObject reprezentuj¹cy œwiat³o latarki w telefonie.")]
    public GameObject phoneLightObject;
    [Header("Item Data")]
    [Tooltip("Dane InventoryItem reprezentuj¹ce ten konkretny telefon. MUSI BYÆ PRZYPISANE!")]
    public InventoryItem phoneItemData;

    [Header("Ustawienia Sterowania")]
    [Tooltip("Klawisz u¿ywany do w³¹czania/wy³¹czania latarki.")]
    public KeyCode toggleLightKey = KeyCode.F; // Mo¿esz zmieniæ na inny klawisz

    [Header("Ustawienia Baterii")]
    [Tooltip("Maksymalny poziom na³adowania baterii.")]
    public float maxBattery = 100.0f;
    [Tooltip("Aktualny poziom na³adowania baterii.")]
    [Range(0f, 100f)] // Ogranicza suwak w inspektorze
    public float currentBattery;
    [Tooltip("Szybkoœæ zu¿ycia baterii na sekundê, gdy latarka jest w³¹czona.")]
    public float batteryDrainRate = 1.0f; // 1 jednostka baterii na sekundê
    [Tooltip("Czy latarka ma siê automatycznie wy³¹czyæ, gdy bateria spadnie do zera?")]
    public bool turnOffOnEmpty = true;

    [Header("DŸwiêki (Opcjonalne)")]
    public AudioSource audioSource; // Przypisz komponent AudioSource (mo¿e byæ na tym samym obiekcie)
    public AudioClip lightOnSound;
    public AudioClip lightOffSound;
    public AudioClip batteryDeadSound; // DŸwiêk próby w³¹czenia przy pustej baterii

    [Header("Stan (Tylko do odczytu)")]
    [SerializeField] // Pokazuje prywatne pole w inspektorze (do debugowania)
    private bool isLightOn = false;
    [SerializeField]
    private bool isSelected = false; // Czy ten telefon jest aktualnie wybranym przedmiotem?

    // Opcjonalne zdarzenia, np. do aktualizacji UI z poziomem baterii
    [System.Serializable]
    public class BatteryChangeEvent : UnityEvent<float, float> { } // Przekazuje current, max
    public BatteryChangeEvent onBatteryChanged;

    void Awake()
    {
        // Upewnij siê, ¿e œwiat³o jest przypisane
        if (phoneLightObject == null)
        {
            Debug.LogError($"PhoneSystem na '{gameObject.name}': Nie przypisano obiektu 'phoneLightObject'!", this);
            this.enabled = false; // Wy³¹cz skrypt, jeœli brakuje kluczowej referencji
            return;
        }

        // Spróbuj znaleŸæ AudioSource, jeœli nie przypisano
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Dodatkowe sprawdzenie w Awake (opcjonalne, ale dobre)
        if (phoneItemData == null)
        {
            Debug.LogError($"PhoneSystem na '{gameObject.name}': Nie przypisano 'Phone Item Data'! Telefon nie bêdzie poprawnie rozpoznawany w ekwipunku.", this);
        }

        // Pocz¹tkowy stan
        isLightOn = false;
        phoneLightObject.SetActive(false); // Upewnij siê, ¿e œwiat³o jest wy³¹czone na starcie
    }

    void Start()
    {
        // Ustaw pocz¹tkowy poziom baterii (mo¿na to póŸniej ³adowaæ z zapisu gry)
        // Jeœli currentBattery nie by³o ustawione w inspektorze, ustaw na max
        if (currentBattery <= 0 && maxBattery > 0) // SprawdŸ, czy nie ustawiono w inspektorze sensownej wartoœci
        {
            currentBattery = maxBattery;
        }
        else
        {
            // Upewnij siê, ¿e startowa wartoœæ jest w zakresie
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }

        // Wywo³aj zdarzenie zmiany baterii na starcie, aby UI siê zaktualizowa³o
        onBatteryChanged?.Invoke(currentBattery, maxBattery);
    }

    void Update()
    {
        bool currentlySelected = false;
        // --- Krok 1: SprawdŸ, czy ten telefon jest aktualnie wybrany ---
        // To jest KLUCZOWA czêœæ integracji z Twoim systemem ekwipunku.
        // Musisz dostosowaæ tê liniê do tego, jak Twój ekwipunek dzia³a.
        // Zak³adam, ¿e masz singleton `CarouselInventory` z metod¹ `GetSelectedItemGameObject()`
        // która zwraca GameObject aktualnie trzymanego przedmiotu.
        if (CarouselInventory.Instance != null) // Upewnij siê, ¿e ekwipunek istnieje
        {
            if (this.phoneItemData == null)
            {
                // Jeœli nie ma danych, ten telefon NIE MO¯E byæ poprawnie zidentyfikowany.
                // Loguj b³¹d tylko raz lub rzadziej, aby nie spamowaæ konsoli.
                if (isSelected) // Jeœli *by³* wybrany, ale teraz straci³ dane
                {
                    Debug.LogError($"Telefon '{gameObject.name}' by³ zaznaczony, ale teraz brakuje mu przypisanego 'phoneItemData'! Odznaczam.", this);
                }
                // Stan pozostaje `currentlySelected = false;`
            }
            else
            {
                // Pobierz aktualnie wybrany przedmiot (jego dane) z ekwipunku
                InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();

                // Porównaj DANE wybranego przedmiotu z DANYMI tego telefonu
                // Porównujemy referencje do ScriptableObject
                currentlySelected = (selectedItem != null && selectedItem == this.phoneItemData);
            }
        }
        else
        {
            // Ekwipunek nie istnieje, wiêc nic nie mo¿e byæ wybrane.
            // Stan pozostaje `currentlySelected = false;`
            // Debug.LogWarning("CarouselInventory.Instance nie znaleziono w Update!");
        }

        // --- Krok 1b: Reaguj na zmianê stanu wybrania ---
        if (currentlySelected != isSelected)
        {
            isSelected = currentlySelected; // Zaktualizuj wewnêtrzny stan

            if (!isSelected && isLightOn)
            {
                // Jeœli w³aœnie zosta³ odznaczony, a œwiat³o by³o w³¹czone, wy³¹cz je.
                // Pobierz nazwy dla logu w bezpieczny sposób
                string selectedName = CarouselInventory.Instance?.GetSelectedItem()?.itemName ?? "null/none"; // Bezpieczne pobranie nazwy
                string myName = this.phoneItemData?.itemName ?? "BRAK DANYCH"; // Bezpieczne pobranie nazwy
                Debug.Log($"Telefon '{gameObject.name}' ({myName}) zosta³ ODZNACZONY (aktualnie wybrano: '{selectedName}'). Wy³¹czam œwiat³o.");
                TurnLightOff();
            }
            else if (isSelected)
            {
                // Loguj tylko jeœli phoneItemData istnieje, aby unikn¹æ b³êdu NullReference
                string myName = this.phoneItemData?.itemName ?? "NIEZNANE DANE";
                Debug.Log($"Telefon '{gameObject.name}' ({myName}) zosta³ ZAZNACZONY.");
            }
        }


        // --- Krok 2: Obs³uga Inputu (tylko jeœli telefon jest wybrany) ---
        if (isSelected && Input.GetKeyDown(toggleLightKey))
        {
            ToggleLight();
        }

        // --- Krok 3: Zu¿ycie baterii (tylko jeœli œwiat³o jest w³¹czone) ---
        if (isLightOn)
        {
            if (currentBattery > 0)
            {
                float previousBattery = currentBattery;
                currentBattery -= batteryDrainRate * Time.deltaTime;
                currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery); // Upewnij siê, ¿e nie spadnie poni¿ej 0

                // Jeœli bateria siê zmieni³a, wywo³aj zdarzenie (np. dla UI)
                if (Mathf.Approximately(currentBattery, previousBattery) == false)
                {
                    onBatteryChanged?.Invoke(currentBattery, maxBattery);
                }


                // SprawdŸ, czy bateria siê wyczerpa³a
                if (currentBattery <= 0 && turnOffOnEmpty)
                {
                    Debug.Log($"Telefon '{gameObject.name}': Bateria wyczerpana, wy³¹czam œwiat³o.");
                    TurnLightOff();
                    PlaySound(batteryDeadSound); // Opcjonalny dŸwiêk roz³adowania
                }
            }
            // Jeœli bateria jest ju¿ na 0, a œwiat³o jakoœ jest w³¹czone (nie powinno siê zdarzyæ przy turnOffOnEmpty=true)
            else if (currentBattery <= 0 && isLightOn)
            {
                TurnLightOff(); // Wymuœ wy³¹czenie
            }
        }
    }

    // --- Metody Kontroluj¹ce Œwiat³o ---

    public void ToggleLight()
    {
        if (isLightOn)
        {
            TurnLightOff();
        }
        else
        {
            TryTurnLightOn();
        }
    }

    public void TryTurnLightOn()
    {
        if (currentBattery > 0)
        {
            TurnLightOn();
        }
        else
        {
            Debug.Log($"Telefon '{gameObject.name}': Próba w³¹czenia œwiat³a, ale bateria jest pusta!");
            PlaySound(batteryDeadSound);
            // Mo¿esz dodaæ tu inny feedback dla gracza
        }
    }

    private void TurnLightOn()
    {
        if (!isLightOn) // Tylko jeœli jest wy³¹czone
        {
            isLightOn = true;
            phoneLightObject.SetActive(true);
            PlaySound(lightOnSound);
            Debug.Log($"Telefon '{gameObject.name}': Œwiat³o w³¹czone.");
            // Nie musimy tutaj wywo³ywaæ onBatteryChanged, bo Update() to zrobi przy zu¿yciu
        }
    }

    private void TurnLightOff()
    {
        if (isLightOn) // Tylko jeœli jest w³¹czone
        {
            isLightOn = false;
            phoneLightObject.SetActive(false);
            PlaySound(lightOffSound);
            Debug.Log($"Telefon '{gameObject.name}': Œwiat³o wy³¹czone.");
            // Nie musimy tutaj wywo³ywaæ onBatteryChanged, bo stan baterii siê nie zmienia przy wy³¹czaniu
        }
    }

    // --- Metody Pomocnicze ---

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- Publiczne Metody (np. do ³adowania baterii z innego skryptu) ---

    public void ChargeBattery(float amount)
    {
        if (amount <= 0) return;

        float previousBattery = currentBattery;
        currentBattery += amount;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery); // Ogranicz do max
        Debug.Log($"Telefon '{gameObject.name}': Na³adowano bateriê o {amount}. Aktualny poziom: {currentBattery}/{maxBattery}");

        // Jeœli bateria siê zmieni³a, wywo³aj zdarzenie
        if (Mathf.Approximately(currentBattery, previousBattery) == false)
        {
            onBatteryChanged?.Invoke(currentBattery, maxBattery);
        }
    }

    // Metoda do ustawienia poziomu baterii (np. przy ³adowaniu gry)
    public void SetBatteryLevel(float level)
    {
        currentBattery = Mathf.Clamp(level, 0f, maxBattery);
        onBatteryChanged?.Invoke(currentBattery, maxBattery);
        // Jeœli po ustawieniu bateria jest 0, a œwiat³o by³o w³¹czone, wy³¹cz je
        if (currentBattery <= 0 && isLightOn)
        {
            TurnLightOff();
        }
    }

    // Metoda do pobrania aktualnego stanu na³adowania (np. dla UI)
    public float GetCurrentBattery() => currentBattery;
    public float GetMaxBattery() => maxBattery;
    public bool IsLightOn() => isLightOn;
}