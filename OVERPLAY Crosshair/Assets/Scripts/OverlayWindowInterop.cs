using System.Runtime.InteropServices;
using UnityEngine;

public static class OverlayWindowInterop
{
    [DllImport("OverlayWindow.dll", EntryPoint = "SetTransparent")]
    public static extern void SetTransparent(bool transparent);

    [DllImport("OverlayWindow.dll", EntryPoint = "SetClickthrough")]
    public static extern void SetClickthrough(bool clickthrough);
}
