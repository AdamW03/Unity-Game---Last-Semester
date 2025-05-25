using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class OptionPauseMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("UI Elements")]
    public TMP_Dropdown ResDropDown;
    public Toggle FullScreenToggle;
    public Button backButton; // Przycisk "Wróæ" (do menu pauzy)

    [Header("External Managers (Wymagane!)")]
    [Tooltip("Referencja do skryptu PauseMenuManager.")]
    public PauseMenuManager pauseMenuManager; // <-- NOWA REFERENCJA

    Resolution[] allResolutions;
    bool isFullScreen;
    int selectedResolutionIndex;

    void Start()
    {
        // --- Sprawdzenie kluczowych referencji ---
        if (pauseMenuManager == null)
        {
            Debug.LogError("OptionMenuController: PauseMenuManager nie jest przypisany w Inspektorze! Funkcja powrotu do menu pauzy nie bêdzie dzia³aæ.", this);
            // Mo¿na by wy³¹czyæ przycisk "Wróæ", jeœli manager nie jest przypisany
            if (backButton != null) backButton.interactable = false;
        }
        if (ResDropDown == null || FullScreenToggle == null)
        {
            Debug.LogError("OptionMenuController: Brakuje referencji do ResDropDown lub FullScreenToggle!", this);
            enabled = false; // Wy³¹cz skrypt, jeœli brakuje podstawowych elementów UI
            return;
        }
        // -----------------------------------------

        isFullScreen = Screen.fullScreen;
        FullScreenToggle.isOn = isFullScreen;

        allResolutions = Screen.resolutions;
        List<string> resolutionOptions = new List<string>();
        selectedResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            string option = allResolutions[i].width + " x " + allResolutions[i].height;
            if (allResolutions[i].refreshRate > 0) // Niektóre rozdzielczoœci mog¹ zwracaæ 0Hz
            {
                option += " @ " + allResolutions[i].refreshRate + "Hz";
            }
            resolutionOptions.Add(option);

            // SprawdŸ, czy rozdzielczoœæ pasuje (bez refresh rate, bo mo¿e byæ ró¿ne)
            if (allResolutions[i].width == Screen.currentResolution.width &&
                allResolutions[i].height == Screen.currentResolution.height)
            {
                // Jeœli pasuje, sprawdŸ te¿ refresh rate, jeœli jest dostêpne
                if (allResolutions[i].refreshRate == 0 || allResolutions[i].refreshRate == Screen.currentResolution.refreshRate)
                {
                    selectedResolutionIndex = i;
                }
            }
        }

        ResDropDown.ClearOptions();
        ResDropDown.AddOptions(resolutionOptions);
        ResDropDown.value = selectedResolutionIndex;
        ResDropDown.RefreshShownValue();

        FullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        ResDropDown.onValueChanged.AddListener(SetResolution);

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners(); // Usuñ poprzednie listenery na wszelki wypadek
            backButton.onClick.AddListener(BackToPauseMenu); // Zmieniono na BackToPauseMenu
            Debug.Log("OptionMenuController: Listener dla przycisku 'Wróæ' (BackToPauseMenu) dodany.");
        }
        else
        {
            Debug.LogWarning("OptionMenuController: Przycisk 'Wróæ' (backButton) nie jest przypisany w Inspektorze.");
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        if (allResolutions == null || resolutionIndex < 0 || resolutionIndex >= allResolutions.Length) return;

        Resolution res = allResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate > 0 ? (int)res.refreshRate : 0); // U¿yj 0 jeœli refreshRate to 0
        selectedResolutionIndex = resolutionIndex;
        Debug.Log($"Rozdzielczoœæ zmieniona na: {res.width}x{res.height} @ {(res.refreshRate > 0 ? res.refreshRate.ToString() : "domyœlny")}Hz, Pe³ny ekran: {isFullScreen}");
    }

    public void SetFullScreen(bool isFullscreenValue)
    {
        isFullScreen = isFullscreenValue;
        // Zastosuj z aktualnie wybran¹ rozdzielczoœci¹
        if (allResolutions != null && selectedResolutionIndex >= 0 && selectedResolutionIndex < allResolutions.Length)
        {
            Resolution res = allResolutions[selectedResolutionIndex];
            Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate > 0 ? (int)res.refreshRate : 0);
            Debug.Log("Pe³ny ekran ustawiony na: " + isFullScreen);
        }
        else
        {
            // Jeœli nie ma wybranej rozdzielczoœci, u¿yj aktualnej rozdzielczoœci ekranu
            Screen.fullScreen = isFullScreen;
            Debug.Log("Pe³ny ekran ustawiony na: " + isFullScreen + " (u¿yto aktualnej rozdzielczoœci)");
        }
    }

    public void BackToPauseMenu()
    {
        if (pauseMenuManager != null)
        {
            // Wywo³aj metodê w PauseMenuManager, która ukrywa panel opcji
            // i pokazuje panel pauzy. Nazwa tej metody zale¿y od Twojej implementacji
            // PauseMenuManager. Przyk³ad:
            pauseMenuManager.CloseSettingsPanel(); // Zak³adaj¹c, ¿e masz tak¹ metodê
            Debug.Log("OptionMenuController: Powrót do menu pauzy...");
        }
        else
        {
            Debug.LogError("OptionMenuController: Nie mo¿na wróciæ do menu pauzy - PauseMenuManager nie jest przypisany!");
            // Awaryjnie: mo¿na by wróciæ do g³ównego menu lub nic nie robiæ
            // BackToMainMenu();
        }
    }
}
