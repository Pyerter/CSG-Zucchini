using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;

    private Resolution[] _Resolutions;
    public TMP_Dropdown resolutionDropdown;

    private void Start()
    {
        _Resolutions = Screen.resolutions;

        int currentResolutionIndex = 0;
        List<string> options = new List<string>();
        for (int i = 0; i < _Resolutions.Length; i++)
        {
            string option = _Resolutions[i].width + " x " + _Resolutions[i].height;
            options.Add(option);

            if (_Resolutions[i].width == Screen.currentResolution.width && _Resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void ChangeVolume(float volume)
    {
        Debug.Log("Volume is: " + volume);
        audioMixer.SetFloat("_MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetGraphicsQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = _Resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}
