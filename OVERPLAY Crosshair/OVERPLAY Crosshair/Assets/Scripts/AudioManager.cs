using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonReleaseSound;
    [SerializeField] private AudioClip sliderChangeSound;
    [SerializeField] private AudioClip sliderReleaseSound;
    
    [Header("Audio Settings")]
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private bool enableSounds = true;
    
    private AudioSource audioSource;
    
    void Awake()
    {
        // Create audio source if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        
        // Generate default sounds if none are assigned
        if (buttonClickSound == null)
            buttonClickSound = ToneGenerator.GenerateClickSound();
        if (buttonReleaseSound == null)
            buttonReleaseSound = ToneGenerator.GenerateReleaseSound();
        if (sliderChangeSound == null)
            sliderChangeSound = ToneGenerator.GenerateSliderSound();
        if (sliderReleaseSound == null)
            sliderReleaseSound = ToneGenerator.GenerateSliderReleaseSound();
    }
    
    public void PlayButtonClick()
    {
        if (enableSounds && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, volume);
        }
    }
    
    public void PlayButtonRelease()
    {
        if (enableSounds && buttonReleaseSound != null)
        {
            audioSource.PlayOneShot(buttonReleaseSound, volume);
        }
    }
    
    public void PlaySliderChange()
    {
        if (enableSounds && sliderChangeSound != null)
        {
            audioSource.PlayOneShot(sliderChangeSound, volume * 0.3f); // Quieter for slider changes
        }
    }
    
    public void PlaySliderRelease()
    {
        if (enableSounds && sliderReleaseSound != null)
        {
            audioSource.PlayOneShot(sliderReleaseSound, volume * 0.5f);
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
    
    public void SetSoundEnabled(bool enabled)
    {
        enableSounds = enabled;
    }
    
    // Static instance for easy access
    public static AudioManager Instance { get; private set; }
    
    void OnEnable()
    {
        Instance = this;
    }
    
    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
