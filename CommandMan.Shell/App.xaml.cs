using System.Configuration;
using System.Data;
using System.Windows;

namespace CommandMan.Shell;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await SignalRServer.StartAsync();
    }
}

