using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIThemeManager : MonoBehaviour
{
    [System.Serializable]
    public class Theme
    {
        public string name;
        [Range(0f, 1f)] public float hueShift = 0f;
        [Range(0f, 2f)] public float saturationMultiplier = 1f;
        [Range(0f, 2f)] public float valueMultiplier = 1f;
    }
    
    [Header("Theme Settings")]
    [SerializeField] private List<Theme> themes = new List<Theme>();
    [SerializeField] private int currentThemeIndex = 0;
    [Range(0f, 1f)] [SerializeField] private float hueShift = 0f;
    [Range(0f, 2f)] [SerializeField] private float saturationMultiplier = 1f;
    [Range(0f, 2f)] [SerializeField] private float valueMultiplier = 1f;
    
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown themeDropdown;
    [SerializeField] private Slider hueShiftSlider;
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private Slider valueSlider;
    
    [Header("Exclusions")]
    [SerializeField] private GameObject crosshairObject;
    [SerializeField] private GameObject[] hsvPreviewObjects;
    
    [Header("Inclusions")]
    [SerializeField] private GameObject[] forceIncludeObjects;
    
    private Shader hueShiftShader;
    private Material hueShiftMaterial;
    private List<Material> originalMaterials = new List<Material>();
    private List<Graphic> uiElements = new List<Graphic>();
    private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    
    // Shader property IDs
    private static readonly int HueShiftID = Shader.PropertyToID("_HueShift");
    private static readonly int SaturationMultiplierID = Shader.PropertyToID("_SaturationMultiplier");
    private static readonly int ValueMultiplierID = Shader.PropertyToID("_ValueMultiplier");
    
    void Awake()
    {
        Debug.Log("UIThemeManager: Awake called");
        
        // Load the hue shift shader
        // Replace the Shader.Find line with:
        hueShiftShader = Resources.Load<Shader>("Shaders/UIHueShiftShader");
        if (hueShiftShader != null)
        {
            hueShiftMaterial = new Material(hueShiftShader);
            Debug.Log("UIThemeManager: Successfully created hue shift material");
        }
        else
        {
            Debug.LogError("UIThemeManager: Could not find shader 'Custom/UIHueShiftShader'");
            Debug.LogError("UIThemeManager: This usually means the shader is not included in the build");
            Debug.LogError("UIThemeManager: Check that the shader file is in the Assets folder and not excluded from build");
            
            // Try alternative shader names
            string[] alternativeShaderNames = {
                "UI/Default",
                "Sprites/Default",
                "Unlit/Texture"
            };
            
            foreach (string shaderName in alternativeShaderNames)
            {
                Shader fallbackShader = Shader.Find(shaderName);
                if (fallbackShader != null)
                {
                    Debug.LogWarning($"UIThemeManager: Using fallback shader '{shaderName}' (no hue shift functionality)");
                    hueShiftShader = fallbackShader;
                    hueShiftMaterial = new Material(hueShiftShader);
                    break;
                }
            }
            
            if (hueShiftMaterial == null)
            {
                Debug.LogError("UIThemeManager: No fallback shader found. Theme system will not work.");
            }
        }
        
        // Initialize default themes if none exist
        if (themes.Count == 0)
        {
            InitializeDefaultThemes();
        }
        
        // Validate shader
        ValidateShader();
    }
    
    private void ValidateShader()
    {
        if (hueShiftMaterial != null)
        {
            // Check if the shader is valid
            if (!hueShiftMaterial.shader.isSupported)
            {
                Debug.LogError("UIThemeManager: Shader is not supported on this platform");
            }
            else
            {
                Debug.Log("UIThemeManager: Shader validation passed");
            }
        }
        else
        {
            Debug.LogError("UIThemeManager: No material available for validation");
        }
    }
    
    void Start()
    {
        // Find all UI elements
        FindUIElements();
        
        // Setup UI controls
        SetupUIControls();
        
        // Apply current theme
        ApplyTheme(currentThemeIndex);
        
        Debug.Log($"UIThemeManager: Found {uiElements.Count} UI elements and {spriteRenderers.Count} SpriteRenderers to theme");
    }
    
    private void InitializeDefaultThemes()
    {
        themes.Clear();
        
        themes.Add(new Theme { name = "Default", hueShift = 0f, saturationMultiplier = 1f, valueMultiplier = 1f });
        themes.Add(new Theme { name = "Blue", hueShift = 0.66f, saturationMultiplier = 1f, valueMultiplier = 1f });
        themes.Add(new Theme { name = "Green", hueShift = 0.33f, saturationMultiplier = 1f, valueMultiplier = 1f });
        themes.Add(new Theme { name = "Purple", hueShift = 0.83f, saturationMultiplier = 1f, valueMultiplier = 1f });
        themes.Add(new Theme { name = "Orange", hueShift = 0.08f, saturationMultiplier = 1f, valueMultiplier = 1f });
        themes.Add(new Theme { name = "Pink", hueShift = 0.92f, saturationMultiplier = 1f, valueMultiplier = 1f });
        
        Debug.Log("UIThemeManager: Initialized default themes");
    }
    
    private void FindUIElements()
    {
        uiElements.Clear();
        spriteRenderers.Clear();
        originalMaterials.Clear();
        
        // Find all UI elements in the scene
        Graphic[] allGraphics = FindObjectsOfType<Graphic>();
        
        foreach (Graphic graphic in allGraphics)
        {
            // Skip crosshair and HSV preview objects
            if (ShouldExcludeElement(graphic.gameObject))
            {
                continue;
            }
            
            uiElements.Add(graphic);
            originalMaterials.Add(graphic.material);
        }
        
        // Find all SpriteRenderers in the scene
        SpriteRenderer[] allSpriteRenderers = FindObjectsOfType<SpriteRenderer>();
        
        foreach (SpriteRenderer spriteRenderer in allSpriteRenderers)
        {
            // Skip crosshair and HSV preview objects
            if (ShouldExcludeElement(spriteRenderer.gameObject))
            {
                continue;
            }
            
            spriteRenderers.Add(spriteRenderer);
            originalMaterials.Add(spriteRenderer.material);
        }
        
        // Force include specific objects
        if (forceIncludeObjects != null)
        {
            foreach (GameObject includeObj in forceIncludeObjects)
            {
                if (includeObj != null)
                {
                    // Check if this object should be excluded first
                    if (ShouldExcludeElement(includeObj))
                    {
                        Debug.Log($"UIThemeManager: Skipping force include object '{includeObj.name}' because it's in exclusion list");
                        continue;
                    }
                    
                    // Include UI Graphics
                    Graphic[] graphics = includeObj.GetComponentsInChildren<Graphic>(true);
                    foreach (Graphic graphic in graphics)
                    {
                        // Check if this graphic is already in our list
                        if (!uiElements.Contains(graphic))
                        {
                            uiElements.Add(graphic);
                            originalMaterials.Add(graphic.material);
                            Debug.Log($"UIThemeManager: Force included graphic: {graphic.name}");
                        }
                    }
                    
                    // Include SpriteRenderers
                    SpriteRenderer[] sprites = includeObj.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer sprite in sprites)
                    {
                        // Check if this sprite renderer is already in our list
                        if (!spriteRenderers.Contains(sprite))
                        {
                            spriteRenderers.Add(sprite);
                            originalMaterials.Add(sprite.material);
                            Debug.Log($"UIThemeManager: Force included SpriteRenderer: {sprite.name}");
                        }
                    }
                }
            }
        }
    }
    
    private bool ShouldExcludeElement(GameObject obj)
    {
        // Exclude crosshair object
        if (crosshairObject != null && obj == crosshairObject)
        {
            return true;
        }
        
        // Exclude HSV preview objects
        if (hsvPreviewObjects != null)
        {
            foreach (GameObject previewObj in hsvPreviewObjects)
            {
                if (previewObj != null && obj == previewObj)
                {
                    return true;
                }
            }
        }
        
        // Exclude objects with "Crosshair" or "HSV" in their name
        string objName = obj.name.ToLower();
        if (objName.Contains("crosshair") || objName.Contains("hsv") || objName.Contains("preview"))
        {
            return true;
        }
        
        return false;
    }
    
    private void SetupUIControls()
    {
        // Setup theme dropdown
        if (themeDropdown != null)
        {
            themeDropdown.ClearOptions();
            List<string> themeNames = new List<string>();
            foreach (Theme theme in themes)
            {
                themeNames.Add(theme.name);
            }
            themeDropdown.AddOptions(themeNames);
            themeDropdown.value = currentThemeIndex;
            themeDropdown.onValueChanged.AddListener(OnThemeDropdownChanged);
        }
        
        // Setup sliders
        if (hueShiftSlider != null)
        {
            hueShiftSlider.value = hueShift;
            hueShiftSlider.onValueChanged.AddListener(OnHueShiftChanged);
        }
        
        if (saturationSlider != null)
        {
            saturationSlider.value = saturationMultiplier;
            saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
        }
        
        if (valueSlider != null)
        {
            valueSlider.value = valueMultiplier;
            valueSlider.onValueChanged.AddListener(OnValueChanged);
        }
    }
    
    public void ApplyTheme(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= themes.Count)
        {
            Debug.LogWarning($"UIThemeManager: Invalid theme index: {themeIndex}");
            return;
        }
        
        currentThemeIndex = themeIndex;
        Theme theme = themes[themeIndex];
        
        hueShift = theme.hueShift;
        saturationMultiplier = theme.saturationMultiplier;
        valueMultiplier = theme.valueMultiplier;
        
        ApplyHueShift();
        
        // Update UI controls
        if (themeDropdown != null && themeDropdown.value != themeIndex)
        {
            themeDropdown.SetValueWithoutNotify(themeIndex);
        }
        if (hueShiftSlider != null)
        {
            hueShiftSlider.SetValueWithoutNotify(hueShift);
        }
        if (saturationSlider != null)
        {
            saturationSlider.SetValueWithoutNotify(saturationMultiplier);
        }
        if (valueSlider != null)
        {
            valueSlider.SetValueWithoutNotify(valueMultiplier);
        }
        
        Debug.Log($"UIThemeManager: Applied theme '{theme.name}'");
    }
    
    private void ApplyHueShift()
    {
        if (hueShiftMaterial == null)
        {
            Debug.LogWarning("UIThemeManager: No hue shift material available");
            return;
        }
        
        // Update shader properties
        hueShiftMaterial.SetFloat(HueShiftID, hueShift);
        hueShiftMaterial.SetFloat(SaturationMultiplierID, saturationMultiplier);
        hueShiftMaterial.SetFloat(ValueMultiplierID, valueMultiplier);
        
        // Apply to all UI elements (checking exclusions each time)
        for (int i = uiElements.Count - 1; i >= 0; i--)
        {
            if (uiElements[i] != null)
            {
                // Check if this element should be excluded
                if (ShouldExcludeElement(uiElements[i].gameObject))
                {
                    // Restore original material for excluded elements
                    if (i < originalMaterials.Count)
                    {
                        uiElements[i].material = originalMaterials[i];
                    }
                    Debug.Log($"UIThemeManager: Excluded UI element from theme: {uiElements[i].name}");
                }
                else
                {
                    uiElements[i].material = hueShiftMaterial;
                }
            }
        }
        
        // Apply to all SpriteRenderers (checking exclusions each time)
        for (int i = spriteRenderers.Count - 1; i >= 0; i--)
        {
            if (spriteRenderers[i] != null)
            {
                // Check if this sprite renderer should be excluded
                if (ShouldExcludeElement(spriteRenderers[i].gameObject))
                {
                    // Restore original material for excluded elements
                    int originalIndex = uiElements.Count + i;
                    if (originalIndex < originalMaterials.Count)
                    {
                        spriteRenderers[i].material = originalMaterials[originalIndex];
                    }
                    Debug.Log($"UIThemeManager: Excluded SpriteRenderer from theme: {spriteRenderers[i].name}");
                }
                else
                {
                    spriteRenderers[i].material = hueShiftMaterial;
                }
            }
        }
        
        Debug.Log($"UIThemeManager: Applied hue shift - H:{hueShift:F2}, S:{saturationMultiplier:F2}, V:{valueMultiplier:F2} to {uiElements.Count} UI elements and {spriteRenderers.Count} SpriteRenderers");
    }
    
    private void OnThemeDropdownChanged(int index)
    {
        ApplyTheme(index);
    }
    
    private void OnHueShiftChanged(float value)
    {
        hueShift = value;
        ApplyHueShift();
    }
    
    private void OnSaturationChanged(float value)
    {
        saturationMultiplier = value;
        ApplyHueShift();
    }
    
    private void OnValueChanged(float value)
    {
        valueMultiplier = value;
        ApplyHueShift();
    }
    
    public void AddTheme(string name, float hueShift, float saturation = 1f, float value = 1f)
    {
        Theme newTheme = new Theme
        {
            name = name,
            hueShift = hueShift,
            saturationMultiplier = saturation,
            valueMultiplier = value
        };
        
        themes.Add(newTheme);
        
        // Update dropdown if it exists
        if (themeDropdown != null)
        {
            themeDropdown.ClearOptions();
            List<string> themeNames = new List<string>();
            foreach (Theme theme in themes)
            {
                themeNames.Add(theme.name);
            }
            themeDropdown.AddOptions(themeNames);
        }
        
        Debug.Log($"UIThemeManager: Added theme '{name}'");
    }
    
    public void RemoveTheme(int index)
    {
        if (index >= 0 && index < themes.Count)
        {
            string themeName = themes[index].name;
            themes.RemoveAt(index);
            
            // Update dropdown if it exists
            if (themeDropdown != null)
            {
                themeDropdown.ClearOptions();
                List<string> themeNames = new List<string>();
                foreach (Theme theme in themes)
                {
                    themeNames.Add(theme.name);
                }
                themeDropdown.AddOptions(themeNames);
            }
            
            Debug.Log($"UIThemeManager: Removed theme '{themeName}'");
        }
    }
    
    public void RestoreOriginalMaterials()
    {
        // Restore UI elements
        for (int i = 0; i < uiElements.Count && i < originalMaterials.Count; i++)
        {
            if (uiElements[i] != null)
            {
                uiElements[i].material = originalMaterials[i];
            }
        }
        
        // Restore SpriteRenderers (they start after UI elements in the originalMaterials list)
        int spriteStartIndex = uiElements.Count;
        for (int i = 0; i < spriteRenderers.Count && (spriteStartIndex + i) < originalMaterials.Count; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[spriteStartIndex + i];
            }
        }
        
        Debug.Log("UIThemeManager: Restored original materials");
    }
    
    void OnDestroy()
    {
        // Clean up
        if (hueShiftMaterial != null)
        {
            DestroyImmediate(hueShiftMaterial);
        }
    }
    
    // Public getters
    public List<Theme> GetThemes() => themes;
    public int GetCurrentThemeIndex() => currentThemeIndex;
    public float GetHueShift() => hueShift;
    public float GetSaturationMultiplier() => saturationMultiplier;
    public float GetValueMultiplier() => valueMultiplier;
    
    // Debug method to test shader functionality
    [ContextMenu("Test Shader")]
    public void TestShader()
    {
        Debug.Log("=== UIThemeManager Shader Test ===");
        Debug.Log($"Shader found: {hueShiftShader != null}");
        Debug.Log($"Material created: {hueShiftMaterial != null}");
        
        if (hueShiftShader != null)
        {
            Debug.Log($"Shader name: {hueShiftShader.name}");
            Debug.Log($"Shader supported: {hueShiftShader.isSupported}");
        }
        
        if (hueShiftMaterial != null)
        {
            Debug.Log($"Material shader: {hueShiftMaterial.shader.name}");
            Debug.Log($"Material shader supported: {hueShiftMaterial.shader.isSupported}");
            
            // Test setting properties
            try
            {
                hueShiftMaterial.SetFloat(HueShiftID, 0.5f);
                hueShiftMaterial.SetFloat(SaturationMultiplierID, 1.5f);
                hueShiftMaterial.SetFloat(ValueMultiplierID, 0.8f);
                Debug.Log("Shader properties set successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting shader properties: {e.Message}");
            }
        }
        
        Debug.Log($"UI Elements found: {uiElements.Count}");
        Debug.Log($"SpriteRenderers found: {spriteRenderers.Count}");
        Debug.Log("=== End Shader Test ===");
    }
}
