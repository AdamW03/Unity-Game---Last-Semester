using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NotebookManager : MonoBehaviour
{
    public static NotebookManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Wykryto drug¹ instancjê NotebookManager. Niszczenie duplikatu.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
    }

    [Header("UI Elements (Wymagane!)")]
    [SerializeField] private GameObject notebookPanel;
    [SerializeField] private TextMeshProUGUI noteContentText;
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("Feedback UI")]
    [SerializeField] private TextMeshProUGUI feedbackMessageText;
    [SerializeField] private float messageDisplayTime = 2.5f;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleNotebookKey = KeyCode.N;
    [SerializeField] private KeyCode previousPageKey = KeyCode.Q;
    [SerializeField] private KeyCode nextPageKey = KeyCode.E;

    [Header("Audio")]
    [Tooltip("DŸwiêk odtwarzany przy przewijaniu strony w notatniku.")]
    [SerializeField] private AudioClip pageTurnSoundClip;
    [Tooltip("DŸwiêk odtwarzany przy otwieraniu notatnika.")]
    [SerializeField] private AudioClip notebookOpenSoundClip; // <-- NOWE POLE
    [Tooltip("DŸwiêk odtwarzany przy zamykaniu notatnika.")]
    [SerializeField] private AudioClip notebookCloseSoundClip; // <-- NOWE POLE

    [Header("Notebook State")]
    public bool hasCollectedNotebookItem = false;

    [Header("Notebook Data")]
    public List<NoteData> collectedNotes = new List<NoteData>();

    private int currentPageIndex = -1;
    private bool isNotebookOpen = false;
    private Coroutine messageCoroutine = null;

    void Start()
    {
        bool referencesOk = ValidateReferences();
        if (!referencesOk)
        {
            enabled = false;
            return;
        }

        notebookPanel.SetActive(false);
        if (feedbackMessageText != null)
        {
            feedbackMessageText.gameObject.SetActive(false);
        }
        isNotebookOpen = false;
        currentPageIndex = -1;
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleNotebookKey))
        {
            if (!hasCollectedNotebookItem)
            {
                ShowFeedbackMessage("You have to find Notebook first!");
                return;
            }

            if (collectedNotes.Count == 0 && !isNotebookOpen) // Sprawdzamy !isNotebookOpen, aby pozwoliæ zamkn¹æ, nawet jeœli jest pusty
            {
                ShowFeedbackMessage("Notebook is empty. Find some notes!");
                // Nie otwieramy pustego UI, ale pozwalamy zamkn¹æ, jeœli by³ otwarty przez pomy³kê
                if (isNotebookOpen) ToggleNotebookUI(); // Pozwól zamkn¹æ
            }
            else
            {
                ToggleNotebookUI();
            }
        }

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

        if (isNotebookOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleNotebookUI();
        }
    }

    public void CollectNotebookItem()
    {
        if (!hasCollectedNotebookItem)
        {
            Debug.Log("Picked up notebook!");
            hasCollectedNotebookItem = true;
            ShowFeedbackMessage("Picked up notebook! Browse your notes [N].");
        }
        else
        {
            Debug.LogWarning("You already have equiped notebook.");
        }
    }

    public void AddNote(NoteData noteToAdd)
    {
        if (noteToAdd == null)
        {
            Debug.LogError("Attempting to add null no notebook!");
            return;
        }

        if (collectedNotes.Any(note => note.pageNumber == noteToAdd.pageNumber))
        {
            Debug.LogWarning($"Note with page {noteToAdd.pageNumber} ('{noteToAdd.noteTitle}') already exists in notebook.");
            return;
        }

        Debug.Log($"Added note: Page {noteToAdd.pageNumber} - {noteToAdd.noteTitle}");
        collectedNotes.Add(noteToAdd);
        collectedNotes.Sort((note1, note2) => note1.pageNumber.CompareTo(note2.pageNumber));
        ShowFeedbackMessage($"Added note: Page {noteToAdd.pageNumber}");

        if (isNotebookOpen)
        {
            currentPageIndex = collectedNotes.FindIndex(note => note == noteToAdd);
            if (currentPageIndex < 0) currentPageIndex = collectedNotes.Count - 1;
            UpdateNotebookUI();
        }
    }

    private void ToggleNotebookUI()
    {
        isNotebookOpen = !isNotebookOpen;
        notebookPanel.SetActive(isNotebookOpen);

        if (isNotebookOpen)
        {
            if (currentPageIndex < 0 && collectedNotes.Count > 0)
            {
                currentPageIndex = 0;
            }
            UpdateNotebookUI();
            PlaySound(notebookOpenSoundClip, "otwierania notatnika (notebookOpenSoundClip)"); // <-- ODTWÓRZ DWIÊK OTWIERANIA
            Debug.Log("Note UI opened.");
        }
        else
        {
            PlaySound(notebookCloseSoundClip, "zamykania notatnika (notebookCloseSoundClip)"); // <-- ODTWÓRZ DWIÊK ZAMYKANIA
            Debug.Log("Note UI closed.");
        }
    }

    private void ShowPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateNotebookUI();
            PlaySound(pageTurnSoundClip, "przewijania strony (pageTurnSoundClip)");
        }
    }

    private void ShowNextPage()
    {
        if (currentPageIndex < collectedNotes.Count - 1)
        {
            currentPageIndex++;
            UpdateNotebookUI();
            PlaySound(pageTurnSoundClip, "przewijania strony (pageTurnSoundClip)");
        }
    }

    // Zmodyfikowana metoda do odtwarzania dŸwiêków, aby by³a bardziej generyczna
    private void PlaySound(AudioClip clipToPlay, string soundDescriptionForLog)
    {
        if (SoundFXManager.Instance != null && clipToPlay != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(clipToPlay, transform, 1f);
        }
        else if (clipToPlay == null)
        {
            Debug.LogWarning($"NotebookManager: Brak przypisanego dŸwiêku {soundDescriptionForLog}.");
        }
        else if (SoundFXManager.Instance == null)
        {
            Debug.LogWarning($"NotebookManager: SoundFXManager.Instance nie znaleziony. Nie mo¿na odtworzyæ dŸwiêku {soundDescriptionForLog}.");
        }
    }

    private void UpdateNotebookUI()
    {
        if (!isNotebookOpen || collectedNotes.Count == 0)
        {
            if (isNotebookOpen)
            {
                noteContentText.text = "No notes to display.";
                pageNumberText.text = "";
            }
            return;
        }

        if (currentPageIndex < 0 || currentPageIndex >= collectedNotes.Count)
        {
            Debug.LogError($"Invalid page index: {currentPageIndex}. Note count: {collectedNotes.Count}. Reset to 0.");
            currentPageIndex = 0;
            if (collectedNotes.Count == 0) return;
        }

        NoteData currentNote = collectedNotes[currentPageIndex];
        if (currentNote != null)
        {
            noteContentText.text = currentNote.noteContent;
            pageNumberText.text = $"Page {currentPageIndex + 1}/{collectedNotes.Count} (Page {currentNote.pageNumber})";
        }
        else
        {
            Debug.LogError($"Found null at index {currentPageIndex} in list collectedNotes!");
            noteContentText.text = "Error while loading note content.";
            pageNumberText.text = "Error";
        }
    }

    private void ShowFeedbackMessage(string message)
    {
        if (feedbackMessageText == null)
        {
            Debug.Log($"Comunicate (UI unassigned): {message}");
            return;
        }

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    private IEnumerator DisplayMessageCoroutine(string message)
    {
        feedbackMessageText.text = message;
        feedbackMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDisplayTime);
        if (feedbackMessageText.gameObject.activeSelf && feedbackMessageText.text == message)
        {
            feedbackMessageText.gameObject.SetActive(false);
        }
        messageCoroutine = null;
    }

    private bool ValidateReferences()
    {
        bool ok = true;
        if (notebookPanel == null)
        {
            Debug.LogError("NotebookManager: 'Notebook Panel' is not assigned!", this);
            ok = false;
        }
        if (noteContentText == null)
        {
            Debug.LogError("NotebookManager: 'Note Content Text' is not assigned!", this);
            ok = false;
        }
        if (pageNumberText == null)
        {
            Debug.LogError("NotebookManager: 'Page Number Text' is not assigned!", this);
            ok = false;
        }
        return ok;
    }
}