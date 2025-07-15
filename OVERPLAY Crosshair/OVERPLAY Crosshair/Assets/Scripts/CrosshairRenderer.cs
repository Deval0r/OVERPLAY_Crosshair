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
    public Color frameColor = Color.white;
    [Range(0, 1)] public float frameOpacity = 1f;
    public float frameScale = 1f;
    [Range(0, 360)] public float frameRotation = 0f;

    // Hairs
    public HairStyle hairStyle = HairStyle.Even;
    public int hairCount = 4;
    public float customAngle = 90f;
    public float hairThickness = 2f;
    public float hairLength = 20f;
    public Color hairColor = Color.white;
    [Range(0, 1)] public float hairOpacity = 1f;
    [Range(0, 360)] public float hairsRotation = 0f;

    // Dot
    public CrosshairShape dotShape = CrosshairShape.Circle;
    public bool dotFilled = true;
    public Color dotColor = Color.white;
    [Range(0, 1)] public float dotOpacity = 1f;
    public float dotScale = 1f;
    [Range(0, 360)] public float dotRotation = 0f;

    // --- UI References ---
    [Header("Frame UI")]
    public TMP_Dropdown frameShapeDropdown;
    public Toggle frameFilledToggle;
    public Slider frameOpacitySlider;
    public Slider frameScaleSlider;
    public Slider frameRotationSlider;
    public Slider frameColorSlider; // always-visible color slider
    public GameObject frameColorPreviewObj; // preview GameObject with sprite
    private UnityEngine.UI.Image frameColorPreviewImg; // cached Image component

    [Header("Hairs UI")]
    public TMP_Dropdown hairStyleDropdown;
    public Slider hairCountSlider;
    public Slider customAngleSlider;
    public Slider hairThicknessSlider;
    public Slider hairLengthSlider;
    public Slider hairsRotationSlider;
    public Slider hairColorSlider; // always-visible color slider
    public GameObject hairColorPreviewObj; // preview GameObject with sprite
    private UnityEngine.UI.Image hairColorPreviewImg; // cached Image component
    public Slider hairOpacitySlider;
    public Toggle hairsExtendPastFrameToggle;
    public Slider hairDistanceSlider;

    [Header("Dot UI")]
    public TMP_Dropdown dotShapeDropdown;
    public Toggle dotFilledToggle;
    public Slider dotColorSlider; // always-visible color slider
    public GameObject dotColorPreviewObj; // preview GameObject with sprite
    private UnityEngine.UI.Image dotColorPreviewImg; // cached Image component
    public Slider dotOpacitySlider;
    public Slider dotScaleSlider;
    public Slider dotRotationSlider;

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

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        DrawFrame(vh);
        DrawHairs(vh);
        DrawDot(vh);
    }

    new void Awake()
    {
        raycastTarget = false;
    }

    new void Start()
    {
        // Cache Image components from preview GameObjects
        if (frameColorPreviewObj) frameColorPreviewImg = frameColorPreviewObj.GetComponent<UnityEngine.UI.Image>();
        if (hairColorPreviewObj) hairColorPreviewImg = hairColorPreviewObj.GetComponent<UnityEngine.UI.Image>();
        if (dotColorPreviewObj) dotColorPreviewImg = dotColorPreviewObj.GetComponent<UnityEngine.UI.Image>();
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
        if (frameColorSlider) {
            frameColorSlider.onValueChanged.AddListener(val => {
                frameColor = Color.HSVToRGB(val, 1f, 1f);
                SetVerticesDirty();
                ShowColorPreview(frameColorPreviewObj, frameColorPreviewImg, frameColor, PreviewType.Frame);
            });
        }

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
                hairColor = Color.HSVToRGB(val, 1f, 1f);
                SetVerticesDirty();
                ShowColorPreview(hairColorPreviewObj, hairColorPreviewImg, hairColor, PreviewType.Hair);
            });
        }
        if (hairOpacitySlider) hairOpacitySlider.onValueChanged.AddListener(val => { hairOpacity = val; SetVerticesDirty(); });
        if (hairsExtendPastFrameToggle) hairsExtendPastFrameToggle.onValueChanged.AddListener(val => { SetVerticesDirty(); });
        if (hairDistanceSlider) hairDistanceSlider.onValueChanged.AddListener(val => { SetVerticesDirty(); });

        // --- Dot UI ---
        if (dotShapeDropdown) dotShapeDropdown.onValueChanged.AddListener(val => { dotShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (dotFilledToggle) dotFilledToggle.onValueChanged.AddListener(val => { dotFilled = val; SetVerticesDirty(); });
        if (dotColorSlider) {
            dotColorSlider.onValueChanged.AddListener(val => {
                dotColor = Color.HSVToRGB(val, 1f, 1f);
                SetVerticesDirty();
                ShowColorPreview(dotColorPreviewObj, dotColorPreviewImg, dotColor, PreviewType.Dot);
            });
        }
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

        // Keybind record button
        if (keybindRecordButton) keybindRecordButton.onClick.AddListener(StartKeybindRecording);

        UpdateHairUI();
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
                    Debug.Log("Keybind held, toggling UI: " + KeybindToString(currentKeybind));
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
            int startIndex = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = rotation + 2 * Mathf.PI * i / segments;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(pos, color, Vector2.zero);
            }
            float thickness = 2f; // outline thickness
            for (int i = 0; i < segments; i++)
            {
                float angleA = rotation + 2 * Mathf.PI * i / segments;
                float angleB = rotation + 2 * Mathf.PI * (i + 1) / segments;
                DrawThickLine(vh,
                    center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius,
                    center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius,
                    thickness, color);
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
            float thickness = 2f;
            for (int i = 0; i < 4; i++)
                DrawThickLine(vh, corners[i], corners[(i + 1) % 4], thickness, color);
        }
    }

    void DrawTriangle(VertexHelper vh, Vector2 center, float size, Color color, bool filled, float rotation = 0f)
    {
        float h = size * Mathf.Sqrt(3) / 2;
        Vector2[] pts = new Vector2[3]
        {
            new Vector2(0, h),
            new Vector2(-size, -h / 2),
            new Vector2(size, -h / 2)
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
            float thickness = 2f;
            for (int i = 0; i < 3; i++)
                DrawThickLine(vh, pts[i], pts[(i + 1) % 3], thickness, color);
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

    private enum PreviewType { Frame, Hair, Dot }

    void ShowColorPreview(GameObject previewObj, UnityEngine.UI.Image previewImg, Color color, PreviewType type)
    {
        if (previewObj == null || previewImg == null) return;
        previewImg.color = new Color(color.r, color.g, color.b, 1f);
        previewObj.SetActive(true);
        // Start fade coroutine
        switch (type) {
            case PreviewType.Frame:
                if (framePreviewFadeCoroutine != null) StopCoroutine(framePreviewFadeCoroutine);
                framePreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewImg, PreviewType.Frame));
                break;
            case PreviewType.Hair:
                if (hairPreviewFadeCoroutine != null) StopCoroutine(hairPreviewFadeCoroutine);
                hairPreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewImg, PreviewType.Hair));
                break;
            case PreviewType.Dot:
                if (dotPreviewFadeCoroutine != null) StopCoroutine(dotPreviewFadeCoroutine);
                dotPreviewFadeCoroutine = StartCoroutine(FadeOutPreview(previewObj, previewImg, PreviewType.Dot));
                break;
        }
    }

    System.Collections.IEnumerator FadeOutPreview(GameObject previewObj, UnityEngine.UI.Image previewImg, PreviewType type)
    {
        yield return new WaitForSeconds(previewFadeDelay);
        float t = 0f;
        Color startColor = previewImg.color;
        while (t < previewFadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / previewFadeDuration);
            previewImg.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        previewObj.SetActive(false);
        // Reset alpha for next time
        previewImg.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        // Null out coroutine reference
        switch (type) {
            case PreviewType.Frame: framePreviewFadeCoroutine = null; break;
            case PreviewType.Hair: hairPreviewFadeCoroutine = null; break;
            case PreviewType.Dot: dotPreviewFadeCoroutine = null; break;
        }
    }

    // --- Color Picker Placeholder ---
    // (Removed old color picker logic)
}