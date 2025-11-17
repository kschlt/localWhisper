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

    /// <summary>
    /// Event fired when hotkey is pressed.
    /// </summary>
    public event EventHandler? HotkeyPressed;

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

            // Fire event
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
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
