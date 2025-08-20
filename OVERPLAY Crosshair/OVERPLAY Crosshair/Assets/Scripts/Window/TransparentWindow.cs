using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class TransparentWindow : MonoBehaviour
{
	public static TransparentWindow Main = null;
	public static Camera Camera = null;	//Used instead of Camera.main

	[Tooltip("What GameObject layers should trigger window focus when the mouse passes over objects?")] //
	[SerializeField] LayerMask clickLayerMask = ~0;

	[Tooltip("Allows Input to be detected even when focus is lost")] //
	[SerializeField] bool useSystemInput = false;

	[Tooltip("Should the window be fullscreen?")] //
	[SerializeField] bool fullscreen = true;

	[Tooltip("Force the window to match ScreenResolution")] //
	[SerializeField] bool customResolution = true;

	[Tooltip("Resolution the overlay should run at")] //
	[SerializeField] Vector2Int screenResolution = new Vector2Int(1280, 720);

	[Tooltip("The framerate the overlay should try to run at")] //
	[SerializeField] int targetFrameRate = 30;



	[Tooltip("Minimum distance from mouse to object to trigger focus (prevents accidental triggers)")] //
	[SerializeField] float minFocusDistance = 0.1f;

	
	/////////////////////
	//Windows DLL stuff//
	/////////////////////
	
	[DllImport("user32.dll")]
	static extern IntPtr GetActiveWindow();
	
	[DllImport("user32.dll")]
	static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
	static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

	[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
	static extern bool GetWindowRect(IntPtr hwnd, out Rectangle rect);
	
	[DllImport("user32.dll")]
	static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

	[DllImportAttribute("user32.dll")]
	static extern bool ReleaseCapture();

	[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

	[DllImport("Dwmapi.dll")]
	static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Rectangle margins);

    [DllImport("VirtualDesktopAccessor.dll")]
    public static extern void PinWindow(System.IntPtr hwnd);

	const int GWL_STYLE = -16;
	const uint WS_POPUP = 0x80000000;
	const uint WS_VISIBLE = 0x10000000;
	const int HWND_TOPMOST = -1;

	const int WM_SYSCOMMAND = 0x112;
	const int WM_MOUSE_MOVE = 0xF012;

	int fWidth;
	int fHeight;
	IntPtr hwnd = IntPtr.Zero;
	Rectangle margins;
	Rectangle windowRect;

	// Fixed: Using Awake() instead of Start() to ensure proper initialization order
	void Awake()
	{
		Main = this;

		Camera = GetComponent<Camera>();
		// FIXED: Set background color to transparent instead of black to prevent brightness flickering
		Camera.backgroundColor = new Color(0, 0, 0, 0);
		Camera.clearFlags = CameraClearFlags.SolidColor;

		// Always match the current screen resolution, and add a small buffer to cover edges
		screenResolution = new Vector2Int(Screen.currentResolution.width + 2, Screen.currentResolution.height + 2);
		Screen.SetResolution(screenResolution.x, screenResolution.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

		// FIXED: Remove conflicting frame rate setting that was causing brightness flickering
		// Application.targetFrameRate = targetFrameRate; // REMOVED: This conflicts with CrosshairRenderer's setting
		Application.runInBackground = true;

#if !UNITY_EDITOR
		fWidth = screenResolution.x;
		fHeight = screenResolution.y;
		margins = new Rectangle() {Left = -1};
		hwnd = GetActiveWindow();

		GetWindowRect(hwnd, out windowRect);

		SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
		SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Pin the overlay window to all virtual desktops
        try {
            PinWindow(hwnd);

        } catch (System.Exception ex) {

        }
#endif
	}

	void Update()
	{
        // Update global key states for keyboard
        if (useSystemInput)
        {
            SystemInput.UpdateKeys();
        }
		if (useSystemInput)
		{
			SystemInput.Process();
		}

		SetClickThrough();

        // Late update key states
        if (useSystemInput)
        {
            SystemInput.LateUpdateKeys();
        }
	}

	// FIXED: Add coroutine to debounce resolution changes
	private System.Collections.IEnumerator UpdateResolutionWithDelay(Vector2Int newResolution)
	{
		// Wait a short time to see if resolution stabilizes
		yield return new WaitForSeconds(0.5f); // FIXED: Increased delay to prevent rapid changes
		
		// Check if resolution is still different after the delay
		Vector2Int currentTargetRes = new Vector2Int(Screen.currentResolution.width + 2, Screen.currentResolution.height + 2);
		if (currentTargetRes == newResolution && screenResolution != newResolution)
		{
			screenResolution = newResolution;
			Screen.SetResolution(screenResolution.x, screenResolution.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
		}
	}

	// FIXED: Ensure camera background color stays transparent to prevent brightness flickering
	void LateUpdate()
	{
		// Ensure camera background color remains transparent
		if (Camera != null && Camera.backgroundColor.a != 0f)
		{
			Camera.backgroundColor = new Color(0, 0, 0, 0);
		}
		
		// FIXED: Prevent frequent resolution checks that cause brightness flickering
		// Only check resolution changes every few frames instead of every frame
		if (Time.frameCount % 30 == 0) // Check every 30 frames (~0.5 seconds at 60fps)
		{
			Vector2Int targetRes = new Vector2Int(Screen.currentResolution.width + 2, Screen.currentResolution.height + 2);
			if (screenResolution != targetRes)
			{
				// Add a small delay to prevent rapid resolution changes
				StartCoroutine(UpdateResolutionWithDelay(targetRes));
			}
		}
	}

	//Returns true if the cursor is over a UI element or 2D physics object
	bool FocusForInput()
	{
		int edgeMargin = 2; // pixels
		Vector2 mousePos = Input.mousePosition;
		if (mousePos.x <= edgeMargin || mousePos.x >= Screen.width - edgeMargin ||
			mousePos.y <= edgeMargin || mousePos.y >= Screen.height - edgeMargin)
		{
			// Mouse is at the edge, do not allow focus/clicks
			return false;
		}
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem && eventSystem.IsPointerOverGameObject())
		{
			return true;
		}

		Vector2 pos = Camera.ScreenToWorldPoint(Input.mousePosition);
		
		// Use OverlapCircle instead of OverlapPoint for more precise detection
		var overlapCollider = Physics2D.OverlapCircle(pos, minFocusDistance, clickLayerMask);
		if (overlapCollider)
		{
			return true;
		}

		return false;
	}

	void SetClickThrough()
	{
		// If the UI is hidden, always enable clickthrough (non-interactive overlay)
		var crosshairRenderer = FindObjectsOfType<CrosshairRenderer>().FirstOrDefault();
		if (crosshairRenderer != null && !crosshairRenderer.uiVisible)
		{
#if !UNITY_EDITOR
			SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
			SetWindowLong(hwnd, -20, (uint)524288 | (uint)32);
			SetLayeredWindowAttributes(hwnd, 0, 255, 2);
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
#endif
			return;
		}

		var focusWindow = FocusForInput();

		//Get window position
		GetWindowRect(hwnd, out windowRect);

#if !UNITY_EDITOR
		if (focusWindow)
		{
			SetWindowLong (hwnd, -20, ~(((uint)524288) | ((uint)32)));
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		}
		else
		{
			SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
			SetWindowLong (hwnd, -20, (uint)524288 | (uint)32);
			SetLayeredWindowAttributes (hwnd, 0, 255, 2);
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		}
#endif
	}

	public static void DragWindow()
	{
#if !UNITY_EDITOR
		if (Screen.fullScreenMode != FullScreenMode.Windowed)
		{
			return;
		}
		ReleaseCapture ();
		SendMessage(Main.hwnd, WM_SYSCOMMAND, WM_MOUSE_MOVE, 0);
		Input.ResetInputAxes();
#endif		
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rectangle
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
}