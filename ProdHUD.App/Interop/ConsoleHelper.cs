using System;
using System.Runtime.InteropServices;

namespace ProdHUD.App.Interop;

internal static class ConsoleHelper
{
    private const int AttachParentProcess = -1;

    /// <summary>
    /// Ensures the WPF process is attached to the launching terminal so that
    /// Console.WriteLine output is visible while developing.
    /// </summary>
    public static void AttachToParentConsole()
    {
        try
        {
            if (!AttachConsole(AttachParentProcess))
            {
                // If there is no parent console (double-click launch), allocate one.
                AllocConsole();
            }
        }
        catch
        {
            // Non-fatal: console logging is best-effort during early phases.
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}

