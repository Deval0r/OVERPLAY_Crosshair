using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq; // Added for .Any() and .Select()
using System.Security.Cryptography;
using System.Text;
//using Window;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using WinForms = System.Windows.Forms;  // ← Keep the alias
#endif




public enum CrosshairShape { Circle, Square, Triangle }
public enum HairStyle { Even, Custom }

[RequireComponent(typeof(CanvasRenderer))]
public class CrosshairRenderer : Graphic
{
    // --- Crosshair Settings ---
    // Frame
    public CrosshairShape frameShape = CrosshairShape.Circle;
    public bool frameFilled = false;
    public Color frameColor = Color.red;
    [Range(0, 1)] public float frameOpacity = 1f;
    public float frameScale = 0.9f;
    [Range(0, 360)] public float frameRotation = 0f;
    public float frameThickness = 5f;

    // Hairs
    public HairStyle hairStyle = HairStyle.Even;
    public int hairCount = 4;
    public float customAngle = 0f;
    public float hairThickness = 5.5f;
    public float hairLength = 24f;
    public Color hairColor = Color.red;
    [Range(0, 1)] public float hairOpacity = 1f;
    [Range(0, 360)] public float hairsRotation = 0f;

    // Dot
    public CrosshairShape dotShape = CrosshairShape.Square;
    public bool dotFilled = true;
    public Color dotColor = Color.red;
    [Range(0, 1)] public float dotOpacity = 1f;
    public float dotScale = 0.9f;
    [Range(0, 360)] public float dotRotation = 45f;

    // --- Frame HSV ---
    [Range(0,1)] public float frameHue = 1f;
    [Range(0,1)] public float frameSaturation = 1f;
    [Range(0,1)] public float frameValue = 1f;
    // --- Hair HSV ---
    [Range(0,1)] public float hairHue = 1f;
    [Range(0,1)] public float hairSaturation = 1f;
    [Range(0,1)] public float hairValue = 1f;
    // --- Dot HSV ---
    [Range(0,1)] public float dotHue = 1f;
    [Range(0,1)] public float dotSaturation = 1f;
    [Range(0,1)] public float dotValue = 1f;

    // --- UI References ---
    [Header("Frame UI")]
    public TMP_Dropdown frameShapeDropdown;
    public Toggle frameFilledToggle;
    public Slider frameOpacitySlider;
    public Slider frameScaleSlider;
    public Slider frameRotationSlider;
    public Slider frameThicknessSlider;
    public Slider frameColorSlider; // always-visible color slider
    public Slider frameSaturationSlider;
    public Slider frameValueSlider;
    public GameObject frameColorPreviewObj; // preview GameObject with sprite
    private SpriteRenderer frameColorPreviewRenderer; // cached SpriteRenderer
    public GameObject frameSaturationRefObj;
    private SpriteRenderer frameSaturationRefRenderer;
    public GameObject frameValueRefObj;
    private SpriteRenderer frameValueRefRenderer;
    public UnityEngine.UI.Button hideCrosshairButton;
    public UnityEngine.UI.Image hideCrosshairButtonImage; // The Image component on the hide crosshair button
    public Sprite crosshairVisibleSprite; // Sprite to show when crosshair is visible
    public Sprite crosshairHiddenSprite; // Sprite to show when crosshair is hidden

    [Header("Hairs UI")]
    public TMP_Dropdown hairStyleDropdown;
    public Slider hairCountSlider;
    public Slider customAngleSlider;
    public Slider hairThicknessSlider;
    public Slider hairLengthSlider;
    public Slider hairsRotationSlider;
    public Slider hairColorSlider; // always-visible color slider
    public Slider hairSaturationSlider;
    public Slider hairValueSlider;
    public GameObject hairColorPreviewObj; // preview GameObject with sprite
    private SpriteRenderer hairColorPreviewRenderer;
    public GameObject hairSaturationRefObj;
    private SpriteRenderer hairSaturationRefRenderer;
    public GameObject hairValueRefObj;
    private SpriteRenderer hairValueRefRenderer;
    public Slider hairOpacitySlider;
    public Toggle hairsExtendPastFrameToggle;
    public Slider hairDistanceSlider;

    [Header("Dot UI")]
    public TMP_Dropdown dotShapeDropdown;
    public Toggle dotFilledToggle;
    public Slider dotColorSlider; // always-visible color slider
    public Slider dotSaturationSlider;
    public Slider dotValueSlider;
    public GameObject dotColorPreviewObj; // preview GameObject with sprite
    private SpriteRenderer dotColorPreviewRenderer;
    public GameObject dotSaturationRefObj;
    private SpriteRenderer dotSaturationRefRenderer;
    public GameObject dotValueRefObj;
    private SpriteRenderer dotValueRefRenderer;
    public Slider dotOpacitySlider;
    public Slider dotScaleSlider;
    public Slider dotRotationSlider;

    [Header("Crosshair Visual Root")]
    public GameObject crosshairVisualRoot; // Assign this to the crosshair graphics root in the Inspector

    [Header("Tabs")]
    public GameObject frameTabRoot;
    public GameObject hairTabRoot;
    public GameObject dotTabRoot;
    public UnityEngine.UI.Button frameTabButton;
    public UnityEngine.UI.Button hairTabButton;
    public UnityEngine.UI.Button dotTabButton;

    [Header("Save/Load")]
    public UnityEngine.UI.Button saveCodeButton;
    public UnityEngine.UI.Button loadCodeButton;
    public UnityEngine.UI.Button generateImageButton;
    public UnityEngine.UI.Button clearButton;
    public TMP_Dropdown presetDropdown;

    [Header("Snapping")]
    public Toggle snapRotationToggle;
    private bool SnapEnabled => snapRotationToggle != null && snapRotationToggle.isOn;

    [Header("Keybinds")]
    public UnityEngine.UI.Button keybindRecordButton;
    public GameObject uiRoot; // Assign your UI root GameObject here
    
    [Header("Preset Keybinds")]
    public Toggle presetKeybindsToggle;
    public GameObject presetKeybindsPanel;
    public UnityEngine.UI.Button[] presetKeybindButtons = new UnityEngine.UI.Button[5];
    public Toggle[] presetHoldToggles = new Toggle[5];
    public UnityEngine.UI.Button clearAllPresetKeybindsButton; // Assign in Inspector

    [Header("Tab Button Positions")] 
    public Vector2 tabLeftPosition = new Vector2(-200, -40); // left and down
    public Vector2 tabMiddlePosition = new Vector2(0, 0);    // center
    public Vector2 tabRightPosition = new Vector2(200, 0);   // right
    public Color tabHighlightColor = new Color(1f, 0.9f, 0.6f, 1f); // highlighted color
    public Color tabNormalColor = Color.white; // normal color
    public float tabHighlightScale = 0.023f; // 15% larger than normal
    public float tabNormalScale = 0.02f;

    [Header("Runtime Texture Generation")]
    private Texture2D frameSaturationTexture;
    private Texture2D hairSaturationTexture;
    private Texture2D dotSaturationTexture;
    
    // Store original sprite properties to maintain size
    private Vector2 originalFrameSatSize;
    private Vector2 originalHairSatSize;
    private Vector2 originalDotSatSize;
    private float originalFrameSatPPU;
    private float originalHairSatPPU;
    private float originalDotSatPPU;

    private enum SpecialKey
    {
        None,
        MouseLeft,
        MouseRight,
        MouseMiddle,
        MouseWheelUp,
        MouseWheelDown
    }

    // Helper to convert SpecialKey to display string
    private static string SpecialKeyToString(SpecialKey key)
    {
        switch (key)
        {
            case SpecialKey.MouseLeft: return "Mouse Left";
            case SpecialKey.MouseRight: return "Mouse Right";
            case SpecialKey.MouseMiddle: return "Mouse Middle";
            case SpecialKey.MouseWheelUp: return "Scroll Up";
            case SpecialKey.MouseWheelDown: return "Scroll Down";
            default: return "";
        }
    }
//
    // Keybinds can now be a mix of KeyCode and SpecialKey
    private struct KeybindEntry
    {
        public KeyCode keyCode;
        public SpecialKey specialKey;
        public KeybindEntry(KeyCode k) { keyCode = k; specialKey = SpecialKey.None; }
        public KeybindEntry(SpecialKey s) { keyCode = KeyCode.None; specialKey = s; }
        public override string ToString()
        {
            if (specialKey != SpecialKey.None) return SpecialKeyToString(specialKey);
            return keyCode.ToString();
        }
    }

    private List<KeybindEntry> currentKeybind = new List<KeybindEntry> { new KeybindEntry(KeyCode.F2) }; // Default: F2 (Fn+F2 is hardware, so use F2)
    private bool recordingKeybind = false;
    private HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>();
    private List<KeyCode> lastPressedKeys = new List<KeyCode>();
    [HideInInspector]
    public bool uiVisible = true;
    private float keybindReleaseTimer = 0f;
    private float keybindReleaseGrace = 0.3f; // seconds
    private bool keybindWasHeld = false;

    private enum TabType { Frame, Hair, Dot }
    private TabType currentTab = TabType.Frame;

    // --- Preset System ---
    private const int PRESET_COUNT = 5;
    private string[] presets = new string[PRESET_COUNT];
    private int currentPresetIndex = 0;
    private bool isLoadingPreset = false; // ADDED: Prevents UI feedback loops

    // --- Preset Keybind System ---
    private List<KeybindEntry>[] presetKeybinds = new List<KeybindEntry>[PRESET_COUNT];
    private bool[] presetHoldModes = new bool[PRESET_COUNT];
    private bool[] presetKeybindWasHeld = new bool[PRESET_COUNT];
    private int[] presetReturnIndex = new int[PRESET_COUNT]; // For hold mode: remember which preset to return to
    private bool recordingPresetKeybind = false;
    private int recordingPresetIndex = -1;
    private HashSet<KeyCode> presetPressedKeys = new HashSet<KeyCode>();
    private List<KeyCode> presetLastPressedKeys = new List<KeyCode>();
    private float presetKeybindReleaseTimer = 0f;

    // --- Tab Button Animation State ---
    private class TabButtonAnimState {
        public Vector2 currentPos, targetPos;
        public Color currentColor, targetColor;
        public float currentScale, targetScale;
        public RectTransform rt;
        public Image img;
        public TabButtonAnimState(UnityEngine.UI.Button btn) {
            if (btn == null) return;
            rt = btn.GetComponent<RectTransform>();
            img = btn.GetComponent<Image>();
            if (rt != null) currentPos = targetPos = rt.anchoredPosition;
            if (img != null) currentColor = targetColor = img.color;
            currentScale = targetScale = 0.02f; // default scale for your buttons
        }
    }
    private TabButtonAnimState frameTabAnim, hairTabAnim, dotTabAnim;
    public float tabLerpSpeed = 12f; // Higher = snappier

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        DrawFrame(vh);
        DrawHairs(vh);
        DrawDot(vh);
    }

    new void Awake()
    {      
        // Enable running in background - CRITICAL for overlay functionality
    UnityEngine.Application.runInBackground = true;
    
    // Prevent the application from pausing when losing focus
    Time.timeScale = 1f;
    
    // Your existing Awake code...
    frameShape = CrosshairShape.Circle;
    frameFilled = false;
        // --- Frame ---
        frameShape = CrosshairShape.Circle;
        frameFilled = false;
        frameColor = Color.red;
        frameOpacity = 1f;
        frameScale = 0.9f;
        frameRotation = 0f;
        frameThickness = 5f;

        // --- Hairs ---
        hairStyle = HairStyle.Even;
        hairCount = 4;
        customAngle = 0f;
        hairThickness = 5.5f;
        hairLength = 24f;
        hairColor = Color.red;
        hairOpacity = 1f;
        hairsRotation = 0f;

        // --- Dot ---
        dotShape = CrosshairShape.Square;
        dotFilled = true;
        dotColor = Color.red;
        dotOpacity = 1f;
        dotScale = 0.9f;
        dotRotation = 45f;

        // --- HSV ---
        frameHue = 1f;
        frameSaturation = 1f;
        frameValue = 1f;
        hairHue = 1f;
        hairSaturation = 1f;
        hairValue = 1f;
        dotHue = 1f;
        dotSaturation = 1f;
        dotValue = 1f;

        raycastTarget = false;
    }

    new void Start()
    {
        UnityEngine.Application.runInBackground = true;
    
    // Set target frame rate to maintain responsiveness
    UnityEngine.Application.targetFrameRate = 60;
    
    // Handle focus events
    UnityEngine.Application.focusChanged += OnApplicationFocus;
        // Cache Image components from preview GameObjects
        if (frameColorPreviewObj) frameColorPreviewRenderer = frameColorPreviewObj.GetComponent<SpriteRenderer>();
        if (hairColorPreviewObj) hairColorPreviewRenderer = hairColorPreviewObj.GetComponent<SpriteRenderer>();
        if (dotColorPreviewObj) dotColorPreviewRenderer = dotColorPreviewObj.GetComponent<SpriteRenderer>();
        if (frameSaturationRefObj) frameSaturationRefRenderer = frameSaturationRefObj.GetComponent<SpriteRenderer>();
        if (frameValueRefObj) frameValueRefRenderer = frameValueRefObj.GetComponent<SpriteRenderer>();
        if (hairSaturationRefObj) hairSaturationRefRenderer = hairSaturationRefObj.GetComponent<SpriteRenderer>();
        if (hairValueRefObj) hairValueRefRenderer = hairValueRefObj.GetComponent<SpriteRenderer>();
        if (dotSaturationRefObj) dotSaturationRefRenderer = dotSaturationRefObj.GetComponent<SpriteRenderer>();
        if (dotValueRefObj) dotValueRefRenderer = dotValueRefObj.GetComponent<SpriteRenderer>();
        // --- Frame UI ---
        if (frameShapeDropdown) frameShapeDropdown.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            frameShape = (CrosshairShape)val; SetVerticesDirty(); 
        });
        if (frameFilledToggle) frameFilledToggle.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            frameFilled = val; SetVerticesDirty(); 
        });
        if (frameOpacitySlider) frameOpacitySlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            frameOpacity = val; SetVerticesDirty(); 
        });
        if (frameScaleSlider) frameScaleSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            frameScale = val; SetVerticesDirty(); 
        });
        if (frameRotationSlider) frameRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = frameRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            frameRotation = value;
            if (SnapEnabled) frameRotationSlider.value = value;
            SetVerticesDirty();
        });
        if (frameThicknessSlider) frameThicknessSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            frameThickness = val; SetVerticesDirty(); 
        });
        if (frameColorSlider) {
            frameColorSlider.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                frameHue = val;
                frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
                SetVerticesDirty();
                ShowColorPreview(frameColorPreviewObj, frameColorPreviewRenderer, frameColor, PreviewType.Frame);
                UpdateSaturationReferenceColor(frameSaturationRefRenderer, frameHue);
            });
        }
        if (frameSaturationSlider) frameSaturationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            frameSaturation = val;
            frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
            SetVerticesDirty();
            ShowReferenceImage(frameSaturationRefObj, frameSaturationRefRenderer, RefType.FrameSaturation);
        });
        if (frameValueSlider) frameValueSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            frameValue = val;
            frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
            SetVerticesDirty();
            ShowReferenceImage(frameValueRefObj, frameValueRefRenderer, RefType.FrameValue);
        });

        // --- Hairs UI ---
        if (hairStyleDropdown) hairStyleDropdown.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            hairStyle = (HairStyle)val; SetVerticesDirty(); UpdateHairUI(); 
        });
        if (hairCountSlider) hairCountSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            hairCount = Mathf.RoundToInt(val); SetVerticesDirty(); 
        });
        if (customAngleSlider) customAngleSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            customAngle = val; SetVerticesDirty(); 
        });
        if (hairThicknessSlider) hairThicknessSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            hairThickness = val; SetVerticesDirty(); 
        });
        if (hairLengthSlider) hairLengthSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            hairLength = val; SetVerticesDirty(); 
        });
        if (hairsRotationSlider) hairsRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = hairsRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            hairsRotation = value;
            if (SnapEnabled) hairsRotationSlider.value = value;
            SetVerticesDirty();
        });
        if (hairColorSlider) {
            hairColorSlider.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                hairHue = val;
                hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
                SetVerticesDirty();
                ShowColorPreview(hairColorPreviewObj, hairColorPreviewRenderer, hairColor, PreviewType.Hair);
                UpdateSaturationReferenceColor(hairSaturationRefRenderer, hairHue);
            });
        }
        if (hairSaturationSlider) hairSaturationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            hairSaturation = val;
            hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
            SetVerticesDirty();
            ShowReferenceImage(hairSaturationRefObj, hairSaturationRefRenderer, RefType.HairSaturation);
        });
        if (hairValueSlider) hairValueSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            hairValue = val;
            hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
            SetVerticesDirty();
            ShowReferenceImage(hairValueRefObj, hairValueRefRenderer, RefType.HairValue);
        });
        if (hairOpacitySlider) hairOpacitySlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            hairOpacity = val; SetVerticesDirty(); 
        });
        if (hairsExtendPastFrameToggle) hairsExtendPastFrameToggle.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            SetVerticesDirty(); 
        });
        if (hairDistanceSlider) hairDistanceSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            SetVerticesDirty(); 
        });

        // --- Dot UI ---
        if (dotShapeDropdown) dotShapeDropdown.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            dotShape = (CrosshairShape)val; SetVerticesDirty(); 
        });
        if (dotFilledToggle) dotFilledToggle.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            dotFilled = val; SetVerticesDirty(); 
        });
        if (dotColorSlider) {
            dotColorSlider.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                dotHue = val;
                dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
                SetVerticesDirty();
                ShowColorPreview(dotColorPreviewObj, dotColorPreviewRenderer, dotColor, PreviewType.Dot);
                UpdateSaturationReferenceColor(dotSaturationRefRenderer, dotHue);
            });
        }
        if (dotSaturationSlider) dotSaturationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            dotSaturation = val;
            dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
            SetVerticesDirty();
            ShowReferenceImage(dotSaturationRefObj, dotSaturationRefRenderer, RefType.DotSaturation);
        });
        if (dotValueSlider) dotValueSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            dotValue = val;
            dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
            SetVerticesDirty();
            ShowReferenceImage(dotValueRefObj, dotValueRefRenderer, RefType.DotValue);
        });
        if (dotOpacitySlider) dotOpacitySlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            dotOpacity = val; SetVerticesDirty(); 
        });
        if (dotScaleSlider) dotScaleSlider.onValueChanged.AddListener(val => { 
            if (isLoadingPreset) return;
            dotScale = val; SetVerticesDirty(); 
        });
        if (dotRotationSlider) dotRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = dotRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            dotRotation = value;
            if (SnapEnabled) dotRotationSlider.value = value;
            SetVerticesDirty();
        });

        if (snapRotationToggle) snapRotationToggle.onValueChanged.AddListener(OnSnapToggleChanged);

        // Tab button listeners
        if (frameTabButton) frameTabButton.onClick.AddListener(() => ShowTab(TabType.Frame));
        if (hairTabButton) hairTabButton.onClick.AddListener(() => ShowTab(TabType.Hair));
        if (dotTabButton) dotTabButton.onClick.AddListener(() => ShowTab(TabType.Dot));
        // Show default tab
        ShowTab(currentTab);

        // Keybind record button
        if (keybindRecordButton) keybindRecordButton.onClick.AddListener(StartKeybindRecording);

        UpdateHairUI();

        if (hideCrosshairButton) hideCrosshairButton.onClick.AddListener(ToggleCrosshair);
        UpdateHideCrosshairButtonText();
        UpdateHideCrosshairButtonSprite();

        if (saveCodeButton) saveCodeButton.onClick.AddListener(SaveCrosshairCode);
        if (loadCodeButton) loadCodeButton.onClick.AddListener(LoadCrosshairCode);
        if (generateImageButton) generateImageButton.onClick.AddListener(GenerateAndOpenImage);
        if (clearButton) clearButton.onClick.AddListener(ClearToDefaults);
        
        // Setup preset system
        SetupPresetSystem();
        
        // Setup preset keybind system
        SetupPresetKeybindSystem();

        if (clearAllPresetKeybindsButton != null)
            clearAllPresetKeybindsButton.onClick.AddListener(ClearAllPresetKeybinds);

        frameTabAnim = new TabButtonAnimState(frameTabButton);
        hairTabAnim = new TabButtonAnimState(hairTabButton);
        dotTabAnim = new TabButtonAnimState(dotTabButton);

        // Immediately set tab button visuals to match the current tab (no lerp on first frame)
        UpdateTabButtonVisuals(currentTab);
        ApplyTabButtonAnimStateInstant(frameTabAnim);
        ApplyTabButtonAnimStateInstant(hairTabAnim);
        ApplyTabButtonAnimStateInstant(dotTabAnim);

        // Initialize saturation gradient textures - first store original sizes
        StoreOriginalSaturationSizes();
        UpdateSaturationReferenceColor(frameSaturationRefRenderer, frameHue);
        UpdateSaturationReferenceColor(hairSaturationRefRenderer, hairHue);
        UpdateSaturationReferenceColor(dotSaturationRefRenderer, dotHue);
    }

    private void StoreOriginalSaturationSizes()
    {
        // Use the actual saturation texture size (960x150) as our reference
        Vector2 defaultSize = new Vector2(960, 150);
        float defaultPPU = 100f;
        
        // Store frame saturation original properties
        if (frameSaturationRefRenderer != null && frameSaturationRefRenderer.sprite != null)
        {
            var sprite = frameSaturationRefRenderer.sprite;
            originalFrameSatSize = sprite.rect.size;
            originalFrameSatPPU = sprite.pixelsPerUnit;
        }
        else
        {
            originalFrameSatSize = defaultSize;
            originalFrameSatPPU = defaultPPU;
        }

        // Store hair saturation original properties
        if (hairSaturationRefRenderer != null && hairSaturationRefRenderer.sprite != null)
        {
            var sprite = hairSaturationRefRenderer.sprite;
            originalHairSatSize = sprite.rect.size;
            originalHairSatPPU = sprite.pixelsPerUnit;
        }
        else
        {
            originalHairSatSize = defaultSize;
            originalHairSatPPU = defaultPPU;
        }

        // Store dot saturation original properties
        if (dotSaturationRefRenderer != null && dotSaturationRefRenderer.sprite != null)
        {
            var sprite = dotSaturationRefRenderer.sprite;
            originalDotSatSize = sprite.rect.size;
            originalDotSatPPU = sprite.pixelsPerUnit;
        }
        else
        {
            originalDotSatSize = defaultSize;
            originalDotSatPPU = defaultPPU;
        }
    }

    private Texture2D GenerateSaturationGradient(float hue, Vector2 size)
    {
        int width = Mathf.RoundToInt(size.x);
        int height = Mathf.RoundToInt(size.y);
        
        // Ensure minimum size to prevent errors
        width = Mathf.Max(width, 32);
        height = Mathf.Max(height, 16);
        
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        
        for (int x = 0; x < width; x++)
        {
            // Calculate saturation from 0 (left/grey) to 1 (right/fully saturated)
            float saturation = (float)x / (width - 1);
            
            // Convert HSV to RGB with fixed Value=1 (full brightness)
            Color pixelColor = Color.HSVToRGB(hue, saturation, 1f);
            
            // Fill the entire column with this color
            for (int y = 0; y < height; y++)
            {
                colors[y * width + x] = pixelColor;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
    }

    private void UpdateSaturationTexture(SpriteRenderer renderer, float hue, ref Texture2D cachedTexture)
    {
        if (renderer == null) return;
        
        // Get the appropriate original size and pixels per unit
        Vector2 originalSize;
        float originalPPU;
        
        if (renderer == frameSaturationRefRenderer)
        {
            originalSize = originalFrameSatSize;
            originalPPU = originalFrameSatPPU;
        }
        else if (renderer == hairSaturationRefRenderer)
        {
            originalSize = originalHairSatSize;
            originalPPU = originalHairSatPPU;
        }
        else if (renderer == dotSaturationRefRenderer)
        {
            originalSize = originalDotSatSize;
            originalPPU = originalDotSatPPU;
        }
        else
        {
            originalSize = new Vector2(256, 32);
            originalPPU = 100f;
        }
        
        // Destroy old texture to prevent memory leaks
        if (cachedTexture != null)
        {
            DestroyImmediate(cachedTexture);
        }
        
        // Generate new gradient texture with current hue and original size
        cachedTexture = GenerateSaturationGradient(hue, originalSize);
        
        // Create sprite from texture with original pixels per unit
        Sprite newSprite = Sprite.Create(
            cachedTexture,
            new Rect(0, 0, originalSize.x, originalSize.y),
            new Vector2(0.5f, 0.5f),
            originalPPU
        );
        
        // Apply the new sprite
        renderer.sprite = newSprite;
        renderer.color = Color.white; // Reset color to white since texture has the colors
    }

    private void ApplyTabButtonAnimStateInstant(TabButtonAnimState anim)
    {
        if (anim == null || anim.rt == null) return;
        anim.currentPos = anim.targetPos;
        anim.currentColor = anim.targetColor;
        anim.currentScale = anim.targetScale;
        anim.rt.anchoredPosition = anim.currentPos;
        if (anim.img != null) anim.img.color = anim.currentColor;
        anim.rt.localScale = Vector3.one * anim.currentScale;
    }

    // FIXED: Improved input detection and edge handling
    private bool GetKeyState(KeybindEntry k)
{
    if (k.specialKey == SpecialKey.None)
    {
        // Always use SystemInput for global keybinds (works both focused and unfocused)
        return SystemInput.GetKey(k.keyCode);
    }
    else
    {
        switch (k.specialKey)
        {
            case SpecialKey.MouseLeft:
                return SystemInput.GetMouseButton(0);
            case SpecialKey.MouseRight:
                return SystemInput.GetMouseButton(1);
            case SpecialKey.MouseMiddle:
                return SystemInput.GetMouseButton(2);
            case SpecialKey.MouseWheelUp:
                return Input.mouseScrollDelta.y > 0.01f; // Mouse wheel still uses Unity Input
            case SpecialKey.MouseWheelDown:
                return Input.mouseScrollDelta.y < -0.01f; // Mouse wheel still uses Unity Input
            default:
                return false;
        }
    }
}



    // FIXED: Edge detection to prevent window minimizing
    private bool IsMouseNearEdge()
    {
        int edgeMargin = 2;
        Vector2 mousePos = Input.mousePosition;
        return mousePos.x <= edgeMargin || mousePos.x >= UnityEngine.Screen.width - edgeMargin ||
               mousePos.y <= edgeMargin || mousePos.y >= UnityEngine.Screen.height - edgeMargin;
    }

    void Update()
    {   
        if (!UnityEngine.Application.runInBackground)
        UnityEngine.Application.runInBackground = true;

        if (recordingKeybind)
        {
            // Check all possible KeyCodes
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None) continue;
                if (Input.GetKeyDown(key))
                    pressedKeys.Add(key);
                if (Input.GetKeyUp(key))
                    pressedKeys.Remove(key);
            }
            // Mouse buttons
            if (Input.GetMouseButtonDown(0)) pressedKeys.Add(KeyCode.Mouse0);
            if (Input.GetMouseButtonUp(0)) pressedKeys.Remove(KeyCode.Mouse0);
            if (Input.GetMouseButtonDown(1)) pressedKeys.Add(KeyCode.Mouse1);
            if (Input.GetMouseButtonUp(1)) pressedKeys.Remove(KeyCode.Mouse1);
            if (Input.GetMouseButtonDown(2)) pressedKeys.Add(KeyCode.Mouse2);
            if (Input.GetMouseButtonUp(2)) pressedKeys.Remove(KeyCode.Mouse2);
            // Mouse wheel (record as a one-shot event)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (scroll > 0) pressedKeys.Add(KeyCode.JoystickButton10);
                else if (scroll < 0) pressedKeys.Add(KeyCode.JoystickButton11);
            }
            // Track the last non-empty set of pressed keys
            if (pressedKeys.Count > 0)
            {
                lastPressedKeys = new List<KeyCode>(pressedKeys);
                keybindReleaseTimer = 0f;
                var displayList = new List<KeybindEntry>();
                foreach (var k in lastPressedKeys)
                {
                    if (k == KeyCode.Mouse0) displayList.Add(new KeybindEntry(SpecialKey.MouseLeft));
                    else if (k == KeyCode.Mouse1) displayList.Add(new KeybindEntry(SpecialKey.MouseRight));
                    else if (k == KeyCode.Mouse2) displayList.Add(new KeybindEntry(SpecialKey.MouseMiddle));
                    else if (k == KeyCode.JoystickButton10) displayList.Add(new KeybindEntry(SpecialKey.MouseWheelUp));
                    else if (k == KeyCode.JoystickButton11) displayList.Add(new KeybindEntry(SpecialKey.MouseWheelDown));
                    else displayList.Add(new KeybindEntry(k));
                }
                if (keybindRecordButton) keybindRecordButton.GetComponentInChildren<TMPro.TMP_Text>().text = KeybindToString(displayList);
            }
            if (pressedKeys.Count == 0 && lastPressedKeys.Count > 0)
            {
                keybindReleaseTimer += Time.unscaledDeltaTime;
                if (keybindReleaseTimer >= keybindReleaseGrace)
                {
                    currentKeybind = new List<KeybindEntry>();
                    foreach (var k in lastPressedKeys)
                    {
                        if (k == KeyCode.Mouse0) currentKeybind.Add(new KeybindEntry(SpecialKey.MouseLeft));
                        else if (k == KeyCode.Mouse1) currentKeybind.Add(new KeybindEntry(SpecialKey.MouseRight));
                        else if (k == KeyCode.Mouse2) currentKeybind.Add(new KeybindEntry(SpecialKey.MouseMiddle));
                        else if (k == KeyCode.JoystickButton10) currentKeybind.Add(new KeybindEntry(SpecialKey.MouseWheelUp));
                        else if (k == KeyCode.JoystickButton11) currentKeybind.Add(new KeybindEntry(SpecialKey.MouseWheelDown));
                        else currentKeybind.Add(new KeybindEntry(k));
                    }
                    recordingKeybind = false;
                    if (keybindRecordButton) keybindRecordButton.GetComponentInChildren<TMPro.TMP_Text>().text = KeybindToString(currentKeybind);
                }
            }
            else if (pressedKeys.Count > 0)
            {
                keybindReleaseTimer = 0f;
            }
        }
        else if (recordingPresetKeybind)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None) continue;
                if (Input.GetKeyDown(key))
                    presetPressedKeys.Add(key);
                if (Input.GetKeyUp(key))
                    presetPressedKeys.Remove(key);
            }
            if (Input.GetMouseButtonDown(0)) presetPressedKeys.Add(KeyCode.Mouse0);
            if (Input.GetMouseButtonUp(0)) presetPressedKeys.Remove(KeyCode.Mouse0);
            if (Input.GetMouseButtonDown(1)) presetPressedKeys.Add(KeyCode.Mouse1);
            if (Input.GetMouseButtonUp(1)) presetPressedKeys.Remove(KeyCode.Mouse1);
            if (Input.GetMouseButtonDown(2)) presetPressedKeys.Add(KeyCode.Mouse2);
            if (Input.GetMouseButtonUp(2)) presetPressedKeys.Remove(KeyCode.Mouse2);
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (scroll > 0) presetPressedKeys.Add(KeyCode.JoystickButton10);
                else if (scroll < 0) presetPressedKeys.Add(KeyCode.JoystickButton11);
            }
            if (presetPressedKeys.Count > 0)
            {
                presetLastPressedKeys = new List<KeyCode>(presetPressedKeys);
                presetKeybindReleaseTimer = 0f;
                var displayList = new List<KeybindEntry>();
                foreach (var k in presetLastPressedKeys)
                {
                    if (k == KeyCode.Mouse0) displayList.Add(new KeybindEntry(SpecialKey.MouseLeft));
                    else if (k == KeyCode.Mouse1) displayList.Add(new KeybindEntry(SpecialKey.MouseRight));
                    else if (k == KeyCode.Mouse2) displayList.Add(new KeybindEntry(SpecialKey.MouseMiddle));
                    else if (k == KeyCode.JoystickButton10) displayList.Add(new KeybindEntry(SpecialKey.MouseWheelUp));
                    else if (k == KeyCode.JoystickButton11) displayList.Add(new KeybindEntry(SpecialKey.MouseWheelDown));
                    else displayList.Add(new KeybindEntry(k));
                }
                if (presetKeybindButtons[recordingPresetIndex] != null)
                {
                    presetKeybindButtons[recordingPresetIndex].GetComponentInChildren<TMPro.TMP_Text>().text = KeybindToString(displayList);
                }
            }
            if (presetPressedKeys.Count == 0 && presetLastPressedKeys.Count > 0)
            {
                presetKeybindReleaseTimer += Time.unscaledDeltaTime;
                if (presetKeybindReleaseTimer >= keybindReleaseGrace)
                {
                    presetKeybinds[recordingPresetIndex] = new List<KeybindEntry>();
                    foreach (var k in presetLastPressedKeys)
                    {
                        if (k == KeyCode.Mouse0) presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(SpecialKey.MouseLeft));
                        else if (k == KeyCode.Mouse1) presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(SpecialKey.MouseRight));
                        else if (k == KeyCode.Mouse2) presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(SpecialKey.MouseMiddle));
                        else if (k == KeyCode.JoystickButton10) presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(SpecialKey.MouseWheelUp));
                        else if (k == KeyCode.JoystickButton11) presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(SpecialKey.MouseWheelDown));
                        else presetKeybinds[recordingPresetIndex].Add(new KeybindEntry(k));
                    }
                    recordingPresetKeybind = false;
                    recordingPresetIndex = -1;
                    UpdatePresetKeybindButtonTexts();
                    SavePresetKeybindsToStorage();
                }
            }
            else if (presetPressedKeys.Count > 0)
            {
                presetKeybindReleaseTimer = 0f;
            }
        }
        else
{
    // FIXED: Improved keybind detection with edge handling
    bool allHeld = currentKeybind.Count > 0;
    foreach (var k in currentKeybind)
    {
        if (!GetKeyState(k)) allHeld = false;
    }
    
    // UI safety: if UI is visible and pointer is over UI, ignore mouse keybinds
    bool mouseKeyInBind = currentKeybind.Any(k => k.specialKey != SpecialKey.None && k.specialKey != SpecialKey.MouseWheelUp && k.specialKey != SpecialKey.MouseWheelDown);
    if (allHeld)
    {
        bool pointerOverUI = uiVisible && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool nearEdge = IsMouseNearEdge();
        
        if (!(mouseKeyInBind && (pointerOverUI || nearEdge)))
        {
            if (!keybindWasHeld)
            {
                ToggleUI();
                keybindWasHeld = true;
            }
        }
    }
    else
    {
        keybindWasHeld = false;
    }
    
    // FIXED: Preset keybind detection with edge handling
    for (int i = 0; i < PRESET_COUNT; i++)
    {
        if (presetKeybinds[i].Count > 0)
        {
            bool allPresetKeysHeld = true;
            foreach (var k in presetKeybinds[i])
            {
                if (!GetKeyState(k))
                {
                    allPresetKeysHeld = false;
                    break;
                }
            }
            bool presetMouseKey = presetKeybinds[i].Any(k => k.specialKey != SpecialKey.None && k.specialKey != SpecialKey.MouseWheelUp && k.specialKey != SpecialKey.MouseWheelDown);
            bool pointerOverUI = uiVisible && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool nearEdge = IsMouseNearEdge();
            
            if (allPresetKeysHeld)
            {
                if (!(presetMouseKey && (pointerOverUI || nearEdge)))
                {
                    if (!presetKeybindWasHeld[i])
                    {
                        if (presetHoldModes[i])
                        {
                            presetReturnIndex[i] = currentPresetIndex;
                            SwitchToPreset(i);
                        }
                        else
                        {
                            SwitchToPreset(i);
                        }
                        presetKeybindWasHeld[i] = true;
                    }
                }
            }
            else
            {
                if (presetKeybindWasHeld[i] && presetHoldModes[i])
                {
                    SwitchToPreset(presetReturnIndex[i]);
                }
                presetKeybindWasHeld[i] = false;
            }
        }
    }
}


        // --- Tab Button Animation ---
        AnimateTabButton(frameTabAnim);
        AnimateTabButton(hairTabAnim);
        AnimateTabButton(dotTabAnim);
    }

    private void AnimateTabButton(TabButtonAnimState anim)
    {
        if (anim == null || anim.rt == null) return;
        anim.currentPos = Vector2.Lerp(anim.currentPos, anim.targetPos, Time.unscaledDeltaTime * tabLerpSpeed);
        anim.currentColor = Color.Lerp(anim.currentColor, anim.targetColor, Time.unscaledDeltaTime * tabLerpSpeed);
        anim.currentScale = Mathf.Lerp(anim.currentScale, anim.targetScale, Time.unscaledDeltaTime * tabLerpSpeed);
        anim.rt.anchoredPosition = anim.currentPos;
        if (anim.img != null) anim.img.color = anim.currentColor;
        anim.rt.localScale = Vector3.one * anim.currentScale;
    }

    void StartKeybindRecording()
    {
        recordingKeybind = true;
        pressedKeys.Clear();
        currentKeybind.Clear();
        lastPressedKeys.Clear();
        keybindReleaseTimer = 0f;
        if (keybindRecordButton) keybindRecordButton.GetComponentInChildren<TMPro.TMP_Text>().text = "Press keys...";
    }

    void ToggleUI()
    {
        uiVisible = !uiVisible;
        if (uiRoot) uiRoot.SetActive(uiVisible);
    }

    string KeybindToString(List<KeybindEntry> keys)
    {
        if (keys == null || keys.Count == 0) return "Set Keybind";
        return string.Join(" + ", keys.Select(k => k.ToString()));
    }

    private void OnSnapToggleChanged(bool isOn)
    {
        // When toggled on, snap all current slider values
        if (isOn)
        {
            if (frameRotationSlider)
            {
                float snapped = Mathf.Round(frameRotationSlider.value / 45f) * 45f;
                frameRotationSlider.value = snapped;
                frameRotation = snapped;
            }
            if (hairsRotationSlider)
            {
                float snapped = Mathf.Round(hairsRotationSlider.value / 45f) * 45f;
                hairsRotationSlider.value = snapped;
                hairsRotation = snapped;
            }
            if (dotRotationSlider)
            {
                float snapped = Mathf.Round(dotRotationSlider.value / 45f) * 45f;
                dotRotationSlider.value = snapped;
                dotRotation = snapped;
            }
            SetVerticesDirty();
        }
    }

    void UpdateHairUI()
    {
        bool isCustom = hairStyle == HairStyle.Custom;
        if (customAngleSlider) customAngleSlider.gameObject.SetActive(isCustom);
        if (hairCountSlider) hairCountSlider.gameObject.SetActive(!isCustom);
    }

    // Continue with all your existing methods unchanged...
    // [Rest of the methods remain exactly the same as your current script]
    private void OnApplicationFocus(bool hasFocus)
{
    // Always keep running regardless of focus state
    UnityEngine.Application.runInBackground = true;
    Time.timeScale = 1f;
    
    // Log focus state for debugging
    Debug.Log($"Application focus changed: {hasFocus}");
}

private void OnApplicationPause(bool pauseStatus)
{
    // Prevent pausing when losing focus
    if (pauseStatus)
    {
        UnityEngine.Application.runInBackground = true;
        Time.timeScale = 1f;
    }
}

private void OnDestroy()
{
    // Clean up event listener
    UnityEngine.Application.focusChanged -= OnApplicationFocus;
    
    // Clean up generated textures
    if (frameSaturationTexture != null) DestroyImmediate(frameSaturationTexture);
    if (hairSaturationTexture != null) DestroyImmediate(hairSaturationTexture);
    if (dotSaturationTexture != null) DestroyImmediate(dotSaturationTexture);
}

    // FIXED: Improved preset switching to prevent feedback loops
    private void OnPresetChanged(int newIndex)
    {
        if (isLoadingPreset) return;
        
        // Autosave current preset before switching (dropdown only)
        presets[currentPresetIndex] = GenerateCrosshairCode();
        SavePresetToStorage(currentPresetIndex);
        currentPresetIndex = newIndex;
        LoadCrosshairFromCode(presets[currentPresetIndex]);
        Debug.Log($"Switched to Preset {newIndex + 1}");
    }

    private void SwitchToPreset(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < PRESET_COUNT)
        {
            // Prevent feedback loops during preset loading
            isLoadingPreset = true;
            
            currentPresetIndex = presetIndex;
            if (presetDropdown != null)
            {
                // Temporarily remove listener to prevent autosave
                presetDropdown.onValueChanged.RemoveListener(OnPresetChanged);
                presetDropdown.value = currentPresetIndex;
                // Re-add listener
                presetDropdown.onValueChanged.AddListener(OnPresetChanged);
            }
            LoadCrosshairFromCode(presets[currentPresetIndex]);
            
            isLoadingPreset = false;
            Debug.Log($"Switched to Preset {presetIndex + 1} via keybind");
        }
    }

    // Rest of your methods remain exactly the same...
    // [Include all remaining methods from your original script without changes]
    // Drawing Methods, Color Preview Logic, Preset System Methods, etc.
    // ... [Copy all remaining methods exactly as they are in your current script]
    // --- Drawing Methods ---
    void DrawFrame(VertexHelper vh)
    {
        Color frameCol = frameColor;
        frameCol.a *= frameOpacity;
        float size = rectTransform.rect.width * 0.5f * frameScale;
        Vector2 center = rectTransform.rect.center;
        float rotRad = -frameRotation * Mathf.Deg2Rad; // Invert for clockwise
        switch (frameShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, frameCol, frameFilled, 64, rotRad);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, frameCol, frameFilled, rotRad);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, frameCol, frameFilled, rotRad);
                break;
        }
    }

    void DrawHairs(VertexHelper vh)
    {
        Color hairCol = hairColor;
        hairCol.a *= hairOpacity;
        float size = rectTransform.rect.width * 0.5f * frameScale;
        Vector2 center = rectTransform.rect.center;
        int count = Mathf.Max(1, hairCount);
        float angleStep = 360f / count;
        float thickness = hairThickness;
        float length = hairLength * frameScale;
        float baseRot = -hairsRotation * Mathf.Deg2Rad; // Invert for clockwise
        float hairDistance = hairDistanceSlider != null ? hairDistanceSlider.value : 0.2f; // Default to 0.2 if not set
        if (hairStyle == HairStyle.Custom)
        {
            angleStep = customAngle;
            count = Mathf.Max(1, Mathf.FloorToInt(360f / angleStep));
        }
        float startRadius = size * hairDistance;
        float endRadius = hairsExtendPastFrameToggle != null && hairsExtendPastFrameToggle.isOn
            ? startRadius + length
            : Mathf.Min(size * 0.9f, startRadius + length);
        for (int i = 0; i < count; i++)
        {
            float angle = baseRot + i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 start = center + dir * startRadius;
            Vector2 end = center + dir * endRadius;
            DrawThickLine(vh, start, end, thickness, hairCol);
        }
    }

    void DrawDot(VertexHelper vh)
    {
        Color dotCol = dotColor;
        dotCol.a *= dotOpacity;
        float size = rectTransform.rect.width * 0.08f * dotScale;
        Vector2 center = rectTransform.rect.center;
        float rotRad = -dotRotation * Mathf.Deg2Rad; // Invert for clockwise
        switch (dotShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, dotCol, dotFilled, 32, rotRad);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, dotCol, dotFilled, rotRad);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, dotCol, dotFilled, rotRad);
                break;
        }
    }

    // --- Helper Methods ---
    // Rotates a point around a pivot by angle (in radians)
    Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Vector2 dir = point - pivot;
        Vector2 rotated = new Vector2(
            dir.x * cos - dir.y * sin,
            dir.x * sin + dir.y * cos
        );
        return rotated + pivot;
    }

    void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, bool filled, int segments, float rotation = 0f)
    {
        if (filled)
        {
            int startIndex = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = rotation + 2 * Mathf.PI * i / segments;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(pos, color, Vector2.zero);
            }
            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
            }
        }
        else
        {
            // Draw as a ring (polygon) to avoid seams
            int startIndex = vh.currentVertCount;
            float halfThickness = frameThickness * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = rotation + 2 * Mathf.PI * i / segments;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outer = center + dir * (radius + halfThickness);
                Vector2 inner = center + dir * (radius - halfThickness);
                vh.AddVert(outer, color, Vector2.zero);
                vh.AddVert(inner, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int idx = startIndex + i * 2;
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx + 1, idx + 3, idx + 2);
            }
        }
    }

    void DrawSquare(VertexHelper vh, Vector2 center, float size, Color color, bool filled, float rotation = 0f)
    {
        Vector2 half = Vector2.one * size;
        Vector2[] corners = new Vector2[4]
        {
            new Vector2(-half.x, half.y),
            new Vector2(half.x, half.y),
            new Vector2(half.x, -half.y),
            new Vector2(-half.x, -half.y)
        };
        // Rotate all corners as a group
        for (int i = 0; i < 4; i++)
            corners[i] = RotatePoint(corners[i] + center, center, rotation);
        if (filled)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < 4; i++)
                vh.AddVert(corners[i], color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
        else
        {
            // Uniform outline: offset corners along direction from center
            int startIndex = vh.currentVertCount;
            float halfThickness = frameThickness * 0.5f;
            for (int i = 0; i <= 4; i++)
            {
                int idx = i % 4;
                Vector2 dir = (corners[idx] - center).normalized;
                Vector2 outer = corners[idx] + dir * halfThickness;
                Vector2 inner = corners[idx] - dir * halfThickness;
                vh.AddVert(outer, color, Vector2.zero);
                vh.AddVert(inner, color, Vector2.zero);
            }
            for (int i = 0; i < 4; i++)
            {
                int idx = startIndex + i * 2;
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx + 1, idx + 3, idx + 2);
            }
        }
    }

    void DrawTriangle(VertexHelper vh, Vector2 center, float size, Color color, bool filled, float rotation = 0f)
    {
        float h = size * Mathf.Sqrt(3) / 2;
        // Flip Y to make triangle point down
        Vector2[] pts = new Vector2[3]
        {
            new Vector2(0, -h),
            new Vector2(-size, h / 2),
            new Vector2(size, h / 2)
        };
        // Rotate all points as a group
        for (int i = 0; i < 3; i++)
            pts[i] = RotatePoint(pts[i] + center, center, rotation);
        if (filled)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < 3; i++)
                vh.AddVert(pts[i], color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
        else
        {
            // Uniform outline: offset corners along direction from center
            int startIndex = vh.currentVertCount;
            float halfThickness = frameThickness * 0.5f;
            for (int i = 0; i <= 3; i++)
            {
                int idx = i % 3;
                Vector2 dir = (pts[idx] - center).normalized;
                Vector2 outer = pts[idx] + dir * halfThickness;
                Vector2 inner = pts[idx] - dir * halfThickness;
                vh.AddVert(outer, color, Vector2.zero);
                vh.AddVert(inner, color, Vector2.zero);
            }
            for (int i = 0; i < 3; i++)
            {
                int idx = startIndex + i * 2;
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx + 1, idx + 3, idx + 2);
            }
        }
    }

    void DrawThickLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);
        int idx = vh.currentVertCount;
        vh.AddVert(start - normal, color, Vector2.zero);
        vh.AddVert(start + normal, color, Vector2.zero);
        vh.AddVert(end + normal, color, Vector2.zero);
        vh.AddVert(end - normal, color, Vector2.zero);
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    // --- Color Preview Fade Logic ---
    private Coroutine framePreviewFadeCoroutine;
    private Coroutine hairPreviewFadeCoroutine;
    private Coroutine dotPreviewFadeCoroutine;
    private float previewFadeDelay = 1.0f; // seconds before fade starts
    private float previewFadeDuration = 0.5f; // fade out duration

    // --- Reference Image Fade Logic ---
    private Coroutine frameSaturationRefFadeCoroutine;
    private Coroutine frameValueRefFadeCoroutine;
    private Coroutine hairSaturationRefFadeCoroutine;
    private Coroutine hairValueRefFadeCoroutine;
    private Coroutine dotSaturationRefFadeCoroutine;
    private Coroutine dotValueRefFadeCoroutine;
    private float refFadeDelay = 1.0f;
    private float refFadeDuration = 0.5f;

    private enum PreviewType { Frame, Hair, Dot }
    private enum RefType { FrameSaturation, FrameValue, HairSaturation, HairValue, DotSaturation, DotValue }

    void ShowColorPreview(GameObject previewObj, SpriteRenderer previewRenderer, Color color, PreviewType type)
    {
        if (previewObj == null || previewRenderer == null) {
            return;
        }
        // Only set alpha to 1, keep RGB as is
        var c = previewRenderer.color;
        previewRenderer.color = new Color(c.r, c.g, c.b, 1f);
        previewObj.SetActive(true);
        // Start fade coroutine
        switch (type) {
            case PreviewType.Frame:
                if (framePreviewFadeCoroutine != null) StopCoroutine(framePreviewFadeCoroutine);
                framePreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewRenderer, PreviewType.Frame));
                break;
            case PreviewType.Hair:
                if (hairPreviewFadeCoroutine != null) StopCoroutine(hairPreviewFadeCoroutine);
                hairPreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewRenderer, PreviewType.Hair));
                break;
            case PreviewType.Dot:
                if (dotPreviewFadeCoroutine != null) StopCoroutine(dotPreviewFadeCoroutine);
                dotPreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewRenderer, PreviewType.Dot));
                break;
        }
    }

    void ShowReferenceImage(GameObject refObj, SpriteRenderer refRenderer, RefType type)
    {
        if (refObj == null || refRenderer == null) return;
        
        // Apply color filter only to saturation references
        if (type == RefType.FrameSaturation || type == RefType.HairSaturation || type == RefType.DotSaturation)
        {
            // Get the current hue for this component
            float currentHue = 0f;
            switch (type)
            {
                case RefType.FrameSaturation:
                    currentHue = frameHue;
                    break;
                case RefType.HairSaturation:
                    currentHue = hairHue;
                    break;
                case RefType.DotSaturation:
                    currentHue = dotHue;
                    break;
            }
            
            // Generate new saturation gradient texture with current hue
            if (type == RefType.FrameSaturation)
            {
                UpdateSaturationTexture(refRenderer, currentHue, ref frameSaturationTexture);
            }
            else if (type == RefType.HairSaturation)
            {
                UpdateSaturationTexture(refRenderer, currentHue, ref hairSaturationTexture);
            }
            else
            {
                UpdateSaturationTexture(refRenderer, currentHue, ref dotSaturationTexture);
            }
        }
        else
        {
            // For value references, keep original behavior
            var c = refRenderer.color;
            refRenderer.color = new Color(c.r, c.g, c.b, 1f);
        }
        
        refObj.SetActive(true);
        // Start fade coroutine
        switch (type)
        {
            case RefType.FrameSaturation:
                if (frameSaturationRefFadeCoroutine != null) StopCoroutine(frameSaturationRefFadeCoroutine);
                frameSaturationRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.FrameSaturation));
                break;
            case RefType.FrameValue:
                if (frameValueRefFadeCoroutine != null) StopCoroutine(frameValueRefFadeCoroutine);
                frameValueRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.FrameValue));
                break;
            case RefType.HairSaturation:
                if (hairSaturationRefFadeCoroutine != null) StopCoroutine(hairSaturationRefFadeCoroutine);
                hairSaturationRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.HairSaturation));
                break;
            case RefType.HairValue:
                if (hairValueRefFadeCoroutine != null) StopCoroutine(hairValueRefFadeCoroutine);
                hairValueRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.HairValue));
                break;
            case RefType.DotSaturation:
                if (dotSaturationRefFadeCoroutine != null) StopCoroutine(dotSaturationRefFadeCoroutine);
                dotSaturationRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.DotSaturation));
                break;
            case RefType.DotValue:
                if (dotValueRefFadeCoroutine != null) StopCoroutine(dotValueRefFadeCoroutine);
                dotValueRefFadeCoroutine = StartCoroutine(FadeOutReference(refObj, refRenderer, RefType.DotValue));
                break;
        }
    }

    System.Collections.IEnumerator FadeOutPreview(GameObject previewObj, SpriteRenderer previewRenderer, PreviewType type)
    {
        yield return new WaitForSeconds(previewFadeDelay);
        float t = 0f;
        Color startColor = previewRenderer.color;
        while (t < previewFadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / previewFadeDuration);
            previewRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        previewObj.SetActive(false);
        // Reset alpha for next time
        previewRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        // Null out coroutine reference
        switch (type) {
            case PreviewType.Frame: framePreviewFadeCoroutine = null; break;
            case PreviewType.Hair: hairPreviewFadeCoroutine = null; break;
            case PreviewType.Dot: dotPreviewFadeCoroutine = null; break;
        }
    }

    System.Collections.IEnumerator FadeOutReference(GameObject refObj, SpriteRenderer refRenderer, RefType type)
    {
        yield return new WaitForSeconds(refFadeDelay);
        float t = 0f;
        Color startColor = refRenderer.color;
        while (t < refFadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / refFadeDuration);
            refRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        refObj.SetActive(false);
        refRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        // Null out coroutine reference
        switch (type)
        {
            case RefType.FrameSaturation: frameSaturationRefFadeCoroutine = null; break;
            case RefType.FrameValue: frameValueRefFadeCoroutine = null; break;
            case RefType.HairSaturation: hairSaturationRefFadeCoroutine = null; break;
            case RefType.HairValue: hairValueRefFadeCoroutine = null; break;
            case RefType.DotSaturation: dotSaturationRefFadeCoroutine = null; break;
            case RefType.DotValue: dotValueRefFadeCoroutine = null; break;
        }
    }

    private bool crosshairHidden = false;

    public void ToggleCrosshair()
    {
        crosshairHidden = !crosshairHidden;
        if (crosshairHidden)
        {
            raycastTarget = false;
            canvasRenderer.cull = true;
        }
        else
        {
            raycastTarget = false; // keep clickthrough
            canvasRenderer.cull = false;
            SetVerticesDirty();
        }
        UpdateHideCrosshairButtonText();
        UpdateHideCrosshairButtonSprite();
    }

    private void UpdateHideCrosshairButtonText()
    {
        if (hideCrosshairButton)
        {
            var text = hideCrosshairButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (text)
                text.text = crosshairHidden ? "Show Crosshair" : "Hide Crosshair";
        }
    }

    private void UpdateHideCrosshairButtonSprite()
    {
        if (hideCrosshairButtonImage != null)
        {
            if (crosshairHidden)
            {
                if (crosshairHiddenSprite != null)
                    hideCrosshairButtonImage.sprite = crosshairHiddenSprite;
            }
            else
            {
                if (crosshairVisibleSprite != null)
                    hideCrosshairButtonImage.sprite = crosshairVisibleSprite;
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Save current crosshair to active preset before quitting
        presets[currentPresetIndex] = GenerateCrosshairCode();
        SavePresetToStorage(currentPresetIndex);
        
        // Save all presets to ensure nothing is lost
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            SavePresetToStorage(i);
        }
        
        // Save preset keybinds
        SavePresetKeybindsToStorage();
        
        Debug.Log("All presets and keybinds saved before application quit");
    }

    // --- Preset System Methods ---
    private void SetupPresetSystem()
    {
        // Initialize preset dropdown
        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            var options = new List<string>();
            for (int i = 0; i < PRESET_COUNT; i++)
            {
                options.Add($"Preset {i + 1}");
            }
            presetDropdown.AddOptions(options);
            presetDropdown.value = currentPresetIndex;
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }
        
        // Load presets from PlayerPrefs or initialize with defaults
        LoadPresetsFromStorage();
        
        // Load the first preset automatically
        LoadCrosshairFromCode(presets[currentPresetIndex]);
        Debug.Log($"Loaded Preset {currentPresetIndex + 1} on startup");
    }

    private void LoadPresetsFromStorage()
    {
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            string key = $"CrosshairPreset_{i}";
            if (PlayerPrefs.HasKey(key))
            {
                presets[i] = PlayerPrefs.GetString(key);
                Debug.Log($"Loaded Preset {i + 1} from storage");
            }
            else
            {
                presets[i] = GenerateCrosshairCode();
                PlayerPrefs.SetString(key, presets[i]);
                Debug.Log($"Initialized Preset {i + 1} with defaults");
            }
        }
        PlayerPrefs.Save();
    }

    private void SavePresetToStorage(int presetIndex)
    {
        string key = $"CrosshairPreset_{presetIndex}";
        PlayerPrefs.SetString(key, presets[presetIndex]);
        PlayerPrefs.Save();
        Debug.Log($"Saved Preset {presetIndex + 1} to storage");
    }

    private string GenerateCrosshairCode()
    {
        // Frame section
        string frameSection = string.Join(",",
            "F",
            FrameShapeToCode(frameShape),
            frameFilled ? "1" : "0",
            frameColor.r.ToString("F3"), frameColor.g.ToString("F3"), frameColor.b.ToString("F3"), frameColor.a.ToString("F3"),
            frameOpacity.ToString("F3"),
            frameScale.ToString("F3"),
            frameRotation.ToString("F3"),
            frameThickness.ToString("F3"),
            frameHue.ToString("F3"), frameSaturation.ToString("F3"), frameValue.ToString("F3")
        );
        // Hairs section
        string hairsSection = string.Join(",",
            "H",
            HairStyleToCode(hairStyle),
            hairCount,
            customAngle.ToString("F3"),
            hairThickness.ToString("F3"),
            hairLength.ToString("F3"),
            hairColor.r.ToString("F3"), hairColor.g.ToString("F3"), hairColor.b.ToString("F3"), hairColor.a.ToString("F3"),
            hairOpacity.ToString("F3"),
            hairsRotation.ToString("F3"),
            hairHue.ToString("F3"), hairSaturation.ToString("F3"), hairValue.ToString("F3"),
            (hairsExtendPastFrameToggle != null && hairsExtendPastFrameToggle.isOn) ? "1" : "0"
        );
        // Dot section
        string dotSection = string.Join(",",
            "D",
            FrameShapeToCode(dotShape),
            dotFilled ? "1" : "0",
            dotColor.r.ToString("F3"), dotColor.g.ToString("F3"), dotColor.b.ToString("F3"), dotColor.a.ToString("F3"),
            dotOpacity.ToString("F3"),
            dotScale.ToString("F3"),
            dotRotation.ToString("F3"),
            dotHue.ToString("F3"), dotSaturation.ToString("F3"), dotValue.ToString("F3")
        );
        return frameSection + ";" + hairsSection + ";" + dotSection;
    }

    private void LoadCrosshairFromCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        
        // FIXED: Set flag to prevent feedback loops
        isLoadingPreset = true;
        
        string[] sections = code.Split(';');
        foreach (var section in sections)
        {
            string[] parts = section.Split(',');
            if (parts.Length == 0) continue;
            switch (parts[0])
            {
                case "F":
                    if (parts.Length >= 14)
                    {
                        frameShape = CodeToFrameShape(parts[1]);
                        frameFilled = parts[2] == "1";
                        frameColor = new Color(ParseF(parts[3]), ParseF(parts[4]), ParseF(parts[5]), ParseF(parts[6]));
                        frameOpacity = ParseF(parts[7]);
                        frameScale = ParseF(parts[8]);
                        frameRotation = ParseF(parts[9]);
                        frameThickness = ParseF(parts[10]);
                        frameHue = ParseF(parts[11]);
                        frameSaturation = ParseF(parts[12]);
                        frameValue = ParseF(parts[13]);
                    }
                    break;
                case "H":
                    if (parts.Length >= 17)
                    {
                        hairStyle = CodeToHairStyle(parts[1]);
                        hairCount = int.Parse(parts[2]);
                        customAngle = ParseF(parts[3]);
                        hairThickness = ParseF(parts[4]);
                        hairLength = ParseF(parts[5]);
                        hairColor = new Color(ParseF(parts[6]), ParseF(parts[7]), ParseF(parts[8]), ParseF(parts[9]));
                        hairOpacity = ParseF(parts[10]);
                        hairsRotation = ParseF(parts[11]);
                        hairHue = ParseF(parts[12]);
                        hairSaturation = ParseF(parts[13]);
                        hairValue = ParseF(parts[14]);
                        if (hairsExtendPastFrameToggle != null)
                            hairsExtendPastFrameToggle.isOn = parts[15] == "1";
                    }
                    break;
                case "D":
                    if (parts.Length >= 13)
                    {
                        dotShape = CodeToFrameShape(parts[1]);
                        dotFilled = parts[2] == "1";
                        dotColor = new Color(ParseF(parts[3]), ParseF(parts[4]), ParseF(parts[5]), ParseF(parts[6]));
                        dotOpacity = ParseF(parts[7]);
                        dotScale = ParseF(parts[8]);
                        dotRotation = ParseF(parts[9]);
                        dotHue = ParseF(parts[10]);
                        dotSaturation = ParseF(parts[11]);
                        dotValue = ParseF(parts[12]);
                    }
                    break;
            }
        }
        SetVerticesDirty();
        UpdateUIFromValues();
        
        // FIXED: Clear flag after loading
        isLoadingPreset = false;
    }

    private void UpdateUIFromValues()
    {
        // Update UI sliders and dropdowns to match current values
        if (frameShapeDropdown) frameShapeDropdown.value = (int)frameShape;
        if (frameFilledToggle) frameFilledToggle.isOn = frameFilled;
        if (frameOpacitySlider) frameOpacitySlider.value = frameOpacity;
        if (frameScaleSlider) frameScaleSlider.value = frameScale;
        if (frameRotationSlider) frameRotationSlider.value = frameRotation;
        if (frameThicknessSlider) frameThicknessSlider.value = frameThickness;
        if (frameColorSlider) frameColorSlider.value = frameHue;
        if (frameSaturationSlider) frameSaturationSlider.value = frameSaturation;
        if (frameValueSlider) frameValueSlider.value = frameValue;
        
        if (hairStyleDropdown) hairStyleDropdown.value = (int)hairStyle;
        if (hairCountSlider) hairCountSlider.value = hairCount;
        if (customAngleSlider) customAngleSlider.value = customAngle;
        if (hairThicknessSlider) hairThicknessSlider.value = hairThickness;
        if (hairLengthSlider) hairLengthSlider.value = hairLength;
        if (hairsRotationSlider) hairsRotationSlider.value = hairsRotation;
        if (hairColorSlider) hairColorSlider.value = hairHue;
        if (hairSaturationSlider) hairSaturationSlider.value = hairSaturation;
        if (hairValueSlider) hairValueSlider.value = hairValue;
        if (hairOpacitySlider) hairOpacitySlider.value = hairOpacity;
        
        if (dotShapeDropdown) dotShapeDropdown.value = (int)dotShape;
        if (dotFilledToggle) dotFilledToggle.isOn = dotFilled;
        if (dotColorSlider) dotColorSlider.value = dotHue;
        if (dotSaturationSlider) dotSaturationSlider.value = dotSaturation;
        if (dotValueSlider) dotValueSlider.value = dotValue;
        if (dotOpacitySlider) dotOpacitySlider.value = dotOpacity;
        if (dotScaleSlider) dotScaleSlider.value = dotScale;
        if (dotRotationSlider) dotRotationSlider.value = dotRotation;
        
        UpdateHairUI();
    }

    // --- Preset Keybind System Methods ---
    private void SetupPresetKeybindSystem()
    {
        // Initialize preset keybinds
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            presetKeybinds[i] = new List<KeybindEntry>();
            presetHoldModes[i] = false;
            presetKeybindWasHeld[i] = false;
            presetReturnIndex[i] = 0;
        }
        
        // Load preset keybinds from storage
        LoadPresetKeybindsFromStorage();
        
        // Setup UI
        if (presetKeybindsToggle != null)
        {
            presetKeybindsToggle.onValueChanged.AddListener(OnPresetKeybindsToggleChanged);
        }
        
        if (presetKeybindButtons != null)
        {
            for (int i = 0; i < presetKeybindButtons.Length && i < PRESET_COUNT; i++)
            {
                int presetIndex = i; // Capture for lambda
                if (presetKeybindButtons[i] != null)
                {
                    presetKeybindButtons[i].onClick.AddListener(() => StartPresetKeybindRecording(presetIndex));
                }
            }
        }
        
        if (presetHoldToggles != null)
        {
            for (int i = 0; i < presetHoldToggles.Length && i < PRESET_COUNT; i++)
            {
                int presetIndex = i; // Capture for lambda
                if (presetHoldToggles[i] != null)
                {
                    presetHoldToggles[i].onValueChanged.AddListener((value) => OnPresetHoldToggleChanged(presetIndex, value));
                }
            }
        }
        
        UpdatePresetKeybindButtonTexts();
    }

    private void OnPresetKeybindsToggleChanged(bool isOn)
    {
        if (presetKeybindsPanel != null)
        {
            presetKeybindsPanel.SetActive(isOn);
        }
    }

    private void StartPresetKeybindRecording(int presetIndex)
    {
        recordingPresetKeybind = true;
        recordingPresetIndex = presetIndex;
        presetPressedKeys.Clear();
        presetLastPressedKeys.Clear();
        presetKeybindReleaseTimer = 0f;
        
        if (presetKeybindButtons[presetIndex] != null)
        {
            presetKeybindButtons[presetIndex].GetComponentInChildren<TMPro.TMP_Text>().text = "Press keys...";
        }
    }

    private void OnPresetHoldToggleChanged(int presetIndex, bool isHold)
    {
        presetHoldModes[presetIndex] = isHold;
        SavePresetKeybindsToStorage();
    }

    private void UpdatePresetKeybindButtonTexts()
    {
        for (int i = 0; i < presetKeybindButtons.Length && i < PRESET_COUNT; i++)
        {
            if (presetKeybindButtons[i] != null)
            {
                var text = presetKeybindButtons[i].GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    text.text = KeybindToString(presetKeybinds[i]);
                }
            }
            // Also update hold toggles
            if (presetHoldToggles != null && i < presetHoldToggles.Length && presetHoldToggles[i] != null)
            {
                presetHoldToggles[i].isOn = presetHoldModes[i];
            }
        }
        // Also update main UI toggle keybind button if you have one (optional)
    }

    private void LoadPresetKeybindsFromStorage()
    {
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            string keybindKey = $"PresetKeybind_{i}";
            string holdKey = $"PresetHold_{i}";
            if (PlayerPrefs.HasKey(keybindKey))
            {
                string keybindString = PlayerPrefs.GetString(keybindKey);
                presetKeybinds[i] = ParseKeybindString(keybindString);
            }
            presetHoldModes[i] = PlayerPrefs.GetInt(holdKey, 0) == 1;
        }
        // Load main UI toggle keybind
        if (PlayerPrefs.HasKey("UIToggleKeybind"))
        {
            currentKeybind = ParseKeybindString(PlayerPrefs.GetString("UIToggleKeybind"));
        }
    }

    private void SavePresetKeybindsToStorage()
    {
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            string keybindKey = $"PresetKeybind_{i}";
            string holdKey = $"PresetHold_{i}";
            PlayerPrefs.SetString(keybindKey, KeybindToString(presetKeybinds[i]));
            PlayerPrefs.SetInt(holdKey, presetHoldModes[i] ? 1 : 0);
        }
        // Save main UI toggle keybind
        PlayerPrefs.SetString("UIToggleKeybind", KeybindToString(currentKeybind));
        PlayerPrefs.Save();
    }

    private List<KeybindEntry> ParseKeybindString(string keybindString)
    {
        var keybind = new List<KeybindEntry>();
        if (!string.IsNullOrEmpty(keybindString))
        {
            string[] parts = keybindString.Split('+');
            foreach (string part in parts)
            {
                if (part.Trim().Equals("Mouse Left", StringComparison.OrdinalIgnoreCase))
                {
                    keybind.Add(new KeybindEntry(SpecialKey.MouseLeft));
                }
                else if (part.Trim().Equals("Mouse Right", StringComparison.OrdinalIgnoreCase))
                {
                    keybind.Add(new KeybindEntry(SpecialKey.MouseRight));
                }
                else if (part.Trim().Equals("Mouse Middle", StringComparison.OrdinalIgnoreCase))
                {
                    keybind.Add(new KeybindEntry(SpecialKey.MouseMiddle));
                }
                else if (part.Trim().Equals("Scroll Up", StringComparison.OrdinalIgnoreCase))
                {
                    keybind.Add(new KeybindEntry(SpecialKey.MouseWheelUp));
                }
                else if (part.Trim().Equals("Scroll Down", StringComparison.OrdinalIgnoreCase))
                {
                    keybind.Add(new KeybindEntry(SpecialKey.MouseWheelDown));
                }
                else if (System.Enum.TryParse(part.Trim(), out KeyCode keyCode))
                {
                    keybind.Add(new KeybindEntry(keyCode));
                }
            }
        }
        return keybind;
    }

    private void ShowTab(TabType type)
    {
        currentTab = type;
        SetTabActive(frameTabRoot, type == TabType.Frame);
        SetTabActive(hairTabRoot, type == TabType.Hair);
        SetTabActive(dotTabRoot, type == TabType.Dot);

        // Move and highlight tab buttons
        UpdateTabButtonVisuals(type);
    }

    private void UpdateTabButtonVisuals(TabType activeTab)
    {
        // Helper to set target position, color, and scale
        void SetTab(TabButtonAnimState anim, UnityEngine.UI.Button btn, Vector2 pos, bool highlight)
        {
            if (anim == null || btn == null) return;
            anim.targetPos = pos;
            anim.targetColor = highlight ? tabHighlightColor : tabNormalColor;
            anim.targetScale = highlight ? tabHighlightScale : tabNormalScale;
            if (highlight && anim.rt != null) anim.rt.SetAsLastSibling();
        }
        SetTab(frameTabAnim, frameTabButton, activeTab == TabType.Frame ? tabLeftPosition : (activeTab == TabType.Hair ? tabMiddlePosition : tabRightPosition), activeTab == TabType.Frame);
        SetTab(hairTabAnim, hairTabButton, activeTab == TabType.Hair ? tabLeftPosition : (activeTab == TabType.Dot ? tabMiddlePosition : tabRightPosition), activeTab == TabType.Hair);
        SetTab(dotTabAnim, dotTabButton, activeTab == TabType.Dot ? tabLeftPosition : (activeTab == TabType.Frame ? tabMiddlePosition : tabRightPosition), activeTab == TabType.Dot);
    }

    private void SetTabActive(GameObject tabRoot, bool active)
    {
        if (tabRoot == null) return;
        tabRoot.SetActive(active);
        var groups = tabRoot.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in groups)
        {
            cg.interactable = active;
            cg.blocksRaycasts = active;
        }
    }

    public void SaveCrosshairCode()
    {
        // Save current crosshair to active preset
        presets[currentPresetIndex] = GenerateCrosshairCode();
        SavePresetToStorage(currentPresetIndex);
        
        // Copy to clipboard
        GUIUtility.systemCopyBuffer = presets[currentPresetIndex];
        Debug.Log($"Preset {currentPresetIndex + 1} saved and copied to clipboard: {presets[currentPresetIndex]}");
    }

    public void GenerateAndOpenImage()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StartCoroutine(CaptureCrosshairScreenshotWithCode());
#endif
    }

    // Helper to get a short hash for filenames
    private string GetCodeHash(string code)
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(code));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8); // 8 hex chars
        }
    }

    private System.Collections.IEnumerator CaptureCrosshairScreenshotWithCode()
    {
        yield return new WaitForEndOfFrame();
        int screenWidth = UnityEngine.Screen.width;
        int screenHeight = UnityEngine.Screen.height;
        int captureSize = 256;
        int x = (screenWidth - captureSize) / 2;
        int y = (screenHeight - captureSize) / 2;
        Texture2D screenshot = new Texture2D(captureSize, captureSize, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(x, y, captureSize, captureSize), 0, 0);
        screenshot.Apply();
        byte[] pngData = screenshot.EncodeToPNG();
        if (pngData != null && pngData.Length > 0)
        {
            string code = GenerateCrosshairCode();
            string hash = GetCodeHash(code);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string imageName = $"Crosshair_{hash}.png";
            string codeName = $"Crosshair_{hash}.txt";
            string imagePath = Path.Combine(desktop, imageName);
            string codePath = Path.Combine(desktop, codeName);
            File.WriteAllBytes(imagePath, pngData);
            File.WriteAllText(codePath, code);
            Debug.Log($"Crosshair screenshot saved to: {imagePath}");
            Debug.Log($"Crosshair code saved to: {codePath}");
            try
            {
                System.Diagnostics.Process.Start(imagePath);
                Debug.Log($"Opened screenshot with default application: {imagePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to open screenshot: {e.Message}");
            }
        }
        Destroy(screenshot);
    }

    private Texture2D RenderCrosshairPreview(int width, int height)
    {
        // Create a RenderTexture (not temporary since we're not using GetTemporary)
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, new Color(0,0,0,0));

        // Create a temporary Canvas and CrosshairRenderer for offscreen rendering
        var go = new GameObject("CrosshairPreviewTemp", typeof(RectTransform));
        var canvasGO = new GameObject("CrosshairPreviewCanvas", typeof(Canvas));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        var camGO = new GameObject("CrosshairPreviewCamera", typeof(Camera));
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = height/1.5f; // Larger orthographic size to ensure full crosshair is visible
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0,0,0,0);
        cam.targetTexture = rt;
        canvas.worldCamera = cam;
        canvas.pixelPerfect = true;
        canvas.planeDistance = 1;
        go.transform.SetParent(canvasGO.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        var previewRenderer = go.AddComponent<CrosshairRenderer>();
        // Copy all relevant fields
        CopyCrosshairSettingsTo(previewRenderer);
        previewRenderer.raycastTarget = false;
        previewRenderer.SetVerticesDirty();
        // Force render
        canvasGO.SetActive(true);
        cam.Render();
        // Read pixels
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        // Cleanup
        RenderTexture.active = prevRT;
        cam.targetTexture = null;
        GameObject.DestroyImmediate(go);
        GameObject.DestroyImmediate(canvasGO);
        GameObject.DestroyImmediate(camGO);
        rt.Release(); // Use Release() instead of ReleaseTemporary() for manually created RenderTexture
        return tex;
    }

    private void CopyCrosshairSettingsTo(CrosshairRenderer target)
    {
        // Copy all relevant fields
        target.frameShape = frameShape;
        target.frameFilled = frameFilled;
        target.frameColor = frameColor;
        target.frameOpacity = frameOpacity;
        target.frameScale = frameScale;
        target.frameRotation = frameRotation;
        target.frameThickness = frameThickness;
        target.hairStyle = hairStyle;
        target.hairCount = hairCount;
        target.customAngle = customAngle;
        target.hairThickness = hairThickness;
        target.hairLength = hairLength;
        target.hairColor = hairColor;
        target.hairOpacity = hairOpacity;
        target.hairsRotation = hairsRotation;
        target.dotShape = dotShape;
        target.dotFilled = dotFilled;
        target.dotColor = dotColor;
        target.dotOpacity = dotOpacity;
        target.dotScale = dotScale;
        target.dotRotation = dotRotation;
        target.frameHue = frameHue;
        target.frameSaturation = frameSaturation;
        target.frameValue = frameValue;
        target.hairHue = hairHue;
        target.hairSaturation = hairSaturation;
        target.hairValue = hairValue;
        target.dotHue = dotHue;
        target.dotSaturation = dotSaturation;
        target.dotValue = dotValue;
        // For the toggle, just copy the value if present
        if (target.hairsExtendPastFrameToggle != null && hairsExtendPastFrameToggle != null)
            target.hairsExtendPastFrameToggle.isOn = hairsExtendPastFrameToggle.isOn;
    }

    public void LoadCrosshairCode()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
try
{
    if (WinForms.Clipboard.ContainsFileDropList())
    {
        var files = WinForms.Clipboard.GetFileDropList();
        if (files != null && files.Count > 0)
        {
                    string filePath = files[0];
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith("Crosshair_") && fileName.EndsWith(".png"))
                    {
                        string hash = fileName.Substring(10, fileName.Length - 14); // between _ and .png
                        string codePath = Path.Combine(Path.GetDirectoryName(filePath), $"Crosshair_{hash}.txt");
                        if (File.Exists(codePath))
                        {
                            string loadedCode = File.ReadAllText(codePath);
                            if (!string.IsNullOrEmpty(loadedCode))
                            {
                                presets[currentPresetIndex] = loadedCode;
                                SavePresetToStorage(currentPresetIndex);
                                LoadCrosshairFromCode(loadedCode);
                                Debug.Log($"Preset {currentPresetIndex + 1} loaded from image file: {fileName}");
                                return;
                            }
                        }
                        Debug.LogWarning($"No code file found for image: {fileName}");
                        return;
                    }
                }
    }
}
catch (System.Exception e)
{
    Debug.LogWarning($"Clipboard file check failed: {e.Message}");
}
#endif
        // Fallback: use text clipboard
        string code = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("No code in clipboard to load");
            return;
        }
        // Check if clipboard is an image filename like Crosshair_{hash}.png (for text clipboard fallback)
        if (code.StartsWith("Crosshair_") && code.EndsWith(".png"))
        {
            string hash = code.Substring(10, code.Length - 14); // between _ and .png
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string codePath = Path.Combine(desktop, $"Crosshair_{hash}.txt");
            if (File.Exists(codePath))
            {
                string loadedCode = File.ReadAllText(codePath);
                if (!string.IsNullOrEmpty(loadedCode))
                {
                    presets[currentPresetIndex] = loadedCode;
                    SavePresetToStorage(currentPresetIndex);
                    LoadCrosshairFromCode(loadedCode);
                    Debug.Log($"Preset {currentPresetIndex + 1} loaded from image filename: {code}");
                    return;
                }
            }
            Debug.LogWarning($"No code file found for image: {code}");
            return;
        }
        // Otherwise, treat as regular code
        presets[currentPresetIndex] = code;
        SavePresetToStorage(currentPresetIndex);
        LoadCrosshairFromCode(code);
        Debug.Log($"Preset {currentPresetIndex + 1} loaded from clipboard: {code}");
    }

    public void ClearToDefaults()
    {
        // Reset to default values (same as Awake)
        frameShape = CrosshairShape.Circle;
        frameFilled = false;
        frameColor = Color.red;
        frameOpacity = 1f;
        frameScale = 0.9f;
        frameRotation = 0f;
        frameThickness = 5f;
        
        hairStyle = HairStyle.Even;
        hairCount = 4;
        customAngle = 0f;
        hairThickness = 5.5f;
        hairLength = 24f;
        hairColor = Color.red;
        hairOpacity = 1f;
        hairsRotation = 0f;
        
        dotShape = CrosshairShape.Square;
        dotFilled = true;
        dotColor = Color.red;
        dotOpacity = 1f;
        dotScale = 0.9f;
        dotRotation = 45f;
        
        frameHue = 1f;
        frameSaturation = 1f;
        frameValue = 1f;
        hairHue = 1f;
        hairSaturation = 1f;
        hairValue = 1f;
        dotHue = 1f;
        dotSaturation = 1f;
        dotValue = 1f;
        
        // Update UI to match
        UpdateUIFromValues();
        SetVerticesDirty();
        
        Debug.Log("Crosshair reset to defaults");
    }

    private string FrameShapeToCode(CrosshairShape shape)
    {
        switch (shape)
        {
            case CrosshairShape.Circle: return "C";
            case CrosshairShape.Square: return "S";
            case CrosshairShape.Triangle: return "T";
            default: return "C";
        }
    }
 
    private CrosshairShape CodeToFrameShape(string code)
    {
        switch (code)
        {
            case "C": return CrosshairShape.Circle;
            case "S": return CrosshairShape.Square;
            case "T": return CrosshairShape.Triangle;
            default: return CrosshairShape.Circle;
        }
    }

    private string HairStyleToCode(HairStyle style)
    {
        switch (style)
        {
            case HairStyle.Even: return "E";
            case HairStyle.Custom: return "C";
            default: return "E";
        }
    }

    private HairStyle CodeToHairStyle(string code)
    {
        switch (code)
        {
            case "E": return HairStyle.Even;
            case "C": return HairStyle.Custom;
            default: return HairStyle.Even;
        }
    }

    private float ParseF(string value)
    {
        if (float.TryParse(value, out float result))
            return result;
        return 0f;
    }

    private void UpdateSaturationReferenceColor(SpriteRenderer renderer, float hue)
    {
        if (renderer == null) return;
        
        // Determine which cached texture to update based on the renderer
        if (renderer == frameSaturationRefRenderer)
        {
            UpdateSaturationTexture(renderer, hue, ref frameSaturationTexture);
        }
        else if (renderer == hairSaturationRefRenderer)
        {
            UpdateSaturationTexture(renderer, hue, ref hairSaturationTexture);
        }
        else if (renderer == dotSaturationRefRenderer)
        {
            UpdateSaturationTexture(renderer, hue, ref dotSaturationTexture);
        }
    }

    public void ClearAllPresetKeybinds()
    {
        // Clear all preset keybinds
        for (int i = 0; i < PRESET_COUNT; i++)
        {
            presetKeybinds[i].Clear();
            presetHoldModes[i] = false;
        }
        
        // Update UI
        UpdatePresetKeybindButtonTexts();
        
        // Save to storage
        SavePresetKeybindsToStorage();
        
        Debug.Log("All preset keybinds cleared");
    }

    // SystemInput class for handling input when Unity is not focused
    // SystemInput class for handling input when Unity is not focused

// SystemInput class for handling input when Unity is not focused
private static class SystemInput
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // Virtual key codes for common keys
    private static readonly Dictionary<KeyCode, int> KeyCodeToVirtualKey = new Dictionary<KeyCode, int>
    {
        { KeyCode.A, 0x41 }, { KeyCode.B, 0x42 }, { KeyCode.C, 0x43 }, { KeyCode.D, 0x44 },
        { KeyCode.E, 0x45 }, { KeyCode.F, 0x46 }, { KeyCode.G, 0x47 }, { KeyCode.H, 0x48 },
        { KeyCode.I, 0x49 }, { KeyCode.J, 0x4A }, { KeyCode.K, 0x4B }, { KeyCode.L, 0x4C },
        { KeyCode.M, 0x4D }, { KeyCode.N, 0x4E }, { KeyCode.O, 0x4F }, { KeyCode.P, 0x50 },
        { KeyCode.Q, 0x51 }, { KeyCode.R, 0x52 }, { KeyCode.S, 0x53 }, { KeyCode.T, 0x54 },
        { KeyCode.U, 0x55 }, { KeyCode.V, 0x56 }, { KeyCode.W, 0x57 }, { KeyCode.X, 0x58 },
        { KeyCode.Y, 0x59 }, { KeyCode.Z, 0x5A },
        
        { KeyCode.Alpha0, 0x30 }, { KeyCode.Alpha1, 0x31 }, { KeyCode.Alpha2, 0x32 },
        { KeyCode.Alpha3, 0x33 }, { KeyCode.Alpha4, 0x34 }, { KeyCode.Alpha5, 0x35 },
        { KeyCode.Alpha6, 0x36 }, { KeyCode.Alpha7, 0x37 }, { KeyCode.Alpha8, 0x38 },
        { KeyCode.Alpha9, 0x39 },
        
        { KeyCode.F1, 0x70 }, { KeyCode.F2, 0x71 }, { KeyCode.F3, 0x72 }, { KeyCode.F4, 0x73 },
        { KeyCode.F5, 0x74 }, { KeyCode.F6, 0x75 }, { KeyCode.F7, 0x76 }, { KeyCode.F8, 0x77 },
        { KeyCode.F9, 0x78 }, { KeyCode.F10, 0x79 }, { KeyCode.F11, 0x7A }, { KeyCode.F12, 0x7B },
        
        { KeyCode.Space, 0x20 }, { KeyCode.Return, 0x0D }, { KeyCode.Escape, 0x1B },
        { KeyCode.Tab, 0x09 }, { KeyCode.Backspace, 0x08 }, { KeyCode.Delete, 0x2E },
        { KeyCode.Insert, 0x2D }, { KeyCode.Home, 0x24 }, { KeyCode.End, 0x23 },
        { KeyCode.PageUp, 0x21 }, { KeyCode.PageDown, 0x22 },
        
        { KeyCode.UpArrow, 0x26 }, { KeyCode.DownArrow, 0x28 },
        { KeyCode.LeftArrow, 0x25 }, { KeyCode.RightArrow, 0x27 },
        
        { KeyCode.LeftShift, 0xA0 }, { KeyCode.RightShift, 0xA1 },
        { KeyCode.LeftControl, 0xA2 }, { KeyCode.RightControl, 0xA3 },
        { KeyCode.LeftAlt, 0xA4 }, { KeyCode.RightAlt, 0xA5 },
        { KeyCode.LeftWindows, 0x5B }, { KeyCode.RightWindows, 0x5C },
        
        { KeyCode.Keypad0, 0x60 }, { KeyCode.Keypad1, 0x61 }, { KeyCode.Keypad2, 0x62 },
        { KeyCode.Keypad3, 0x63 }, { KeyCode.Keypad4, 0x64 }, { KeyCode.Keypad5, 0x65 },
        { KeyCode.Keypad6, 0x66 }, { KeyCode.Keypad7, 0x67 }, { KeyCode.Keypad8, 0x68 },
        { KeyCode.Keypad9, 0x69 }, { KeyCode.KeypadPeriod, 0x6E }, { KeyCode.KeypadDivide, 0x6F },
        { KeyCode.KeypadMultiply, 0x6A }, { KeyCode.KeypadMinus, 0x6D }, { KeyCode.KeypadPlus, 0x6B },
        { KeyCode.KeypadEnter, 0x0D },
        
        // Mouse buttons
        { KeyCode.Mouse0, 0x01 }, { KeyCode.Mouse1, 0x02 }, { KeyCode.Mouse2, 0x04 }
    };

    public static bool GetKey(KeyCode keyCode)
    {
        try
        {
            if (KeyCodeToVirtualKey.TryGetValue(keyCode, out int vKey))
            {
                short keyState = GetAsyncKeyState(vKey);
                bool isPressed = (keyState & 0x8000) != 0;
                
                // Debug logging for troubleshooting
                if (isPressed && keyCode == KeyCode.F2)
                {
                    UnityEngine.Debug.Log($"SystemInput detected {keyCode} press (unfocused)");
                }
                
                return isPressed;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"SystemInput.GetKey failed for {keyCode}: {e.Message}");
        }
        return false;
    }

    public static bool GetMouseButton(int button)
    {
        try
        {
            int vKey = 0;
            switch (button)
            {
                case 0: vKey = 0x01; break; // Left mouse button
                case 1: vKey = 0x02; break; // Right mouse button
                case 2: vKey = 0x04; break; // Middle mouse button
                default: return false;
            }
            
            short keyState = GetAsyncKeyState(vKey);
            return (keyState & 0x8000) != 0;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"SystemInput.GetMouseButton failed for button {button}: {e.Message}");
            return false;
        }
    }
#else
    public static bool GetKey(KeyCode keyCode)
    {
        return false;
    }

    public static bool GetMouseButton(int button)
    {
        return false;
    }
#endif


}

}

