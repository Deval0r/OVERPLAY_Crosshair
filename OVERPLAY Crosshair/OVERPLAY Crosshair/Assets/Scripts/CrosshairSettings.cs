using UnityEngine;

public enum CrosshairShape { Circle, Square, Triangle }
public enum HairStyle { Even, Custom }

public class CrosshairSettings : MonoBehaviour
{
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
}
