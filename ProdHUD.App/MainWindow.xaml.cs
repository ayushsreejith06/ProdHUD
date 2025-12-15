using System;
using System.Windows;
using System.Windows.Input;
using ProdHUD.App.Input;

namespace ProdHUD.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private FnKeyListener? _fnListener;
    private const double OpaqueOpacity = 1.0;
    private const double TranslucentOpacity = 0.6;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _fnListener = new FnKeyListener();
        _fnListener.KeyStateChanged += OnFnKeyStateChanged;
        ApplyPassThroughOpacity(isPassThroughDisabled: true);

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
            ApplyPassThroughOpacity(isPassThroughDisabled: isPressed);
        });
    }

    private void ApplyPassThroughOpacity(bool isPassThroughDisabled)
    {
        Opacity = isPassThroughDisabled ? OpaqueOpacity : TranslucentOpacity;

        // #region agent log
        AppendDebugLog(new DebugLogEntry(
            sessionId: "debug-session",
            runId: "pre-fix",
            hypothesisId: "D",
            location: "MainWindow::ApplyPassThroughOpacity",
            message: "Opacity updated",
            data: new { isPassThroughDisabled, opacity = Opacity }
        ));
        // #endregion
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
    // #endregion
}