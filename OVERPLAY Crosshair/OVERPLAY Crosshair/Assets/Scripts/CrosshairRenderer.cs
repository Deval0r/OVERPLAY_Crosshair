using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Button frameColorButton;

    [Header("Hairs UI")]
    public TMP_Dropdown hairStyleDropdown;
    public Slider hairCountSlider;
    public Slider customAngleSlider;
    public Slider hairThicknessSlider;
    public Slider hairLengthSlider;
    public Slider hairsRotationSlider;
    public Button hairColorButton;
    public Slider hairOpacitySlider;
    public Toggle hairsExtendPastFrameToggle;
    public Slider hairDistanceSlider;

    [Header("Dot UI")]
    public TMP_Dropdown dotShapeDropdown;
    public Toggle dotFilledToggle;
    public Button dotColorButton;
    public Slider dotOpacitySlider;
    public Slider dotScaleSlider;
    public Slider dotRotationSlider;

    [Header("Snapping")]
    public Toggle snapRotationToggle;
    private bool SnapEnabled => snapRotationToggle != null && snapRotationToggle.isOn;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        DrawFrame(vh);
        DrawHairs(vh);
        DrawDot(vh);
    }

    void Start()
    {
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
        if (frameColorButton) frameColorButton.onClick.AddListener(() => PickColor(c => { frameColor = c; SetVerticesDirty(); }));

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
        if (hairColorButton) hairColorButton.onClick.AddListener(() => PickColor(c => { hairColor = c; SetVerticesDirty(); }));
        if (hairOpacitySlider) hairOpacitySlider.onValueChanged.AddListener(val => { hairOpacity = val; SetVerticesDirty(); });
        if (hairsExtendPastFrameToggle) hairsExtendPastFrameToggle.onValueChanged.AddListener(val => { SetVerticesDirty(); });
        if (hairDistanceSlider) hairDistanceSlider.onValueChanged.AddListener(val => { SetVerticesDirty(); });

        // --- Dot UI ---
        if (dotShapeDropdown) dotShapeDropdown.onValueChanged.AddListener(val => { dotShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (dotFilledToggle) dotFilledToggle.onValueChanged.AddListener(val => { dotFilled = val; SetVerticesDirty(); });
        if (dotColorButton) dotColorButton.onClick.AddListener(() => PickColor(c => { dotColor = c; SetVerticesDirty(); }));
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

        UpdateHairUI();
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

    // --- Color Picker Placeholder ---
    public Slider colorPickerSlider;
    public Image colorPreviewImage;
    private System.Action<Color> colorPickerCallback;

    void PickColor(System.Action<Color> onColorPicked)
    {
        colorPickerCallback = onColorPicked;
        colorPickerSlider.gameObject.SetActive(true);
        colorPreviewImage.gameObject.SetActive(true);

        // Optionally, set the slider to the current color's hue
        colorPickerSlider.value = 0; // or set based on current color

        colorPickerSlider.onValueChanged.RemoveAllListeners();
        colorPickerSlider.onValueChanged.AddListener(OnColorSliderChanged);
    }

    void OnColorSliderChanged(float value)
    {
        // Map slider value [0,1] to a color (e.g., hue)
        Color color = Color.HSVToRGB(value, 1f, 1f);
        colorPreviewImage.color = color;
        colorPickerCallback?.Invoke(color);
    }

    public void OnColorSliderReleased()
    {
        colorPickerSlider.gameObject.SetActive(false);
        colorPreviewImage.gameObject.SetActive(false);
    }
}