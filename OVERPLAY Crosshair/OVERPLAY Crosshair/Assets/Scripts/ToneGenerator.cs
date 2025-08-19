using UnityEngine;

public class ToneGenerator : MonoBehaviour
{
    [Header("Tone Settings")]
    [SerializeField] private float sampleRate = 44100f;
    [SerializeField] private float duration = 0.1f;
    
    public static AudioClip GenerateTone(float frequency, float duration = 0.1f, float volume = 0.5f)
    {
        int samples = Mathf.RoundToInt(44100f * duration);
        AudioClip clip = AudioClip.Create("Tone", samples, 1, 44100, false);
        
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float time = i / 44100f;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume;
            
            // Apply fade out to prevent clicks
            float fadeOut = 1f - (i / (float)samples);
            data[i] *= fadeOut;
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    public static AudioClip GenerateClickSound()
    {
        return GenerateTone(800f, 0.05f, 0.3f);
    }
    
    public static AudioClip GenerateReleaseSound()
    {
        return GenerateTone(600f, 0.03f, 0.2f);
    }
    
    public static AudioClip GenerateSliderSound()
    {
        return GenerateTone(400f, 0.02f, 0.1f);
    }
    
    public static AudioClip GenerateSliderReleaseSound()
    {
        return GenerateTone(500f, 0.04f, 0.15f);
    }
}
