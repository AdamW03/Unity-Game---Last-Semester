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
    [Tooltip("Panel zawieraj�cy ca�� karuzel�.")]
    [SerializeField] private GameObject carouselPanel;
    [Tooltip("Obiekt Image dla poprzedniego przedmiotu.")]
    [SerializeField] private Image previousItemImage;
    [Tooltip("Obiekt Image dla aktualnego przedmiotu.")]
    [SerializeField] private Image currentItemImage;
    [Tooltip("Obiekt Image dla nast�pnego przedmiotu.")]
    [SerializeField] private Image nextItemImage;

    [Header("UI Elements (Opcjonalne)")]
    [Tooltip("Tekst wy�wietlaj�cy nazw� aktualnego przedmiotu.")]
    [SerializeField] private TextMeshProUGUI currentItemNameText;
    [Tooltip("Tekst wy�wietlany, gdy ekwipunek jest pusty.")]
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
    [SerializeField] private Sprite defaultIcon; // Opcjonalnie: Domy�lna ikona, je�li przedmiot jej nie ma

    private bool isInventoryOpen = false;

    void Start()
    {
        // Sprawd� krytyczne referencje UI na starcie
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            Debug.LogError("CarouselInventory: Brakuje podstawowych referencji do element�w UI w Inspektorze! Ekwipunek nie b�dzie dzia�a� poprawnie.", this);
            enabled = false; // Wy��cz skrypt, aby unikn�� b��d�w
            return;
        }

        // Ukryj panel i ewentualny tekst "pusty" na starcie
        carouselPanel.SetActive(false);
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Obs�uga otwierania/zamykania
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            isInventoryOpen = !isInventoryOpen;
            UpdateUI();
        }

        // Nawigacja dzia�a tylko, gdy ekwipunek jest OTWARTY i ma WI�CEJ NI� 1 przedmiot
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
        // Opcjonalna obs�uga u�ycia przedmiotu (te� tylko gdy otwarty i niepusty)
        // if (isInventoryOpen && items.Count > 0 && Input.GetKeyDown(KeyCode.R))
        // {
        //     UseCurrentItem();
        // }
    }

    public void AddItem(InventoryItem itemToAdd)
    {
        if (itemToAdd == null)
        {
            Debug.LogWarning("Pr�bowano doda� null jako przedmiot do ekwipunku.");
            return;
        }

        Debug.Log($"Dodano do ekwipunku: {itemToAdd.itemName}");
        items.Add(itemToAdd);

        // Je�li to by� pierwszy przedmiot, ustaw go jako aktualny
        if (items.Count == 1)
        {
            currentItemIndex = 0;
        }

        // Je�li ekwipunek jest otwarty, od�wie� widok
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
            Debug.Log($"Usuni�to z ekwipunku: {removedItem.itemName}");
            items.RemoveAt(currentItemIndex);

            // Dostosuj indeks
            if (items.Count == 0)
            {
                currentItemIndex = -1; // Ekwipunek sta� si� pusty
            }
            else if (currentItemIndex >= items.Count) // Je�li usun�li�my ostatni element
            {
                currentItemIndex = items.Count - 1; // Wybierz nowy ostatni
            }
            // W przeciwnym razie indeks pozostaje (nast�pny element zaj�� miejsce)

            // Je�li ekwipunek jest otwarty, od�wie� widok
            if (isInventoryOpen)
            {
                UpdateUI();
            }
        }
    }

    // --- G��WNA AKTUALIZACJA UI ---
    private void UpdateUI()
    {
        // 1. Sprawd� podstawowe referencje (ponownie, dla pewno�ci)
        if (carouselPanel == null || previousItemImage == null || currentItemImage == null || nextItemImage == null)
        {
            // Log b��du zosta� ju� pokazany w Start(), wi�c tu mo�na pomin��
            return;
        }

        // 2. Ustaw widoczno�� g��wnego panelu
        carouselPanel.SetActive(isInventoryOpen);

        // 3. Je�li panel jest ukryty, zako�cz
        if (!isInventoryOpen)
        {
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false); // Ukryj te� tekst "pusty"
            return;
        }

        // --- Panel jest OTWARTY ---

        // 4. Obs�uga stanu PUSTEGO ekwipunku
        if (items.Count == 0)
        {
            currentItemIndex = -1; // Upewnij si�
            // Ukryj wszystkie obrazy przedmiot�w
            previousItemImage.enabled = false;
            currentItemImage.enabled = false;
            nextItemImage.enabled = false;
            // Ukryj nazw� przedmiotu
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            // Poka� tekst "Ekwipunek Pusty" (je�li jest przypisany)
            if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(true);
            return; // Zako�cz, bo nie ma co wy�wietla�
        }

        // --- Ekwipunek NIE JEST PUSTY ---

        // 5. Ukryj tekst "Ekwipunek Pusty" (je�li jest)
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);

        // 6. Upewnij si�, �e indeks jest prawid�owy
        if (currentItemIndex < 0 || currentItemIndex >= items.Count)
        {
            currentItemIndex = 0; // Resetuj do pierwszego elementu, je�li co� posz�o nie tak
        }

        // 7. Wy�wietl �RODKOWY przedmiot
        InventoryItem currentItemData = items[currentItemIndex];
        if (currentItemData != null)
        {
            currentItemImage.sprite = currentItemData.icon ?? defaultIcon; // U�yj ikony przedmiotu lub domy�lnej
            currentItemImage.color = centerItemColor;
            currentItemImage.transform.localScale = Vector3.one;
            currentItemImage.enabled = true; // Poka� obrazek

            // Poka� nazw� (je�li jest przypisany tekst)
            if (currentItemNameText != null)
            {
                currentItemNameText.text = currentItemData.itemName;
                currentItemNameText.gameObject.SetActive(true);
            }
        }
        else // Je�li jakim� cudem element na li�cie jest null
        {
            currentItemImage.enabled = false;
            if (currentItemNameText != null) currentItemNameText.gameObject.SetActive(false);
            Debug.LogError($"Element na li�cie items na indeksie {currentItemIndex} jest null!");
        }


        // 8. Obs�uga BOCZNYCH przedmiot�w (tylko je�li jest wi�cej ni� 1)
        bool hasMoreThanOneItem = items.Count > 1;

        // W��cz/Wy��cz boczne obrazki globalnie
        previousItemImage.enabled = hasMoreThanOneItem;
        nextItemImage.enabled = hasMoreThanOneItem;

        if (hasMoreThanOneItem)
        {
            // Wy�wietl POPRZEDNI przedmiot
            int prevIndex = (currentItemIndex - 1 + items.Count) % items.Count;
            InventoryItem prevItemData = items[prevIndex];
            if (prevItemData != null)
            {
                previousItemImage.sprite = prevItemData.icon ?? defaultIcon;
                previousItemImage.color = sideItemColor;
                previousItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
                // previousItemImage.enabled jest ju� ustawione na true
            }
            else // Je�li poprzedni element jest null
            {
                previousItemImage.enabled = false; // Ukryj ten konkretny obrazek
            }


            // Wy�wietl NAST�PNY przedmiot
            int nextIndex = (currentItemIndex + 1) % items.Count;
            InventoryItem nextItemData = items[nextIndex];
            if (nextItemData != null)
            {
                nextItemImage.sprite = nextItemData.icon ?? defaultIcon;
                nextItemImage.color = sideItemColor;
                nextItemImage.transform.localScale = new Vector3(sideItemScale, sideItemScale, 1f);
                // nextItemImage.enabled jest ju� ustawione na true
            }
            else // Je�li nast�pny element jest null
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
    // Opcjonalna metoda UseCurrentItem (doda� logik� u�ycia)
    // public void UseCurrentItem() { /* ... */ }
}