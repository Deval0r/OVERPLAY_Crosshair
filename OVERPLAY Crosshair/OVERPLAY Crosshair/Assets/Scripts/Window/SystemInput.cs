using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SystemInput
{
	//TODO: Add Keyboard input (see bottom of script)
	
	//Keys
	const int VK_LBUTTON = 0x01; //Left Mouse Button
	const int VK_RBUTTON = 0x02; //Right Mouse Button
	const int VK_MBUTTON = 0x02; //Middle Mouse Button (Mouse wheel button)
	const int SM_SWAPBUTTON = 23; //0 = default, non-zero = LMB/RMB swapped

	//Key states
	const int BUTTONDOWNFRAME = -32767;
	const int BUTTONDOWN = -32768;
	const int BUTTONUP = 0; //Not sure if there's a specific buttonUp

	[DllImport("user32.dll", EntryPoint = "SetCursorPos")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out Vector2Int lpMousePoint);	//Cursor coordinates start top-left, rather than Unity's bottom-left, so y axis will need to be modified

	[DllImport("user32.dll")]
	public static extern short GetAsyncKeyState(int virtualKeyCode);

	[DllImport("user32.dll")]
	public static extern short GetSystemMetrics(int metricsCode);

	//TODO: Work out a way to handle generic key states, so we don't need multiple bools for each key 
	static bool mouseButton0Down = false;
	static bool mouseButton1Down = false;
	static bool lastMouseButton0Down = false;
	static bool lastMouseButton1Down = false;
	static bool hasPressedButton0 = false;
	static bool hasPressedButton1 = false;

	/// <summary>
	///   <para>Returns whether the given mouse button is held down.</para>
	/// </summary>
	/// <param name="button"></param>
	public static bool GetMouseButton(int button = 0)
	{
		return (button == 0) ? hasPressedButton0 : hasPressedButton1;
	}

	/// <summary>
	///   <para>Returns true during the frame the user pressed the given mouse button.</para>
	/// </summary>
	/// <param name="button"></param>
	public static bool GetMouseButtonDown(int button = 0)
	{
		return (button == 0) ? mouseButton0Down : mouseButton1Down;
	}

	/// <summary>
	///   <para>Returns true during the frame the user releases the given mouse button.</para>
	/// </summary>
	/// <param name="button"></param>
	public static bool GetMouseButtonUp(int button = 0)
	{
		return (button == 0) ? (!hasPressedButton0 && lastMouseButton0Down) : (!hasPressedButton1 && lastMouseButton1Down);
	}
	
	public static Vector2Int GetCursorPosition()
	{
		GetCursorPos(out var point);
		return point;
	}

	public static void SetCursorPosition(Vector2Int point)
	{
		SetCursorPos(point.x, point.y);
	}

	public static void Process()
	{
		CheckMouseButtons();
	}

	static void CheckMouseButtons()
	{
		lastMouseButton0Down = hasPressedButton0;
		lastMouseButton1Down = hasPressedButton1;
		mouseButton0Down = false;
		mouseButton1Down = false;

		var mbp0 = MouseButtonPressed(0);
		var mbp1 = MouseButtonPressed(1);

		//Check MouseButton0
		if (!hasPressedButton0 && mbp0)
		{
			hasPressedButton0 = true;
			mouseButton0Down = true;
		}
		else if (hasPressedButton0 && !mbp0)
		{
			hasPressedButton0 = false;
		}

		//Check MouseButton1
		if (!hasPressedButton1 && mbp1)
		{
			hasPressedButton1 = true;
			mouseButton1Down = true;
		}
		else if (hasPressedButton1 && !mbp1)
		{
			hasPressedButton1 = false;
		}
	}

	static bool MouseButtonPressed(int button)
	{
		bool state = false;
		bool swapped = GetSystemMetrics(SM_SWAPBUTTON) > 0;
		switch (button)
		{
			case 0:
				state = GetAsyncKeyState(swapped ? VK_RBUTTON : VK_LBUTTON) == BUTTONDOWN;
				break;
			case 1:
				state = GetAsyncKeyState(swapped ? VK_LBUTTON : VK_RBUTTON) == BUTTONDOWN;
				break;
			case 2:
				state = GetAsyncKeyState(VK_MBUTTON) == BUTTONDOWN;
				break;
			default:
				return false;
		}

		return state;
	}

	//TODO: Keyboard Input stuff
    // Map Unity KeyCode to Virtual-Key codes (partial, extend as needed)
    public static Dictionary<KeyCode, int> VK_KeyCodes = new Dictionary<KeyCode, int>()
    {
        // Letters
        {KeyCode.A, 0x41}, {KeyCode.B, 0x42}, {KeyCode.C, 0x43}, {KeyCode.D, 0x44}, {KeyCode.E, 0x45}, {KeyCode.F, 0x46}, {KeyCode.G, 0x47}, {KeyCode.H, 0x48}, {KeyCode.I, 0x49}, {KeyCode.J, 0x4A}, {KeyCode.K, 0x4B}, {KeyCode.L, 0x4C}, {KeyCode.M, 0x4D}, {KeyCode.N, 0x4E}, {KeyCode.O, 0x4F}, {KeyCode.P, 0x50}, {KeyCode.Q, 0x51}, {KeyCode.R, 0x52}, {KeyCode.S, 0x53}, {KeyCode.T, 0x54}, {KeyCode.U, 0x55}, {KeyCode.V, 0x56}, {KeyCode.W, 0x57}, {KeyCode.X, 0x58}, {KeyCode.Y, 0x59}, {KeyCode.Z, 0x5A},
        // Numbers
        {KeyCode.Alpha0, 0x30}, {KeyCode.Alpha1, 0x31}, {KeyCode.Alpha2, 0x32}, {KeyCode.Alpha3, 0x33}, {KeyCode.Alpha4, 0x34}, {KeyCode.Alpha5, 0x35}, {KeyCode.Alpha6, 0x36}, {KeyCode.Alpha7, 0x37}, {KeyCode.Alpha8, 0x38}, {KeyCode.Alpha9, 0x39},
        // Function keys
        {KeyCode.F1, 0x70}, {KeyCode.F2, 0x71}, {KeyCode.F3, 0x72}, {KeyCode.F4, 0x73}, {KeyCode.F5, 0x74}, {KeyCode.F6, 0x75}, {KeyCode.F7, 0x76}, {KeyCode.F8, 0x77}, {KeyCode.F9, 0x78}, {KeyCode.F10, 0x79}, {KeyCode.F11, 0x7A}, {KeyCode.F12, 0x7B},
        // Modifiers
        {KeyCode.LeftShift, 0xA0}, {KeyCode.RightShift, 0xA1}, {KeyCode.LeftControl, 0xA2}, {KeyCode.RightControl, 0xA3}, {KeyCode.LeftAlt, 0xA4}, {KeyCode.RightAlt, 0xA5},
        // Space, Enter, Escape, Tab
        {KeyCode.Space, 0x20}, {KeyCode.Return, 0x0D}, {KeyCode.Escape, 0x1B}, {KeyCode.Tab, 0x09},
        // Arrow keys
        {KeyCode.UpArrow, 0x26}, {KeyCode.DownArrow, 0x28}, {KeyCode.LeftArrow, 0x25}, {KeyCode.RightArrow, 0x27},
        // Keypad
        {KeyCode.Keypad0, 0x60}, {KeyCode.Keypad1, 0x61}, {KeyCode.Keypad2, 0x62}, {KeyCode.Keypad3, 0x63}, {KeyCode.Keypad4, 0x64}, {KeyCode.Keypad5, 0x65}, {KeyCode.Keypad6, 0x66}, {KeyCode.Keypad7, 0x67}, {KeyCode.Keypad8, 0x68}, {KeyCode.Keypad9, 0x69},
        // Others (add more as needed)
    };

    static Dictionary<KeyCode, bool> lastKeyState = new Dictionary<KeyCode, bool>();
    static Dictionary<KeyCode, bool> currentKeyState = new Dictionary<KeyCode, bool>();

    // Call this every frame (from TransparentWindow.Update or similar)
    public static void UpdateKeys()
    {
        foreach (var pair in VK_KeyCodes)
        {
            bool isDown = (GetAsyncKeyState(pair.Value) & 0x8000) != 0;
            currentKeyState[pair.Key] = isDown;
        }
    }

    public static bool GetKey(KeyCode key)
    {
        if (currentKeyState.TryGetValue(key, out var isDown))
            return isDown;
        return false;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        bool last = false, now = false;
        lastKeyState.TryGetValue(key, out last);
        currentKeyState.TryGetValue(key, out now);
        return now && !last;
    }

    public static bool GetKeyUp(KeyCode key)
    {
        bool last = false, now = false;
        lastKeyState.TryGetValue(key, out last);
        currentKeyState.TryGetValue(key, out now);
        return !now && last;
    }

    // Call this at the end of each frame
    public static void LateUpdateKeys()
    {
        foreach (var pair in currentKeyState)
        {
            lastKeyState[pair.Key] = pair.Value;
        }
    }
}