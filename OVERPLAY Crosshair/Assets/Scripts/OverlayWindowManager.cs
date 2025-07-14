using UnityEngine;

public class OverlayWindowManager : MonoBehaviour
{
    public GameObject settingsPanel;

    void Start()
    {
        OverlayWindowInterop.SetTransparent(true);
        SetClickthrough(true);
    }

    public void SetClickthrough(bool clickthrough)
    {
        OverlayWindowInterop.SetClickthrough(clickthrough);
    }

    void Update()
    {
        // If settings panel is visible, disable clickthrough
        SetClickthrough(!settingsPanel.activeSelf);
    }
}