using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class CrosshairRenderer : Graphic
{
    public CrosshairSettings settings;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (settings == null) return;

        // Draw frame (circle/square/triangle)
        DrawFrame(vh);

        // Draw hairs
        DrawHairs(vh);

        // Draw dot
        DrawDot(vh);
    }

    void DrawFrame(VertexHelper vh)
    {
        Color frameCol = settings.frameColor;
        frameCol.a *= settings.frameOpacity;
        float size = rectTransform.rect.width * 0.5f * settings.frameScale;
        Vector2 center = rectTransform.rect.center;

        switch (settings.frameShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, frameCol, settings.frameFilled, 64);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, frameCol, settings.frameFilled);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, frameCol, settings.frameFilled);
                break;
        }
    }

    void DrawHairs(VertexHelper vh)
    {
        Color hairCol = settings.hairColor;
        hairCol.a *= settings.hairOpacity;
        float size = rectTransform.rect.width * 0.5f * settings.frameScale;
        Vector2 center = rectTransform.rect.center;
        int count = Mathf.Max(1, settings.hairCount);
        float angleStep = 360f / count;
        float thickness = settings.hairThickness;
        float length = settings.hairLength * settings.frameScale;

        if (settings.hairStyle == HairStyle.Custom)
        {
            angleStep = settings.customAngle;
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
        Color dotCol = settings.dotColor;
        dotCol.a *= settings.dotOpacity;
        float size = rectTransform.rect.width * 0.08f * settings.dotScale;
        Vector2 center = rectTransform.rect.center;

        switch (settings.dotShape)
        {
            case CrosshairShape.Circle:
                DrawCircle(vh, center, size, dotCol, settings.dotFilled, 32);
                break;
            case CrosshairShape.Square:
                DrawSquare(vh, center, size, dotCol, settings.dotFilled);
                break;
            case CrosshairShape.Triangle:
                DrawTriangle(vh, center, size, dotCol, settings.dotFilled);
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
}