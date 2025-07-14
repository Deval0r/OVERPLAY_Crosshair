using UnityEngine;

/// <summary>
/// Forces sprite renderers to stay fully opaque and prevents transparency issues
/// </summary>
public class ForceOpaqueRenderer : MonoBehaviour
{
    [Header("Opaque Settings")]
    [Tooltip("Force the sprite to always be fully opaque")]
    public bool forceOpaque = true;
    
    [Tooltip("Check this to apply to all child sprite renderers")]
    public bool applyToChildren = true;
    
    [Tooltip("How often to check and fix the color (in seconds, 0 = every frame)")]
    public float checkInterval = 0.1f;
    
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float lastCheckTime;
    
    void Start()
    {
        // Get all sprite renderers
        if (applyToChildren)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
        else
        {
            spriteRenderers = GetComponents<SpriteRenderer>();
        }
        
        // Store original colors
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
        
        // Force initial opaque state
        ForceOpaqueState();
    }
    
    void Update()
    {
        if (!forceOpaque) return;
        
        // Check at intervals to avoid performance issues
        if (Time.time - lastCheckTime >= checkInterval)
        {
            ForceOpaqueState();
            lastCheckTime = Time.time;
        }
    }
    
    void ForceOpaqueState()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color currentColor = spriteRenderers[i].color;
                
                // Force alpha to 1.0 (fully opaque)
                if (currentColor.a != 1.0f)
                {
                    currentColor.a = 1.0f;
                    spriteRenderers[i].color = currentColor;
                }
                
                // Ensure the material is using the default sprite material
                if (spriteRenderers[i].material != null && 
                    spriteRenderers[i].material.shader.name.Contains("Particles"))
                {
                    // Reset to default sprite material if it's using a particle shader
                    spriteRenderers[i].material = null; // This will use the default
                }
            }
        }
    }
    
    void OnValidate()
    {
        // Force immediate update when values change in inspector
        if (Application.isPlaying)
        {
            ForceOpaqueState();
        }
    }
    
    void OnDisable()
    {
        // Restore original colors when disabled
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && i < originalColors.Length)
                {
                    spriteRenderers[i].color = originalColors[i];
                }
            }
        }
    }
} 