using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace CommandMan.Shell;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MainWindow()
    {
        InitializeComponent();
        InitializeAsync();
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

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            // The message comes as a JSON string, we need to parse it
            var rawMessage = e.TryGetWebMessageAsString();
            
            if (string.IsNullOrEmpty(rawMessage))
            {
                rawMessage = e.WebMessageAsJson;
            }

            System.Diagnostics.Debug.WriteLine($"Received message: {rawMessage}");

            var request = JsonSerializer.Deserialize<BridgeRequest>(rawMessage, JsonOptions);

            if (request == null) return;

            System.Diagnostics.Debug.WriteLine($"Parsed action: {request.Action}, Path: {request.Path}");

            switch (request.Action)
            {
                case "getDirectoryContents":
                    HandleGetDirectoryContents(request.Path);
                    break;
                case "getDrives":
                    HandleGetDrives();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            SendErrorToWebView(ex.Message);
        }
    }

    private void HandleGetDirectoryContents(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var items = new List<FileSystemItem>();

            // Add parent directory navigation
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Parent != null)
            {
                items.Add(new FileSystemItem
                {
                    Name = "..",
                    Path = dirInfo.Parent.FullName,
                    IsDirectory = true,
                    Size = 0,
                    Modified = DateTime.MinValue
                });
            }

            // Add directories
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    items.Add(new FileSystemItem
                    {
                        Name = info.Name,
                        Path = info.FullName,
                        IsDirectory = true,
                        Size = 0,
                        Modified = info.LastWriteTime
                    });
                }
                catch { /* Skip inaccessible directories */ }
            }

            // Add files
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    items.Add(new FileSystemItem
                    {
                        Name = info.Name,
                        Path = info.FullName,
                        IsDirectory = false,
                        Size = info.Length,
                        Modified = info.LastWriteTime,
                        Extension = info.Extension
                    });
                }
                catch { /* Skip inaccessible files */ }
            }

            var response = new BridgeResponse
            {
                Action = "directoryContents",
                Data = items,
                CurrentPath = path
            };

            SendMessageToWebView(response);
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Failed to read directory: {ex.Message}");
        }
    }

    private void HandleGetDrives()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new DriveItem
                {
                    Name = d.Name,
                    Label = string.IsNullOrEmpty(d.VolumeLabel) ? d.DriveType.ToString() : d.VolumeLabel,
                    TotalSize = d.TotalSize,
                    FreeSpace = d.AvailableFreeSpace,
                    DriveType = d.DriveType.ToString()
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Found {drives.Count} drives");

            var response = new BridgeResponse
            {
                Action = "drives",
                Drives = drives
            };

            SendMessageToWebView(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting drives: {ex.Message}");
            SendErrorToWebView($"Failed to get drives: {ex.Message}");
        }
    }

    private void SendMessageToWebView(object message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        System.Diagnostics.Debug.WriteLine($"Sending to WebView: {json}");
        webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void SendErrorToWebView(string errorMessage)
    {
        var response = new BridgeResponse
        {
            Action = "error",
            Error = errorMessage
        };
        SendMessageToWebView(response);
    }
}

public class BridgeRequest
{
    [JsonPropertyName("Action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("Path")]
    public string? Path { get; set; }
}

public class BridgeResponse
{
    [JsonPropertyName("Action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("Data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("CurrentPath")]
    public string? CurrentPath { get; set; }
    
    [JsonPropertyName("Error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("Drives")]
    public List<DriveItem>? Drives { get; set; }
}

public class FileSystemItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("IsDirectory")]
    public bool IsDirectory { get; set; }
    
    [JsonPropertyName("Size")]
    public long Size { get; set; }
    
    [JsonPropertyName("Modified")]
    public DateTime Modified { get; set; }
    
    [JsonPropertyName("Extension")]
    public string? Extension { get; set; }
}

public class DriveItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("TotalSize")]
    public long TotalSize { get; set; }
    
    [JsonPropertyName("FreeSpace")]
    public long FreeSpace { get; set; }
    
    [JsonPropertyName("DriveType")]
    public string DriveType { get; set; } = string.Empty;
}