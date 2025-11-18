using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using LocalWhisper.Core;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// Wizard Step 3: Hotkey Configuration
/// </summary>
/// <remarks>
/// Implements US-042: Wizard Step 3 - Hotkey Selection
/// - Custom HotkeyTextBox control for capturing hotkey combinations
/// - Conflict detection via RegisterHotKey (error 1409 = already registered)
/// - Provides recommendations for good hotkey choices
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 7)
/// </remarks>
public partial class HotkeyStep : UserControl
{
    // Win32 API for hotkey conflict detection
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int TestHotkeyId = 9000; // Arbitrary ID for testing
    private ModifierKeys _hotkeyModifiers = ModifierKeys.None;
    private Key _hotkeyKey = Key.None;
    private bool _hasConflict = false;

    public event EventHandler? HotkeyChanged;

    public HotkeyStep()
    {
        InitializeComponent();

        // Set default hotkey (Ctrl+Shift+D)
        HotkeyInput.HotkeyModifiers = ModifierKeys.Control | ModifierKeys.Shift;
        HotkeyInput.HotkeyKey = Key.D;

        // Trigger initial validation
        Loaded += (s, e) => ValidateDefaultHotkey();
    }

    private void ValidateDefaultHotkey()
    {
        _hotkeyModifiers = HotkeyInput.HotkeyModifiers;
        _hotkeyKey = HotkeyInput.HotkeyKey;
        CheckForConflicts();
    }

    private void HotkeyInput_HotkeyChanged(object sender, EventArgs e)
    {
        _hotkeyModifiers = HotkeyInput.HotkeyModifiers;
        _hotkeyKey = HotkeyInput.HotkeyKey;

        CheckForConflicts();
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CheckForConflicts()
    {
        if (_hotkeyModifiers == ModifierKeys.None || _hotkeyKey == Key.None)
        {
            ConflictPanel.Visibility = Visibility.Collapsed;
            _hasConflict = false;
            return;
        }

        var window = Window.GetWindow(this);
        if (window == null)
        {
            // Window not loaded yet, skip conflict check
            return;
        }

        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            // Window handle not available yet
            return;
        }

        var fsModifiers = ConvertToWin32Modifiers(_hotkeyModifiers);
        var vk = KeyInterop.VirtualKeyFromKey(_hotkeyKey);

        // Attempt to register hotkey
        var registered = RegisterHotKey(windowHandle, TestHotkeyId, fsModifiers, (uint)vk);

        if (registered)
        {
            // Success - hotkey is available
            UnregisterHotKey(windowHandle, TestHotkeyId); // Immediately unregister
            ConflictPanel.Visibility = Visibility.Collapsed;
            _hasConflict = false;

            AppLogger.LogInformation("Hotkey available", new
            {
                Modifiers = _hotkeyModifiers.ToString(),
                Key = _hotkeyKey.ToString()
            });
        }
        else
        {
            // Failure - check if it's due to conflict
            var errorCode = Marshal.GetLastWin32Error();

            if (errorCode == 1409) // ERROR_HOTKEY_ALREADY_REGISTERED
            {
                ConflictPanel.Visibility = Visibility.Visible;
                ConflictStatus.Text = "⚠ Warnung: Diese Tastenkombination wird bereits von einer anderen Anwendung verwendet.";
                ConflictStatus.Foreground = System.Windows.Media.Brushes.Orange;
                _hasConflict = true;

                AppLogger.LogWarning("Hotkey conflict detected", new
                {
                    Modifiers = _hotkeyModifiers.ToString(),
                    Key = _hotkeyKey.ToString(),
                    ErrorCode = errorCode
                });
            }
            else
            {
                // Other error
                ConflictPanel.Visibility = Visibility.Visible;
                ConflictStatus.Text = $"⚠ Fehler bei Hotkey-Registrierung (Code: {errorCode})";
                ConflictStatus.Foreground = System.Windows.Media.Brushes.Red;
                _hasConflict = true;

                AppLogger.LogWarning("Hotkey registration error", new
                {
                    Modifiers = _hotkeyModifiers.ToString(),
                    Key = _hotkeyKey.ToString(),
                    ErrorCode = errorCode
                });
            }
        }
    }

    private uint ConvertToWin32Modifiers(ModifierKeys modifiers)
    {
        uint fsModifiers = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
            fsModifiers |= 0x0001; // MOD_ALT

        if (modifiers.HasFlag(ModifierKeys.Control))
            fsModifiers |= 0x0002; // MOD_CONTROL

        if (modifiers.HasFlag(ModifierKeys.Shift))
            fsModifiers |= 0x0004; // MOD_SHIFT

        if (modifiers.HasFlag(ModifierKeys.Windows))
            fsModifiers |= 0x0008; // MOD_WIN

        return fsModifiers;
    }

    public ModifierKeys GetHotkeyModifiers() => _hotkeyModifiers;

    public Key GetHotkeyKey() => _hotkeyKey;

    public bool HasConflict() => _hasConflict;

    public bool IsValid()
    {
        return _hotkeyModifiers != ModifierKeys.None &&
               _hotkeyKey != Key.None;
    }
}
