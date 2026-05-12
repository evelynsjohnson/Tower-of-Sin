using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Screen")]
    public AudioMixer mainAudioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider narrationSlider;

    [Header("Video Screen")]
    public TMP_Dropdown screenTypeDropdown;

    private void Start()
    {
        // AUDIO LOADING
        // Load saved volumes (default to 0.75f if not set)
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float savedBGMusic = PlayerPrefs.GetFloat("BGMusicVolume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        float savedNarration = PlayerPrefs.GetFloat("NarrationVolume", 0.75f);

        // Visually update the sliders
        if (masterSlider != null) masterSlider.value = savedMaster;
        if (musicSlider != null) musicSlider.value = savedBGMusic;
        if (sfxSlider != null) sfxSlider.value = savedSFX;
        if (narrationSlider != null) narrationSlider.value = savedNarration;

        // Apply the volumes to the AudioMixer
        SetMasterVolume(savedMaster);
        SetBGMusicVolume(savedBGMusic);
        SetSFXVolume(savedSFX);
        SetNarrationVolume(savedNarration);

        // VIDEO LOADING
        int savedScreenMode = PlayerPrefs.GetInt("ScreenMode", 0);

        if (screenTypeDropdown != null)
        {
            screenTypeDropdown.value = savedScreenMode;
            screenTypeDropdown.RefreshShownValue();
        }

        SetScreenMode(savedScreenMode);
    }

    public void SetMasterVolume(float value)
    {
        // MathF.Log10(value) * 20 is used to convert a linear 0.0001 - 1 slider to logarithmic decibels
        float dbValue = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mainAudioMixer.SetFloat("MasterVolume", dbValue);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetBGMusicVolume(float value)
    {
        float dbValue = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mainAudioMixer.SetFloat("BGMusicVolume", dbValue);
        PlayerPrefs.SetFloat("BGMusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        float dbValue = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mainAudioMixer.SetFloat("SFXVolume", dbValue);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
    public void SetNarrationVolume(float value)
    {
        float dbValue = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mainAudioMixer.SetFloat("NarrationVolume", dbValue);
        PlayerPrefs.SetFloat("NarrationVolume", value);
    }


    public void SetScreenMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0: // Fullscreen
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // Windowed
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2: // Windowed Borderless
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        // Save the player's preference
        PlayerPrefs.SetInt("ScreenMode", modeIndex);
    }
}