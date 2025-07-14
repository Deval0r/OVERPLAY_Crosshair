using UnityEngine;
using UnityEngine.UI;

public class CrosshairSettings : MonoBehaviour
{
    // ... your fields ...
}

public class CrosshairCustomizerUI : MonoBehaviour
{
    public CrosshairSettings settings;
    public CrosshairRenderer renderer;

    // Frame UI
    public Dropdown frameShapeDropdown;
    public Toggle frameFilledToggle;
    public Slider frameOpacitySlider;
    public Slider frameScaleSlider;
    public Button frameColorButton;

    // Hairs UI
    public Dropdown hairStyleDropdown;
    public Slider hairCountSlider;
    public Slider customAngleSlider;
    public Slider hairThicknessSlider;
    public Slider hairLengthSlider;
    public Button hairColorButton;
    public Slider hairOpacitySlider;

    // Dot UI
    public Dropdown dotShapeDropdown;
    public Toggle dotFilledToggle;
    public Button dotColorButton;
    public Slider dotOpacitySlider;
    public Slider dotScaleSlider;

    // Color pickers (assume you have a method to open a color dialog)
    private void Start()
    {
        // Frame
        frameShapeDropdown.onValueChanged.AddListener(val => { settings.frameShape = (CrosshairShape)val; renderer.SetVerticesDirty(); });
        frameFilledToggle.onValueChanged.AddListener(val => { settings.frameFilled = val; renderer.SetVerticesDirty(); });
        frameOpacitySlider.onValueChanged.AddListener(val => { settings.frameOpacity = val; renderer.SetVerticesDirty(); });
        frameScaleSlider.onValueChanged.AddListener(val => { settings.frameScale = val; renderer.SetVerticesDirty(); });
        frameColorButton.onClick.AddListener(() => PickColor(c => { settings.frameColor = c; renderer.SetVerticesDirty(); }));

        // Hairs
        hairStyleDropdown.onValueChanged.AddListener(val => { settings.hairStyle = (HairStyle)val; renderer.SetVerticesDirty(); UpdateHairUI(); });
        hairCountSlider.onValueChanged.AddListener(val => { settings.hairCount = Mathf.RoundToInt(val); renderer.SetVerticesDirty(); });
        customAngleSlider.onValueChanged.AddListener(val => { settings.customAngle = val; renderer.SetVerticesDirty(); });
        hairThicknessSlider.onValueChanged.AddListener(val => { settings.hairThickness = val; renderer.SetVerticesDirty(); });
        hairLengthSlider.onValueChanged.AddListener(val => { settings.hairLength = val; renderer.SetVerticesDirty(); });
        hairColorButton.onClick.AddListener(() => PickColor(c => { settings.hairColor = c; renderer.SetVerticesDirty(); }));
        hairOpacitySlider.onValueChanged.AddListener(val => { settings.hairOpacity = val; renderer.SetVerticesDirty(); });

        // Dot
        dotShapeDropdown.onValueChanged.AddListener(val => { settings.dotShape = (CrosshairShape)val; renderer.SetVerticesDirty(); });
        dotFilledToggle.onValueChanged.AddListener(val => { settings.dotFilled = val; renderer.SetVerticesDirty(); });
        dotColorButton.onClick.AddListener(() => PickColor(c => { settings.dotColor = c; renderer.SetVerticesDirty(); }));
        dotOpacitySlider.onValueChanged.AddListener(val => { settings.dotOpacity = val; renderer.SetVerticesDirty(); });
        dotScaleSlider.onValueChanged.AddListener(val => { settings.dotScale = val; renderer.SetVerticesDirty(); });

        UpdateHairUI();
    }

    // Example color picker callback (replace with your own implementation)
    void PickColor(System.Action<Color> onColorPicked)
    {
        // Open your color picker dialog and call onColorPicked with the selected color
        // For now, just set to white as a placeholder
        onColorPicked?.Invoke(Color.white);
    }

    void UpdateHairUI()
    {
        // Enable custom angle slider only if style is Custom
        bool isCustom = settings.hairStyle == HairStyle.Custom;
        customAngleSlider.gameObject.SetActive(isCustom);
        hairCountSlider.gameObject.SetActive(!isCustom);
    }
}