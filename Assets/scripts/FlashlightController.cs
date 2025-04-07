using UnityEngine;
using UnityEngine.UI; // Jeœli bêdziesz chcia³ dodaæ UI baterii

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight; // Przypisz komponent Light z tego samego obiektu w Inspektorze

    [Header("Runtime State")]
    [ReadOnly] public float currentBattery; // Aktualny stan baterii (tylko do odczytu w inspektorze dla debugowania)
    public float maxBattery { get; private set; } // Ustawiane przy inicjalizacji
    private float batteryDepletionRate; // Ustawiane przy inicjalizacji
    private bool isFlashlightOn = false;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F; // Klawisz do w³¹czania/wy³¹czania latarki

    // --- Opcjonalne: UI Baterii ---
    // public Slider batterySlider; // Przypisz suwak UI
    // public GameObject batteryUiContainer; // Panel zawieraj¹cy UI baterii

    void Awake()
    {
        // Upewnij siê, ¿e œwiat³o jest przypisane i wy³¹czone na starcie
        if (flashlightLight == null)
        {
            Debug.LogError("FlashlightController: Komponent Light nie jest przypisany!", this);
            enabled = false; // Wy³¹cz skrypt, jeœli brakuje œwiat³a
            return;
        }
        flashlightLight.enabled = false;
        isFlashlightOn = false;

        // Ukryj UI baterii na starcie (jeœli istnieje)
        // if (batteryUiContainer != null) batteryUiContainer.SetActive(false);
    }

    // Metoda do inicjalizacji latarki danymi z ScriptableObject
    public void Initialize(FlashlightItem itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("Initialize FlashlightController called with null itemData!", this);
            return;
        }
        maxBattery = itemData.maxBattery;
        batteryDepletionRate = itemData.batteryDepletionRate;
        currentBattery = maxBattery; // Start z pe³n¹ bateri¹
        Debug.Log($"Flashlight Initialized: MaxBattery={maxBattery}, DepletionRate={batteryDepletionRate}");

        // Poka¿ UI baterii (jeœli istnieje)
        // if (batteryUiContainer != null) batteryUiContainer.SetActive(true);
        UpdateBatteryUI();
    }


    void Update()
    {
        // SprawdŸ, czy mo¿na kontrolowaæ latarkê (np. tylko gdy jest aktywna)
        if (gameObject.activeInHierarchy) // SprawdŸ czy obiekt latarki jest aktywny w scenie
        {
            // Obs³uga w³¹czania/wy³¹czania
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleFlashlight();
            }

            // Roz³adowywanie baterii
            if (isFlashlightOn)
            {
                currentBattery -= batteryDepletionRate * Time.deltaTime;
                UpdateBatteryUI();

                if (currentBattery <= 0)
                {
                    currentBattery = 0;
                    TurnOff(); // Automatycznie wy³¹cz, gdy bateria siê skoñczy
                    Debug.Log("Flashlight battery depleted!");
                    // Mo¿na dodaæ dŸwiêk roz³adowania itp.
                }
            }
        }
    }

    public void ToggleFlashlight()
    {
        if (isFlashlightOn)
        {
            TurnOff();
        }
        else
        {
            // W³¹cz tylko jeœli jest bateria
            if (currentBattery > 0)
            {
                TurnOn();
            }
            else
            {
                Debug.Log("Cannot turn on flashlight - battery empty!");
                // Opcjonalnie: Odtwórz dŸwiêk klikniêcia bez w³¹czenia
            }
        }
    }

    private void TurnOn()
    {
        if (currentBattery > 0)
        {
            isFlashlightOn = true;
            flashlightLight.enabled = true;
            // Opcjonalnie: Odtwórz dŸwiêk w³¹czenia
            Debug.Log("Flashlight ON");
        }
    }

    private void TurnOff()
    {
        isFlashlightOn = false;
        flashlightLight.enabled = false;
        // Opcjonalnie: Odtwórz dŸwiêk wy³¹czenia
        Debug.Log("Flashlight OFF");
    }

    // Metoda do ³adowania baterii
    public void ChargeBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);
        Debug.Log($"Flashlight charged. Current battery: {currentBattery}/{maxBattery}");
        UpdateBatteryUI();

        // Jeœli latarka by³a wy³¹czona z powodu braku baterii, a teraz jest na³adowana,
        // u¿ytkownik musi j¹ ponownie w³¹czyæ manualnie (lub mo¿na j¹ w³¹czyæ automatycznie, jeœli chcesz)
    }

    private void UpdateBatteryUI()
    {
        // Aktualizuj UI, jeœli jest skonfigurowane
        // if (batterySlider != null)
        // {
        //     batterySlider.value = currentBattery / maxBattery;
        // }
    }

    // Dodaj atrybut, aby pole by³o tylko do odczytu w inspektorze
    public class ReadOnlyAttribute : PropertyAttribute { }
#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
#endif
}