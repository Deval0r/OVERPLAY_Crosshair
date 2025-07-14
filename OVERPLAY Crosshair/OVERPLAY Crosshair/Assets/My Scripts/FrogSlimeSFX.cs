using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FrogSlimeSFX : MonoBehaviour
{
    [Header("Ogg Slime Sound Clips")]
    [Tooltip("Add multiple .ogg squish sounds here. Unity supports them natively!")]
    public AudioClip[] slimeSounds;

    [Header("Sound Response Tuning")]
    [Tooltip("Scale impact force into volume range.")]
    public float volumeMultiplier = 0.1f;

    [Tooltip("Lowest pitch variation for playback.")]
    public float minPitch = 0.9f;

    [Tooltip("Highest pitch variation for playback.")]
    public float maxPitch = 1.2f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Ensures 2D sound for desktop-friendly playback
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (slimeSounds.Length == 0) return;

        // Pick a random .ogg clip
        AudioClip chosenClip = slimeSounds[Random.Range(0, slimeSounds.Length)];

        // Modulate volume based on collision intensity
        float impactForce = collision.relativeVelocity.magnitude;
        float finalVolume = Mathf.Clamp01(impactForce * volumeMultiplier);

        // Randomize pitch for squishy charm
        float randomizedPitch = Random.Range(minPitch, maxPitch);
        audioSource.pitch = randomizedPitch;

        // Play the selected .ogg sound
        audioSource.PlayOneShot(chosenClip, finalVolume);
    }
}