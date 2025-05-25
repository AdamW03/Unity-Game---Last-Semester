using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Opcjonalne, dla nazw

public class CarouselInventory : MonoBehaviour
{
    // --- Singleton ---
    public static CarouselInventory Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; /* DontDestroyOnLoad(gameObject); */ }
    }
    // -----------------

    [Header("UI Elements (Wymagane!)")]
    [Tooltip("Panel zawieraj¹cy ca³¹ karuzelê.")]
    [SerializeField] private GameObject carouselPanel;
    [Tooltip("Obiekt Image dla poprzedniego przedmiotu.")]
    [SerializeField] private Image previousItemImage;
    [Tooltip("Obiekt Image dla aktualnego przedmiotu.")]
    [SerializeField] private Image currentItemImage;
    [Tooltip("Obiekt Image dla nastêpnego przedmiotu.")]
    [SerializeField] private Image nextItemImage;

    [Header("UI Elements (Opcjonalne)")]
    [Tooltip("Tekst wyœwietlaj¹cy nazwê aktualnego przedmiotu.")]
    [SerializeField] private TextMeshProUGUI currentItemNameText;
    [Tooltip("Tekst wyœwietlany, gdy ekwipunek jest pusty.")]
    [SerializeField] private TextMeshProUGUI emptyInventoryText;

    [Header("Inventory Data")]
    public List<InventoryItem> items = new List<InventoryItem>();
    private int currentItemIndex = -1;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.I;
    [SerializeField] private KeyCode nextItemKey = KeyCode.E;
    [SerializeField] private KeyCode previousItemKey = KeyCode.Q;

    [Header("Audio")] // <-- NOWA SEKCJA
    [Tooltip("DŸwiêk odtwarzany przy przewijaniu przedmiotów w ekwipunku.")]
    [SerializeField] private AudioClip itemScrollSoundClip; // <-- NOWE POLE NA DWIÊK PRZEWIJANIA

    [Header("Visual Settings")]
    [SerializeField] private float sideItemScale = 0.7f;
    [SerializeField] private Color sideItemColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color centerItemColor = Color.white;
    [SerializeField] private Sprite defaultIcon;

    private bool isInventoryOpen = false;

    void Start()
    {
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            Debug.LogError("CarouselInventory: Brakuje podstawowych referencji do elementów UI w Inspektorze! Ekwipunek nie bêdzie dzia³aæ poprawnie.", this);
            enabled = false;
            return;
        }
        carouselPanel.SetActive(false);
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            isInventoryOpen = !isInventoryOpen;
            UpdateUI();
            // Mo¿esz dodaæ dŸwiêk otwierania/zamykania ekwipunku tutaj, jeœli chcesz
        }

        if (isInventoryOpen && items.Count > 1)
        {
            bool indexChanged = false;
            if (Input.GetKeyDown(nextItemKey))
            {
                currentItemIndex = (currentItemIndex + 1) % items.Count;
                indexChanged = true;
            }
            else if (Input.GetKeyDown(previousItemKey))
            {
                currentItemIndex = (currentItemIndex - 1 + items.Count) % items.Count;
                indexChanged = true;
            }

            if (indexChanged)
            {
                PlayItemScrollSound(); // <-- ODTWÓRZ DWIÊK
                UpdateUI();
            }
        }
    }

    // --- NOWA METODA DO ODTWARZANIA DWIÊKU PRZEWIJANIA ---
    private void PlayItemScrollSound()
    {
        if (SoundFXManager.Instance != null && itemScrollSoundClip != null)
        {
            // Odtwarzamy dŸwiêk w pozycji kamery gracza (lub innej odpowiedniej)
            // Dla prostoty, u¿yjemy transform.position tego obiektu CarouselInventory.
            SoundFXManager.Instance.PlaySoundFXClip(itemScrollSoundClip, transform, 1f);
        }
        else if (itemScrollSoundClip == null)
        {
            // Ten log mo¿e byæ zbyt czêsty, jeœli nie przypiszesz dŸwiêku, wiêc mo¿na go zakomentowaæ
            // Debug.LogWarning("CarouselInventory: Brak przypisanego dŸwiêku przewijania przedmiotów (itemScrollSoundClip).");
        }
        else if (SoundFXManager.Instance == null)
        {
            Debug.LogWarning("CarouselInventory: SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku przewijania.");
        }
    }
    // ---------------------------------------------------------

    public void AddItem(InventoryItem itemToAdd)
    {
        if (itemToAdd == null)
        {
            Debug.LogWarning("Próbowano dodaæ null jako przedmiot do ekwipunku.");
            return;
        }
        Debug.Log($"Added to inventory: {itemToAdd.itemName}");
        items.Add(itemToAdd);
        if (items.Count == 1)
        {
            currentItemIndex = 0;
        }
        if (isInventoryOpen)
        {
            UpdateUI();
        }
    }

    public void RemoveCurrentItem()
    {
        if (currentItemIndex != -1 && items.Count > 0)
        {
            InventoryItem removedItem = items[currentItemIndex];
            Debug.Log($"Deleted from inventory: {removedItem.itemName}");
            items.RemoveAt(currentItemIndex);
            if (items.Count == 0)
            {
                currentItemIndex = -1;
            }
            else if (currentItemIndex >= items.Count)
            {
                currentItemIndex = items.Count - 1;
            }
            if (isInventoryOpen)
            {
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            return;
        }
        carouselPanel.SetActive(isInventoryOpen);
        if (!isInventoryOpen)
        {
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);
            return;
        }

        if (items.Count == 0)
        {
            currentItemIndex = -1;
            previousItemImage.enabled = false;
            currentItemImage.enabled = false;
            nextItemImage.enabled = false;
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(true);
            return;
        }

        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);
        if (currentItemIndex < 0 || currentItemIndex >= items.Count)
        {
            currentItemIndex = 0;
        }

        InventoryItem currentItemData = items[currentItemIndex];
        if (currentItemData != null)
        {
            currentItemImage.sprite = currentItemData.icon ?? defaultIcon;
            currentItemImage.color = centerItemColor;
            currentItemImage.transform.localScale = Vector3.one;
            currentItemImage.enabled = true;
            if (currentItemNameText != null)
            {
                currentItemNameText.text = currentItemData.itemName;
                currentItemNameText.gameObject.SetActive(true);
            }
        }
        else
        {
            currentItemImage.enabled = false;
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            Debug.LogError($"Element na liœcie items na indeksie {currentItemIndex} jest null!");
        }

        bool hasMoreThanOneItem = items.Count > 1;
        previousItemImage.enabled = hasMoreThanOneItem;
        nextItemImage.enabled = hasMoreThanOneItem;

        if (hasMoreThanOneItem)
        {
            int prevIndex = (currentItemIndex - 1 + items.Count) % items.Count;
            InventoryItem prevItemData = items[prevIndex];
            if (prevItemData != null)
            {
                previousItemImage.sprite = prevItemData.icon ?? defaultIcon;
                previousItemImage.color = sideItemColor;
                previousItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
            }
            else
            {
                previousItemImage.enabled = false;
            }

            int nextIndex = (currentItemIndex + 1) % items.Count;
            InventoryItem nextItemData = items[nextIndex];
            if (nextItemData != null)
            {
                nextItemImage.sprite = nextItemData.icon ?? defaultIcon;
                nextItemImage.color = sideItemColor;
                nextItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
            }
            else
            {
                nextItemImage.enabled = false;
            }
        }
    }

    public InventoryItem GetSelectedItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < items.Count)
        {
            return items[currentItemIndex];
        }
        return null;
    }

    public bool HasItem(InventoryItem itemData)
    {
        if (itemData == null) return false;
        return items.Contains(itemData);
    }

    public bool RemoveItem(InventoryItem itemToRemove)
    {
        if (itemToRemove == null || items.Count == 0)
        {
            return false;
        }
        int indexToRemove = items.IndexOf(itemToRemove);
        if (indexToRemove != -1)
        {
            Debug.Log($"Usuwanie z ekwipunku: {itemToRemove.itemName}");
            bool wasCurrentItem = (indexToRemove == currentItemIndex);
            items.RemoveAt(indexToRemove);
            if (items.Count == 0)
            {
                currentItemIndex = -1;
            }
            else
            {
                if (indexToRemove < currentItemIndex)
                {
                    currentItemIndex--;
                }
                else if (wasCurrentItem && currentItemIndex >= items.Count)
                {
                    currentItemIndex = items.Count > 0 ? items.Count - 1 : 0;
                }
                if (currentItemIndex >= items.Count && items.Count > 0)
                {
                    currentItemIndex = 0;
                }
                else if (items.Count == 0)
                {
                    currentItemIndex = -1;
                }
            }
            if (isInventoryOpen)
            {
                UpdateUI();
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"Próbowano usun¹æ '{itemToRemove.itemName}', ale nie znaleziono go w ekwipunku.");
            return false;
        }
    }
}