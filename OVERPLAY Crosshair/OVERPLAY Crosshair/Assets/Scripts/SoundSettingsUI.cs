using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Sound Settings UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle soundEnabledToggle;
    [SerializeField] private TMP_Text volumeText;
    
    void Start()
    {
        // Initialize UI with current settings
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("SoundVolume", 0.5f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        if (soundEnabledToggle != null)
        {
            soundEnabledToggle.isOn = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            soundEnabledToggle.onValueChanged.AddListener(OnSoundEnabledChanged);
        }
        
        UpdateVolumeText();
    }
    
    void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }
        PlayerPrefs.SetFloat("SoundVolume", value);
        PlayerPrefs.Save();
        UpdateVolumeText();
    }
    
    void OnSoundEnabledChanged(bool enabled)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSoundEnabled(enabled);
        }
        PlayerPrefs.SetInt("SoundEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    void UpdateVolumeText()
    {
        if (volumeText != null && volumeSlider != null)
        {
            int volumePercent = Mathf.RoundToInt(volumeSlider.value * 100f);
            volumeText.text = $"Volume: {volumePercent}%";
        }
    }
}
