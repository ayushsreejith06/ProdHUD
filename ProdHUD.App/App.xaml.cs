using System.Windows;
using ProdHUD.App.Interop;

namespace ProdHUD.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConsoleHelper.AttachToParentConsole();
    }
}

