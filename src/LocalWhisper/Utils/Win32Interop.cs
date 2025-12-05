using System.Runtime.InteropServices;

namespace LocalWhisper.Utils;

/// <summary>
/// P/Invoke wrappers for Win32 APIs.
/// </summary>
/// <remarks>
/// Provides low-level Windows API access for:
/// - Global hotkey registration (RegisterHotKey/UnregisterHotKey)
/// - Window message handling (WM_HOTKEY)
///
/// See: docs/iterations/iteration-01-hotkey-skeleton.md (HotkeyManager section)
/// </remarks>
public static class Win32Interop
{
    // Hotkey registration
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Window messages
    public const int WM_HOTKEY = 0x0312;

    // Modifier keys
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000; // Windows 7+ (prevents key-repeat on hold)

    // Virtual key codes (commonly used)
    public const uint VK_A = 0x41;
    public const uint VK_D = 0x44;
    public const uint VK_R = 0x52;
    public const uint VK_F12 = 0x7B;
    public const uint VK_CONTROL = 0x11;
    public const uint VK_SHIFT = 0x10;
    public const uint VK_MENU = 0x12; // Alt key

    // Low-level keyboard hook
    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Convert modifier string to Win32 modifier flags.
    /// </summary>
    /// <param name="modifier">Modifier string ("Ctrl", "Shift", "Alt", "Win")</param>
    /// <returns>Win32 modifier flag</returns>
    public static uint ModifierStringToFlags(string modifier)
    {
        return modifier switch
        {
            "Ctrl" => MOD_CONTROL,
            "Shift" => MOD_SHIFT,
            "Alt" => MOD_ALT,
            "Win" => MOD_WIN,
            _ => throw new ArgumentException($"Unknown modifier: {modifier}")
        };
    }

    /// <summary>
    /// Convert key string to virtual key code.
    /// </summary>
    /// <param name="key">Key string (e.g., "D", "F12", "A")</param>
    /// <returns>Virtual key code</returns>
    public static uint KeyStringToVirtualKey(string key)
    {
        // Single letter keys (A-Z)
        if (key.Length == 1 && char.IsLetter(key[0]))
        {
            return (uint)char.ToUpper(key[0]);
        }

        // Function keys (F1-F12)
        if (key.StartsWith("F") && int.TryParse(key.Substring(1), out int fKeyNum) && fKeyNum >= 1 && fKeyNum <= 12)
        {
            return (uint)(0x70 + fKeyNum - 1); // F1=0x70, F2=0x71, ..., F12=0x7B
        }

        // Number keys (0-9)
        if (key.Length == 1 && char.IsDigit(key[0]))
        {
            return (uint)key[0]; // '0'=0x30, '1'=0x31, ..., '9'=0x39
        }

        // Special keys
        return key switch
        {
            "Space" => 0x20,
            "Enter" => 0x0D,
            "Tab" => 0x09,
            "Escape" => 0x1B,
            _ => throw new ArgumentException($"Unknown key: {key}")
        };
    }

    /// <summary>
    /// Get last Win32 error code.
    /// </summary>
    public static int GetLastError()
    {
        return Marshal.GetLastWin32Error();
    }
}
