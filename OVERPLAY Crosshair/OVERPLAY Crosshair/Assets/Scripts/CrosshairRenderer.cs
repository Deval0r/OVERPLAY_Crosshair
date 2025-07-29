using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;
using System.Globalization;
using UnityEngine.EventSystems;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public enum SpecialKey
{
    None,
    MouseLeft,
    MouseRight,
    MouseMiddle,
    MouseWheelUp,
    MouseWheelDown
}

[System.Serializable]
public class KeybindEntry
{
    public KeyCode keyCode = KeyCode.None;
    public SpecialKey specialKey = SpecialKey.None;
}

public class CrosshairRenderer : Graphic
{
    [Header("Crosshair Settings")]
    public bool enableCrosshair = true;
    public Color crosshairColor = Color.white;
    public float crosshairThickness = 2f;
    public float crosshairGap = 5f;
    public float crosshairLength = 15f;
    public bool enableOutline = true;
    public Color outlineColor = Color.black;
    public float outlineThickness = 1f;
    public bool enableDot = true;
    public Color dotColor = Color.white;
    public float dotSize = 2f;
    public bool dotOutline = true;
    public Color dotOutlineColor = Color.black;
    public float dotOutlineThickness = 1f;
    public bool enableFrame = false;
    public Color frameColor = Color.white;
    public float frameThickness = 2f;
    public float frameSize = 30f;
    public bool frameOutline = true;
    public Color frameOutlineColor = Color.black;
    public float frameOutlineThickness = 1f;
    public bool enableHairs = false;
    public Color hairsColor = Color.white;
    public float hairsThickness = 1f;
    public float hairsLength = 50f;
    public bool hairsOutline = true;
    public Color hairsOutlineColor = Color.black;
    public float hairsOutlineThickness = 1f;
    public bool enableMovementError = false;
    public float movementErrorAmount = 2f;
    public float movementErrorDecay = 0.95f;
    public bool enableFiringError = false;
    public float firingErrorAmount = 5f;
    public float firingErrorDecay = 0.9f;
    public float frameRotation = 0f;
    public float hairsRotation = 0f;
    public float dotRotation = 0f;

    [Header("Toggle Settings")]
    public List<KeybindEntry> keybind = new List<KeybindEntry>();
    public bool uiVisible = false;

    [Header("UI References")]
    public GameObject uiContainer;
    public Text statusText;
    public Button importButton;
    public Button exportButton;
    public Button resetButton;
    public Button copyButton;
    public Button pasteButton;
    public InputField crosshairCodeInput;
    public Dropdown presetDropdown;
    public Toggle snapToggle;

    [Header("Sliders")]
    public Slider crosshairThicknessSlider;
    public Slider crosshairGapSlider;
    public Slider crosshairLengthSlider;
    public Slider outlineThicknessSlider;
    public Slider dotSizeSlider;
    public Slider dotOutlineThicknessSlider;
    public Slider frameThicknessSlider;
    public Slider frameSizeSlider;
    public Slider frameOutlineThicknessSlider;
    public Slider hairsThicknessSlider;
    public Slider hairsLengthSlider;
    public Slider hairsOutlineThicknessSlider;
    public Slider movementErrorAmountSlider;
    public Slider movementErrorDecaySlider;
    public Slider firingErrorAmountSlider;
    public Slider firingErrorDecaySlider;
    public Slider frameRotationSlider;
    public Slider hairsRotationSlider;
    public Slider dotRotationSlider;

    [Header("Toggles")]
    public Toggle enableCrosshairToggle;
    public Toggle enableOutlineToggle;
    public Toggle enableDotToggle;
    public Toggle dotOutlineToggle;
    public Toggle enableFrameToggle;
    public Toggle frameOutlineToggle;
    public Toggle enableHairsToggle;
    public Toggle hairsOutlineToggle;
    public Toggle enableMovementErrorToggle;
    public Toggle enableFiringErrorToggle;

    [Header("Color Buttons")]
    public Button crosshairColorButton;
    public Button outlineColorButton;
    public Button dotColorButton;
    public Button dotOutlineColorButton;
    public Button frameColorButton;
    public Button frameOutlineColorButton;
    public Button hairsColorButton;
    public Button hairsOutlineColorButton;

    [Header("Keybind UI")]
    public Button keybindButton;
    public Text keybindText;
    public GameObject keybindPopup;
    public Button clearKeybindButton;
    public Button confirmKeybindButton;
    public Button cancelKeybindButton;

    [Header("Color Picker")]
    public GameObject colorPickerContainer;
    public Slider hueSlider;
    public Slider satSlider;
    public Slider valSlider;
    public Slider alphaSlider;
    public Image colorPreview;
    public Button colorConfirmButton;
    public Button colorCancelButton;
    public InputField hexInput;

    // ... rest of your script


    // ... rest of your script remains exactly the same ...


    private Vector2 movementError = Vector2.zero;
    private Vector2 firingError = Vector2.zero;
    private bool keybindWasHeld = false;
    private bool detectingKeybind = false;
    private List<KeybindEntry> tempKeybind = new List<KeybindEntry>();
    private Color targetColor;
    private System.Action<Color> colorCallback;

    private List<string> presets = new List<string>();
    private int currentPresetIndex = 0;
    private bool isLoadingPreset = false;

    private float lastKeybindCooldown = 0f;
    private const float KEYBIND_COOLDOWN = 0.1f;

    public bool SnapEnabled { get; private set; } = false;

    private void Start()
    {
        InitializeUI();
        LoadPresets();
        UpdateKeybindText();
        
        if (uiContainer != null)
        {
            uiContainer.SetActive(uiVisible);
        }
    }

    private void Update()
    {
        UpdateErrorEffects();
        HandleKeybindDetection();
        
        if (!detectingKeybind)
        {
            HandleToggleKeybind();
        }
    }

    private void HandleToggleKeybind()
    {
        if (Time.time - lastKeybindCooldown < KEYBIND_COOLDOWN) return;

        bool allHeld = keybind.Count > 0;
        bool mouseKeyInBind = false;

        foreach (var k in keybind)
        {
            if (!GetKeyState(k)) 
            {
                allHeld = false;
            }
            if (k.specialKey != SpecialKey.None && k.specialKey != SpecialKey.MouseWheelUp && k.specialKey != SpecialKey.MouseWheelDown)
            {
                mouseKeyInBind = true;
            }
        }

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
                    lastKeybindCooldown = Time.time;
                }
            }
        }
        else
        {
            keybindWasHeld = false;
        }

        // Handle preset keybinds
        for (int i = 0; i < presets.Count; i++)
        {
            List<KeybindEntry> currentPresetKeybind = GetPresetKeybind(i);
            if (currentPresetKeybind.Count > 0)
            {
                bool allPresetKeysHeld = true;
                foreach (var k in currentPresetKeybind)
                {
                    if (!GetKeyState(k))
                    {
                        allPresetKeysHeld = false;
                        break;
                    }
                }

                if (allPresetKeysHeld && currentPresetIndex != i)
                {
                    SwitchToPreset(i);
                    break;
                }
            }
        }
    }

    private bool GetKeyState(KeybindEntry k)
{
    if (k.specialKey == SpecialKey.None)
    {
        // For keyboard keys, use appropriate input system based on focus
        return UnityEngine.Application.isFocused ? Input.GetKey(k.keyCode) : SystemInput.GetKey(k.keyCode);
    }
    else
    {
        switch (k.specialKey)
        {
            case SpecialKey.MouseLeft:
                return UnityEngine.Application.isFocused ? Input.GetMouseButton(0) : SystemInput.GetMouseButton(0);
            case SpecialKey.MouseRight:
                return UnityEngine.Application.isFocused ? Input.GetMouseButton(1) : SystemInput.GetMouseButton(1);
            case SpecialKey.MouseMiddle:
                return UnityEngine.Application.isFocused ? Input.GetMouseButton(2) : SystemInput.GetMouseButton(2);
            case SpecialKey.MouseWheelUp:
                return Input.mouseScrollDelta.y > 0.01f;
            case SpecialKey.MouseWheelDown:
                return Input.mouseScrollDelta.y < -0.01f;
            default:
                return false;
        }
    }
}



    private bool IsMouseNearEdge()
{
    int edgeMargin = 2;
    Vector2 mousePos = Input.mousePosition;
    return mousePos.x <= edgeMargin || mousePos.x >= UnityEngine.Screen.width - edgeMargin ||
           mousePos.y <= edgeMargin || mousePos.y >= UnityEngine.Screen.height - edgeMargin;
}



    private List<KeybindEntry> GetPresetKeybind(int presetIndex)
    {
        List<KeybindEntry> keybindEntries = new List<KeybindEntry>();
        
        switch (presetIndex)
        {
            case 0: keybindEntries.Add(new KeybindEntry { keyCode = KeyCode.Alpha1 }); break;
            case 1: keybindEntries.Add(new KeybindEntry { keyCode = KeyCode.Alpha2 }); break;
            case 2: keybindEntries.Add(new KeybindEntry { keyCode = KeyCode.Alpha3 }); break;
            case 3: keybindEntries.Add(new KeybindEntry { keyCode = KeyCode.Alpha4 }); break;
            case 4: keybindEntries.Add(new KeybindEntry { keyCode = KeyCode.Alpha5 }); break;
        }
        
        return keybindEntries;
    }

    private void SwitchToPreset(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < presets.Count)
        {
            // Autosave current preset before switching
            presets[currentPresetIndex] = GenerateCrosshairCode();
            SavePresetToStorage(currentPresetIndex);
            
            // Temporarily disable listeners to prevent feedback loops
            RemoveSliderListeners();
            
            currentPresetIndex = presetIndex;
            isLoadingPreset = true;
            
            // Load the new preset
            LoadCrosshairFromCode(presets[currentPresetIndex]);
            
            // Update UI
            if (presetDropdown != null)
            {
                presetDropdown.value = currentPresetIndex;
            }
            
            isLoadingPreset = false;
            
            // Re-enable listeners
            AddSliderListeners();
            
            Debug.Log($"Switched to Preset {presetIndex + 1}");
        }
    }

    private void RemoveSliderListeners()
    {
        if (frameRotationSlider) frameRotationSlider.onValueChanged.RemoveAllListeners();
        if (hairsRotationSlider) hairsRotationSlider.onValueChanged.RemoveAllListeners();
        if (dotRotationSlider) dotRotationSlider.onValueChanged.RemoveAllListeners();
        if (crosshairThicknessSlider) crosshairThicknessSlider.onValueChanged.RemoveAllListeners();
        if (crosshairGapSlider) crosshairGapSlider.onValueChanged.RemoveAllListeners();
        if (crosshairLengthSlider) crosshairLengthSlider.onValueChanged.RemoveAllListeners();
        if (outlineThicknessSlider) outlineThicknessSlider.onValueChanged.RemoveAllListeners();
        if (dotSizeSlider) dotSizeSlider.onValueChanged.RemoveAllListeners();
        if (dotOutlineThicknessSlider) dotOutlineThicknessSlider.onValueChanged.RemoveAllListeners();
        if (frameThicknessSlider) frameThicknessSlider.onValueChanged.RemoveAllListeners();
        if (frameSizeSlider) frameSizeSlider.onValueChanged.RemoveAllListeners();
        if (frameOutlineThicknessSlider) frameOutlineThicknessSlider.onValueChanged.RemoveAllListeners();
        if (hairsThicknessSlider) hairsThicknessSlider.onValueChanged.RemoveAllListeners();
        if (hairsLengthSlider) hairsLengthSlider.onValueChanged.RemoveAllListeners();
        if (hairsOutlineThicknessSlider) hairsOutlineThicknessSlider.onValueChanged.RemoveAllListeners();
        if (movementErrorAmountSlider) movementErrorAmountSlider.onValueChanged.RemoveAllListeners();
        if (movementErrorDecaySlider) movementErrorDecaySlider.onValueChanged.RemoveAllListeners();
        if (firingErrorAmountSlider) firingErrorAmountSlider.onValueChanged.RemoveAllListeners();
        if (firingErrorDecaySlider) firingErrorDecaySlider.onValueChanged.RemoveAllListeners();
    }

    private void AddSliderListeners()
    {
        if (frameRotationSlider) frameRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = frameRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            frameRotation = value;
            if (SnapEnabled) frameRotationSlider.value = value;
            SetVerticesDirty();
        });
        
        if (hairsRotationSlider) hairsRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = hairsRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            hairsRotation = value;
            if (SnapEnabled) hairsRotationSlider.value = value;
            SetVerticesDirty();
        });
        
        if (dotRotationSlider) dotRotationSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            float value = dotRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            dotRotation = value;
            if (SnapEnabled) dotRotationSlider.value = value;
            SetVerticesDirty();
        });

        // Add all other slider listeners with the isLoadingPreset check
        if (crosshairThicknessSlider) crosshairThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            crosshairThickness = val;
            SetVerticesDirty();
        });

        if (crosshairGapSlider) crosshairGapSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            crosshairGap = val;
            SetVerticesDirty();
        });

        if (crosshairLengthSlider) crosshairLengthSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            crosshairLength = val;
            SetVerticesDirty();
        });

        if (outlineThicknessSlider) outlineThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            outlineThickness = val;
            SetVerticesDirty();
        });

        if (dotSizeSlider) dotSizeSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            dotSize = val;
            SetVerticesDirty();
        });

        if (dotOutlineThicknessSlider) dotOutlineThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            dotOutlineThickness = val;
            SetVerticesDirty();
        });

        if (frameThicknessSlider) frameThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            frameThickness = val;
            SetVerticesDirty();
        });

        if (frameSizeSlider) frameSizeSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            frameSize = val;
            SetVerticesDirty();
        });

        if (frameOutlineThicknessSlider) frameOutlineThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            frameOutlineThickness = val;
            SetVerticesDirty();
        });

        if (hairsThicknessSlider) hairsThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            hairsThickness = val;
            SetVerticesDirty();
        });

        if (hairsLengthSlider) hairsLengthSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            hairsLength = val;
            SetVerticesDirty();
        });

        if (hairsOutlineThicknessSlider) hairsOutlineThicknessSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            hairsOutlineThickness = val;
            SetVerticesDirty();
        });

        if (movementErrorAmountSlider) movementErrorAmountSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            movementErrorAmount = val;
        });

        if (movementErrorDecaySlider) movementErrorDecaySlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            movementErrorDecay = val;
        });

        if (firingErrorAmountSlider) firingErrorAmountSlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            firingErrorAmount = val;
        });

        if (firingErrorDecaySlider) firingErrorDecaySlider.onValueChanged.AddListener(val => {
            if (isLoadingPreset) return;
            firingErrorDecay = val;
        });
    }

    private void UpdateErrorEffects()
    {
        if (enableMovementError)
        {
            movementError *= movementErrorDecay;
        }
        else
        {
            movementError = Vector2.zero;
        }

        if (enableFiringError)
        {
            firingError *= firingErrorDecay;
        }
        else
        {
            firingError = Vector2.zero;
        }
    }

    private void HandleKeybindDetection()
    {
        if (!detectingKeybind) return;

        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (keyCode == KeyCode.None) continue;

            if (Input.GetKeyDown(keyCode))
            {
                if (keyCode == KeyCode.Escape)
                {
                    tempKeybind.Clear();
                    break;
                }

                KeybindEntry entry = new KeybindEntry { keyCode = keyCode };
                if (!tempKeybind.Any(k => k.keyCode == keyCode && k.specialKey == SpecialKey.None))
                {
                    tempKeybind.Add(entry);
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            KeybindEntry entry = new KeybindEntry { specialKey = SpecialKey.MouseLeft };
            if (!tempKeybind.Any(k => k.specialKey == SpecialKey.MouseLeft))
            {
                tempKeybind.Add(entry);
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            KeybindEntry entry = new KeybindEntry { specialKey = SpecialKey.MouseRight };
            if (!tempKeybind.Any(k => k.specialKey == SpecialKey.MouseRight))
            {
                tempKeybind.Add(entry);
            }
        }
        if (Input.GetMouseButtonDown(2))
        {
            KeybindEntry entry = new KeybindEntry { specialKey = SpecialKey.MouseMiddle };
            if (!tempKeybind.Any(k => k.specialKey == SpecialKey.MouseMiddle))
            {
                tempKeybind.Add(entry);
            }
        }

        if (Input.mouseScrollDelta.y > 0.01f)
        {
            KeybindEntry entry = new KeybindEntry { specialKey = SpecialKey.MouseWheelUp };
            if (!tempKeybind.Any(k => k.specialKey == SpecialKey.MouseWheelUp))
            {
                tempKeybind.Add(entry);
            }
        }
        if (Input.mouseScrollDelta.y < -0.01f)
        {
            KeybindEntry entry = new KeybindEntry { specialKey = SpecialKey.MouseWheelDown };
            if (!tempKeybind.Any(k => k.specialKey == SpecialKey.MouseWheelDown))
            {
                tempKeybind.Add(entry);
            }
        }

        UpdateKeybindPreview();
    }

    private void UpdateKeybindPreview()
    {
        if (keybindText != null)
        {
            string preview = "Press keys... ";
            if (tempKeybind.Count > 0)
            {
                preview = string.Join(" + ", tempKeybind.Select(GetKeybindDisplayName));
            }
            keybindText.text = preview;
        }
    }

    private void ToggleUI()
    {
        uiVisible = !uiVisible;
        if (uiContainer != null)
        {
            uiContainer.SetActive(uiVisible);
        }
        
        UpdateStatusText();
    }

    private void InitializeUI()
    {
        SetupButtons();
        SetupSliders();
        SetupToggles();
        SetupKeybindUI();
        SetupColorPicker();
        SetupPresets();
        UpdateStatusText();
        
        AddSliderListeners();
    }

    private void SetupButtons()
    {
        if (importButton) importButton.onClick.AddListener(ImportCrosshair);
        if (exportButton) exportButton.onClick.AddListener(ExportCrosshair);
        if (resetButton) resetButton.onClick.AddListener(ResetCrosshair);
        if (copyButton) copyButton.onClick.AddListener(CopyCrosshairCode);
        if (pasteButton) pasteButton.onClick.AddListener(PasteCrosshairCode);

        if (crosshairColorButton) crosshairColorButton.onClick.AddListener(() => OpenColorPicker(crosshairColor, c => crosshairColor = c));
        if (outlineColorButton) outlineColorButton.onClick.AddListener(() => OpenColorPicker(outlineColor, c => outlineColor = c));
        if (dotColorButton) dotColorButton.onClick.AddListener(() => OpenColorPicker(dotColor, c => dotColor = c));
        if (dotOutlineColorButton) dotOutlineColorButton.onClick.AddListener(() => OpenColorPicker(dotOutlineColor, c => dotOutlineColor = c));
        if (frameColorButton) frameColorButton.onClick.AddListener(() => OpenColorPicker(frameColor, c => frameColor = c));
        if (frameOutlineColorButton) frameOutlineColorButton.onClick.AddListener(() => OpenColorPicker(frameOutlineColor, c => frameOutlineColor = c));
        if (hairsColorButton) hairsColorButton.onClick.AddListener(() => OpenColorPicker(hairsColor, c => hairsColor = c));
        if (hairsOutlineColorButton) hairsOutlineColorButton.onClick.AddListener(() => OpenColorPicker(hairsOutlineColor, c => hairsOutlineColor = c));
    }

    private void SetupSliders()
    {
        // Note: Listeners are added in AddSliderListeners() method
        if (crosshairThicknessSlider) crosshairThicknessSlider.value = crosshairThickness;
        if (crosshairGapSlider) crosshairGapSlider.value = crosshairGap;
        if (crosshairLengthSlider) crosshairLengthSlider.value = crosshairLength;
        if (outlineThicknessSlider) outlineThicknessSlider.value = outlineThickness;
        if (dotSizeSlider) dotSizeSlider.value = dotSize;
        if (dotOutlineThicknessSlider) dotOutlineThicknessSlider.value = dotOutlineThickness;
        if (frameThicknessSlider) frameThicknessSlider.value = frameThickness;
        if (frameSizeSlider) frameSizeSlider.value = frameSize;
        if (frameOutlineThicknessSlider) frameOutlineThicknessSlider.value = frameOutlineThickness;
        if (hairsThicknessSlider) hairsThicknessSlider.value = hairsThickness;
        if (hairsLengthSlider) hairsLengthSlider.value = hairsLength;
        if (hairsOutlineThicknessSlider) hairsOutlineThicknessSlider.value = hairsOutlineThickness;
        if (movementErrorAmountSlider) movementErrorAmountSlider.value = movementErrorAmount;
        if (movementErrorDecaySlider) movementErrorDecaySlider.value = movementErrorDecay;
        if (firingErrorAmountSlider) firingErrorAmountSlider.value = firingErrorAmount;
        if (firingErrorDecaySlider) firingErrorDecaySlider.value = firingErrorDecay;
        if (frameRotationSlider) frameRotationSlider.value = frameRotation;
        if (hairsRotationSlider) hairsRotationSlider.value = hairsRotation;
        if (dotRotationSlider) dotRotationSlider.value = dotRotation;
    }

    private void SetupToggles()
    {
        if (enableCrosshairToggle)
        {
            enableCrosshairToggle.isOn = enableCrosshair;
            enableCrosshairToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableCrosshair = val;
                SetVerticesDirty();
            });
        }

        if (enableOutlineToggle)
        {
            enableOutlineToggle.isOn = enableOutline;
            enableOutlineToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableOutline = val;
                SetVerticesDirty();
            });
        }

        if (enableDotToggle)
        {
            enableDotToggle.isOn = enableDot;
            enableDotToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableDot = val;
                SetVerticesDirty();
            });
        }

        if (dotOutlineToggle)
        {
            dotOutlineToggle.isOn = dotOutline;
            dotOutlineToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                dotOutline = val;
                SetVerticesDirty();
            });
        }

        if (enableFrameToggle)
        {
            enableFrameToggle.isOn = enableFrame;
            enableFrameToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableFrame = val;
                SetVerticesDirty();
            });
        }

        if (frameOutlineToggle)
        {
            frameOutlineToggle.isOn = frameOutline;
            frameOutlineToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                frameOutline = val;
                SetVerticesDirty();
            });
        }

        if (enableHairsToggle)
        {
            enableHairsToggle.isOn = enableHairs;
            enableHairsToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableHairs = val;
                SetVerticesDirty();
            });
        }

        if (hairsOutlineToggle)
        {
            hairsOutlineToggle.isOn = hairsOutline;
            hairsOutlineToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                hairsOutline = val;
                SetVerticesDirty();
            });
        }

        if (enableMovementErrorToggle)
        {
            enableMovementErrorToggle.isOn = enableMovementError;
            enableMovementErrorToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableMovementError = val;
            });
        }

        if (enableFiringErrorToggle)
        {
            enableFiringErrorToggle.isOn = enableFiringError;
            enableFiringErrorToggle.onValueChanged.AddListener(val => {
                if (isLoadingPreset) return;
                enableFiringError = val;
            });
        }

        if (snapToggle)
        {
            snapToggle.isOn = SnapEnabled;
            snapToggle.onValueChanged.AddListener(val => {
                SnapEnabled = val;
                
                if (SnapEnabled)
                {
                    if (frameRotationSlider)
                    {
                        float snappedValue = Mathf.Round(frameRotation / 45f) * 45f;
                        frameRotation = snappedValue;
                        frameRotationSlider.value = snappedValue;
                    }
                    
                    if (hairsRotationSlider)
                    {
                        float snappedValue = Mathf.Round(hairsRotation / 45f) * 45f;
                        hairsRotation = snappedValue;
                        hairsRotationSlider.value = snappedValue;
                    }
                    
                    if (dotRotationSlider)
                    {
                        float snappedValue = Mathf.Round(dotRotation / 45f) * 45f;
                        dotRotation = snappedValue;
                        dotRotationSlider.value = snappedValue;
                    }
                    
                    SetVerticesDirty();
                }
            });
        }
    }

    private void SetupKeybindUI()
    {
        if (keybindButton) keybindButton.onClick.AddListener(StartKeybindDetection);
        if (clearKeybindButton) clearKeybindButton.onClick.AddListener(ClearKeybind);
        if (confirmKeybindButton) confirmKeybindButton.onClick.AddListener(ConfirmKeybind);
        if (cancelKeybindButton) cancelKeybindButton.onClick.AddListener(CancelKeybind);
    }

    private void SetupColorPicker()
    {
        if (hueSlider) hueSlider.onValueChanged.AddListener(UpdateColorPreview);
        if (satSlider) satSlider.onValueChanged.AddListener(UpdateColorPreview);
        if (valSlider) valSlider.onValueChanged.AddListener(UpdateColorPreview);
        if (alphaSlider) alphaSlider.onValueChanged.AddListener(UpdateColorPreview);
        if (colorConfirmButton) colorConfirmButton.onClick.AddListener(ConfirmColor);
        if (colorCancelButton) colorCancelButton.onClick.AddListener(CancelColor);
        if (hexInput) hexInput.onEndEdit.AddListener(OnHexInputChanged);
    }

    private void SetupPresets()
    {
        if (presetDropdown)
        {
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }
    }

    private void OnPresetChanged(int newIndex)
    {
        if (isLoadingPreset) return;
        
        // Autosave current preset before switching
        presets[currentPresetIndex] = GenerateCrosshairCode();
        SavePresetToStorage(currentPresetIndex);
        
        // Switch to new preset
        SwitchToPreset(newIndex);
    }

    private void LoadPresets()
    {
        presets.Clear();
        
        for (int i = 0; i < 5; i++)
        {
            string key = $"CrosshairPreset_{i}";
            string presetCode = PlayerPrefs.GetString(key, "");
            
            if (string.IsNullOrEmpty(presetCode))
            {
                presetCode = GenerateDefaultPreset(i);
            }
            
            presets.Add(presetCode);
        }

        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int i = 0; i < presets.Count; i++)
            {
                options.Add($"Preset {i + 1}");
            }
            presetDropdown.AddOptions(options);
            presetDropdown.value = currentPresetIndex;
        }

        if (presets.Count > 0)
        {
            LoadCrosshairFromCode(presets[currentPresetIndex]);
        }
    }

    private string GenerateDefaultPreset(int index)
    {
        switch (index)
        {
            case 0: return "OVERPLAY-CROSSHAIR-V3;1;FFFFFF;2;5;15;1;000000;1;1;FFFFFF;2;1;000000;1;0;FFFFFF;2;30;1;000000;1;0;FFFFFF;1;50;1;000000;1;0;2;0.95;0;5;0.9;0;0;0";
            case 1: return "OVERPLAY-CROSSHAIR-V3;1;00FF00;3;3;20;1;000000;1;1;00FF00;3;1;000000;1;0;FFFFFF;2;30;1;000000;1;0;FFFFFF;1;50;1;000000;1;0;2;0.95;0;5;0.9;0;0;0";
            case 2: return "OVERPLAY-CROSSHAIR-V3;1;FF0000;1;8;12;1;000000;1;0;FF0000;4;0;000000;1;0;FFFFFF;2;30;1;000000;1;0;FFFFFF;1;50;1;000000;1;0;2;0.95;0;5;0.9;0;0;0";
            case 3: return "OVERPLAY-CROSSHAIR-V3;0;FFFFFF;2;5;15;1;000000;1;1;FFFF00;1;1;000000;1;1;FFFF00;1;25;1;000000;1;0;FFFFFF;1;50;1;000000;1;0;2;0.95;0;5;0.9;0;0;0";
            case 4: return "OVERPLAY-CROSSHAIR-V3;1;FFFFFF;1;0;25;0;000000;1;1;FFFFFF;1;1;000000;1;0;FFFFFF;2;30;1;000000;1;1;FFFFFF;2;100;1;000000;1;0;2;0.95;0;5;0.9;45;90;0";
            default: return "OVERPLAY-CROSSHAIR-V3;1;FFFFFF;2;5;15;1;000000;1;1;FFFFFF;2;1;000000;1;0;FFFFFF;2;30;1;000000;1;0;FFFFFF;1;50;1;000000;1;0;2;0.95;0;5;0.9;0;0;0";
        }
    }

    private void SavePresetToStorage(int index)
    {
        if (index >= 0 && index < presets.Count)
        {
            string key = $"CrosshairPreset_{index}";
            PlayerPrefs.SetString(key, presets[index]);
            PlayerPrefs.Save();
        }
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            string keybindStr = keybind.Count > 0 ? string.Join(" + ", keybind.Select(GetKeybindDisplayName)) : "None";
            statusText.text = $"Toggle Key: {keybindStr} | UI: {(uiVisible ? "Visible" : "Hidden")}";
        }
    }

    private string GetKeybindDisplayName(KeybindEntry entry)
    {
        if (entry.specialKey != SpecialKey.None)
        {
            switch (entry.specialKey)
            {
                case SpecialKey.MouseLeft: return "LMB";
                case SpecialKey.MouseRight: return "RMB";
                case SpecialKey.MouseMiddle: return "MMB";
                case SpecialKey.MouseWheelUp: return "MWU";
                case SpecialKey.MouseWheelDown: return "MWD";
                default: return entry.specialKey.ToString();
            }
        }
        return entry.keyCode.ToString();
    }

    private void StartKeybindDetection()
    {
        detectingKeybind = true;
        tempKeybind.Clear();
        if (keybindPopup) keybindPopup.SetActive(true);
        UpdateKeybindPreview();
    }

    private void ClearKeybind()
    {
        keybind.Clear();
        UpdateKeybindText();
        UpdateStatusText();
    }

    private void ConfirmKeybind()
    {
        keybind.Clear();
        keybind.AddRange(tempKeybind);
        detectingKeybind = false;
        if (keybindPopup) keybindPopup.SetActive(false);
        UpdateKeybindText();
        UpdateStatusText();
    }

    private void CancelKeybind()
    {
        detectingKeybind = false;
        tempKeybind.Clear();
        if (keybindPopup) keybindPopup.SetActive(false);
        UpdateKeybindText();
    }

    private void UpdateKeybindText()
    {
        if (keybindText != null)
        {
            if (keybind.Count > 0)
            {
                keybindText.text = string.Join(" + ", keybind.Select(GetKeybindDisplayName));
            }
            else
            {
                keybindText.text = "None";
            }
        }
    }

    private void OpenColorPicker(Color currentColor, System.Action<Color> callback)
    {
        targetColor = currentColor;
        colorCallback = callback;
        
        Color.RGBToHSV(currentColor, out float h, out float s, out float v);
        
        if (hueSlider) hueSlider.value = h;
        if (satSlider) satSlider.value = s;
        if (valSlider) valSlider.value = v;
        if (alphaSlider) alphaSlider.value = currentColor.a;
        
        UpdateColorPreview(0);
        UpdateHexInput();
        
        if (colorPickerContainer) colorPickerContainer.SetActive(true);
    }

    private void UpdateColorPreview(float value)
    {
        if (hueSlider && satSlider && valSlider && alphaSlider)
        {
            Color newColor = Color.HSVToRGB(hueSlider.value, satSlider.value, valSlider.value);
            newColor.a = alphaSlider.value;
            targetColor = newColor;
            
            if (colorPreview) colorPreview.color = targetColor;
            UpdateHexInput();
        }
    }

    private void UpdateHexInput()
    {
        if (hexInput)
        {
            string hex = ColorUtility.ToHtmlStringRGBA(targetColor);
            hexInput.text = hex;
        }
    }

    private void OnHexInputChanged(string hexValue)
    {
        if (ColorUtility.TryParseHtmlString("#" + hexValue, out Color newColor))
        {
            targetColor = newColor;
            
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            
            if (hueSlider) hueSlider.value = h;
            if (satSlider) satSlider.value = s;
            if (valSlider) valSlider.value = v;
            if (alphaSlider) alphaSlider.value = newColor.a;
            if (colorPreview) colorPreview.color = targetColor;
        }
    }

    private void ConfirmColor()
    {
        colorCallback?.Invoke(targetColor);
        if (colorPickerContainer) colorPickerContainer.SetActive(false);
        SetVerticesDirty();
    }

    private void CancelColor()
    {
        if (colorPickerContainer) colorPickerContainer.SetActive(false);
    }

    private void ImportCrosshair()
    {
        if (crosshairCodeInput != null && !string.IsNullOrEmpty(crosshairCodeInput.text))
        {
            LoadCrosshairFromCode(crosshairCodeInput.text);
        }
    }

    private void ExportCrosshair()
    {
        if (crosshairCodeInput != null)
        {
            crosshairCodeInput.text = GenerateCrosshairCode();
        }
    }

    private void ResetCrosshair()
    {
        enableCrosshair = true;
        crosshairColor = Color.white;
        crosshairThickness = 2f;
        crosshairGap = 5f;
        crosshairLength = 15f;
        enableOutline = true;
        outlineColor = Color.black;
        outlineThickness = 1f;
        enableDot = true;
        dotColor = Color.white;
        dotSize = 2f;
        dotOutline = true;
        dotOutlineColor = Color.black;
        dotOutlineThickness = 1f;
        enableFrame = false;
        frameColor = Color.white;
        frameThickness = 2f;
        frameSize = 30f;
        frameOutline = true;
        frameOutlineColor = Color.black;
        frameOutlineThickness = 1f;
        enableHairs = false;
        hairsColor = Color.white;
        hairsThickness = 1f;
        hairsLength = 50f;
        hairsOutline = true;
        hairsOutlineColor = Color.black;
        hairsOutlineThickness = 1f;
        enableMovementError = false;
        movementErrorAmount = 2f;
        movementErrorDecay = 0.95f;
        enableFiringError = false;
        firingErrorAmount = 5f;
        firingErrorDecay = 0.9f;
        frameRotation = 0f;
        hairsRotation = 0f;
        dotRotation = 0f;

        UpdateSliderValues();
        UpdateToggleValues();
        SetVerticesDirty();
    }

    private void UpdateSliderValues()
    {
        if (crosshairThicknessSlider) crosshairThicknessSlider.value = crosshairThickness;
        if (crosshairGapSlider) crosshairGapSlider.value = crosshairGap;
        if (crosshairLengthSlider) crosshairLengthSlider.value = crosshairLength;
        if (outlineThicknessSlider) outlineThicknessSlider.value = outlineThickness;
        if (dotSizeSlider) dotSizeSlider.value = dotSize;
        if (dotOutlineThicknessSlider) dotOutlineThicknessSlider.value = dotOutlineThickness;
        if (frameThicknessSlider) frameThicknessSlider.value = frameThickness;
        if (frameSizeSlider) frameSizeSlider.value = frameSize;
        if (frameOutlineThicknessSlider) frameOutlineThicknessSlider.value = frameOutlineThickness;
        if (hairsThicknessSlider) hairsThicknessSlider.value = hairsThickness;
        if (hairsLengthSlider) hairsLengthSlider.value = hairsLength;
        if (hairsOutlineThicknessSlider) hairsOutlineThicknessSlider.value = hairsOutlineThickness;
        if (movementErrorAmountSlider) movementErrorAmountSlider.value = movementErrorAmount;
        if (movementErrorDecaySlider) movementErrorDecaySlider.value = movementErrorDecay;
        if (firingErrorAmountSlider) firingErrorAmountSlider.value = firingErrorAmount;
        if (firingErrorDecaySlider) firingErrorDecaySlider.value = firingErrorDecay;
        if (frameRotationSlider) frameRotationSlider.value = frameRotation;
        if (hairsRotationSlider) hairsRotationSlider.value = hairsRotation;
        if (dotRotationSlider) dotRotationSlider.value = dotRotation;
    }

    private void UpdateToggleValues()
    {
        if (enableCrosshairToggle) enableCrosshairToggle.isOn = enableCrosshair;
        if (enableOutlineToggle) enableOutlineToggle.isOn = enableOutline;
        if (enableDotToggle) enableDotToggle.isOn = enableDot;
        if (dotOutlineToggle) dotOutlineToggle.isOn = dotOutline;
        if (enableFrameToggle) enableFrameToggle.isOn = enableFrame;
        if (frameOutlineToggle) frameOutlineToggle.isOn = frameOutline;
        if (enableHairsToggle) enableHairsToggle.isOn = enableHairs;
        if (hairsOutlineToggle) hairsOutlineToggle.isOn = hairsOutline;
        if (enableMovementErrorToggle) enableMovementErrorToggle.isOn = enableMovementError;
        if (enableFiringErrorToggle) enableFiringErrorToggle.isOn = enableFiringError;
    }

    private void CopyCrosshairCode()
    {
        string code = GenerateCrosshairCode();
        GUIUtility.systemCopyBuffer = code;
        Debug.Log("Crosshair code copied to clipboard");
    }

    private void PasteCrosshairCode()
    {
        string code = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(code))
        {
            LoadCrosshairFromCode(code);
            if (crosshairCodeInput != null)
            {
                crosshairCodeInput.text = code;
            }
        }
    }

    public string GenerateCrosshairCode()
    {
        List<string> values = new List<string>
        {
            "OVERPLAY-CROSSHAIR-V3",
            enableCrosshair ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(crosshairColor),
            crosshairThickness.ToString("F1", CultureInfo.InvariantCulture),
            crosshairGap.ToString("F1", CultureInfo.InvariantCulture),
            crosshairLength.ToString("F1", CultureInfo.InvariantCulture),
            enableOutline ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(outlineColor),
            outlineThickness.ToString("F1", CultureInfo.InvariantCulture),
            enableDot ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(dotColor),
            dotSize.ToString("F1", CultureInfo.InvariantCulture),
            dotOutline ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(dotOutlineColor),
            dotOutlineThickness.ToString("F1", CultureInfo.InvariantCulture),
            enableFrame ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(frameColor),
            frameThickness.ToString("F1", CultureInfo.InvariantCulture),
            frameSize.ToString("F1", CultureInfo.InvariantCulture),
            frameOutline ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(frameOutlineColor),
            frameOutlineThickness.ToString("F1", CultureInfo.InvariantCulture),
            enableHairs ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(hairsColor),
            hairsThickness.ToString("F1", CultureInfo.InvariantCulture),
            hairsLength.ToString("F1", CultureInfo.InvariantCulture),
            hairsOutline ? "1" : "0",
            ColorUtility.ToHtmlStringRGB(hairsOutlineColor),
            hairsOutlineThickness.ToString("F1", CultureInfo.InvariantCulture),
            enableMovementError ? "1" : "0",
            movementErrorAmount.ToString("F1", CultureInfo.InvariantCulture),
            movementErrorDecay.ToString("F2", CultureInfo.InvariantCulture),
            enableFiringError ? "1" : "0",
            firingErrorAmount.ToString("F1", CultureInfo.InvariantCulture),
            firingErrorDecay.ToString("F2", CultureInfo.InvariantCulture),
            frameRotation.ToString("F0", CultureInfo.InvariantCulture),
            hairsRotation.ToString("F0", CultureInfo.InvariantCulture),
            dotRotation.ToString("F0", CultureInfo.InvariantCulture)
        };

        return string.Join(";", values);
    }

    public void LoadCrosshairFromCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return;

        string[] parts = code.Split(';');
        if (parts.Length < 39 || parts[0] != "OVERPLAY-CROSSHAIR-V3") return;

        try
        {
            isLoadingPreset = true;

            enableCrosshair = parts[1] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[2], out crosshairColor);
            crosshairThickness = float.Parse(parts[3], CultureInfo.InvariantCulture);
            crosshairGap = float.Parse(parts[4], CultureInfo.InvariantCulture);
            crosshairLength = float.Parse(parts[5], CultureInfo.InvariantCulture);
            enableOutline = parts[6] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[7], out outlineColor);
            outlineThickness = float.Parse(parts[8], CultureInfo.InvariantCulture);
            enableDot = parts[9] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[10], out dotColor);
            dotSize = float.Parse(parts[11], CultureInfo.InvariantCulture);
            dotOutline = parts[12] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[13], out dotOutlineColor);
            dotOutlineThickness = float.Parse(parts[14], CultureInfo.InvariantCulture);
            enableFrame = parts[15] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[16], out frameColor);
            frameThickness = float.Parse(parts[17], CultureInfo.InvariantCulture);
            frameSize = float.Parse(parts[18], CultureInfo.InvariantCulture);
            frameOutline = parts[19] == "1";
                        ColorUtility.TryParseHtmlString("#" + parts[20], out frameOutlineColor);
            frameOutlineThickness = float.Parse(parts[21], CultureInfo.InvariantCulture);
            enableHairs = parts[22] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[23], out hairsColor);
            hairsThickness = float.Parse(parts[24], CultureInfo.InvariantCulture);
            hairsLength = float.Parse(parts[25], CultureInfo.InvariantCulture);
            hairsOutline = parts[26] == "1";
            ColorUtility.TryParseHtmlString("#" + parts[27], out hairsOutlineColor);
            hairsOutlineThickness = float.Parse(parts[28], CultureInfo.InvariantCulture);
            enableMovementError = parts[29] == "1";
            movementErrorAmount = float.Parse(parts[30], CultureInfo.InvariantCulture);
            movementErrorDecay = float.Parse(parts[31], CultureInfo.InvariantCulture);
            enableFiringError = parts[32] == "1";
            firingErrorAmount = float.Parse(parts[33], CultureInfo.InvariantCulture);
            firingErrorDecay = float.Parse(parts[34], CultureInfo.InvariantCulture);
            frameRotation = float.Parse(parts[35], CultureInfo.InvariantCulture);
            hairsRotation = float.Parse(parts[36], CultureInfo.InvariantCulture);
            dotRotation = float.Parse(parts[37], CultureInfo.InvariantCulture);

            UpdateSliderValues();
            UpdateToggleValues();
            SetVerticesDirty();

            isLoadingPreset = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading crosshair code: {e.Message}");
            isLoadingPreset = false;
        }
    }

    public void AddMovementError(Vector2 error)
    {
        if (enableMovementError)
        {
            movementError += error * movementErrorAmount;
        }
    }

    public void AddFiringError(Vector2 error)
    {
        if (enableFiringError)
        {
            firingError += error * firingErrorAmount;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!enableCrosshair) return;

        Vector2 center = rectTransform.rect.center;
        Vector2 totalError = movementError + firingError;
        Vector2 adjustedCenter = center + totalError;

        if (enableCrosshair)
        {
            DrawCrosshair(vh, adjustedCenter);
        }

        if (enableDot)
        {
            DrawDot(vh, adjustedCenter);
        }

        if (enableFrame)
        {
            DrawFrame(vh, adjustedCenter);
        }

        if (enableHairs)
        {
            DrawHairs(vh, adjustedCenter);
        }
    }

    private void DrawCrosshair(VertexHelper vh, Vector2 center)
    {
        float halfGap = crosshairGap * 0.5f;
        float halfThickness = crosshairThickness * 0.5f;

        // Horizontal line
        Vector2[] horizontalVerts = new Vector2[]
        {
            new Vector2(center.x - crosshairLength - halfGap, center.y - halfThickness),
            new Vector2(center.x - halfGap, center.y - halfThickness),
            new Vector2(center.x - halfGap, center.y + halfThickness),
            new Vector2(center.x - crosshairLength - halfGap, center.y + halfThickness),
            new Vector2(center.x + halfGap, center.y - halfThickness),
            new Vector2(center.x + crosshairLength + halfGap, center.y - halfThickness),
            new Vector2(center.x + crosshairLength + halfGap, center.y + halfThickness),
            new Vector2(center.x + halfGap, center.y + halfThickness)
        };

        // Vertical line
        Vector2[] verticalVerts = new Vector2[]
        {
            new Vector2(center.x - halfThickness, center.y - crosshairLength - halfGap),
            new Vector2(center.x + halfThickness, center.y - crosshairLength - halfGap),
            new Vector2(center.x + halfThickness, center.y - halfGap),
            new Vector2(center.x - halfThickness, center.y - halfGap),
            new Vector2(center.x - halfThickness, center.y + halfGap),
            new Vector2(center.x + halfThickness, center.y + halfGap),
            new Vector2(center.x + halfThickness, center.y + crosshairLength + halfGap),
            new Vector2(center.x - halfThickness, center.y + crosshairLength + halfGap)
        };

        if (enableOutline)
        {
            DrawOutlinedQuads(vh, horizontalVerts, crosshairColor, outlineColor, outlineThickness);
            DrawOutlinedQuads(vh, verticalVerts, crosshairColor, outlineColor, outlineThickness);
        }
        else
        {
            DrawQuads(vh, horizontalVerts, crosshairColor);
            DrawQuads(vh, verticalVerts, crosshairColor);
        }
    }

    private void DrawDot(VertexHelper vh, Vector2 center)
    {
        float rotationRad = dotRotation * Mathf.Deg2Rad;
        float halfSize = dotSize * 0.5f;

        Vector2[] dotVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfSize, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y + halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize, center.y + halfSize), center, rotationRad)
        };

        if (dotOutline)
        {
            DrawOutlinedQuad(vh, dotVerts, dotColor, dotOutlineColor, dotOutlineThickness);
        }
        else
        {
            DrawQuad(vh, dotVerts, dotColor);
        }
    }

    private void DrawFrame(VertexHelper vh, Vector2 center)
    {
        float rotationRad = frameRotation * Mathf.Deg2Rad;
        float halfSize = frameSize * 0.5f;
        float halfThickness = frameThickness * 0.5f;

        // Top
        Vector2[] topVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfSize, center.y + halfSize - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y + halfSize - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y + halfSize + halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize, center.y + halfSize + halfThickness), center, rotationRad)
        };

        // Bottom
        Vector2[] bottomVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfSize, center.y - halfSize - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y - halfSize - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize, center.y - halfSize + halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize, center.y - halfSize + halfThickness), center, rotationRad)
        };

        // Left
        Vector2[] leftVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfSize - halfThickness, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize + halfThickness, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize + halfThickness, center.y + halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfSize - halfThickness, center.y + halfSize), center, rotationRad)
        };

        // Right
        Vector2[] rightVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x + halfSize - halfThickness, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize + halfThickness, center.y - halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize + halfThickness, center.y + halfSize), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfSize - halfThickness, center.y + halfSize), center, rotationRad)
        };

        if (frameOutline)
        {
            DrawOutlinedQuad(vh, topVerts, frameColor, frameOutlineColor, frameOutlineThickness);
            DrawOutlinedQuad(vh, bottomVerts, frameColor, frameOutlineColor, frameOutlineThickness);
            DrawOutlinedQuad(vh, leftVerts, frameColor, frameOutlineColor, frameOutlineThickness);
            DrawOutlinedQuad(vh, rightVerts, frameColor, frameOutlineColor, frameOutlineThickness);
        }
        else
        {
            DrawQuad(vh, topVerts, frameColor);
            DrawQuad(vh, bottomVerts, frameColor);
            DrawQuad(vh, leftVerts, frameColor);
            DrawQuad(vh, rightVerts, frameColor);
        }
    }

    private void DrawHairs(VertexHelper vh, Vector2 center)
    {
        float rotationRad = hairsRotation * Mathf.Deg2Rad;
        float halfThickness = hairsThickness * 0.5f;

        // Top hair
        Vector2[] topHairVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfThickness, center.y), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfThickness, center.y), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfThickness, center.y + hairsLength), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfThickness, center.y + hairsLength), center, rotationRad)
        };

        // Bottom hair
        Vector2[] bottomHairVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - halfThickness, center.y - hairsLength), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfThickness, center.y - hairsLength), center, rotationRad),
            RotatePoint(new Vector2(center.x + halfThickness, center.y), center, rotationRad),
            RotatePoint(new Vector2(center.x - halfThickness, center.y), center, rotationRad)
        };

        // Left hair
        Vector2[] leftHairVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x - hairsLength, center.y - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x, center.y - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x, center.y + halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x - hairsLength, center.y + halfThickness), center, rotationRad)
        };

        // Right hair
        Vector2[] rightHairVerts = new Vector2[]
        {
            RotatePoint(new Vector2(center.x, center.y - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + hairsLength, center.y - halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x + hairsLength, center.y + halfThickness), center, rotationRad),
            RotatePoint(new Vector2(center.x, center.y + halfThickness), center, rotationRad)
        };

        if (hairsOutline)
        {
            DrawOutlinedQuad(vh, topHairVerts, hairsColor, hairsOutlineColor, hairsOutlineThickness);
            DrawOutlinedQuad(vh, bottomHairVerts, hairsColor, hairsOutlineColor, hairsOutlineThickness);
            DrawOutlinedQuad(vh, leftHairVerts, hairsColor, hairsOutlineColor, hairsOutlineThickness);
            DrawOutlinedQuad(vh, rightHairVerts, hairsColor, hairsOutlineColor, hairsOutlineThickness);
        }
        else
        {
            DrawQuad(vh, topHairVerts, hairsColor);
            DrawQuad(vh, bottomHairVerts, hairsColor);
            DrawQuad(vh, leftHairVerts, hairsColor);
            DrawQuad(vh, rightHairVerts, hairsColor);
        }
    }

    private Vector2 RotatePoint(Vector2 point, Vector2 center, float angleRad)
    {
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        Vector2 dir = point - center;
        return center + new Vector2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos);
    }

    private void DrawQuad(VertexHelper vh, Vector2[] verts, Color color)
    {
        int startIndex = vh.currentVertCount;
        
        for (int i = 0; i < 4; i++)
        {
            vh.AddVert(verts[i], color, Vector2.zero);
        }
        
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private void DrawQuads(VertexHelper vh, Vector2[] verts, Color color)
    {
        if (verts.Length >= 4)
        {
            DrawQuad(vh, new Vector2[] { verts[0], verts[1], verts[2], verts[3] }, color);
        }
        if (verts.Length >= 8)
        {
            DrawQuad(vh, new Vector2[] { verts[4], verts[5], verts[6], verts[7] }, color);
        }
    }

    private void DrawOutlinedQuad(VertexHelper vh, Vector2[] verts, Color fillColor, Color outlineColor, float outlineWidth)
    {
        Vector2[] outlineVerts = ExpandQuad(verts, outlineWidth);
        DrawQuad(vh, outlineVerts, outlineColor);
        DrawQuad(vh, verts, fillColor);
    }

    private void DrawOutlinedQuads(VertexHelper vh, Vector2[] verts, Color fillColor, Color outlineColor, float outlineWidth)
    {
        if (verts.Length >= 4)
        {
            Vector2[] quad1 = new Vector2[] { verts[0], verts[1], verts[2], verts[3] };
            DrawOutlinedQuad(vh, quad1, fillColor, outlineColor, outlineWidth);
        }
        if (verts.Length >= 8)
        {
            Vector2[] quad2 = new Vector2[] { verts[4], verts[5], verts[6], verts[7] };
            DrawOutlinedQuad(vh, quad2, fillColor, outlineColor, outlineWidth);
        }
    }

    private Vector2[] ExpandQuad(Vector2[] quad, float expansion)
    {
        Vector2 center = (quad[0] + quad[1] + quad[2] + quad[3]) * 0.25f;
        Vector2[] expanded = new Vector2[4];
        
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = (quad[i] - center).normalized;
            expanded[i] = quad[i] + direction * expansion;
        }
        
        return expanded;
    }
}

            