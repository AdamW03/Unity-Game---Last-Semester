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
    [SerializeField] private TextMeshProUGUI emptyInventoryText; // NOWE: Tekst dla pustego stanu

    [Header("Inventory Data")]
    public List<InventoryItem> items = new List<InventoryItem>();
    private int currentItemIndex = -1;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.I;
    [SerializeField] private KeyCode nextItemKey = KeyCode.E;
    [SerializeField] private KeyCode previousItemKey = KeyCode.Q;

    [Header("Visual Settings")]
    [SerializeField] private float sideItemScale = 0.7f;
    [SerializeField] private Color sideItemColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color centerItemColor = Color.white;
    [SerializeField] private Sprite defaultIcon; // Opcjonalnie: Domyœlna ikona, jeœli przedmiot jej nie ma

    private bool isInventoryOpen = false;

    void Start()
    {
        // SprawdŸ krytyczne referencje UI na starcie
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            Debug.LogError("CarouselInventory: Brakuje podstawowych referencji do elementów UI w Inspektorze! Ekwipunek nie bêdzie dzia³aæ poprawnie.", this);
            enabled = false; // Wy³¹cz skrypt, aby unikn¹æ b³êdów
            return;
        }

        // Ukryj panel i ewentualny tekst "pusty" na starcie
        carouselPanel.SetActive(false);
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Obs³uga otwierania/zamykania
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            isInventoryOpen = !isInventoryOpen;
            UpdateUI();
        }

        // Nawigacja dzia³a tylko, gdy ekwipunek jest OTWARTY i ma WIÊCEJ NI¯ 1 przedmiot
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
                UpdateUI();
            }
        }
        // Opcjonalna obs³uga u¿ycia przedmiotu (te¿ tylko gdy otwarty i niepusty)
        // if (isInventoryOpen && items.Count > 0 && Input.GetKeyDown(KeyCode.R))
        // {
        //     UseCurrentItem();
        // }
    }

    public void AddItem(InventoryItem itemToAdd)
    {
        if (itemToAdd == null)
        {
            Debug.LogWarning("Próbowano dodaæ null jako przedmiot do ekwipunku.");
            return;
        }

        Debug.Log($"Dodano do ekwipunku: {itemToAdd.itemName}");
        items.Add(itemToAdd);

        // Jeœli to by³ pierwszy przedmiot, ustaw go jako aktualny
        if (items.Count == 1)
        {
            currentItemIndex = 0;
        }

        // Jeœli ekwipunek jest otwarty, odœwie¿ widok
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
            Debug.Log($"Usuniêto z ekwipunku: {removedItem.itemName}");
            items.RemoveAt(currentItemIndex);

            // Dostosuj indeks
            if (items.Count == 0)
            {
                currentItemIndex = -1; // Ekwipunek sta³ siê pusty
            }
            else if (currentItemIndex >= items.Count) // Jeœli usunêliœmy ostatni element
            {
                currentItemIndex = items.Count - 1; // Wybierz nowy ostatni
            }
            // W przeciwnym razie indeks pozostaje (nastêpny element zaj¹³ miejsce)

            // Jeœli ekwipunek jest otwarty, odœwie¿ widok
            if (isInventoryOpen)
            {
                UpdateUI();
            }
        }
    }

    // --- G£ÓWNA AKTUALIZACJA UI ---
    private void UpdateUI()
    {
        // 1. SprawdŸ podstawowe referencje (ponownie, dla pewnoœci)
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            // Log b³êdu zosta³ ju¿ pokazany w Start(), wiêc tu mo¿na pomin¹æ
            return;
        }

        // 2. Ustaw widocznoœæ g³ównego panelu
        carouselPanel.SetActive(isInventoryOpen);

        // 3. Jeœli panel jest ukryty, zakoñcz
        if (!isInventoryOpen)
        {
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false); // Ukryj te¿ tekst "pusty"
            return;
        }

        // --- Panel jest OTWARTY ---

        // 4. Obs³uga stanu PUSTEGO ekwipunku
        if (items.Count == 0)
        {
            currentItemIndex = -1; // Upewnij siê
            // Ukryj wszystkie obrazy przedmiotów
            previousItemImage.enabled = false;
            currentItemImage.enabled = false;
            nextItemImage.enabled = false;
            // Ukryj nazwê przedmiotu
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            // Poka¿ tekst "Ekwipunek Pusty" (jeœli jest przypisany)
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(true);
            return; // Zakoñcz, bo nie ma co wyœwietlaæ
        }

        // --- Ekwipunek NIE JEST PUSTY ---

        // 5. Ukryj tekst "Ekwipunek Pusty" (jeœli jest)
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);

        // 6. Upewnij siê, ¿e indeks jest prawid³owy
        if (currentItemIndex < 0 || currentItemIndex >= items.Count)
        {
            currentItemIndex = 0; // Resetuj do pierwszego elementu, jeœli coœ posz³o nie tak
        }

        // 7. Wyœwietl ŒRODKOWY przedmiot
        InventoryItem currentItemData = items[currentItemIndex];
        if (currentItemData != null)
        {
            currentItemImage.sprite = currentItemData.icon ?? defaultIcon; // U¿yj ikony przedmiotu lub domyœlnej
            currentItemImage.color = centerItemColor;
            currentItemImage.transform.localScale = Vector3.one;
            currentItemImage.enabled = true; // Poka¿ obrazek

            // Poka¿ nazwê (jeœli jest przypisany tekst)
            if (currentItemNameText != null)
            {
                currentItemNameText.text = currentItemData.itemName;
                currentItemNameText.gameObject.SetActive(true);
            }
        }
        else // Jeœli jakimœ cudem element na liœcie jest null
        {
            currentItemImage.enabled = false;
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            Debug.LogError($"Element na liœcie items na indeksie {currentItemIndex} jest null!");
        }


        // 8. Obs³uga BOCZNYCH przedmiotów (tylko jeœli jest wiêcej ni¿ 1)
        bool hasMoreThanOneItem = items.Count > 1;

        // W³¹cz/Wy³¹cz boczne obrazki globalnie
        previousItemImage.enabled = hasMoreThanOneItem;
        nextItemImage.enabled = hasMoreThanOneItem;

        if (hasMoreThanOneItem)
        {
            // Wyœwietl POPRZEDNI przedmiot
            int prevIndex = (currentItemIndex - 1 + items.Count) % items.Count;
            InventoryItem prevItemData = items[prevIndex];
            if (prevItemData != null)
            {
                previousItemImage.sprite = prevItemData.icon ?? defaultIcon;
                previousItemImage.color = sideItemColor;
                previousItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
                // previousItemImage.enabled jest ju¿ ustawione na true
            }
            else // Jeœli poprzedni element jest null
            {
                previousItemImage.enabled = false; // Ukryj ten konkretny obrazek
            }


            // Wyœwietl NASTÊPNY przedmiot
            int nextIndex = (currentItemIndex + 1) % items.Count;
            InventoryItem nextItemData = items[nextIndex];
            if (nextItemData != null)
            {
                nextItemImage.sprite = nextItemData.icon ?? defaultIcon;
                nextItemImage.color = sideItemColor;
                nextItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
                // nextItemImage.enabled jest ju¿ ustawione na true
            }
            else // Jeœli nastêpny element jest null
            {
                nextItemImage.enabled = false; // Ukryj ten konkretny obrazek
            }
        }
    }

    // przekarz aktualnei wybrany przedmiot w ekwipunku
    public InventoryItem GetSelectedItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < items.Count)
        {
            return items[currentItemIndex];
        }
        return null;
    }
    // Opcjonalna metoda UseCurrentItem (dodaæ logikê u¿ycia)
    // public void UseCurrentItem() { /* ... */ }
}