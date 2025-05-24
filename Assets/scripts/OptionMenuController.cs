using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OptionMenuController : MonoBehaviour
{
    public TMP_Dropdown ResDropDown;
    public Toggle FullScreenToggle;

    Resolution[] allResolutions;
    bool isFullScreen;
    int selectedResolutionIndex;

    void Start()
    {
        isFullScreen = true;
        allResolutions = Screen.resolutions;

        List<string> resolutionOptions = new List<string>();
        selectedResolutionIndex = 0;

        // Populate dropdown with available resolutions
        for (int i = 0; i < allResolutions.Length; i++)
        {
            string option = allResolutions[i].width + " x " + allResolutions[i].height + " @ " + allResolutions[i].refreshRate + "Hz";
            resolutionOptions.Add(option);

            // Set the currently active resolution index
            if (allResolutions[i].width == Screen.currentResolution.width &&
                allResolutions[i].height == Screen.currentResolution.height &&
                allResolutions[i].refreshRate == Screen.currentResolution.refreshRate)
            {
                selectedResolutionIndex = i;
            }
        }

        ResDropDown.ClearOptions();
        ResDropDown.AddOptions(resolutionOptions);
        ResDropDown.value = selectedResolutionIndex;
        ResDropDown.RefreshShownValue();

        // Listen for dropdown and toggle changes
        ResDropDown.onValueChanged.AddListener(SetResolution);
        FullScreenToggle.isOn = isFullScreen;
        FullScreenToggle.onValueChanged.AddListener(SetFullScreen);
    }

    // Called when resolution dropdown is changed
    public void SetResolution(int resolutionIndex)
    {
        Resolution res = allResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate);
        selectedResolutionIndex = resolutionIndex;
        Debug.Log($"Resolution changed to: {res.width}x{res.height} @ {res.refreshRate}Hz");
    }

    // Called when fullscreen toggle is changed
    public void SetFullScreen(bool isFullscreen)
    {
        isFullScreen = isFullscreen;
        Resolution res = allResolutions[selectedResolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullScreen, res.refreshRate);
        Debug.Log("Fullscreen set to: " + isFullScreen);
    }
}
