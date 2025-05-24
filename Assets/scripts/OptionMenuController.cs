using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class OptionMenuController : MonoBehaviour
{
    public TMP_Dropdown ResDropDown;
    public Toggle FullScreenToggle;
    public Button backButton; // <-- ADDED THIS VARIABLE

    Resolution[] allResolutions;
    bool isFullScreen;
    int selectedResolutionIndex;

    void Start()
    {
        isFullScreen = Screen.fullScreen;
        FullScreenToggle.isOn = isFullScreen;

        allResolutions = Screen.resolutions;

        List<string> resolutionOptions = new List<string>();
        selectedResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            string option = allResolutions[i].width + " x " + allResolutions[i].height;
            if (allResolutions[i].refreshRate > 0)
            {
                option += " @ " + allResolutions[i].refreshRate + "Hz";
            }
            resolutionOptions.Add(option);

            bool isCurrent = allResolutions[i].width == Screen.currentResolution.width &&
                             allResolutions[i].height == Screen.currentResolution.height;
            if (isCurrent)
            {
                selectedResolutionIndex = i;
            }
        }

        ResDropDown.ClearOptions();
        ResDropDown.AddOptions(resolutionOptions);

        ResDropDown.value = selectedResolutionIndex;
        ResDropDown.RefreshShownValue();

        FullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        ResDropDown.onValueChanged.AddListener(SetResolution);

        // <-- ADDED THIS LISTENER
        if (backButton != null) // Optional: Check if button is assigned to prevent errors
        {
            backButton.onClick.AddListener(Back);
            Debug.Log("Back button listener added.");
        }
        else
        {
            Debug.LogWarning("Back button is not assigned in the Inspector.");
        }
        // -->
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution res = allResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate);
        selectedResolutionIndex = resolutionIndex;
        Debug.Log($"Resolution changed to: {res.width}x{res.height} @ {res.refreshRate}Hz");
    }

    public void SetFullScreen(bool isFullscreenValue)
    {
        isFullScreen = isFullscreenValue;
        Resolution res = allResolutions[selectedResolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate);
        Debug.Log("Fullscreen set to: " + isFullScreen);
    }

    public void Back()
    {
        SceneManager.LoadScene("menu");
        Debug.Log("Returning to menu scene...");
    }
}