﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class DialogBoxSettings : DialogBox
{
    [SerializeField]
    private Toggle languageToggle;
    [SerializeField]
    private GameObject langDropDown;

    [SerializeField]
    private Toggle fpsToggle;
    [SerializeField]
    private GameObject fpsObject;

    [SerializeField]
    private Toggle fullScreenToggle;

    [SerializeField]
    private Slider musicVolume;

    [SerializeField]
    private Resolution[] myResolutions;
    [SerializeField]
    private Dropdown resolutionDropdown;

    [SerializeField]
    private Dropdown aliasingDropdown;
    [SerializeField]
    private Dropdown vSyncDropdown;
    [SerializeField]
    private Dropdown qualityDropdown;

    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private Button saveButton;

    private void OnEnable()
    {
        myResolutions = Screen.resolutions;
        closeButton.onClick.AddListener(OnClickClose);
        saveButton.onClick.AddListener(OnClickSave);

        fpsToggle.onValueChanged.AddListener(arg0 => OnFPSToggle());
        languageToggle.onValueChanged.AddListener(arg0 => OnLangageToggle());
        fullScreenToggle.onValueChanged.AddListener(arg0 => OnFullScreenToggle());
        resolutionDropdown.onValueChanged.AddListener(arg0 => OnResolutionChange());
        aliasingDropdown.onValueChanged.AddListener(arg0 => OnAliasingChange());
        vSyncDropdown.onValueChanged.AddListener(arg0 => OnVSyncChange());
        qualityDropdown.onValueChanged.AddListener(arg0 => OnQualityChange());
        musicVolume.onValueChanged.AddListener(arg0 => OnMusicChange());

        CreateResolutionDropDown();
        LoadSetting();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Close();
        }
    }

    public void OnLangageToggle()
    {
        langDropDown.SetActive(languageToggle.isOn);
    }

    public void OnFPSToggle()
    {
        fpsObject.SetActive(fpsToggle.isOn);
    }

    public void OnFullScreenToggle()
    {
        // TODO : Implement full screen toggle
    }

    public void OnQualityChange()
    {
        QualitySettings.masterTextureLimit = qualityDropdown.options.Count - 1 - qualityDropdown.value;
    }

    public void OnVSyncChange()
    {
        // TODO : Implement VSync changes
    }

    public void OnResolutionChange()
    {
        // TODO : Implement Resolution changes
    }

    public void OnAliasingChange()
    {
        // TODO : Implement AA changes
    }

    public void OnMusicChange()
    {
        // TODO : Implement Music changes
    }

    public void OnClickClose()
    {
        Close();
    }

    public void OnClickSave()
    {
        this.Close();
        SaveSetting();
    }

    public void SaveSetting()
    {
        GameSettings.Set("DialogBoxSettings_musicVolume", musicVolume.normalizedValue);

        GameSettings.Set("DialogBoxSettings_langToggle", languageToggle.isOn);
        GameSettings.Set("DialogBoxSettings_fpsToggle", fpsToggle.isOn);
        GameSettings.Set("DialogBoxSettings_fullScreenToggle", fullScreenToggle.isOn);

        GameSettings.Set("DialogBoxSettings_qualityDropdown", qualityDropdown.value);
        GameSettings.Set("DialogBoxSettings_vSyncDropdown", vSyncDropdown.value);
        GameSettings.Set("DialogBoxSettings_resolutionDropdown", resolutionDropdown.value);
        GameSettings.Set("DialogBoxSettings_aliasingDropdown", aliasingDropdown.value);
    }

    private void LoadSetting()
    {
        musicVolume.normalizedValue = GameSettings.Get("DialogBoxSettings_musicVolume", 0.5f);

        languageToggle.isOn = GameSettings.Get("DialogBoxSettings_langToggle", true);
        fpsToggle.isOn = GameSettings.Get("DialogBoxSettings_fpsToggle", true);
        fullScreenToggle.isOn = GameSettings.Get("DialogBoxSettings_fullScreenToggle", true);

        qualityDropdown.value = GameSettings.Get("DialogBoxSettings_qualityDropdown", 0);
        vSyncDropdown.value = GameSettings.Get("DialogBoxSettings_vSyncDropdown", 0);
        resolutionDropdown.value = GameSettings.Get("DialogBoxSettings_resolutionDropdown", 0);
        aliasingDropdown.value = GameSettings.Get("DialogBoxSettings_aliasingDropdown", 0);
    }

    private void CreateResolutionDropDown()
    {
        List<string> myResolutionStrings = myResolutions.Select(resolution => resolution.ToString()).ToList();
        resolutionDropdown.AddOptions(myResolutionStrings);
    }
}