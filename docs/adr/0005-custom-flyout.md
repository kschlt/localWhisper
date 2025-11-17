# ADR-0005: Custom Flyout Notification (Not Windows Toast)

**Status:** Accepted
**Date:** 2025-09-17
**Affected Requirements:** FR-015; NFR-004

---

## Context

We need to notify the user when a transcription is complete and the text is in the clipboard. The notification should be:
- Quick to display (≤ 0.5s after clipboard write per NFR-004)
- Non-intrusive (doesn't steal focus or block workflow)
- Reliable (works consistently across Windows versions)
- Customizable (we control appearance and behavior)

**Requirements:**
- Show message: "Transkript im Clipboard" (German) or "Transcript in Clipboard" (English)
- Appear near system tray (where user expects app-related notifications)
- Auto-dismiss after ~3 seconds
- No user interaction required (informational only)
- Must not steal focus from active window (user is about to paste)

---

## Options Considered

### Option A: Custom WPF Flyout (Window)

**Description:**
Create a custom WPF `Window` with borderless style, positioned near the system tray. Use fade-in/fade-out animations and auto-dismiss timer.

**Pros:**
+ **Full control:** We control positioning, styling, animation, timing.
+ **Reliable:** No dependency on Windows notification system or Group Policy settings.
+ **Consistent:** Works identically on Windows 10, 11, and future versions.
+ **Fast:** Can display immediately (no OS notification queue).
+ **No permission issues:** Doesn't require notification permissions or Action Center.

**Cons:**
- **More code:** Need to implement window positioning, animations, timer (~100-150 LOC).
- **Positioning complexity:** Must calculate position relative to tray icon (varies by screen resolution, taskbar position).

---

### Option B: Windows Toast Notifications (`ToastNotification` API)

**Description:**
Use Windows 10/11 toast notification system via `Microsoft.Toolkit.Uwp.Notifications` or native WinRT APIs.

**Pros:**
+ **Native look:** Matches Windows notification style.
+ **Less code:** Library handles display, queuing, dismissal.

**Cons:**
- **Unreliable timing:** Notifications can be delayed by OS (especially if Action Center is busy).
- **Group Policy interference:** Corporate IT may disable toast notifications.
- **Focus stealing:** Toasts can steal focus or interrupt workflow (especially on Windows 10).
- **Slower:** May take > 0.5s to appear (fails NFR-004 target).
- **Limited control:** Hard to position near tray; appears in notification area/Action Center.
- **Complexity:** Requires app registration and notification manifest (adds packaging complexity).

---

### Option C: System Tray Balloon Tip (`NotifyIcon.ShowBalloonTip`)

**Description:**
Use the legacy balloon tip feature of `NotifyIcon`.

**Pros:**
+ **Simple:** One-line API call: `notifyIcon.ShowBalloonTip(3000, "Title", "Message", ToolTipIcon.Info)`.
+ **Near tray:** Appears directly from tray icon.

**Cons:**
- **Deprecated:** Microsoft discourages balloon tips in favor of toast notifications (since Windows 10).
- **Ugly:** Dated appearance (yellow bubble, doesn't match modern Windows design).
- **Unreliable:** May not show if user has disabled balloon tips (common in corporate environments).
- **Poor control:** Cannot customize appearance or animation.

---

## Decision

We choose **Option A: Custom WPF Flyout**.

**Rationale:**
1. **Reliability:** We control everything; no OS or policy interference.
2. **Performance:** Can display immediately, meeting NFR-004 target (≤ 0.5s).
3. **User experience:** Non-intrusive, near tray (expected location), modern appearance.
4. **Consistency:** Works identically on all Windows versions; no surprises.
5. **Customizability:** Can adjust style, position, timing, animation to fit app's design.

**Trade-off:**
- Slightly more implementation effort (~100-150 LOC), but worth it for reliability and control.

---

## Consequences

### Positive

✅ **Reliable timing:** Flyout appears instantly after clipboard write (no OS delays).

✅ **No permission issues:** Works without notification permissions or Action Center setup.

✅ **Consistent behavior:** Same experience on Windows 10, 11, and future versions.

✅ **Control over UX:** We can tweak position, size, colors, animation speed.

✅ **No focus stealing:** Flyout is topmost but doesn't take input focus (user can continue typing/pasting).

### Negative

❌ **More implementation work:** Need to code window positioning, animations, timer.
  - **Mitigation:**
    - Use WPF `Storyboard` for fade animations (built-in, easy).
    - Use `System.Windows.Forms.Screen` to find tray icon position.
    - Code is straightforward (~100-150 LOC).

❌ **Positioning complexity:** Taskbar can be on any edge (bottom, top, left, right).
  - **Mitigation:**
    - Detect taskbar position via `SystemParameters.WorkArea`.
    - Position flyout near bottom-right (most common) or adapt based on taskbar edge.

### Neutral

⚪ **Not "native":** Doesn't use Windows notification system, but users won't notice (our flyout looks modern).

---

## Implementation Notes

### Flyout Window Structure

**XAML:**
```xml
<Window x:Class="FlyoutNotification"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        Width="300" Height="80">
    <Border Background="#2C2C2C" CornerRadius="8" Padding="15">
        <TextBlock x:Name="MessageText"
                   Foreground="White"
                   FontSize="14"
                   Text="Transkript im Clipboard" />
    </Border>
</Window>
```

**Positioning:**
```csharp
void PositionFlyout()
{
    var screen = Screen.PrimaryScreen.WorkingArea;
    var taskbarEdge = DetectTaskbarEdge(); // Bottom, Top, Left, Right

    if (taskbarEdge == TaskbarEdge.Bottom)
    {
        this.Left = screen.Right - this.Width - 20;
        this.Top = screen.Bottom - this.Height - 20;
    }
    // ... handle other edges
}
```

**Animations:**
```csharp
void Show()
{
    PositionFlyout();
    this.Opacity = 0;
    this.Show();

    // Fade in
    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
    this.BeginAnimation(Window.OpacityProperty, fadeIn);

    // Auto-dismiss after 3 seconds
    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
    timer.Tick += (s, e) => { timer.Stop(); FadeOutAndClose(); };
    timer.Start();
}

void FadeOutAndClose()
{
    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
    fadeOut.Completed += (s, e) => this.Close();
    this.BeginAnimation(Window.OpacityProperty, fadeOut);
}
```

### Timing Verification

**Instrumentation:**
```csharp
var sw = Stopwatch.StartNew();
clipboardService.Write(text);
var clipboardTime = sw.ElapsedMilliseconds;

flyout.Show();
var flyoutTime = sw.ElapsedMilliseconds;

logger.LogInformation("Flyout display latency: {Latency}ms", flyoutTime - clipboardTime);
// Verify: < 500ms
```

---

## Future Enhancements

**Possible improvements (post-v0.1):**
- **Action buttons:** Add "Copy Again" or "Open History" buttons (though may complicate UX).
- **Rich content:** Show first 50 chars of transcript in flyout.
- **Custom themes:** Let user choose light/dark mode for flyout.
- **Sound notification:** Optional sound cue when flyout appears.

**Not planned for v0.1:** Keep flyout simple and fast.

---

## Related Decisions

- **ADR-0001:** WPF enables custom window creation with animations.

---

## Related Requirements

**Functional:**
- FR-015: Toast/Status → Implemented as custom flyout

**Non-Functional:**
- NFR-004: Usability (flyout ≤ 0.5s after clipboard write)

---

## Related Documents

- **Architecture Overview:** `architecture/architecture-overview.md` (Component: FlyoutNotification)
- **Runtime Flows:** `architecture/runtime-flows.md` (Flow 1: E2E Dictation)

---

**Last updated:** 2025-09-17
**Version:** v1
