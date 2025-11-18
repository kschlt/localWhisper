using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LocalWhisper.UI.Controls;

/// <summary>
/// Custom TextBox for capturing hotkey combinations.
/// </summary>
/// <remarks>
/// Implements US-042: Wizard Step 3 - Hotkey Configuration
/// - Captures keyboard input via PreviewKeyDown
/// - Requires at least one modifier (Ctrl/Shift/Alt/Win)
/// - Formats hotkey as "Ctrl+Shift+D"
/// - Prevents default TextBox behavior (copy/paste)
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 3)
/// See: docs/decisions/iteration-5-research-findings.md (Q9)
/// </remarks>
public class HotkeyTextBox : TextBox
{
    public static readonly DependencyProperty HotkeyModifiersProperty =
        DependencyProperty.Register(
            nameof(HotkeyModifiers),
            typeof(ModifierKeys),
            typeof(HotkeyTextBox),
            new PropertyMetadata(ModifierKeys.None));

    public static readonly DependencyProperty HotkeyKeyProperty =
        DependencyProperty.Register(
            nameof(HotkeyKey),
            typeof(Key),
            typeof(HotkeyTextBox),
            new PropertyMetadata(Key.None));

    /// <summary>
    /// Captured modifier keys (Ctrl, Shift, Alt, Win).
    /// </summary>
    public ModifierKeys HotkeyModifiers
    {
        get => (ModifierKeys)GetValue(HotkeyModifiersProperty);
        set => SetValue(HotkeyModifiersProperty, value);
    }

    /// <summary>
    /// Captured main key.
    /// </summary>
    public Key HotkeyKey
    {
        get => (Key)GetValue(HotkeyKeyProperty);
        set => SetValue(HotkeyKeyProperty, value);
    }

    /// <summary>
    /// Whether a valid hotkey is currently set.
    /// </summary>
    public bool HasHotkey => HotkeyModifiers != ModifierKeys.None && HotkeyKey != Key.None;

    /// <summary>
    /// Event fired when the hotkey changes.
    /// </summary>
    public event EventHandler? HotkeyChanged;

    public HotkeyTextBox()
    {
        // Make read-only to prevent typing
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;

        // Placeholder text
        Text = "Klicken und Tastenkombination drücken...";
        Foreground = System.Windows.Media.Brushes.Gray;

        // Capture keyboard input
        PreviewKeyDown += HotkeyTextBox_PreviewKeyDown;

        // Prevent context menu (cut/copy/paste)
        ContextMenu = null;
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true; // Prevent default TextBox behavior

        // Get actual key (Handle System keys like Alt, F10)
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;

        // Clear on Escape, Delete, or Backspace
        if (key == Key.Escape || key == Key.Delete || key == Key.Back)
        {
            ClearHotkey();
            return;
        }

        // Ignore modifier-only presses (require a main key)
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Require at least one modifier
        if (modifiers == ModifierKeys.None)
        {
            Text = "Bitte Modifikatortaste verwenden (Ctrl, Shift, Alt)";
            Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }

        // Set hotkey
        HotkeyModifiers = modifiers;
        HotkeyKey = key;

        // Update display
        Text = FormatHotkey(modifiers, key);
        Foreground = System.Windows.Media.Brushes.Black;

        // Fire changed event
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearHotkey()
    {
        HotkeyModifiers = ModifierKeys.None;
        HotkeyKey = Key.None;
        Text = "Klicken und Tastenkombination drücken...";
        Foreground = System.Windows.Media.Brushes.Gray;
    }

    /// <summary>
    /// Format hotkey as string (e.g., "Ctrl+Shift+D").
    /// </summary>
    public static string FormatHotkey(ModifierKeys modifiers, Key key)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");

        parts.Add(key.ToString());

        return string.Join("+", parts);
    }

    /// <summary>
    /// Set hotkey programmatically.
    /// </summary>
    public void SetHotkey(ModifierKeys modifiers, Key key)
    {
        HotkeyModifiers = modifiers;
        HotkeyKey = key;
        Text = FormatHotkey(modifiers, key);
        Foreground = System.Windows.Media.Brushes.Black;

        // Fire changed event
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
    }
}
