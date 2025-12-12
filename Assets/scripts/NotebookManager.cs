using UnityEngine;
using TMPro;
using System.Collections; 
using System.Collections.Generic;
using System.Linq; 

public class NotebookManager : MonoBehaviour
{
    
    // Prosty wzorzec Singleton dla ³atwego dostêpu z innych skryptów
    public static NotebookManager Instance { get; private set; }

    void Awake()
    {
        // Zapewnienie istnienia tylko jednej instancji NotebookManager
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Wykryto drug¹ instancjê NotebookManager. Niszczenie duplikatu.", this);
            Destroy(gameObject); // Zniszcz duplikat
        }
        else
        {
            Instance = this;
            // Opcjonalnie: zachowaj miêdzy scenami, jeœli ekwipunek/notatki maj¹ byæ trwa³e
            // DontDestroyOnLoad(gameObject);
        }
    }


    [Header("UI Elements (Wymagane!)")]
    [Tooltip("G³ówny panel UI notatnika, który bêdzie pokazywany/ukrywany.")]
    [SerializeField] private GameObject notebookPanel;

    [Tooltip("Element TextMeshPro do wyœwietlania treœci aktualnej notatki.")]
    [SerializeField] private TextMeshProUGUI noteContentText;

    [Tooltip("Element TextMeshPro do wyœwietlania numeru strony/indeksu.")]
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("Feedback UI")]
    [Tooltip("Element TextMeshPro do wyœwietlania tymczasowych komunikatów (np. 'Musisz znaleŸæ notatnik'). Przypisz obiekt TextMeshPro z g³ównego Canvasa.")]
    [SerializeField] private TextMeshProUGUI feedbackMessageText;

    [Tooltip("Jak d³ugo (w sekundach) komunikaty zwrotne maj¹ byæ widoczne.")]
    [SerializeField] private float messageDisplayTime = 2.5f;

    [Header("Controls")]
    [Tooltip("Klawisz do otwierania/zamykania interfejsu notatnika.")]
    [SerializeField] private KeyCode toggleNotebookKey = KeyCode.N;

    [Tooltip("Klawisz do przechodzenia do poprzedniej strony notatki.")]
    [SerializeField] private KeyCode previousPageKey = KeyCode.Q;

    [Tooltip("Klawisz do przechodzenia do nastêpnej strony notatki.")]
    [SerializeField] private KeyCode nextPageKey = KeyCode.E;

    [Header("Notebook State")]
    [Tooltip("Czy gracz podniós³ ju¿ fizyczny przedmiot 'Notatnik'? Ta flaga jest ustawiana przez metodê CollectNotebookItem().")]
    public bool hasCollectedNotebookItem = false; // Publiczne do wgl¹du, ale zarz¹dzane przez metodê

    [Header("Notebook Data")]
    [Tooltip("Lista zebranych notatek (zasobów NoteData). Zarz¹dzana automatycznie przez skrypt.")]
    public List<NoteData> collectedNotes = new List<NoteData>(); // Publiczne do wgl¹du w Inspektorze

    // Indeks aktualnie wyœwietlanej notatki w posortowanej liœcie collectedNotes. -1 oznacza brak wybranej.
    private int currentPageIndex = -1;
    // Czy panel UI notatnika jest aktualnie widoczny?
    private bool isNotebookOpen = false;
    // Referencja do aktywnej korutyny wyœwietlaj¹cej komunikat (aby móc j¹ zatrzymaæ).
    private Coroutine messageCoroutine = null;


    void Start()
    {
        // --- Sprawdzenie kluczowych referencji UI przy starcie ---
        bool referencesOk = ValidateReferences();

        // Jeœli brakuje kluczowych referencji, wy³¹cz skrypt, aby unikn¹æ b³êdów w trakcie gry
        if (!referencesOk)
        {
            enabled = false; // Wy³¹cz ten komponent
            return;
        }

        // --- Inicjalizacja stanu pocz¹tkowego ---
        notebookPanel.SetActive(false); // Upewnij siê, ¿e panel notatnika jest ukryty
        if (feedbackMessageText != null)
        {
            feedbackMessageText.gameObject.SetActive(false); // Ukryj tekst komunikatu
        }
        isNotebookOpen = false; // Stan UI jest zamkniêty
        currentPageIndex = -1; // ¯adna strona nie jest wybrana
    }

    void Update()
    {
        // Obs³uga wejœcia gracza w ka¿dej klatce
        HandleInput();
    }


    private void HandleInput()
    {
        // --- Otwieranie/Zamykanie Notatnika Klawiszem 'N' (lub innym zdefiniowanym) ---
        if (Input.GetKeyDown(toggleNotebookKey))
        {
            // 1. SprawdŸ, czy gracz posiada fizyczny przedmiot "Notatnik"
            if (!hasCollectedNotebookItem)
            {
                // Jeœli nie, poka¿ komunikat i przerwij dalsze dzia³anie dla tego klawisza
                ShowFeedbackMessage("Musisz najpierw znaleŸæ Notatnik!");
                return;
            }

            // 2. SprawdŸ, czy gracz zebra³ jakiekolwiek notatki (kartki)
            // (Dzia³amy tylko jeœli gracz ma ju¿ przedmiot Notatnik)
            if (collectedNotes.Count == 0)
            {
                // Jeœli ma Notatnik, ale jest pusty, poka¿ odpowiedni komunikat
                ShowFeedbackMessage("Notatnik jest pusty. ZnajdŸ jakieœ notatki!");
                // Nie otwieramy pustego UI
            }
            else
            {
                // Jeœli gracz ma Notatnik ORAZ zebra³ notatki, prze³¹cz widocznoœæ UI
                ToggleNotebookUI();
            }
        }

        // --- Nawigacja Stronami Klawiszami 'Q'/'E' (tylko gdy notatnik jest otwarty i ma wiêcej ni¿ 1 stronê) ---
        if (isNotebookOpen && collectedNotes.Count > 1)
        {
            if (Input.GetKeyDown(previousPageKey))
            {
                ShowPreviousPage();
            }
            else if (Input.GetKeyDown(nextPageKey))
            {
                ShowNextPage();
            }
        }

        // --- Zamykanie Notatnika Klawiszem Escape (tylko gdy jest otwarty) ---
        if (isNotebookOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            // U¿ywamy tej samej metody co dla klawisza 'N' do zamkniêcia
            ToggleNotebookUI();
        }
    }

    public void CollectNotebookItem()
    {
        // Ustaw flagê tylko jeœli jeszcze nie zosta³a ustawiona
        if (!hasCollectedNotebookItem)
        {
            Debug.Log("Podniesiono przedmiot Notatnik!");
            hasCollectedNotebookItem = true; // Ustaw flagê posiadania
            // Poinformuj gracza
            ShowFeedbackMessage("Zebra³eœ Notatnik! Mo¿esz teraz przegl¹daæ notatki [N].");
        }
        else
        {
            // Opcjonalnie: obs³u¿ próbê ponownego podniesienia
            Debug.LogWarning("Próbowano ponownie zebraæ przedmiot Notatnik.");
            // Mo¿na pokazaæ komunikat "Ju¿ masz Notatnik"
            // ShowFeedbackMessage("Ju¿ masz Notatnik.");
        }
    }

    /// <param name="noteToAdd">Zasób NoteData podniesionej notatki.</param>
    public void AddNote(NoteData noteToAdd)
    {
        // Sprawdzenie, czy przekazano prawid³owe dane
        if (noteToAdd == null)
        {
            Debug.LogError("Próbowano dodaæ null jako NoteData do notatnika!");
            return;
        }

        // Opcjonalnie: Sprawdzenie, czy notatka o tym samym numerze strony ju¿ istnieje
        if (collectedNotes.Any(note => note.pageNumber == noteToAdd.pageNumber))
        {
            Debug.LogWarning($"Notatka ze stron¹ {noteToAdd.pageNumber} ('{noteToAdd.noteTitle}') ju¿ istnieje w notatniku. Pomijanie.");
            // Mo¿na poinformowaæ gracza
            // ShowFeedbackMessage($"Masz ju¿ notatkê ze strony {noteToAdd.pageNumber}.");
            return; // Nie dodawaj duplikatu strony
        }

        // Dodaj notatkê do listy
        Debug.Log($"Dodano notatkê: Strona {noteToAdd.pageNumber} - {noteToAdd.noteTitle}");
        collectedNotes.Add(noteToAdd);

        // --- WA¯NE: Sortuj listê notatek po numerze strony ---
        // U¿ywamy wyra¿enia lambda do porównania pageNumber dwóch notatek
        collectedNotes.Sort((note1, note2) => note1.pageNumber.CompareTo(note2.pageNumber));
        // -----------------------------------------------------

        // Poinformuj gracza o dodaniu notatki
        ShowFeedbackMessage($"Dodano notatkê: Strona {noteToAdd.pageNumber}");

        // Jeœli notatnik jest akurat otwarty, odœwie¿ jego widok
        if (isNotebookOpen)
        {
            // ZnajdŸ indeks nowo dodanej (i ju¿ posortowanej) notatki
            currentPageIndex = collectedNotes.FindIndex(note => note == noteToAdd);
            // Zabezpieczenie: jeœli FindIndex nie znajdzie (nie powinno siê zdarzyæ), ustaw na ostatni¹
            if (currentPageIndex < 0) currentPageIndex = collectedNotes.Count - 1;
            // Zaktualizuj UI, aby pokazaæ now¹ stronê lub odœwie¿on¹ numeracjê
            UpdateNotebookUI();
        }
    }

    private void ToggleNotebookUI()
    {
        isNotebookOpen = !isNotebookOpen; // Odwróæ stan (otwarty/zamkniêty)
        notebookPanel.SetActive(isNotebookOpen); // Poka¿ lub ukryj panel

        if (isNotebookOpen) // Jeœli w³aœnie otworzyliœmy panel
        {
            // Ustaw indeks na pierwsz¹ stronê, jeœli nie by³ jeszcze ustawiony (np. przy pierwszym otwarciu)
            if (currentPageIndex < 0)
            {
                currentPageIndex = 0; // Poka¿ pierwsz¹ notatkê
            }
            UpdateNotebookUI(); // Zaktualizuj treœæ i numer strony
            // Opcjonalnie: Mo¿na tu zatrzymaæ czas gry
            // Time.timeScale = 0f;
            Debug.Log("Otwarto UI Notatnika.");
        }
        else // Jeœli w³aœnie zamknêliœmy panel
        {
            // Opcjonalnie: Mo¿na tu wznowiæ czas gry
            // Time.timeScale = 1f;
            Debug.Log("Zamkniêto UI Notatnika.");
        }
    }

    private void ShowPreviousPage()
    {
        // SprawdŸ, czy nie jesteœmy ju¿ na pierwszej stronie (indeks 0)
        if (currentPageIndex > 0)
        {
            currentPageIndex--; // Zmniejsz indeks
            UpdateNotebookUI(); // Zaktualizuj wyœwietlan¹ treœæ
        }
        // Opcjonalnie: Mo¿na dodaæ zawijanie do ostatniej strony
        // else if (collectedNotes.Count > 1) { currentPageIndex = collectedNotes.Count - 1; UpdateNotebookUI(); }
    }

    private void ShowNextPage()
    {
        // SprawdŸ, czy nie jesteœmy ju¿ na ostatniej stronie
        if (currentPageIndex < collectedNotes.Count - 1)
        {
            currentPageIndex++; // Zwiêksz indeks
            UpdateNotebookUI(); // Zaktualizuj wyœwietlan¹ treœæ
        }
        // Opcjonalnie: Mo¿na dodaæ zawijanie do pierwszej strony
        // else if (collectedNotes.Count > 1) { currentPageIndex = 0; UpdateNotebookUI(); }
    }

    private void UpdateNotebookUI()
    {
        // SprawdŸ, czy UI jest otwarte i czy mamy jakiekolwiek notatki
        if (!isNotebookOpen || collectedNotes.Count == 0)
        {
            // Jeœli UI jest otwarte, ale lista jest pusta (co nie powinno siê zdarzyæ przy obecnej logice otwierania),
            // poka¿ stan b³êdu/pusty.
            if (isNotebookOpen)
            {
                noteContentText.text = "Brak notatek do wyœwietlenia."; // Lub pusty string ""
                pageNumberText.text = "";
            }
            return; // Nie rób nic wiêcej
        }

        // SprawdŸ poprawnoœæ indeksu (dodatkowe zabezpieczenie)
        if (currentPageIndex < 0 || currentPageIndex >= collectedNotes.Count)
        {
            Debug.LogError($"Nieprawid³owy indeks strony notatki: {currentPageIndex}. Liczba notatek: {collectedNotes.Count}. Resetowanie do 0.");
            currentPageIndex = 0; // Spróbuj zresetowaæ do pierwszej strony
                                  // Jeœli po resecie nadal nie ma notatek (skrajny przypadek), wyjdŸ
            if (collectedNotes.Count == 0) return;
        }

        // Pobierz dane notatki dla aktualnego indeksu
        NoteData currentNote = collectedNotes[currentPageIndex];

        // Wyœwietl dane w UI
        if (currentNote != null)
        {
            // Ustaw treœæ notatki
            noteContentText.text = currentNote.noteContent;
            // Ustaw numeracjê stron (np. "Notatka 3/10 (Strona 15)")
            pageNumberText.text = $"Notatka {currentPageIndex + 1}/{collectedNotes.Count} (Strona {currentNote.pageNumber})";
        }
        else
        {
            // Obs³uga b³êdu, jeœli element na liœcie jest null (nie powinno siê zdarzyæ)
            Debug.LogError($"Znaleziono null na indeksie {currentPageIndex} w liœcie collectedNotes!");
            noteContentText.text = "B³¹d ³adowania treœci notatki.";
            pageNumberText.text = "B³¹d";
        }
    }

    /// <param name="message">Tekst komunikatu do wyœwietlenia.</param>
    private void ShowFeedbackMessage(string message)
    {
        // SprawdŸ, czy referencja do UI komunikatu jest ustawiona
        if (feedbackMessageText == null)
        {
            Debug.Log($"Komunikat (UI nieprzypisane): {message}"); // Wypisz w konsoli, jeœli UI brakuje
            return;
        }

        // Jeœli poprzedni komunikat (korutyna) jeszcze dzia³a, zatrzymaj go
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null; // Zresetuj referencjê
        }
        // Uruchom now¹ korutynê do wyœwietlenia i ukrycia bie¿¹cego komunikatu
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    /// <param name="message">Tekst komunikatu do pokazania i ukrycia.</param>
    private IEnumerator DisplayMessageCoroutine(string message)
    {
        // Ustaw tekst i poka¿ obiekt UI
        feedbackMessageText.text = message;
        feedbackMessageText.gameObject.SetActive(true);

        // Poczekaj okreœlony czas
        yield return new WaitForSeconds(messageDisplayTime);

        // Ukryj obiekt UI tylko jeœli tekst siê nie zmieni³ w miêdzyczasie
        // (zapobiega ukryciu nowszego komunikatu przez starsz¹ korutynê)
        if (feedbackMessageText.gameObject.activeSelf && feedbackMessageText.text == message)
        {
            feedbackMessageText.gameObject.SetActive(false);
        }
        // Zresetuj referencjê do korutyny po zakoñczeniu
        messageCoroutine = null;
    }

    private bool ValidateReferences()
    {
        bool ok = true;
        if (notebookPanel == null)
        {
            Debug.LogError("NotebookManager: 'Notebook Panel' nie jest przypisany!", this);
            ok = false;
        }
        if (noteContentText == null)
        {
            Debug.LogError("NotebookManager: 'Note Content Text' nie jest przypisany!", this);
            ok = false;
        }
        if (pageNumberText == null)
        {
            Debug.LogError("NotebookManager: 'Page Number Text' nie jest przypisany!", this);
            ok = false;
        }
        // feedbackMessageText jest opcjonalny, wiêc nie sprawdzamy go tutaj jako krytycznego
        return ok;
    }

}