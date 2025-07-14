using UnityEngine;

public class CrosshairRenderer : MonoBehaviour
{
    public Color crosshairColor = Color.green;
    public float length = 20f;
    public float thickness = 2f;

    void OnGUI()
    {
        Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
        Color oldColor = GUI.color;
        GUI.color = crosshairColor;

        // Horizontal line
        GUI.DrawTexture(new Rect(center.x - length, center.y - thickness / 2, length * 2, thickness), Texture2D.whiteTexture);
        // Vertical line
        GUI.DrawTexture(new Rect(center.x - thickness / 2, center.y - length, thickness, length * 2), Texture2D.whiteTexture);

        GUI.color = oldColor;
    }
}
