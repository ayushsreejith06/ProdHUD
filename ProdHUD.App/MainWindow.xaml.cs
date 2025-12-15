using System;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ProdHUD.App.Input;

namespace ProdHUD.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private FnKeyListener? _fnListener;
    private bool _isPassThroughDisabled;
    private IntPtr _hwnd;
    private const double OpaqueOpacity = 1.0;
    private const double TranslucentOpacity = 0.35;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (_isPassThroughDisabled && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _fnListener = new FnKeyListener();
        _fnListener.KeyStateChanged += OnFnKeyStateChanged;
        ApplyPassThroughState(isPassThroughDisabled: false);

        // #region agent log
        AppendDebugLog(new DebugLogEntry(
            sessionId: "debug-session",
            runId: "pre-fix",
            hypothesisId: "D",
            location: "MainWindow::OnLoaded",
            message: "Window loaded and listener attached",
            data: new { initialOpacity = Opacity }
        ));
        // #endregion
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        ApplyPassThroughState(_isPassThroughDisabled);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_fnListener is not null)
        {
            _fnListener.KeyStateChanged -= OnFnKeyStateChanged;
        }
        _fnListener?.Dispose();
    }

    private void OnFnKeyStateChanged(FnKeyListener.KeyStateChangePayload change)
    {
        if (change.VirtualKey != FnKeyListener.DefaultPassThroughVk)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            // When F2 is pressed we want the window opaque; when unpressed it becomes translucent.
            var isPressed = change.IsPressed;
            ApplyPassThroughState(isPassThroughDisabled: isPressed);
        });
    }

    private void ApplyPassThroughState(bool isPassThroughDisabled)
    {
        _isPassThroughDisabled = isPassThroughDisabled;
        Opacity = isPassThroughDisabled ? OpaqueOpacity : TranslucentOpacity;
        IsHitTestVisible = isPassThroughDisabled;
        RootGrid.IsHitTestVisible = isPassThroughDisabled;
        RootGrid.IsEnabled = isPassThroughDisabled;
        ResizeMode = isPassThroughDisabled ? ResizeMode.CanResize : ResizeMode.NoResize;
        UpdateHitTestTransparency(allowClickThrough: !isPassThroughDisabled);

        // #region agent log
        AppendDebugLog(new DebugLogEntry(
            sessionId: "debug-session",
            runId: "pre-fix",
            hypothesisId: "D",
            location: "MainWindow::ApplyPassThroughState",
            message: "Pass-through state updated",
            data: new
            {
                isPassThroughDisabled,
                windowHitTestVisible = IsHitTestVisible,
                isHitTestVisible = RootGrid.IsHitTestVisible,
                resizeMode = ResizeMode.ToString(),
                opacity = Opacity,
                allowsTransparency = AllowsTransparency,
                windowBackground = Background?.ToString(),
                rootBackground = RootGrid?.Background?.ToString()
            }
        ));
        // #endregion
    }

    private void UpdateHitTestTransparency(bool allowClickThrough)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        const int GwlExstyle = -20;
        const int WsExTransparent = 0x00000020;
        const int WsExLayered = 0x00080000;

        var styles = GetWindowLongPtr(_hwnd, GwlExstyle);
        var stylesInt = styles.ToInt64();

        // Always keep layered; toggle transparent based on pass-through.
        stylesInt |= WsExLayered;
        if (allowClickThrough)
        {
            stylesInt |= WsExTransparent;
        }
        else
        {
            stylesInt &= ~WsExTransparent;
        }

        SetWindowLongPtr(_hwnd, GwlExstyle, new IntPtr(stylesInt));
    }

    // #region agent log
    private const string DebugLogPath = @"c:\Users\ayush_6b\Desktop\Personal\ProdHUD\.cursor\debug.log";

    private readonly record struct DebugLogEntry(
        string sessionId,
        string runId,
        string hypothesisId,
        string location,
        string message,
        object data,
        long? timestamp = null
    );

    private static void AppendDebugLog(DebugLogEntry entry)
    {
        try
        {
            var enriched = entry with { timestamp = entry.timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            var json = System.Text.Json.JsonSerializer.Serialize(enriched);
            System.IO.File.AppendAllText(DebugLogPath, json + Environment.NewLine);
        }
        catch
        {
            // Swallow any logging errors; instrumentation must not break app flow.
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    // #endregion
}