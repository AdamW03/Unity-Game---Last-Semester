using UnityEngine;
using UnityEngine.UI; // Potrzebne dla Slidera
using TMPro; // Potrzebne dla TextMeshPro

public class BatteryUIController : MonoBehaviour
{
    [Header("Referencje do UI")]
    [SerializeField] private Slider batterySlider;
    [SerializeField] private TextMeshProUGUI batteryText;
    [SerializeField] private GameObject uiContainer; // Przeci¹gnij tu g³ówny obiekt UI, np. BatteryBar

    void Start()
    {
        Debug.Log("--- BatteryUIController.Start() --- SKRYPT DZIA£A. PRÓBUJÊ UKRYÆ UI.");

        if (uiContainer != null)
        {
            uiContainer.SetActive(false);
            Debug.Log("--- SUKCES! --- Obiekt " + uiContainer.name + " zosta³ ukryty.");
        }
        else
        {
            Debug.LogError("--- B£¥D KRYTYCZNY --- Pole 'uiContainer' w BatteryUIController jest PUSTE (null)!");
        }
    }

    // Ta publiczna metoda bêdzie wywo³ywana przez event z PhoneSystem
    public void UpdateBatteryDisplay(float currentBattery, float maxBattery)
    {
        Debug.Log($"UpdateBatteryDisplay OTRZYMANO: current={currentBattery}, max={maxBattery}");

        if (maxBattery <= 0) return;

        float fillValue = currentBattery / maxBattery;
        Debug.Log($"Obliczono fillValue dla slidera: {fillValue}");

        if (batterySlider != null)
        {
            batterySlider.value = fillValue;
        }
        else
        {
            Debug.LogError("B£¥D: Referencja 'batterySlider' jest PUSTA!");
        }

        if (batteryText != null)
        {
            int percentage = Mathf.RoundToInt(fillValue * 100f);
            batteryText.text = percentage.ToString() + "%";
        }
        else
        {
            Debug.LogError("B£¥D: Referencja 'batteryText' jest PUSTA!");
        }
    }

    // Metody do pokazywania i ukrywania UI
    public void ShowUI()
    {
        Debug.Log("KROK 2/3: Metoda ShowUI() zosta³a wywo³ana!"); // <-- DODAJ TEN LOG
        if (uiContainer != null)
        {
            Debug.Log("KROK 3/3: Aktywujê obiekt UI: " + uiContainer.name); // <-- DODAJ TEN LOG
            uiContainer.SetActive(true);
        }
        else
        {
            Debug.LogError("B£¥D KRYTYCZNY: Pole 'uiContainer' w BatteryUIController jest PUSTE (null)!");
        }
    }

    public void HideUI()
    {
        if (uiContainer != null)
        {
            uiContainer.SetActive(false);
        }
    }
}