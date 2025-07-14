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

    // Hairs
    public HairStyle hairStyle = HairStyle.Even;
    public int hairCount = 4;
    public float customAngle = 90f;
    public float hairThickness = 2f;
    public float hairLength = 20f;
    public Color hairColor = Color.white;
    [Range(0, 1)] public float hairOpacity = 1f;

    // Dot
    public CrosshairShape dotShape = CrosshairShape.Circle;
    public bool dotFilled = true;
    public Color dotColor = Color.white;
    [Range(0, 1)] public float dotOpacity = 1f;
    public float dotScale = 1f;

    // --- UI References ---
    [Header("Frame UI")]
    public TMP_Dropdown frameShapeDropdown;
    public Toggle frameFilledToggle;
    public Slider frameOpacitySlider;
    public Slider frameScaleSlider;
    public Button frameColorButton;

    [Header("Hairs UI")]
    public TMP_Dropdown hairStyleDropdown;
    public Slider hairCountSlider;
    public Slider customAngleSlider;
    public Slider hairThicknessSlider;
    public Slider hairLengthSlider;
    public Button hairColorButton;
    public Slider hairOpacitySlider;

    [Header("Dot UI")]
    public TMP_Dropdown dotShapeDropdown;
    public Toggle dotFilledToggle;
    public Button dotColorButton;
    public Slider dotOpacitySlider;
    public Slider dotScaleSlider;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        // Draw frame
        DrawFrame(vh);
        // Draw hairs
        DrawHairs(vh);
        // Draw dot
        DrawDot(vh);
    }

    void Start()
    {
        // --- Frame UI ---
        if (frameShapeDropdown) frameShapeDropdown.onValueChanged.AddListener(val => { frameShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (frameFilledToggle) frameFilledToggle.onValueChanged.AddListener(val => { frameFilled = val; SetVerticesDirty(); });
        if (frameOpacitySlider) frameOpacitySlider.onValueChanged.AddListener(val => { frameOpacity = val; SetVerticesDirty(); });
        if (frameScaleSlider) frameScaleSlider.onValueChanged.AddListener(val => { frameScale = val; SetVerticesDirty(); });
        if (frameColorButton) frameColorButton.onClick.AddListener(() => PickColor(c => { frameColor = c; SetVerticesDirty(); }));

        // --- Hairs UI ---
        if (hairStyleDropdown) hairStyleDropdown.onValueChanged.AddListener(val => { hairStyle = (HairStyle)val; SetVerticesDirty(); UpdateHairUI(); });
        if (hairCountSlider) hairCountSlider.onValueChanged.AddListener(val => { hairCount = Mathf.RoundToInt(val); SetVerticesDirty(); });
        if (customAngleSlider) customAngleSlider.onValueChanged.AddListener(val => { customAngle = val; SetVerticesDirty(); });
        if (hairThicknessSlider) hairThicknessSlider.onValueChanged.AddListener(val => { hairThickness = val; SetVerticesDirty(); });
        if (hairLengthSlider) hairLengthSlider.onValueChanged.AddListener(val => { hairLength = val; SetVerticesDirty(); });
        if (hairColorButton) hairColorButton.onClick.AddListener(() => PickColor(c => { hairColor = c; SetVerticesDirty(); }));
        if (hairOpacitySlider) hairOpacitySlider.onValueChanged.AddListener(val => { hairOpacity = val; SetVerticesDirty(); });

        // --- Dot UI ---
        if (dotShapeDropdown) dotShapeDropdown.onValueChanged.AddListener(val => { dotShape = (CrosshairShape)val; SetVerticesDirty(); });
        if (dotFilledToggle) dotFilledToggle.onValueChanged.AddListener(val => { dotFilled = val; SetVerticesDirty(); });
        if (dotColorButton) dotColorButton.onClick.AddListener(() => PickColor(c => { dotColor = c; SetVerticesDirty(); }));
        if (dotOpacitySlider) dotOpacitySlider.onValueChanged.AddListener(val => { dotOpacity = val; SetVerticesDirty(); });
        if (dotScaleSlider) dotScaleSlider.onValueChanged.AddListener(val => { dotScale = val; SetVerticesDirty(); });

        UpdateHairUI();
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
        switch (frameShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, frameCol, frameFilled, 64);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, frameCol, frameFilled);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, frameCol, frameFilled);
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
        if (hairStyle == HairStyle.Custom)
        {
            angleStep = customAngle;
            count = Mathf.Max(1, Mathf.FloorToInt(360f / angleStep));
        }
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 start = center + dir * (size * 0.2f);
            Vector2 end = center + dir * (size * 0.9f);
            DrawThickLine(vh, start, end, thickness, hairCol);
        }
    }

    void DrawDot(VertexHelper vh)
    {
        Color dotCol = dotColor;
        dotCol.a *= dotOpacity;
        float size = rectTransform.rect.width * 0.08f * dotScale;
        Vector2 center = rectTransform.rect.center;
        switch (dotShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, dotCol, dotFilled, 32);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, dotCol, dotFilled);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, dotCol, dotFilled);
                break;
        }
    }

    // --- Helper Methods ---
    void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, bool filled, int segments)
    {
        if (filled)
        {
            int startIndex = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
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
                float angle = 2 * Mathf.PI * i / segments;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(pos, color, Vector2.zero);
            }
            float thickness = 2f; // outline thickness
            for (int i = 0; i < segments; i++)
            {
                DrawThickLine(vh,
                    center + new Vector2(Mathf.Cos(2 * Mathf.PI * i / segments), Mathf.Sin(2 * Mathf.PI * i / segments)) * radius,
                    center + new Vector2(Mathf.Cos(2 * Mathf.PI * (i + 1) / segments), Mathf.Sin(2 * Mathf.PI * (i + 1) / segments)) * radius,
                    thickness, color);
            }
        }
    }

    void DrawSquare(VertexHelper vh, Vector2 center, float size, Color color, bool filled)
    {
        Vector2 half = Vector2.one * size;
        Vector2 tl = center + new Vector2(-half.x, half.y);
        Vector2 tr = center + new Vector2(half.x, half.y);
        Vector2 br = center + new Vector2(half.x, -half.y);
        Vector2 bl = center + new Vector2(-half.x, -half.y);
        if (filled)
        {
            int start = vh.currentVertCount;
            vh.AddVert(tl, color, Vector2.zero);
            vh.AddVert(tr, color, Vector2.zero);
            vh.AddVert(br, color, Vector2.zero);
            vh.AddVert(bl, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
        else
        {
            float thickness = 2f;
            DrawThickLine(vh, tl, tr, thickness, color);
            DrawThickLine(vh, tr, br, thickness, color);
            DrawThickLine(vh, br, bl, thickness, color);
            DrawThickLine(vh, bl, tl, thickness, color);
        }
    }

    void DrawTriangle(VertexHelper vh, Vector2 center, float size, Color color, bool filled)
    {
        float h = size * Mathf.Sqrt(3) / 2;
        Vector2 p1 = center + new Vector2(0, h);
        Vector2 p2 = center + new Vector2(-size, -h / 2);
        Vector2 p3 = center + new Vector2(size, -h / 2);
        if (filled)
        {
            int start = vh.currentVertCount;
            vh.AddVert(p1, color, Vector2.zero);
            vh.AddVert(p2, color, Vector2.zero);
            vh.AddVert(p3, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
        else
        {
            float thickness = 2f;
            DrawThickLine(vh, p1, p2, thickness, color);
            DrawThickLine(vh, p2, p3, thickness, color);
            DrawThickLine(vh, p3, p1, thickness, color);
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
    void PickColor(System.Action<Color> onColorPicked)
    {
        // Replace with your color picker dialog
        onColorPicked?.Invoke(Color.white);
    }
}