using UnityEngine;
using TMPro; // Potrzebne dla TMP_Dropdown i TMP_Text
using UnityEngine.UI; // Potrzebne dla ColorBlock, Selectable, Image, Scrollbar itp.

public class DropdownStyler : MonoBehaviour
{
    // Przeciągnij swój obiekt Dropdown z Hierarchy tutaj w Inspectorze
    public TMP_Dropdown targetDropdown;

    // *** NOWA PUBLICZNA ZMIENNA DO PRZYPISANIA SCROLLBARA ***
    [Header("Bezpośrednie Przypisanie (Opcjonalne)")]
    [Tooltip("Przeciągnij tutaj obiekt Scrollbar Vertical z Template Dropdowna dla pewności")]
    public Scrollbar targetScrollbar;


    [Header("Główne Kolory Dropdowna (przycisku)")]
    public Color buttonNormalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color buttonHighlightedColor = new Color(0.2f, 0.2f, 1.0f, 1f);
    public Color buttonPressedColor = new Color(0.1f, 0.1f, 0.5f, 1f);
    public Color buttonDisabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Style Tekstu na Zamkniętym Dropdownie (Caption)")]
    public Color captionTextColor = Color.white;

    [Header("Style Tekstu Elementów Listy (Item)")]
    public Color itemTextColor = Color.black;
    public FontStyles itemFontStyles = FontStyles.Normal;

    [Header("Kolor Tła Panelu Rozwijanego")]
    public Color panelBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.7f); // Jasnoszare, 70% widoczności

    [Header("Style Elementu Listy (Tło, Kolor po najechaniu)")]
    public Color itemNormalBackgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.5f); // Białe, 50% widoczności
    public Color itemHighlightedBackgroundColor = new Color(0.6f, 0.9f, 1.0f, 0.6f); // Jasnoniebieskie, 60% widoczności
    public Color itemPressedBackgroundColor = new Color(0.2f, 0.5f, 1.0f, 0.7f); // Niebieskie, 70% widoczności
    public Color itemSelectedBackgroundColor = new Color(0.2f, 0.5f, 1.0f, 0.7f); // Często takie samo jak highlighted/pressed

    [Header("Style Scrollbara (jeśli obecny)")]
    public Color scrollbarTrackColor = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Ciemnoszare, 60% widoczności
    public Color scrollbarHandleNormalColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Szare, pełna alpha
    public Color scrollbarHandleHighlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Jaśniejsze szare
    public Color scrollbarHandlePressedColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Ciemniejsze szare
    public Color scrollbarHandleDisabledColor = new Color(0.9f, 0.9f, 0.9f, 0.5f); // Prawie białe, półprzezroczyste

    // Ścieżka fallbackowa, jeśli targetScrollbar nie zostanie przypisany
    private const string ScrollbarPath = "Scrollbar Vertical";


    void Awake()
    {
        if (targetDropdown == null)
        {
            Debug.LogError("Target Dropdown nie został przypisany w Inspectorze!", this);
            return;
        }

        ApplyStyles();
    }

    void ApplyStyles()
    {
        // --- Stylowanie Głównego Przycisku Dropdowna ---
        ColorBlock buttonColors = targetDropdown.colors;
        buttonColors.normalColor = buttonNormalColor;
        buttonColors.highlightedColor = buttonHighlightedColor;
        buttonColors.pressedColor = buttonPressedColor;
        buttonColors.disabledColor = buttonDisabledColor;
        targetDropdown.colors = buttonColors;

        // Stylowanie tekstu wyświetlanego na zamkniętym dropdownie (Caption Text)
        if (targetDropdown.captionText is TMP_Text tmpCaptionText)
        {
            tmpCaptionText.color = captionTextColor;
        }

        // --- Stylowanie Panelu Rozwijanego i Elementów Listy ---

        // Stylowanie tekstu pojedynczego elementu na liście rozwijanej (Item Text)
        if (targetDropdown.itemText is TMP_Text tmpItemText)
        {
            tmpItemText.color = itemTextColor;
            tmpItemText.fontStyle = itemFontStyles;
        }

        // Stylowanie Tła Panelu Rozwijanego:
        Transform panelTransform = targetDropdown.template;
        if (panelTransform != null)
        {
            Image panelImage = panelTransform.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panelTransform.Find("Background")?.GetComponent<Image>();
            }

            if (panelImage != null)
            {
                panelImage.color = panelBackgroundColor;
            }
            else
            {
                Debug.LogWarning("Nie udało się znaleźć komponentu Image dla tła panelu Dropdowna w szablonie Dropdowna. Stylowanie tła panelu może być niedostępne tym sposobem.", this);
            }
        }
        else
        {
            Debug.LogWarning("Szablon Dropdowna (template) nie został przypisany w komponencie TMP_Dropdown. Nie można ostylować panelu, elementów listy ani scrollbara.", this);
            // Jeśli template jest null, reszta stylowania (itemów i scrollbara) i tak nie zadziała,
            // więc można tu zakończyć funkcję, albo pozwolić jej kontynuować dla lepszego logowania.
            // Zostawiamy kontynuację, żeby dostać więcej warningów o nieznalezionych elementach.
        }


        // Stylowanie Elementu Listy (Tło, Kolor po najechaniu):
        // Ta część potrzebuje dostępu do Template, więc sprawdzamy template != null wyżej.
        if (targetDropdown.template != null)
        {
            Transform itemTemplateTransform = targetDropdown.template.Find("Viewport/Content/Item"); // Typowa ścieżka
            if (itemTemplateTransform != null)
            {
                Selectable itemSelectable = itemTemplateTransform.GetComponent<Selectable>();
                if (itemSelectable != null)
                {
                    ColorBlock itemColors = itemSelectable.colors;
                    itemColors.normalColor = itemNormalBackgroundColor;
                    itemColors.highlightedColor = itemHighlightedBackgroundColor;
                    itemColors.pressedColor = itemPressedBackgroundColor;
                    itemColors.selectedColor = itemSelectedBackgroundColor;
                    itemSelectable.colors = itemColors;
                }
                else
                {
                    Debug.LogWarning("Znaleziono Item Template, ale nie ma na nim komponentu Selectable. Stylowanie tła elementów listy może nie działać.", this);
                }
            }
            else
            {
                Debug.LogWarning("Nie udało się znaleźć prototypu elementu listy (Item) w szablonie Dropdowna. Stylowanie tła elementów może być niedostępne tym sposobem.", this);
            }

            // --- NOWA LOGIKA: Stylowanie Scrollbara ---
            Scrollbar scrollbarToStyle = null;

            // Próbujemy użyć bezpośrednio przypisanego scrollbara
            if (targetScrollbar != null)
            {
                scrollbarToStyle = targetScrollbar;
                Debug.Log("Używam bezpośrednio przypisanego scrollbara.", this);
            }
            else
            {
                // Jeśli nie przypisano bezpośrednio, szukamy go po ścieżce w Template
                Transform scrollbarTransform = targetDropdown.template.Find(ScrollbarPath);
                if (scrollbarTransform != null)
                {
                    scrollbarToStyle = scrollbarTransform.GetComponent<Scrollbar>();
                    if (scrollbarToStyle != null)
                    {
                        Debug.Log("Znaleziono scrollbar po ścieżce: " + ScrollbarPath, this);
                    }
                    else
                    {
                        Debug.LogWarning("Znaleziono obiekt '" + ScrollbarPath + "', ale nie ma na nim komponentu Scrollbar.", this);
                    }
                }
                else
                {
                    Debug.LogWarning("Nie znaleziono obiektu scrollbara po standardowej ścieżce '" + ScrollbarPath + "' w szablonie Dropdowna. Spróbuj przypisać go ręcznie w polu 'Target Scrollbar'.", this);
                }
            }


            // Jeśli udało się znaleźć scrollbar (bezpośrednio lub przez Find)
            if (scrollbarToStyle != null)
            {
                // 1. Stylowanie TŁA (Track) Scrollbara
                // Tło to zazwyczaj komponent Image na tym samym GameObject co Scrollbar
                Image scrollbarBackgroundImage = scrollbarToStyle.GetComponent<Image>(); // Szukamy Image na obiekcie Scrollbara
                if (scrollbarBackgroundImage == null && scrollbarToStyle.transform.parent != null)
                {
                    // Czasami Image jest na obiekcie rodzicu Scrollbara (np. w standardowym Scrollbar UI)
                    // Chociaż w Dropdownie TMP Image jest zazwyczaj na samym Scrollbarze.
                    // Ta linijka jest raczej zbędna dla TMP_Dropdown, ale dodana na wszelki wypadek.
                    scrollbarBackgroundImage = scrollbarToStyle.transform.parent.GetComponent<Image>();
                }


                if (scrollbarBackgroundImage != null)
                {
                    scrollbarBackgroundImage.color = scrollbarTrackColor;
                }
                else
                {
                    // Dodatkowy log, jeśli Image nie znaleziono nawet na obiekcie Scrollbara
                    Debug.LogWarning("Nie znaleziono komponentu Image dla tła (track) scrollbara na obiekcie Scrollbar ani jego rodzicu. Upewnij się, że obiekt Scrollbar ma komponent Image, który renderuje tło.", this);
                }


                // 2. Stylowanie UCHWYTU (Handle) Scrollbara
                // Kolory uchwytu są zarządzane przez ColorBlock na komponencie Scrollbar
                ColorBlock scrollbarColors = scrollbarToStyle.colors;
                scrollbarColors.normalColor = scrollbarHandleNormalColor;
                scrollbarColors.highlightedColor = scrollbarHandleHighlightedColor;
                scrollbarColors.pressedColor = scrollbarHandlePressedColor;
                scrollbarColors.disabledColor = scrollbarHandleDisabledColor;
                scrollbarToStyle.colors = scrollbarColors; // Zastosuj zmodyfikowaną strukturę

                // Jeśli chcesz też stylować Image samego uchwytu (np. jego sprite) - OPCPONALNIE
                // Znajdź obiekt uchwytu - zazwyczaj jest dzieckiem scrollbara o nazwie "Handle"
                Transform handleTransform = scrollbarToStyle.transform.Find("Sliding Area/Handle"); // Typowa ścieżka
                if (handleTransform != null)
                {
                    Image handleImage = handleTransform.GetComponent<Image>();
                    // Możesz tutaj zmienić grafikę uchwytu: handleImage.sprite = yourCustomHandleSprite;
                    // Zmienianie koloru Image uchwytu bezpośrednio (handleImage.color)
                    // może kolidować z ColorBlock na komponencie Scrollbar, więc zazwyczaj
                    // lepiej stylować kolory uchwytu przez Scrollbar.colors.
                }
            }
            // else: Warningi zostały już wyświetlone w sekcji szukania scrollbara
        }


        Debug.Log("Próba zastosowania stylów Dropdowna przez skrypt: " + gameObject.name + " zakończona.");
    }
}