using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Add TextMeshPro support

[RequireComponent(typeof(CanvasGroup))]
public class UIDissolveEffect : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 2.0f; // Increased from 0.4f for better visibility
    [SerializeField] private Vector2 gridSize = new Vector2(16, 12);
    [SerializeField] private float dissolveSpeed = 1f;
    [SerializeField] private Vector2 dissolveDirection = new Vector2(0.5f, 0.5f); // Center point
    
    [Header("Sound Settings")]
    [SerializeField] private AudioClip uiOpenSound;
    [SerializeField] private AudioClip uiCloseSound;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 1.0f;
    
    [Header("References")]
    [SerializeField] private Shader dissolveShader;
    
    private CanvasGroup canvasGroup;
    private Material originalMaterial;
    private Material instanceMaterial;
    private Image[] uiImages;
    private TextMeshProUGUI[] uiTexts; // Add TextMeshPro support
    private SpriteRenderer[] spriteRenderers; // Add SpriteRenderer support
    private bool isDissolving = false;
    private MonoBehaviour coroutineRunner; // Reference to a MonoBehaviour that stays active
    
    // Store original materials and colors for restoration
    private Material[] originalImageMaterials;
    private Material[] originalTextMaterials;
    private Material[] originalSpriteMaterials;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    
    // Shader property IDs for performance
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int GridSizeID = Shader.PropertyToID("_GridSize");
    private static readonly int DissolveSpeedID = Shader.PropertyToID("_DissolveSpeed");
    private static readonly int DissolveDirectionID = Shader.PropertyToID("_DissolveDirection");
    
    void Awake()
    {
        Debug.Log("UIDissolveEffect: Awake called");
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Get all UI Images, TextMeshPro texts, and SpriteRenderers in this GameObject and children
        uiImages = GetComponentsInChildren<Image>(true); // Include inactive objects
        uiTexts = GetComponentsInChildren<TextMeshProUGUI>(true); // Include inactive objects
        
        // Also get regular Text components as fallback
        Text[] regularTexts = GetComponentsInChildren<Text>(true);
        
        // Combine TextMeshPro and regular Text components
        if (regularTexts != null && regularTexts.Length > 0)
        {
            if (uiTexts != null)
            {
                // Extend the array to include regular texts
                TextMeshProUGUI[] combinedTexts = new TextMeshProUGUI[uiTexts.Length + regularTexts.Length];
                uiTexts.CopyTo(combinedTexts, 0);
                
                // Convert regular Text components to TextMeshProUGUI (we'll handle them differently)
                for (int i = 0; i < regularTexts.Length; i++)
                {
                    // We'll handle regular Text components in the fade methods
                }
                uiTexts = combinedTexts;
            }
            else
            {
                uiTexts = new TextMeshProUGUI[regularTexts.Length];
            }
        }
        
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true); // Include inactive objects
        Debug.Log($"UIDissolveEffect: Found {uiImages?.Length ?? 0} UI Images, {uiTexts?.Length ?? 0} UI Texts, and {spriteRenderers?.Length ?? 0} SpriteRenderers");
        
        // Store original materials and colors
        if (uiImages != null)
        {
            originalImageMaterials = new Material[uiImages.Length];
            for (int i = 0; i < uiImages.Length; i++)
            {
                if (uiImages[i] != null)
                {
                    originalImageMaterials[i] = uiImages[i].material;
                }
            }
        }
        
        if (uiTexts != null)
        {
            originalTextMaterials = new Material[uiTexts.Length];
            originalTextColors = new Color[uiTexts.Length];
            for (int i = 0; i < uiTexts.Length; i++)
            {
                if (uiTexts[i] != null)
                {
                    originalTextMaterials[i] = uiTexts[i].material;
                    originalTextColors[i] = uiTexts[i].color;
                }
            }
        }
        
        if (spriteRenderers != null)
        {
            originalSpriteMaterials = new Material[spriteRenderers.Length];
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalSpriteMaterials[i] = spriteRenderers[i].material;
                    originalSpriteColors[i] = spriteRenderers[i].color;
                }
            }
        }
        
        // Find a parent GameObject that stays active to run coroutines on
        Transform parent = transform.parent;
        while (parent != null)
        {
            MonoBehaviour parentMono = parent.GetComponent<MonoBehaviour>();
            if (parentMono != null)
            {
                coroutineRunner = parentMono;
                Debug.Log($"UIDissolveEffect: Found coroutine runner: {parentMono.name}");
                break;
            }
            parent = parent.parent;
        }
        
        // If no suitable parent found, use this GameObject but we'll handle inactive state differently
        if (coroutineRunner == null)
        {
            coroutineRunner = this;
            Debug.Log("UIDissolveEffect: Using self as coroutine runner");
        }
        
        // Create instance material if dissolve shader is assigned
        if (dissolveShader != null)
        {
            Debug.Log("UIDissolveEffect: Using assigned dissolve shader");
            instanceMaterial = new Material(dissolveShader);
        }
        else
        {
            // Try to load the shader automatically
            Debug.Log("UIDissolveEffect: No shader assigned, trying to find 'Custom/UIDissolveShader'");
            dissolveShader = Shader.Find("Custom/UIDissolveShader");
            if (dissolveShader != null)
            {
                Debug.Log("UIDissolveEffect: Successfully found shader 'Custom/UIDissolveShader'");
                instanceMaterial = new Material(dissolveShader);
            }
            else
            {
                Debug.LogError("UIDissolveEffect: Could not find shader 'Custom/UIDissolveShader'");
            }
        }
        
        Debug.Log($"UIDissolveEffect: Material created: {instanceMaterial != null}");
    }
    
    void Start()
    {
        // Initialize shader properties
        if (instanceMaterial != null)
        {
            instanceMaterial.SetVector(GridSizeID, gridSize);
            instanceMaterial.SetFloat(DissolveSpeedID, dissolveSpeed);
            instanceMaterial.SetVector(DissolveDirectionID, dissolveDirection);
        }
        else
        {
            Debug.LogWarning("UIDissolveEffect: No dissolve material created. Make sure the shader 'Custom/UIDissolveShader' is available.");
        }
        
        Debug.Log($"UIDissolveEffect: Initial sound volume set to: {soundVolume}");
    }
    
    private void PlayUIOpenSound()
    {
        if (uiOpenSound != null)
        {
            float currentVolume = Mathf.Clamp01(soundVolume);
            AudioSource.PlayClipAtPoint(uiOpenSound, Camera.main.transform.position, currentVolume);
            Debug.Log($"UIDissolveEffect: Played UI open sound. Volume: {currentVolume}");
        }
    }
    
    private void PlayUICloseSound()
    {
        if (uiCloseSound != null)
        {
            float currentVolume = Mathf.Clamp01(soundVolume);
            AudioSource.PlayClipAtPoint(uiCloseSound, Camera.main.transform.position, currentVolume);
            Debug.Log($"UIDissolveEffect: Played UI close sound. Volume: {currentVolume}");
        }
    }
    
    public void DissolveIn()
    {
        Debug.Log($"UIDissolveEffect: DissolveIn called. isDissolving: {isDissolving}, coroutineRunner: {coroutineRunner != null}");
        if (!isDissolving && coroutineRunner != null)
        {
            // Play UI open sound if assigned
            PlayUIOpenSound();
            
            // If this GameObject is inactive, activate it first
            if (!gameObject.activeInHierarchy)
            {
                Debug.Log("UIDissolveEffect: Activating GameObject for DissolveIn");
                gameObject.SetActive(true);
            }
            
            // Refresh original materials to ensure theme colors are current
            RefreshOriginalMaterials();
            
            Debug.Log("UIDissolveEffect: Starting DissolveIn coroutine");
            coroutineRunner.StartCoroutine(DissolveInCoroutine());
        }
        else
        {
            Debug.LogWarning($"UIDissolveEffect: Cannot start DissolveIn - isDissolving: {isDissolving}, coroutineRunner: {coroutineRunner != null}");
        }
    }
    
    public void DissolveOut()
    {
        Debug.Log($"UIDissolveEffect: DissolveOut called. isDissolving: {isDissolving}, coroutineRunner: {coroutineRunner != null}");
        if (!isDissolving && coroutineRunner != null)
        {
            // Play UI close sound if assigned
            PlayUICloseSound();
            
            Debug.Log("UIDissolveEffect: Starting DissolveOut coroutine");
            coroutineRunner.StartCoroutine(DissolveOutCoroutine());
        }
        else
        {
            Debug.LogWarning($"UIDissolveEffect: Cannot start DissolveOut - isDissolving: {isDissolving}, coroutineRunner: {coroutineRunner != null}");
        }
    }
    
    private IEnumerator DissolveInCoroutine()
    {
        Debug.Log("UIDissolveEffect: DissolveInCoroutine started");
        isDissolving = true;
        
        // Ensure the GameObject is active
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log("UIDissolveEffect: Activating GameObject in DissolveInCoroutine");
            gameObject.SetActive(true);
        }
        
        // Set initial state
        canvasGroup.alpha = 1f;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(DissolveAmountID, 0f);
            ApplyDissolveMaterial();
            Debug.Log("UIDissolveEffect: Applied dissolve material for DissolveIn");
        }
        else
        {
            Debug.LogWarning("UIDissolveEffect: No instance material available for DissolveIn");
        }
        
        // Animate dissolve in
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dissolveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            if (instanceMaterial != null)
            {
                instanceMaterial.SetFloat(DissolveAmountID, t);
            }
            
            // Also fade in text and sprite elements that can't use the shader
            FadeTextElements(t);
            FadeSpriteElements(t);
            
            yield return null;
        }
        
        // Ensure final state
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(DissolveAmountID, 1f);
        }
        FadeTextElements(1f);
        FadeSpriteElements(1f);
        
        // Reapply original materials to ensure theme is properly set
        RestoreOriginalMaterial();
        
        // Reapply theme shader to ensure hue shift is maintained
        UIThemeManager themeManager = FindObjectOfType<UIThemeManager>();
        if (themeManager != null)
        {
            // Use the public ApplyTheme method to reapply the current theme
            int currentThemeIndex = themeManager.GetCurrentThemeIndex();
            themeManager.ApplyTheme(currentThemeIndex);
            Debug.Log($"UIDissolveEffect: Reapplied theme {currentThemeIndex} after dissolve effect");
        }
        
        Debug.Log("UIDissolveEffect: DissolveInCoroutine completed");
        isDissolving = false;
    }
    
    private IEnumerator DissolveOutCoroutine()
    {
        Debug.Log("UIDissolveEffect: DissolveOutCoroutine started");
        isDissolving = true;
        
        // Set initial state
        canvasGroup.alpha = 1f;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(DissolveAmountID, 1f);
            ApplyDissolveMaterial();
            Debug.Log("UIDissolveEffect: Applied dissolve material for DissolveOut");
        }
        else
        {
            Debug.LogWarning("UIDissolveEffect: No instance material available for DissolveOut");
        }
        
        // Animate dissolve out
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dissolveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            if (instanceMaterial != null)
            {
                instanceMaterial.SetFloat(DissolveAmountID, 1f - t);
            }
            
            // Also fade out text and sprite elements that can't use the shader
            FadeTextElements(1f - t);
            FadeSpriteElements(1f - t);
            
            yield return null;
        }
        
        // Ensure final state
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(DissolveAmountID, 0f);
        }
        FadeTextElements(0f);
        FadeSpriteElements(0f);
        
        // Restore original materials before disabling
        RestoreOriginalMaterial();
        
        // Disable the GameObject
        Debug.Log("UIDissolveEffect: Disabling GameObject after DissolveOut");
        gameObject.SetActive(false);
        
        Debug.Log("UIDissolveEffect: DissolveOutCoroutine completed");
        isDissolving = false;
    }
    
    private void ApplyDissolveMaterial()
    {
        Debug.Log($"UIDissolveEffect: ApplyDissolveMaterial called. instanceMaterial: {instanceMaterial != null}, uiImages: {uiImages?.Length ?? 0}, uiTexts: {uiTexts?.Length ?? 0}, spriteRenderers: {spriteRenderers?.Length ?? 0}");
        if (instanceMaterial != null)
        {
            int appliedCount = 0;
            
            // Apply to Images
            if (uiImages != null)
            {
                foreach (Image image in uiImages)
                {
                    if (image != null)
                    {
                        image.material = instanceMaterial;
                        appliedCount++;
                        Debug.Log($"UIDissolveEffect: Applied material to Image: {image.name}");
                    }
                }
            }
            
            // Apply to TextMeshPro texts
            if (uiTexts != null)
            {
                foreach (TextMeshProUGUI text in uiTexts)
                {
                    if (text != null)
                    {
                        text.material = instanceMaterial;
                        appliedCount++;
                        Debug.Log($"UIDissolveEffect: Applied material to Text: {text.name}");
                    }
                }
            }
            
            // Apply to all TMP_Text components (includes TextMeshProUGUI and other TMP variants)
            TMP_Text[] allTMPTexts = GetComponentsInChildren<TMP_Text>(true);
            if (allTMPTexts != null)
            {
                foreach (TMP_Text text in allTMPTexts)
                {
                    if (text != null)
                    {
                        text.material = instanceMaterial;
                        appliedCount++;
                        Debug.Log($"UIDissolveEffect: Applied material to TMP_Text: {text.name}");
                    }
                }
            }
            
            // Apply to SpriteRenderers
            if (spriteRenderers != null)
            {
                foreach (SpriteRenderer spriteRenderer in spriteRenderers)
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.material = instanceMaterial;
                        appliedCount++;
                        Debug.Log($"UIDissolveEffect: Applied material to SpriteRenderer: {spriteRenderer.name}");
                    }
                }
            }
            
            Debug.Log($"UIDissolveEffect: Applied material to {appliedCount} elements");
        }
        else
        {
            Debug.LogWarning($"UIDissolveEffect: Cannot apply material - instanceMaterial: {instanceMaterial != null}");
        }
    }
    
    private void FadeTextElements(float alpha)
    {
        // Fade text elements that might not work well with the shader
        if (uiTexts != null)
        {
            foreach (TextMeshProUGUI text in uiTexts)
            {
                if (text != null)
                {
                    Color color = text.color;
                    color.a = alpha;
                    text.color = color;
                }
            }
        }
        
        // Also fade regular Text components
        Text[] regularTexts = GetComponentsInChildren<Text>(true);
        if (regularTexts != null)
        {
            foreach (Text text in regularTexts)
            {
                if (text != null)
                {
                    Color color = text.color;
                    color.a = alpha;
                    text.color = color;
                }
            }
        }
        
        // Also fade any other text-like components we might have missed
        TMP_Text[] allTMPTexts = GetComponentsInChildren<TMP_Text>(true);
        if (allTMPTexts != null)
        {
            foreach (TMP_Text text in allTMPTexts)
            {
                if (text != null)
                {
                    Color color = text.color;
                    color.a = alpha;
                    text.color = color;
                }
            }
        }
    }
    
    private void FadeSpriteElements(float alpha)
    {
        // Fade sprite elements that might not work well with the shader
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = alpha;
                    spriteRenderer.color = color;
                }
            }
        }
    }
    
    private void RestoreOriginalMaterial()
    {
        // Restore original materials for Images
        if (uiImages != null && originalImageMaterials != null)
        {
            for (int i = 0; i < uiImages.Length && i < originalImageMaterials.Length; i++)
            {
                if (uiImages[i] != null)
                {
                    uiImages[i].material = originalImageMaterials[i];
                }
            }
        }
        
        // Restore original materials and colors for Texts
        if (uiTexts != null && originalTextMaterials != null && originalTextColors != null)
        {
            for (int i = 0; i < uiTexts.Length && i < originalTextMaterials.Length && i < originalTextColors.Length; i++)
            {
                if (uiTexts[i] != null)
                {
                    uiTexts[i].material = originalTextMaterials[i];
                    uiTexts[i].color = originalTextColors[i];
                }
            }
        }
        
        // Restore original materials and colors for SpriteRenderers
        if (spriteRenderers != null && originalSpriteMaterials != null && originalSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalSpriteMaterials.Length && i < originalSpriteColors.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].material = originalSpriteMaterials[i];
                    spriteRenderers[i].color = originalSpriteColors[i];
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up instance material
        if (instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
        }
    }
    
    // Public methods to adjust dissolve settings
    public void SetGridSize(Vector2 newGridSize)
    {
        gridSize = newGridSize;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetVector(GridSizeID, gridSize);
        }
    }
    
    public void SetDissolveDirection(Vector2 newDirection)
    {
        dissolveDirection = newDirection;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetVector(DissolveDirectionID, dissolveDirection);
        }
    }
    
    public void SetDissolveDuration(float newDuration)
    {
        dissolveDuration = newDuration;
    }
    
    // Public methods to set custom sounds
    public void SetUIOpenSound(AudioClip sound)
    {
        uiOpenSound = sound;
    }
    
    public void SetUICloseSound(AudioClip sound)
    {
        uiCloseSound = sound;
    }
    
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        Debug.Log($"UIDissolveEffect: Sound volume set to: {soundVolume}");
    }
    
    public float GetSoundVolume()
    {
        return soundVolume;
    }
    
    // Refresh the cached original materials and colors to ensure theme colors are current
    private void RefreshOriginalMaterials()
    {
        // Refresh image materials
        if (uiImages != null)
        {
            originalImageMaterials = new Material[uiImages.Length];
            for (int i = 0; i < uiImages.Length; i++)
            {
                if (uiImages[i] != null)
                {
                    originalImageMaterials[i] = uiImages[i].material;
                }
            }
        }
        
        // Refresh text materials and colors
        if (uiTexts != null)
        {
            originalTextMaterials = new Material[uiTexts.Length];
            originalTextColors = new Color[uiTexts.Length];
            for (int i = 0; i < uiTexts.Length; i++)
            {
                if (uiTexts[i] != null)
                {
                    originalTextMaterials[i] = uiTexts[i].material;
                    originalTextColors[i] = uiTexts[i].color;
                }
            }
        }
        
        // Refresh sprite renderer materials and colors
        if (spriteRenderers != null)
        {
            originalSpriteMaterials = new Material[spriteRenderers.Length];
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalSpriteMaterials[i] = spriteRenderers[i].material;
                    originalSpriteColors[i] = spriteRenderers[i].color;
                }
            }
        }
        
        Debug.Log("UIDissolveEffect: Refreshed original materials and colors");
    }
}

