using UnityEngine;
using UnityEngine.UI;
using TMPro; // We need this to talk to TextMeshPro Dropdowns!

public class OptionsResetButton : MonoBehaviour
{
    [Header("Options Screens (Tabs)")]
    public GameObject gameplayScreen;
    public GameObject videoScreen;
    public GameObject audioScreen;
    public GameObject keybindScreen;

    [Header("Audio UI Elements")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider narrationSlider;

    [Header("Gameplay UI Elements")]
    public Slider shakeSlider;

    [Header("Video UI Elements")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown graphicsDropdown;
    public TMP_Dropdown screenTypeDropdown;

    public void ResetCurrentTab()
    {
        if (audioScreen != null && audioScreen.activeInHierarchy)
        {
            ResetAudioSettings();
        }
        else if (videoScreen != null && videoScreen.activeInHierarchy)
        {
            ResetVideoSettings();
        }
        else if (gameplayScreen != null && gameplayScreen.activeInHierarchy)
        {
            ResetGameplaySettings();
        }
        else if (keybindScreen != null && keybindScreen.activeInHierarchy)
        {
            ResetKeybindSettings();
        }
    }

    private void ResetAudioSettings()
    {
        if (masterSlider != null) masterSlider.value = 0.75f;
        if (musicSlider != null) musicSlider.value = 0.75f;
        if (sfxSlider != null) sfxSlider.value = 0.75f;
        if (narrationSlider != null) narrationSlider.value = 0.75f;
    }

    private void ResetVideoSettings()
    {

        if (screenTypeDropdown != null)
        {
            screenTypeDropdown.value = 0; // Default to 'Fullscreen'
            screenTypeDropdown.RefreshShownValue(); // Forces the UI text to update
        }

        if (graphicsDropdown != null)
        {
            graphicsDropdown.value = 1; // Default to Medium
            graphicsDropdown.RefreshShownValue();
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = 9;
            resolutionDropdown.RefreshShownValue();
        }

        Debug.Log("Video settings reset to default.");
    }

    private void ResetGameplaySettings()
    {
        if (shakeSlider != null) shakeSlider.value = 0.75f;

        Debug.Log("Gameplay settings reset to default.");
    }

    private void ResetKeybindSettings()
    {
        // Ready for when you build the keybinds tab!
        Debug.Log("Keybinds reset to default.");
    }
}