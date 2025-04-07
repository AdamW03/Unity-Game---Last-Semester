// Plik: PlayerEquipment.cs
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private CarouselInventory inventory; // Przypisz obiekt z CarouselInventory
    [SerializeField] private Transform itemHoldPoint; // Przypisz punkt, gdzie ma siê pojawiæ przedmiot

    [Header("Controls")]
    [SerializeField] private KeyCode useItemKey = KeyCode.R; // Klawisz do "u¿ycia" ³adowarki

    private GameObject currentlyHeldItemObject = null; // Aktualnie trzymany obiekt (instancja prefabu)
    private FlashlightController currentFlashlightController = null; // Referencja do kontrolera latarki, jeœli jest trzymana
    private InventoryItem currentlyEquippedItemData = null; // Dane (ScriptableObject) aktualnie trzymanego przedmiotu

    void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("PlayerEquipment: CarouselInventory nie jest przypisany!", this);
            enabled = false;
            return;
        }
        if (itemHoldPoint == null)
        {
            Debug.LogError("PlayerEquipment: ItemHoldPoint nie jest przypisany!", this);
            enabled = false;
            return;
        }
        // Upewnij siê, ¿e na start nic nie jest trzymane
        UnequipCurrentItem();
    }

    void Update()
    {
        // Sprawdzaj, czy zmieni³ siê wybrany przedmiot w ekwipunku
        CheckEquippedItem();

        // Sprawdzaj input dla u¿ycia ³adowarki
        HandleChargingInput();
    }

    private void CheckEquippedItem()
    {
        InventoryItem selectedItem = inventory.GetSelectedItem(); // Pobierz aktualnie wybrany item z karuzeli

        // Jeœli wybrany przedmiot jest inny ni¿ aktualnie trzymany LUB jeœli nic nie jest wybrane, a coœ trzymamy
        if (selectedItem != currentlyEquippedItemData)
        {
            // Jeœli by³ jakiœ przedmiot, schowaj go
            if (currentlyEquippedItemData != null)
            {
                UnequipCurrentItem();
            }

            // Jeœli wybrano nowy, prawid³owy przedmiot, spróbuj go wyposa¿yæ
            if (selectedItem != null)
            {
                EquipItem(selectedItem);
            }
            // Aktualizuj dane o trzymanym przedmiocie (nawet jeœli to null)
            currentlyEquippedItemData = selectedItem;
        }
    }

    private void EquipItem(InventoryItem itemData)
    {
        // SprawdŸ, czy to latarka (FlashlightItem) i czy ma przypisany prefab
        if (itemData is FlashlightItem flashlightData && flashlightData.heldPrefab != null)
        {
            // Stwórz instancjê prefabu w punkcie trzymania
            currentlyHeldItemObject = Instantiate(flashlightData.heldPrefab, itemHoldPoint.position, itemHoldPoint.rotation, itemHoldPoint); // Ustaw jako dziecko itemHoldPoint
            // Pobierz kontroler latarki z instancji
            currentFlashlightController = currentlyHeldItemObject.GetComponent<FlashlightController>();

            if (currentFlashlightController != null)
            {
                // Zainicjalizuj latarkê danymi z ScriptableObject
                // UWAGA: Tutaj potencjalnie mo¿na by wczytaæ zapisany stan baterii, na razie startuje z pe³n¹.
                currentFlashlightController.Initialize(flashlightData);
                Debug.Log($"Wyposa¿ono latarkê: {flashlightData.itemName}");
            }
            else
            {
                Debug.LogError($"Prefab '{flashlightData.heldPrefab.name}' dla {flashlightData.itemName} nie ma komponentu FlashlightController!", flashlightData.heldPrefab);
                // Zniszcz obiekt, bo jest bezu¿yteczny bez kontrolera
                Destroy(currentlyHeldItemObject);
                currentlyHeldItemObject = null;
            }
        }
        // --- W przysz³oœci: Obs³uga innych typów trzymanych przedmiotów ---
        // else if (itemData is WeaponItem weaponData && weaponData.heldPrefab != null) { ... }
        else
        {
            // Przedmiot nie jest latark¹ lub nie ma prefabu - nie mo¿na go trzymaæ w ten sposób
            Debug.Log($"Przedmiot '{itemData.itemName}' nie jest latark¹ z prefabem, nie bêdzie trzymany w rêce.");
            // currentlyHeldItemObject pozostaje null
        }
        // Aktualizuj dane o tym, co trzymamy (wa¿ne nawet jeœli equip siê nie powiód³)
        // currentlyEquippedItemData jest ustawiane w CheckEquippedItem()
    }


    private void UnequipCurrentItem()
    {
        if (currentlyHeldItemObject != null)
        {
            // Potencjalnie zapisz stan baterii przed zniszczeniem
            // if (currentFlashlightController != null) { /* Zapisz currentFlashlightController.currentBattery gdzieœ */ }

            Destroy(currentlyHeldItemObject); // Zniszcz obiekt trzymany w rêce
            Debug.Log($"Schowano przedmiot: {currentlyEquippedItemData?.itemName ?? "Nieznany"}");
        }
        currentlyHeldItemObject = null;
        currentFlashlightController = null;
        currentlyEquippedItemData = null; // Upewnij siê, ¿e dane te¿ s¹ wyczyszczone
    }

    // --- Obs³uga £adowania ---
    private void HandleChargingInput()
    {
        // SprawdŸ tylko jeœli gracz trzyma latarkê i w ekwipunku jest wybrany jakiœ przedmiot
        if (Input.GetKeyDown(useItemKey) && currentFlashlightController != null && inventory.GetSelectedItem() != null)
        {
            InventoryItem selectedInventoryItem = inventory.GetSelectedItem();

            // SprawdŸ, czy wybrany przedmiot to ³adowarka (na razie zak³adamy, ¿e ma specyficzn¹ nazwê)
            // Lepszym rozwi¹zaniem by³oby stworzenie ChargerItem jak FlashlightItem.
            if (selectedInventoryItem.itemName == "£adowarka") // TODO: U¿yj typu ChargerItem zamiast nazwy!
            {
                // Tutaj mo¿esz dodaæ logikê iloœci ³adowania, np. z ChargerItem
                float chargeAmount = 50f; // Przyk³adowa wartoœæ na³adowania

                currentFlashlightController.ChargeBattery(chargeAmount);

                // Opcjonalnie: zu¿yj ³adowarkê
                inventory.RemoveCurrentItem(); // Usuñ ³adowarkê z ekwipunku po u¿yciu
                Debug.Log("U¿yto ³adowarki.");

                // Po usuniêciu przedmiotu, trzeba ponownie sprawdziæ ekwipunek,
                // bo wybrany przedmiot siê zmieni³ (lub go nie ma).
                // Funkcja CheckEquippedItem() w nastêpnej klatce Update() powinna to za³atwiæ.
            }
        }
    }
}