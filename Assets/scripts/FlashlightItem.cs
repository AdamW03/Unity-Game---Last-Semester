using UnityEngine;

[CreateAssetMenu(fileName = "New Flashlight Item", menuName = "Inventory/Flashlight Item")]
public class FlashlightItem : InventoryItem // Dziedziczy po InventoryItem
{
    [Header("Flashlight Settings")]
    public GameObject heldPrefab; // Prefab telefonu, który bêdzie trzymany
    public float maxBattery = 100f; // Maksymalna pojemnoœæ baterii
    [Tooltip("Units consumed per second when the flashlight is on.")]
    public float batteryDepletionRate = 1f; // Szybkoœæ zu¿ycia baterii (jednostki na sekundê)

    // Uwaga: Aktualny stan baterii nie bêdzie przechowywany tutaj,
    // poniewa¿ ScriptableObjects przechowuj¹ dane wspó³dzielone.
    // Stan baterii bêdzie zarz¹dzany przez instancjê latarki w scenie.
}