using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Snapping")]
    public Toggle snapRotationToggle;
    private bool SnapEnabled => snapRotationToggle != null && snapRotationToggle.isOn;

    [Header("Keybinds")]
    public UnityEngine.UI.Button keybindRecordButton;
    public GameObject uiRoot; // Assign your UI root GameObject here

    private List<KeyCode> currentKeybind = new List<KeyCode> { KeyCode.F2 }; // Default: F2 (Fn+F2 is hardware, so use F2)
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

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        DrawFrame(vh);
        DrawHairs(vh);
        DrawDot(vh);
    }

    new void Awake()
    {
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
        if (frameShapeDropdown) frameShapeDropdown.onValueChanged.AddListener(val => { frameShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (frameFilledToggle) frameFilledToggle.onValueChanged.AddListener(val => { frameFilled = val; SetVerticesDirty(); });
        if (frameOpacitySlider) frameOpacitySlider.onValueChanged.AddListener(val => { frameOpacity = val; SetVerticesDirty(); });
        if (frameScaleSlider) frameScaleSlider.onValueChanged.AddListener(val => { frameScale = val; SetVerticesDirty(); });
        if (frameRotationSlider) frameRotationSlider.onValueChanged.AddListener(val => {
            float value = frameRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            frameRotation = value;
            if (SnapEnabled) frameRotationSlider.value = value;
            SetVerticesDirty();
        });
        if (frameThicknessSlider) frameThicknessSlider.onValueChanged.AddListener(val => { frameThickness = val; SetVerticesDirty(); });
        if (frameColorSlider) {
            frameColorSlider.onValueChanged.AddListener(val => {
                frameHue = val;
                frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
                SetVerticesDirty();
                ShowColorPreview(frameColorPreviewObj, frameColorPreviewRenderer, frameColor, PreviewType.Frame);
            });
        }
        if (frameSaturationSlider) frameSaturationSlider.onValueChanged.AddListener(val => {
            frameSaturation = val;
            frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
            SetVerticesDirty();
            ShowReferenceImage(frameSaturationRefObj, frameSaturationRefRenderer, RefType.FrameSaturation);
        });
        if (frameValueSlider) frameValueSlider.onValueChanged.AddListener(val => {
            frameValue = val;
            frameColor = Color.HSVToRGB(frameHue, frameSaturation, frameValue);
            SetVerticesDirty();
            ShowReferenceImage(frameValueRefObj, frameValueRefRenderer, RefType.FrameValue);
        });

        // --- Hairs UI ---
        if (hairStyleDropdown) hairStyleDropdown.onValueChanged.AddListener(val => { hairStyle = (HairStyle)val; SetVerticesDirty(); UpdateHairUI(); });
        if (hairCountSlider) hairCountSlider.onValueChanged.AddListener(val => { hairCount = Mathf.RoundToInt(val); SetVerticesDirty(); });
        if (customAngleSlider) customAngleSlider.onValueChanged.AddListener(val => { customAngle = val; SetVerticesDirty(); });
        if (hairThicknessSlider) hairThicknessSlider.onValueChanged.AddListener(val => { hairThickness = val; SetVerticesDirty(); });
        if (hairLengthSlider) hairLengthSlider.onValueChanged.AddListener(val => { hairLength = val; SetVerticesDirty(); });
        if (hairsRotationSlider) hairsRotationSlider.onValueChanged.AddListener(val => {
            float value = hairsRotationSlider.value;
            if (SnapEnabled) value = Mathf.Round(value / 45f) * 45f;
            hairsRotation = value;
            if (SnapEnabled) hairsRotationSlider.value = value;
            SetVerticesDirty();
        });
        if (hairColorSlider) {
            hairColorSlider.onValueChanged.AddListener(val => {
                hairHue = val;
                hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
                SetVerticesDirty();
                ShowColorPreview(hairColorPreviewObj, hairColorPreviewRenderer, hairColor, PreviewType.Hair);
            });
        }
        if (hairSaturationSlider) hairSaturationSlider.onValueChanged.AddListener(val => {
            hairSaturation = val;
            hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
            SetVerticesDirty();
            ShowReferenceImage(hairSaturationRefObj, hairSaturationRefRenderer, RefType.HairSaturation);
        });
        if (hairValueSlider) hairValueSlider.onValueChanged.AddListener(val => {
            hairValue = val;
            hairColor = Color.HSVToRGB(hairHue, hairSaturation, hairValue);
            SetVerticesDirty();
            ShowReferenceImage(hairValueRefObj, hairValueRefRenderer, RefType.HairValue);
        });
        if (hairOpacitySlider) hairOpacitySlider.onValueChanged.AddListener(val => { hairOpacity = val; SetVerticesDirty(); });
        if (hairsExtendPastFrameToggle) hairsExtendPastFrameToggle.onValueChanged.AddListener(val => { SetVerticesDirty(); });
        if (hairDistanceSlider) hairDistanceSlider.onValueChanged.AddListener(val => { SetVerticesDirty(); });

        // --- Dot UI ---
        if (dotShapeDropdown) dotShapeDropdown.onValueChanged.AddListener(val => { dotShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (dotFilledToggle) dotFilledToggle.onValueChanged.AddListener(val => { dotFilled = val; SetVerticesDirty(); });
        if (dotColorSlider) {
            dotColorSlider.onValueChanged.AddListener(val => {
                dotHue = val;
                dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
                SetVerticesDirty();
                ShowColorPreview(dotColorPreviewObj, dotColorPreviewRenderer, dotColor, PreviewType.Dot);
            });
        }
        if (dotSaturationSlider) dotSaturationSlider.onValueChanged.AddListener(val => {
            dotSaturation = val;
            dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
            SetVerticesDirty();
            ShowReferenceImage(dotSaturationRefObj, dotSaturationRefRenderer, RefType.DotSaturation);
        });
        if (dotValueSlider) dotValueSlider.onValueChanged.AddListener(val => {
            dotValue = val;
            dotColor = Color.HSVToRGB(dotHue, dotSaturation, dotValue);
            SetVerticesDirty();
            ShowReferenceImage(dotValueRefObj, dotValueRefRenderer, RefType.DotValue);
        });
        if (dotOpacitySlider) dotOpacitySlider.onValueChanged.AddListener(val => { dotOpacity = val; SetVerticesDirty(); });
        if (dotScaleSlider) dotScaleSlider.onValueChanged.AddListener(val => { dotScale = val; SetVerticesDirty(); });
        if (dotRotationSlider) dotRotationSlider.onValueChanged.AddListener(val => {
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

        if (saveCodeButton) saveCodeButton.onClick.AddListener(SaveCrosshairCode);
        if (loadCodeButton) loadCodeButton.onClick.AddListener(LoadCrosshairCode);
    }

    void Update()
    {
        if (recordingKeybind)
        {
            // Only check keys supported by SystemInput, but use Unity Input for recording
            foreach (var key in SystemInput.VK_KeyCodes.Keys)
            {
                if (Input.GetKeyDown(key))
                    pressedKeys.Add(key);
                if (Input.GetKeyUp(key))
                    pressedKeys.Remove(key);
            }
            // Track the last non-empty set of pressed keys
            if (pressedKeys.Count > 0)
            {
                lastPressedKeys = new List<KeyCode>(pressedKeys);
                keybindReleaseTimer = 0f;
                if (keybindRecordButton) keybindRecordButton.GetComponentInChildren<TMPro.TMP_Text>().text = KeybindToString(lastPressedKeys);
            }
            // If no keys are pressed, start grace timer
            if (pressedKeys.Count == 0 && lastPressedKeys.Count > 0)
            {
                keybindReleaseTimer += Time.unscaledDeltaTime;
                if (keybindReleaseTimer >= keybindReleaseGrace)
                {
                    currentKeybind = new List<KeyCode>(lastPressedKeys);
                    recordingKeybind = false;
                    if (keybindRecordButton) keybindRecordButton.GetComponentInChildren<TMPro.TMP_Text>().text = KeybindToString(currentKeybind);
                }
            }
            else if (pressedKeys.Count > 0)
            {
                keybindReleaseTimer = 0f;
            }
        }
        else
        {
            // Manual cooldown-based keybind detection
            bool allHeld = currentKeybind.Count > 0;
            foreach (var k in currentKeybind)
            {
                if (!SystemInput.GetKey(k)) allHeld = false;
            }
            if (allHeld)
            {
                if (!keybindWasHeld)
                {
                    ToggleUI();
                    keybindWasHeld = true;
                }
            }
            else
            {
                keybindWasHeld = false;
            }
        }
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

    string KeybindToString(List<KeyCode> keys)
    {
        if (keys == null || keys.Count == 0) return "Set Keybind";
        return string.Join(" + ", keys);
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
        // Only set alpha to 1, keep RGB as is
        var c = refRenderer.color;
        refRenderer.color = new Color(c.r, c.g, c.b, 1f);
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

    // --- Color Picker Placeholder ---
    // (Removed old color picker logic)

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

    private void ShowTab(TabType type)
    {
        currentTab = type;
        SetTabActive(frameTabRoot, type == TabType.Frame);
        SetTabActive(hairTabRoot, type == TabType.Hair);
        SetTabActive(dotTabRoot, type == TabType.Dot);
        // (Optional: visually highlight selected button here)
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
        string code = frameSection + ";" + hairsSection + ";" + dotSection;
        GUIUtility.systemCopyBuffer = code;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Render crosshair to 256x256 Texture2D and copy to clipboard as image
        Texture2D preview = RenderCrosshairPreview(256, 256);
        if (preview != null)
        {
            ClipboardImageHelper.CopyImageToClipboard(preview);
            Destroy(preview);
        }
#endif
    }

    private Texture2D RenderCrosshairPreview(int width, int height)
    {
        // Create a temporary RenderTexture
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
        cam.orthographicSize = height/2f;
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
        RenderTexture.ReleaseTemporary(rt);
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
        string code = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(code)) return;
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
            case HairStyle.Custom: return "U";
            default: return "E";
        }
    }
    private HairStyle CodeToHairStyle(string code)
    {
        switch (code)
        {
            case "E": return HairStyle.Even;
            case "U": return HairStyle.Custom;
            default: return HairStyle.Even;
        }
    }
    private float ParseF(string s)
    {
        float f = 0f;
        float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f);
        return f;
    }

#if UNITY_EDITOR
    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, results);
            if (results.Count == 0)
            {
                // Debug.Log("No UI element under mouse.");
            }
            else
            {
                // foreach (var r in results)
                // {
                //     Debug.Log($"UI under mouse: {r.gameObject.name} (sortingLayer={r.sortingLayer}, sortingOrder={r.sortingOrder})");
                // }
            }
        }
    }
#endif
}