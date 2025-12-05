using System.Windows.Interop;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Utils;

namespace LocalWhisper.Services;

/// <summary>
/// Manages global hotkey registration and events.
/// </summary>
/// <remarks>
/// Registers hotkey via Win32 RegisterHotKey API.
/// Fires HotkeyPressed event when hotkey is detected.
///
/// Note: Win32 RegisterHotKey only fires on key DOWN, not UP.
/// For Iteration 1, we auto-transition through states on single press.
/// Iteration 2 will add proper key-up detection if needed.
///
/// See: US-001 (Hotkey Toggles State), US-003 (Hotkey Conflict)
/// See: docs/iterations/iteration-01-hotkey-skeleton.md
/// </remarks>
public class HotkeyManager : IDisposable
{
    private const int HOTKEY_ID = 1;
    private HwndSource? _hwndSource;
    private bool _isRegistered;
    private bool _disposed;
    private IntPtr _hookHandle = IntPtr.Zero;
    private Win32Interop.LowLevelKeyboardProc? _hookProc;
    private HotkeyConfig? _config;
    private bool _hotkeyCurrentlyPressed;

    /// <summary>
    /// Event fired when hotkey is pressed.
    /// </summary>
    public event EventHandler? HotkeyPressed;

    /// <summary>
    /// Event fired when hotkey is released.
    /// </summary>
    public event EventHandler? HotkeyReleased;

    /// <summary>
    /// Register global hotkey.
    /// </summary>
    /// <param name="windowHandle">Window handle for message handling</param>
    /// <param name="config">Hotkey configuration</param>
    /// <returns>True if registration succeeded, false if hotkey is already in use</returns>
    public bool RegisterHotkey(IntPtr windowHandle, HotkeyConfig config)
    {
        if (_isRegistered)
        {
            AppLogger.LogWarning("Hotkey already registered, unregistering first");
            UnregisterHotkey();
        }

        _config = config;

        // Convert config to Win32 flags
        uint modifiers = 0;
        foreach (var modifier in config.Modifiers)
        {
            modifiers |= Win32Interop.ModifierStringToFlags(modifier);
        }

        // Add MOD_NOREPEAT to prevent key-repeat events (Windows 7+)
        modifiers |= Win32Interop.MOD_NOREPEAT;

        uint virtualKey = Win32Interop.KeyStringToVirtualKey(config.Key);

        // Attempt registration
        bool success = Win32Interop.RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, virtualKey);

        if (!success)
        {
            var errorCode = Win32Interop.GetLastError();
            AppLogger.LogWarning("Hotkey registration failed", new
            {
                Modifiers = string.Join("+", config.Modifiers),
                Key = config.Key,
                ErrorCode = errorCode,
                Reason = errorCode == 1409 ? "Hotkey already in use" : "Unknown error"
            });
            return false;
        }

        // Hook into window message processing
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource.AddHook(WndProc);

        // Install low-level keyboard hook for detecting key releases
        _hookProc = KeyboardHookCallback;
        _hookHandle = Win32Interop.SetWindowsHookEx(
            Win32Interop.WH_KEYBOARD_LL,
            _hookProc,
            Win32Interop.GetModuleHandle(null),
            0);

        if (_hookHandle == IntPtr.Zero)
        {
            AppLogger.LogWarning("Failed to install keyboard hook for key-up detection");
        }

        _isRegistered = true;

        AppLogger.LogInformation("Hotkey registered successfully", new
        {
            Modifiers = string.Join("+", config.Modifiers),
            Key = config.Key
        });

        return true;
    }

    /// <summary>
    /// Unregister hotkey.
    /// </summary>
    public void UnregisterHotkey()
    {
        if (!_isRegistered || _hwndSource == null)
        {
            return;
        }

        _hwndSource.RemoveHook(WndProc);
        Win32Interop.UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);

        // Unhook keyboard hook
        if (_hookHandle != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _isRegistered = false;

        AppLogger.LogInformation("Hotkey unregistered");
    }

    /// <summary>
    /// Window procedure to handle WM_HOTKEY messages.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            AppLogger.LogDebug("Hotkey pressed (WM_HOTKEY received)");

            _hotkeyCurrentlyPressed = true;

            // Fire event
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Low-level keyboard hook callback to detect key releases.
    /// </summary>
    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _hotkeyCurrentlyPressed && _config != null)
        {
            int wParamInt = wParam.ToInt32();

            // Check for key-up events
            if (wParamInt == Win32Interop.WM_KEYUP || wParamInt == Win32Interop.WM_SYSKEYUP)
            {
                var hookStruct = Marshal.PtrToStructure<Win32Interop.KBDLLHOOKSTRUCT>(lParam);
                uint vkCode = hookStruct.vkCode;

                // Get the virtual key code for our configured key
                uint configuredKey = Win32Interop.KeyStringToVirtualKey(_config.Key);

                // Check if the released key is our configured key
                if (vkCode == configuredKey)
                {
                    // Check if modifiers are still held (they should be released too, or we just released the main key)
                    bool ctrlPressed = (Win32Interop.GetAsyncKeyState((int)Win32Interop.VK_CONTROL) & 0x8000) != 0;
                    bool shiftPressed = (Win32Interop.GetAsyncKeyState((int)Win32Interop.VK_SHIFT) & 0x8000) != 0;
                    bool altPressed = (Win32Interop.GetAsyncKeyState((int)Win32Interop.VK_MENU) & 0x8000) != 0;

                    bool ctrlRequired = _config.Modifiers.Contains("Ctrl");
                    bool shiftRequired = _config.Modifiers.Contains("Shift");
                    bool altRequired = _config.Modifiers.Contains("Alt");

                    // Fire release event if any required modifier is released OR if main key is released
                    // (This handles the case where user releases keys in different order)
                    if (!ctrlPressed && ctrlRequired ||
                        !shiftPressed && shiftRequired ||
                        !altPressed && altRequired ||
                        vkCode == configuredKey)
                    {
                        AppLogger.LogDebug("Hotkey released");
                        _hotkeyCurrentlyPressed = false;

                        // Fire event
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        return Win32Interop.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterHotkey();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
