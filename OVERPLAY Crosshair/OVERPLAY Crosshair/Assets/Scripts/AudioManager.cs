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
    [SerializeField] private float pitchVariation = 0.07f;
    
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
        
        // FIXED: Prevent this app from muting other applications
        audioSource.ignoreListenerVolume = true; // Don't affect global audio
        audioSource.ignoreListenerPause = true;  // Don't pause when other apps pause
        audioSource.priority = 0; // High priority to ensure it plays
        
        // FIXED: Additional settings to prevent interference with other apps
        audioSource.spatialBlend = 0f; // 2D sound (not 3D)
        audioSource.dopplerLevel = 0f; // No doppler effect
        audioSource.reverbZoneMix = 0f; // No reverb
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 500f;
        audioSource.minDistance = 1f;
        
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
    
    private void PlaySoundWithRandomPitch(AudioClip clip, float volumeMultiplier = 1.0f)
    {
        if (enableSounds && clip != null)
        {
            // Set random pitch variation
            float randomPitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.pitch = randomPitch;
            
            // Play the sound
            audioSource.PlayOneShot(clip, volume * volumeMultiplier);
            
            // Reset pitch back to normal
            audioSource.pitch = 1.0f;
        }
    }
    
    public void PlayButtonClick()
    {
        PlaySoundWithRandomPitch(buttonClickSound, 1.0f);
    }
    
    public void PlayButtonRelease()
    {
        PlaySoundWithRandomPitch(buttonReleaseSound, 1.0f);
    }
    
    public void PlaySliderChange()
    {
        PlaySoundWithRandomPitch(sliderChangeSound, 0.3f); // Quieter for slider changes
    }
    
    public void PlaySliderRelease()
    {
        PlaySoundWithRandomPitch(sliderReleaseSound, 0.5f);
    }
    
    // FIXED: Add UI opening/closing sounds
    public void PlayUIOpen()
    {
        PlaySoundWithRandomPitch(buttonClickSound, 0.8f);
    }
    
    public void PlayUIClose()
    {
        PlaySoundWithRandomPitch(buttonReleaseSound, 0.8f);
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
