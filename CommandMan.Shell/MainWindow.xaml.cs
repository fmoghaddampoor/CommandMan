using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using CommandMan.Shell.Models;
using CommandMan.Shell.Services;
using CommandMan.Shell.ViewModels;

namespace CommandMan.Shell;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly FileSystemService _fileSystemService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MainWindow()
    {
        InitializeComponent();

        // Manual Dependency Injection
        var progressService = new ProgressService();
        _fileSystemService = new FileSystemService(progressService);
        var driveService = new DriveService();
        var configService = new ConfigService();

        _viewModel = new MainWindowViewModel(
            _fileSystemService,
            driveService,
            configService,
            progressService);

        _viewModel.SendMessage = SendMessageToWebView;

        InitializeAsync();

        this.Closed += (s, e) => _fileSystemService.Dispose();
    }

    private async void InitializeAsync()
    {
        await webView.EnsureCoreWebView2Async(null);

        // Set up message handler from JavaScript
        webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // Navigate to the Angular app (development server or built files)
        var angularPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
        if (File.Exists(angularPath))
        {
            webView.CoreWebView2.Navigate(angularPath);
        }
        else
        {
            // Development mode - use Angular dev server
            webView.CoreWebView2.Navigate("http://localhost:4200");
        }
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            // The message comes as a JSON string, we need to parse it
            var rawMessage = e.TryGetWebMessageAsString();
            
            if (string.IsNullOrEmpty(rawMessage))
            {
                rawMessage = e.WebMessageAsJson;
            }

            var request = JsonSerializer.Deserialize<BridgeRequest>(rawMessage, JsonOptions);

            if (request != null)
            {
                await _viewModel.HandleRequest(request);
            }
        }
        catch (Exception ex)
        {
            SendMessageToWebView(new BridgeResponse
            {
                Action = "error",
                Error = ex.Message
            });
        }
    }

    private void SendMessageToWebView(BridgeResponse response)
    {
        // Thread safety: WebView2 must be accessed from the UI thread
        if (!this.Dispatcher.CheckAccess())
        {
            this.Dispatcher.Invoke(() => SendMessageToWebView(response));
            return;
        }

        try
        {
            if (webView?.CoreWebView2 != null)
            {
                var json = JsonSerializer.Serialize(response, JsonOptions);
                webView.CoreWebView2.PostWebMessageAsJson(json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send message to WebView2: {ex.Message}");
        }
    }
}