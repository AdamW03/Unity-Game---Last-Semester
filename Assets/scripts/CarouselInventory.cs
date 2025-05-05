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
        UpdateHeldItemVisuals();
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


    // Pusta reka -----
    [Header("Gameplay Integration")]
    [Tooltip("ScriptableObject reprezentuj¹cy 'pust¹ rêkê' - wymagane!")]
    [SerializeField] private InventoryItem emptyHandItem; // <-- NOWE: Referencja do pustej rêki

    // NOWE: Mapowanie danych przedmiotu (ScriptableObject) na jego fizyczny obiekt w rêce gracza
    [System.Serializable]
    public class ItemVisualMapping
    {
        public InventoryItem itemData; // Asset przedmiotu
        public GameObject itemVisual; // GameObject w scenie (np. phoneLightPlayer)
    }
    [Tooltip("Lista mapowañ przedmiotów na ich wizualne odpowiedniki w rêce gracza.")]
    [SerializeField] private List<ItemVisualMapping> itemVisuals = new List<ItemVisualMapping>();
    // ----- Pusta reka 

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

        // SprawdŸ krytyczn¹ referencjê do pustej rêki
        if (emptyHandItem == null)
        {
            Debug.LogError("CarouselInventory: Brakuje referencji do 'Empty Hand Item' w Inspektorze! Funkcjonalnoœæ trzymanych przedmiotów nie bêdzie dzia³aæ.", this);
        }

        // Ukryj panel i ewentualny tekst "pusty" na starcie
        carouselPanel.SetActive(false);
        if (emptyInventoryText != null) emptyInventoryText.gameObject.SetActive(false);

        // Upewnij siê, ¿e na starcie nic nie jest trzymane (lub jest to co powinno byæ)
        InitializeInventoryState();
    }

    void Update()
    {
        // Obs³uga otwierania/zamykania
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            isInventoryOpen = !isInventoryOpen;
            UpdateUI(); // Aktualizuj tylko UI karuzeli
        }

        // Nawigacja dzia³a tylko, gdy ekwipunek jest OTWARTY i ma JAKIEKOLWIEK przedmioty (wliczaj¹c pust¹ rêkê)
        if (isInventoryOpen && items.Count > 0) // Zmieniono warunek z > 1 na > 0
        {
            bool indexChanged = false;
            if (Input.GetKeyDown(nextItemKey) && items.Count > 1) // Przewijanie tylko jeœli > 1
            {
                currentItemIndex = (currentItemIndex + 1) % items.Count;
                indexChanged = true;
            }
            else if (Input.GetKeyDown(previousItemKey) && items.Count > 1) // Przewijanie tylko jeœli > 1
            {
                currentItemIndex = (currentItemIndex - 1 + items.Count) % items.Count;
                indexChanged = true;
            }

            if (indexChanged)
            {
                UpdateUI();
                UpdateHeldItemVisuals(); // <-- NOWE: Aktualizuj trzymany przedmiot
            }
        }
    }

    public void AddItem(InventoryItem itemToAdd)
    {
        if (itemToAdd == null)
        {
            Debug.LogWarning("Próbowano dodaæ null jako przedmiot do ekwipunku.");
            return;
        }

        // Nie dodawaj "Pustej Rêki" bezpoœrednio przez tê metodê (jest zarz¹dzana wewnêtrznie)
        if (itemToAdd == emptyHandItem)
        {
            Debug.LogWarning("Nie dodawaj 'Empty Hand Item' rêcznie przez AddItem.");
            return;
        }

        // --- Logika dodawania "Pustej Rêki" ---
        bool inventoryWasEmpty = items.Count == 0;
        if (inventoryWasEmpty && emptyHandItem != null)
        {
            items.Add(emptyHandItem); // Dodaj "Pust¹ Rêkê" jako pierwszy element
            Debug.Log($"Dodano {emptyHandItem.itemName} jako opcjê.");
        }
        // ------------------------------------
        Debug.Log($"Dodano do ekwipunku: {itemToAdd.itemName}");
        items.Add(itemToAdd);

        // Jeœli to by³ pierwszy *rzeczywisty* przedmiot (a "Pusta Rêka" zosta³a dodana tu¿ przed nim)
        // lub jeœli "Pusta Rêka" ju¿ tam by³a, wybierz nowo dodany przedmiot.
        if (inventoryWasEmpty)
        {
            // Jeœli dodaliœmy pust¹ rêkê i przedmiot, indeks nowego to 1
            // Jeœli z jakiegoœ powodu nie ma pustej rêki, indeks to 0
            currentItemIndex = items.IndexOf(itemToAdd);
        }
        else if (currentItemIndex == -1 && items.Count > 0)
        {
            // Jeœli ekwipunek nie by³ pusty, ale nic nie by³o wybrane (dziwny stan), wybierz nowy
            currentItemIndex = items.IndexOf(itemToAdd);
        }
        // Jeœli coœ ju¿ by³o wybrane, pozostawiamy wybór bez zmian, chyba ¿e chcesz inaczej.


        // Aktualizuj UI i trzymany przedmiot
        UpdateUI();
        UpdateHeldItemVisuals();

        //// Jeœli to by³ pierwszy przedmiot, ustaw go jako aktualny
        //if (items.Count == 1)
        //{
        //    currentItemIndex = 0;
        //}

        //// Jeœli ekwipunek jest otwarty, odœwie¿ widok
        //if (isInventoryOpen)
        //{
        //    UpdateUI();
        //}
    }

    // Zmodyfikowana wersja RemoveItem, aby obs³ugiwa³a "Pust¹ Rêkê"
    public bool RemoveItem(InventoryItem itemToRemove)
    {
        if (itemToRemove == null || items.Count == 0 || itemToRemove == emptyHandItem)
        {
            // Nie pozwalamy usun¹æ "Pustej Rêki" bezpoœrednio
            if (itemToRemove == emptyHandItem) Debug.LogWarning("Nie mo¿na usun¹æ 'Empty Hand Item' bezpoœrednio.");
            return false;
        }

        int indexToRemove = items.IndexOf(itemToRemove);

        if (indexToRemove != -1)
        {
            Debug.Log($"Usuwanie z ekwipunku: {itemToRemove.itemName}");
            bool wasCurrentItem = (indexToRemove == currentItemIndex);

            items.RemoveAt(indexToRemove);

            // --- Logika usuwania "Pustej Rêki" ---
            // Jeœli po usuniêciu przedmiotu zosta³ tylko "EmptyHandItem"
            if (items.Count == 1 && items[0] == emptyHandItem)
            {
                Debug.Log($"Usuwanie ostatniego rzeczywistego przedmiotu. Usuwanie równie¿ {emptyHandItem.itemName}.");
                items.Clear(); // Usuñ wszystko (czyli pust¹ rêkê)
                currentItemIndex = -1; // Ekwipunek jest teraz naprawdê pusty
            }
            // --- Koniec logiki usuwania "Pustej Rêki" ---
            else if (items.Count > 0) // Jeœli zosta³y inne przedmioty (w tym potencjalnie pusta rêka)
            {
                // Dostosuj indeks, jeœli usuniêto coœ przed aktualnym lub sam aktualny
                if (wasCurrentItem)
                {
                    // Wybierz poprzedni element (lub pierwszy, jeœli usuniêto pierwszy)
                    currentItemIndex = Mathf.Max(0, indexToRemove - 1);
                    // LUB: wybierz nastêpny (modulo) - kwestia preferencji
                    // currentItemIndex = indexToRemove % items.Count;
                }
                else if (indexToRemove < currentItemIndex)
                {
                    currentItemIndex--; // Przesuñ indeks w lewo
                }
                // Upewnij siê, ¿e indeks jest poprawny (na wszelki wypadek)
                if (currentItemIndex >= items.Count)
                {
                    currentItemIndex = items.Count - 1;
                }
                if (currentItemIndex < 0 && items.Count > 0)
                { // Jeœli jakimœ cudem sta³ siê < 0
                    currentItemIndex = 0;
                }
            }
            else // Jeœli lista sta³a siê pusta (co obs³u¿yliœmy wy¿ej, ale dla pewnoœci)
            {
                currentItemIndex = -1;
            }

            // Aktualizuj UI i trzymany przedmiot
            UpdateUI();
            UpdateHeldItemVisuals();
            return true; // Usuniêto pomyœlnie
        }
        else
        {
            Debug.LogWarning($"Próbowano usun¹æ '{itemToRemove.itemName}', ale nie znaleziono go w ekwipunku.");
            return false; // Nie znaleziono przedmiotu
        }
    }

    // Wygodna metoda do usuwania aktualnie wybranego przedmiotu (jeœli nie jest to pusta rêka)
    public void RemoveCurrentItem()
    {
        InventoryItem current = GetSelectedItem();
        if (current != null && current != emptyHandItem)
        {
            RemoveItem(current);
        }
        else if (current == emptyHandItem)
        {
            Debug.Log("Nie mo¿na usun¹æ 'Pustej Rêki'.");
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
            // Jeœli indeks z jakiegoœ powodu jest z³y, ustaw na pierwszy element (czêsto "Pusta Rêka")
            currentItemIndex = 0;
            UpdateHeldItemVisuals(); // Zaktualizuj trzymany przedmiot po korekcie indeksu
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

    // --- NOWA METODA: Aktualizacja widocznoœci fizycznych przedmiotów ---
    private void UpdateHeldItemVisuals()
    {
        InventoryItem selectedItemData = GetSelectedItem(); // Pobierz aktualnie wybrany InventoryItem

        // PrzejdŸ przez wszystkie zmapowane wizualizacje
        foreach (var mapping in itemVisuals)
        {
            if (mapping.itemVisual != null) // Czy obiekt fizyczny istnieje?
            {
                // Czy aktualnie wybrany przedmiot (selectedItemData) odpowiada
                // przedmiotowi zdefiniowanemu w tym mapowaniu (mapping.itemData)?
                // ORAZ upewnij siê, ¿e wybrany przedmiot NIE JEST pust¹ rêk¹.
                bool shouldBeActive = selectedItemData != null &&
                                      mapping.itemData == selectedItemData &&
                                      selectedItemData != emptyHandItem;

                // Ustaw widocznoœæ obiektu fizycznego
                mapping.itemVisual.SetActive(shouldBeActive);
            }
        }

        // Jeœli wybrana jest "Pusta Rêka" (lub nic nie jest wybrane, co oznacza pusty ekwipunek),
        // upewnij siê, ¿e WSZYSTKIE wizualizacje s¹ wy³¹czone (pêtla powy¿ej ju¿ to robi).
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
    public bool HasItem(InventoryItem itemData)
    {
        if (itemData == null || itemData == emptyHandItem) return false;
        // Przeszukaj listê, ignoruj¹c potencjaln¹ pust¹ rêkê
        foreach (var item in items)
        {
            if (item == itemData) return true;
        }
        return false;
        // Alternatywnie, jeœli nie boisz siê LINQ:
        // return items.Contains(itemData) && itemData != emptyHandItem;
    }

    //public bool RemoveItem(InventoryItem itemToRemove)
    //{
    //    if (itemToRemove == null || items.Count == 0)
    //    {
    //        return false;
    //    }

    //    int indexToRemove = items.IndexOf(itemToRemove); // ZnajdŸ indeks przedmiotu

    //    if (indexToRemove != -1) // Znaleziono przedmiot
    //    {
    //        Debug.Log($"Usuwanie z ekwipunku: {itemToRemove.itemName}");
    //        bool wasCurrentItem = (indexToRemove == currentItemIndex);

    //        items.RemoveAt(indexToRemove);

    //        // Dostosuj indeks, jeœli usuniêto aktualny lub jeœli indeks sta³ siê nieprawid³owy
    //        if (items.Count == 0)
    //        {
    //            currentItemIndex = -1; // Ekwipunek sta³ siê pusty
    //        }
    //        else
    //        {
    //            // Jeœli usunêliœmy przedmiot PRZED aktualnym lub aktualny, musimy potencjalnie cofn¹æ indeks
    //            if (indexToRemove < currentItemIndex)
    //            {
    //                currentItemIndex--; // Przesuñ indeks w lewo
    //            }
    //            // Jeœli usunêliœmy ostatni element (a by³ on aktualny)
    //            else if (wasCurrentItem && currentItemIndex >= items.Count)
    //            {
    //                // Nowy aktualny to ostatni z pozosta³ych LUB pierwszy jeœli by³ jedyny
    //                currentItemIndex = items.Count > 0 ? items.Count - 1 : 0;
    //            }
    //            // Upewnij siê, ¿e indeks jest zawsze poprawny po usuniêciu
    //            if (currentItemIndex >= items.Count && items.Count > 0)
    //            {
    //                currentItemIndex = 0; // W razie problemu, ustaw na pierwszy
    //            }
    //            else if (items.Count == 0) // Jeœli sta³ siê pusty
    //            {
    //                currentItemIndex = -1;
    //            }
    //        }

    //        // Jeœli ekwipunek jest otwarty, odœwie¿ widok
    //        if (isInventoryOpen)
    //        {
    //            UpdateUI();
    //        }
    //        return true; // Usuniêto pomyœlnie
    //    }
    //    else
    //    {
    //        Debug.LogWarning($"Próbowano usun¹æ '{itemToRemove.itemName}', ale nie znaleziono go w ekwipunku.");
    //        return false; // Nie znaleziono przedmiotu
    //    }
    //}

    void InitializeInventoryState()
    {
        items.Clear(); // Zacznij z czyst¹ list¹ logiczn¹
        currentItemIndex = -1; // Nic nie jest wybrane

        // Jeœli *zaczynasz grê* z jakimiœ przedmiotami, dodaj je tutaj u¿ywaj¹c AddItem
        // np. AddItem(startingFlashlightData);

        // Jeœli ekwipunek jest PUSTY na starcie, nie dodajemy "Pustej Rêki" do listy `items`
        // ale upewniamy siê, ¿e ¿aden fizyczny przedmiot nie jest widoczny.
        UpdateHeldItemVisuals(); // To wy³¹czy wszystkie wizualizacje
        UpdateUI(); // Zaktualizuj UI, aby pokazaæ stan pusty (jeœli jest zamkniêty, nic siê nie stanie)
    }
}

