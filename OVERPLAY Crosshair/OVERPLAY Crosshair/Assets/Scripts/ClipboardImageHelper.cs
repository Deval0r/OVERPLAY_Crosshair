// ClipboardImageHelper.cs
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using UnityEngine;

public static class ClipboardImageHelper
{
    public static void CopyImageToClipboard(Texture2D tex)
    {
        if (tex == null) return;
        try
        {
            byte[] pngData = tex.EncodeToPNG();
            using (var ms = new MemoryStream(pngData))
            {
                using (var bmp = new Bitmap(ms))
                {
                    Clipboard.SetImage(bmp);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to copy image to clipboard: " + ex.Message);
        }
    }
}
#endif 