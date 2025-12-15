using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace ProdHUD.App.Input;

/// <summary>
/// Low-level global keyboard hook that watches a configurable virtual key
/// (intended to be the Fn-equivalent) and writes state changes to the console.
/// </summary>
internal sealed class FnKeyListener : IDisposable
{
    public const int DefaultPassThroughVk = 0x71; // F2
    public const int DefaultFxVk = 0x73;          // F4

    public event Action<KeyStateChangePayload>? KeyStateChanged;

    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeydown = 0x0104;
    private const int WmSyskeyup = 0x0105;

    private readonly HashSet<int> _targetVks;
    private readonly Dictionary<int, string> _keyNames;
    private readonly Dictionary<int, bool> _pressed;
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    /// <summary>
    /// Listens for the provided virtual keys (defaults: F2, F4) and logs press/release.
    /// </summary>
    /// <param name="overrideVirtualKeys">Optional set of virtual keys to watch; defaults apply when empty.</param>
    public FnKeyListener(params int[] overrideVirtualKeys)
    {
        var keys = ResolveVirtualKeys(overrideVirtualKeys);
        _targetVks = new HashSet<int>(keys);
        _keyNames = _targetVks.ToDictionary(vk => vk, vk => KeyInterop.KeyFromVirtualKey(vk).ToString());
        _pressed = _targetVks.ToDictionary(vk => vk, _ => false);

        _proc = HookCallback;
        _hookId = SetHook(_proc);

        Console.WriteLine(BuildArmedMessage(_targetVks));
        Console.WriteLine("press and release the configured keys to see state changes.");

        // #region agent log
        AppendDebugLog(new DebugLogEntry(
            sessionId: "debug-session",
            runId: "pre-fix",
            hypothesisId: "A",
            location: "FnKeyListener::.ctor",
            message: "FnKeyListener initialized",
            data: new { keys = _targetVks.ToArray() }
        ));
        // #endregion
    }

    private static IReadOnlyList<int> ResolveVirtualKeys(IReadOnlyCollection<int> overrideVirtualKeys)
    {
        if (overrideVirtualKeys.Count > 0)
        {
            return overrideVirtualKeys.ToArray();
        }

        var env = Environment.GetEnvironmentVariable("PRODHUD_FN_VK");
        if (!string.IsNullOrWhiteSpace(env))
        {
            // Allow comma-separated list: e.g., "113,114,115"
            var parsed = env.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.TryParse(v, out var vk) ? vk : (int?)null)
                .Where(vk => vk.HasValue)
                .Select(vk => vk!.Value)
                .ToArray();

            if (parsed.Length > 0)
            {
                return parsed;
            }
        }

        // Defaults for this phase: F2, F4.
        return new[] { 0x71, 0x73 };
    }

    private static string BuildArmedMessage(IEnumerable<int> vks)
    {
        var sb = new StringBuilder("listener armed for keys: ");
        sb.Append(string.Join(", ", vks.Select(vk => $"{KeyInterop.KeyFromVirtualKey(vk)} (0x{vk:X})")));
        return sb.ToString();
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule ?? throw new InvalidOperationException("Process module unavailable.");
        var moduleHandle = GetModuleHandle(curModule.ModuleName);
        return SetWindowsHookEx(WhKeyboardLl, proc, moduleHandle, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            var vkCode = Marshal.ReadInt32(lParam);

            if (_targetVks.Contains(vkCode))
            {
                var name = _keyNames[vkCode];
                var isPressed = _pressed[vkCode];

                switch (message)
                {
                    case WmKeydown:
                    case WmSyskeydown:
                        if (!isPressed)
                        {
                            isPressed = true;
                            _pressed[vkCode] = true;
                            Console.WriteLine($"key {name}: pressed");
                            // #region agent log
                            AppendDebugLog(new DebugLogEntry(
                                sessionId: "debug-session",
                                runId: "pre-fix",
                                hypothesisId: "B",
                                location: "FnKeyListener::HookCallback",
                                message: "Key pressed",
                                data: new { vkCode, name }
                            ));
                            // #endregion
                            RaiseKeyStateChanged(vkCode, name, true);
                        }
                        break;
                    case WmKeyup:
                    case WmSyskeyup:
                        if (isPressed)
                        {
                            isPressed = false;
                            _pressed[vkCode] = false;
                            Console.WriteLine($"key {name}: unpressed");
                            // #region agent log
                            AppendDebugLog(new DebugLogEntry(
                                sessionId: "debug-session",
                                runId: "pre-fix",
                                hypothesisId: "B",
                                location: "FnKeyListener::HookCallback",
                                message: "Key released",
                                data: new { vkCode, name }
                            ));
                            // #endregion
                            RaiseKeyStateChanged(vkCode, name, false);
                        }
                        break;
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            _ = UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private void RaiseKeyStateChanged(int vkCode, string name, bool isPressed)
    {
        var payload = new KeyStateChangePayload(vkCode, name, isPressed);
        KeyStateChanged?.Invoke(payload);

        // #region agent log
        AppendDebugLog(new DebugLogEntry(
            sessionId: "debug-session",
            runId: "pre-fix",
            hypothesisId: "C",
            location: "FnKeyListener::RaiseKeyStateChanged",
            message: "Notified subscribers",
            data: new { payload.VirtualKey, payload.KeyName, payload.IsPressed }
        ));
        // #endregion
    }

    public readonly record struct KeyStateChangePayload(int VirtualKey, string KeyName, bool IsPressed);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

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
            var json = JsonSerializer.Serialize(enriched);
            File.AppendAllText(DebugLogPath, json + Environment.NewLine);
        }
        catch
        {
            // Swallow any logging errors; instrumentation must not break app flow.
        }
    }
    // #endregion
}

