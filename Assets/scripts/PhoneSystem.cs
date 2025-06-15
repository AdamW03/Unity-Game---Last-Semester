using UnityEngine;
using UnityEngine.Events;

public class PhoneSystem : MonoBehaviour
{
    public UnityEvent onPhoneSelected;
    public UnityEvent onPhoneDeselected;

    [Header("Referencje")]
    public GameObject phoneLightObject;
    [Header("Item Data")]
    public InventoryItem phoneItemData;

    [Header("Ustawienia Sterowania")]
    public KeyCode toggleLightKey = KeyCode.F;

    [Header("Ustawienia Baterii")]
    public float maxBattery = 100.0f;
    [Range(0f, 100f)]
    public float currentBattery;
    public float batteryDrainRate = 1.0f;
    public bool turnOffOnEmpty = true;

    [Header("DŸwiêki (Opcjonalne)")]
    // Usuniêto: public AudioSource audioSource; // Ju¿ nie potrzebujemy bezpoœredniej referencji do AudioSource tutaj
    public AudioClip lightOnSound;
    public AudioClip lightOffSound;
    public AudioClip batteryDeadSound;

    [Header("Stan (Tylko do odczytu)")]
    [SerializeField]
    private bool isLightOn = false;
    [SerializeField]
    private bool isSelected = false;

    [System.Serializable]
    public class BatteryChangeEvent : UnityEvent<float, float> { }
    public BatteryChangeEvent onBatteryChanged;

    private bool playerHasPhone = false;
    void Awake()
    {
        if (phoneLightObject == null)
        {
            Debug.LogError($"PhoneSystem na '{gameObject.name}': Nie przypisano obiektu 'phoneLightObject'!", this);
            this.enabled = false;
            return;
        }

        // Usuniêto: if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (phoneItemData == null)
        {
            Debug.LogError($"PhoneSystem na '{gameObject.name}': Nie przypisano 'Phone Item Data'!", this);
        }

        isLightOn = false;
        phoneLightObject.SetActive(false);
    }

    void Start()
    {
        if (currentBattery <= 0 && maxBattery > 0)
        {
            currentBattery = maxBattery;
        }
        else
        {
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }
        onBatteryChanged?.Invoke(currentBattery, maxBattery);
    }

    void Update()
    {
        if (!playerHasPhone)
        {
            return; 
        }

        bool currentlySelected = false;
        if (CarouselInventory.Instance != null)
        {
            if (this.phoneItemData != null)
            {
                InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();
                currentlySelected = (selectedItem != null && selectedItem == this.phoneItemData);
            }
        }

        if (currentlySelected != isSelected)
        {
            isSelected = currentlySelected;
            if (isSelected)
            {
                // Ta czêœæ wykona siê, gdy gracz WYBIERZE telefon
                onPhoneSelected?.Invoke(); // Poka¿ UI
            }
            else
            {
                // Ta czêœæ wykona siê, gdy gracz ODZNACZY telefon (wybierze inny przedmiot)
                onPhoneDeselected?.Invoke(); // Ukryj UI
                if (isLightOn)
                {
                    TurnLightOff(); // Jeœli latarka by³a w³¹czona, wy³¹cz j¹
                }
            }
        }

        if (isSelected && Input.GetKeyDown(toggleLightKey))
        {
            ToggleLight();
        }

        if (isLightOn)
        {
            if (currentBattery > 0)
            {
                float previousBattery = currentBattery;
                currentBattery -= batteryDrainRate * Time.deltaTime;
                currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

                if (Mathf.Approximately(currentBattery, previousBattery) == false)
                {
                    onBatteryChanged?.Invoke(currentBattery, maxBattery);
                }

                if (currentBattery <= 0 && turnOffOnEmpty)
                {
                    Debug.Log($"Telefon '{gameObject.name}': Bateria wyczerpana, wy³¹czam œwiat³o.");
                    TurnLightOff(); // DŸwiêk wy³¹czenia zostanie odtworzony
                    PlaySoundWithFXManager(batteryDeadSound, "roz³adowania baterii (batteryDeadSound)"); // DŸwiêk roz³adowania
                }
            }
            else if (currentBattery <= 0 && isLightOn)
            {
                TurnLightOff();
            }
        }
    }

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
            PlaySoundWithFXManager(batteryDeadSound, "pustej baterii przy próbie w³¹czenia (batteryDeadSound)");
        }
    }

    private void TurnLightOn()
    {
        if (!isLightOn)
        {
            isLightOn = true;
            phoneLightObject.SetActive(true);
            PlaySoundWithFXManager(lightOnSound, "w³¹czenia latarki (lightOnSound)"); // <-- ZMIANA
            Debug.Log($"Telefon '{gameObject.name}': Œwiat³o w³¹czone.");
        }
    }

    private void TurnLightOff()
    {
        if (isLightOn)
        {
            isLightOn = false;
            phoneLightObject.SetActive(false);
            PlaySoundWithFXManager(lightOffSound, "wy³¹czenia latarki (lightOffSound)"); // <-- ZMIANA
            Debug.Log($"Telefon '{gameObject.name}': Œwiat³o wy³¹czone.");
        }
    }

    // Zmodyfikowana metoda PlaySound na PlaySoundWithFXManager
    private void PlaySoundWithFXManager(AudioClip clip, string soundDescriptionForLog)
    {
        if (SoundFXManager.Instance != null && clip != null)
        {
            // Odtwarzamy dŸwiêk w pozycji tego obiektu telefonu
            SoundFXManager.Instance.PlaySoundFXClip(clip, transform, 1f);
        }
        else if (clip == null)
        {
            // Ten log mo¿e byæ zbyt czêsty, jeœli nie przypiszesz dŸwiêku, wiêc mo¿na go zakomentowaæ
            // Debug.LogWarning($"PhoneSystem: Brak przypisanego dŸwiêku {soundDescriptionForLog} dla telefonu '{gameObject.name}'.");
        }
        else if (SoundFXManager.Instance == null)
        {
            Debug.LogWarning($"PhoneSystem: SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku {soundDescriptionForLog} dla telefonu '{gameObject.name}'.");
        }
    }

    public void ChargeBattery(float amount)
    {
        if (amount <= 0) return;
        float previousBattery = currentBattery;
        currentBattery += amount;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        Debug.Log($"Telefon '{gameObject.name}': Na³adowano bateriê o {amount}. Aktualny poziom: {currentBattery}/{maxBattery}");
        if (Mathf.Approximately(currentBattery, previousBattery) == false)
        {
            onBatteryChanged?.Invoke(currentBattery, maxBattery);
        }
    }

    public void SetBatteryLevel(float level)
    {
        currentBattery = Mathf.Clamp(level, 0f, maxBattery);
        onBatteryChanged?.Invoke(currentBattery, maxBattery);
        if (currentBattery <= 0 && isLightOn)
        {
            TurnLightOff();
        }
    }

    public void NotifyPhonePickedUp()
    {
        playerHasPhone = true;
        Debug.Log("PhoneSystem zosta³ poinformowany, ¿e gracz podniós³ telefon. System jest teraz aktywny.");
    }

    public float GetCurrentBattery() => currentBattery;
    public float GetMaxBattery() => maxBattery;
    public bool IsLightOn() => isLightOn;
}