using System.Windows;
using System.Threading.Tasks;

namespace CommandMan.Shell;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Global exception handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        await SignalRServer.StartAsync();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"UI Error: {e.Exception.Message}", "CommandMan Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            MessageBox.Show($"Fatal Error: {ex.Message}", "CommandMan Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        MessageBox.Show($"Background Task Error: {e.Exception.Message}", "CommandMan Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.SetObserved();
    }
}
