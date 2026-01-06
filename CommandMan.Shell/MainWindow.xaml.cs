using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.AspNetCore.SignalR;

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

    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

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

        // Clean up watchers on close
        this.Closed += (s, e) => {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
        };

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
                    HandleGetDirectoryContents(request.Path, request.PaneId);
                    break;
                case "getDrives":
                    HandleGetDrives();
                    break;
                case "getAppInfo":
                    HandleGetAppInfo();
                    break;
                case "saveState":
                    if (request.State != null) HandleSaveState(request.State);
                    break;
                case "getState":
                    HandleGetState();
                    break;
                case "createDirectory":
                    HandleCreateDirectory(request.Path, request.Name, request.PaneId);
                    break;
                case "openPath":
                    HandleOpenPath(request.Path);
                    break;
                case "deleteItems":
                    if (request.Items != null)
                        _ = HandleDeleteItems(request.Items, request.PaneId);
                    break;
                case "renameItem":
                    HandleRenameItem(request.Path, request.Name, request.PaneId);
                    break;
                case "copyItems":
                    if (request.Items != null && request.TargetPath != null)
                        _ = HandleCopyItems(request.Items, request.TargetPath, request.PaneId);
                    break;
                case "moveItems":
                    if (request.Items != null && request.TargetPath != null)
                        _ = HandleMoveItems(request.Items, request.TargetPath, request.PaneId);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            SendErrorToWebView(ex.Message);
        }
    }

    private void HandleCreateDirectory(string? path, string? name, string? paneId)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) return;

        try
        {
            var fullPath = Path.Combine(path, name);
            if (Directory.Exists(fullPath))
            {
                throw new Exception("Directory already exists.");
            }

            Directory.CreateDirectory(fullPath);
            
            // Refresh the pane
            HandleGetDirectoryContents(path, paneId, name);
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Failed to create directory: {ex.Message}");
        }
    }

    private void HandleOpenPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Failed to open path: {ex.Message}");
        }
    }

    private async Task HandleDeleteItems(List<string> items, string? paneId)
    {
        if (items == null || items.Count == 0) return;
        
        try
        {
            var totalCount = items.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var path = items[i];
                var fileName = Path.GetFileName(path);
                
                int progress = (int)((double)i / totalCount * 100);
                if (SignalRServer.HubContext != null)
                    await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", $"Deleting {fileName}...", progress);

                if (Directory.Exists(path))
                {
                    await Task.Run(() => Directory.Delete(path, true));
                }
                else if (File.Exists(path))
                {
                    await Task.Run(() => File.Delete(path));
                }
            }

            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", "Complete", 100);
            
            await Task.Delay(300);
            
            // Refresh the pane
            if (items.Count > 0)
            {
                var parentPath = Path.GetDirectoryName(items[0]);
                HandleGetDirectoryContents(parentPath, paneId);
            }
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Failed to delete items: {ex.Message}");
        }
        finally
        {
            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", null, 0);
        }
    }

    private void HandleRenameItem(string? oldPath, string? newName, string? paneId)
    {
        if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newName)) return;
        try
        {
            var parentDir = Path.GetDirectoryName(oldPath);
            if (parentDir == null) return;
            
            var newPath = Path.Combine(parentDir, newName);
            
            if (Directory.Exists(oldPath))
            {
                Directory.Move(oldPath, newPath);
            }
            else if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }

            // Refresh the pane and focus the new name
            HandleGetDirectoryContents(parentDir, paneId, newName);
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Failed to rename item: {ex.Message}");
        }
    }


    private async Task HandleCopyItems(List<string> items, string targetPath, string? paneId)
    {
        try
        {
            var totalCount = items.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var item = items[i];
                var fileName = Path.GetFileName(item);
                var destPath = Path.Combine(targetPath, fileName);

                // Report progress
                int progress = (int)((double)i / totalCount * 100);
                if (SignalRServer.HubContext != null)
                    await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", $"Copying {fileName}...", progress);

                if (Directory.Exists(item))
                {
                    await Task.Run(() => CopyDirectory(item, destPath));
                }
                else
                {
                    await Task.Run(() => File.Copy(item, destPath, true));
                }
            }

            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", "Complete", 100);
            
            await Task.Delay(500); // Small delay to show completion
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Copy failed: {ex.Message}");
        }
        finally
        {
            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", null, 0);

            // Refresh target pane
            HandleGetDirectoryContents(targetPath, paneId == "left" ? "right" : "left");
        }
    }

    private async Task HandleMoveItems(List<string> items, string targetPath, string? paneId)
    {
        try
        {
            var totalCount = items.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var item = items[i];
                var fileName = Path.GetFileName(item);
                var destPath = Path.Combine(targetPath, fileName);

                int progress = (int)((double)i / totalCount * 100);
                if (SignalRServer.HubContext != null)
                    await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", $"Moving {fileName}...", progress);

                if (Directory.Exists(item))
                {
                    if (Directory.Exists(destPath))
                    {
                        throw new IOException($"Target directory already exists: {destPath}");
                    }
                    await Task.Run(() => Directory.Move(item, destPath));
                }
                else
                {
                    await Task.Run(() => File.Move(item, destPath, true));
                }
            }

            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", "Complete", 100);
            
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            SendErrorToWebView($"Move failed: {ex.Message}");
        }
        finally
        {
            if (SignalRServer.HubContext != null)
                await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", null, 0);

            // Refresh both panes
            HandleGetDirectoryContents(targetPath, paneId == "left" ? "right" : "left");
            if (items.Count > 0)
            {
                var sourcePath = Path.GetDirectoryName(items[0]);
                HandleGetDirectoryContents(sourcePath, paneId);
            }
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }
    }

    private void UpdateWatcher(string path, string? paneId)
    {
        if (string.IsNullOrEmpty(paneId) || !Directory.Exists(path)) return;

        if (_watchers.TryGetValue(paneId, out var oldWatcher))
        {
            oldWatcher.EnableRaisingEvents = false;
            oldWatcher.Dispose();
            _watchers.Remove(paneId);
        }

        try
        {
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => OnFileSystemChanged(paneId, path);
            watcher.Deleted += (s, e) => OnFileSystemChanged(paneId, path);
            watcher.Renamed += (s, e) => OnFileSystemChanged(paneId, path);
            watcher.Changed += (s, e) => OnFileSystemChanged(paneId, path);

            _watchers[paneId] = watcher;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up watcher: {ex.Message}");
        }
    }

    private void OnFileSystemChanged(string paneId, string path)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var response = new BridgeResponse
            {
                Action = "refreshPane",
                PaneId = paneId,
                CurrentPath = path
            };
            SendMessageToWebView(response);
        });
    }

    private void HandleGetAppInfo()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var response = new BridgeResponse
        {
            Action = "appInfo",
            Data = new { Version = version, AppName = "CommandMan" }
        };
        SendMessageToWebView(response);
    }

    private void HandleGetDirectoryContents(string? path, string? paneId, string? focusItem = null)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Update real-time watcher
        UpdateWatcher(path, paneId);

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
                CurrentPath = path,
                PaneId = paneId,
                FocusItem = focusItem
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

    // State Persistence
    private readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CommandMan", "config.json");

    private void HandleSaveState(AppState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }

    private void HandleGetState()
    {
        try
        {
            AppState state;
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                state = JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? CreateDefaultState();
                
                // Validate paths
                var defaultConfig = CreateDefaultState();
                if (string.IsNullOrEmpty(state.LeftPath) || !Directory.Exists(state.LeftPath))
                    state.LeftPath = defaultConfig.LeftPath;
                if (string.IsNullOrEmpty(state.RightPath) || !Directory.Exists(state.RightPath))
                    state.RightPath = defaultConfig.RightPath;
            }
            else
            {
                state = CreateDefaultState();
            }

            var response = new BridgeResponse
            {
                Action = "state",
                Data = state
            };
            SendMessageToWebView(response);
        }
        catch
        {
            SendMessageToWebView(new BridgeResponse { Action = "state", Data = CreateDefaultState() });
        }
    }

    private AppState CreateDefaultState()
    {
        // Find first available fixed drive
        var drive = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed)?.Name ?? "C:\\";

        return new AppState
        {
            LeftPath = drive,
            RightPath = drive
        };
    }
}

public class BridgeRequest
{
    [JsonPropertyName("Action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("Path")]
    public string? Path { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Items")]
    public List<string>? Items { get; set; }

    [JsonPropertyName("TargetPath")]
    public string? TargetPath { get; set; }

    [JsonPropertyName("PaneId")]
    public string? PaneId { get; set; }

    [JsonPropertyName("State")]
    public AppState? State { get; set; }
}

public class AppState
{
    [JsonPropertyName("LeftPath")]
    public string LeftPath { get; set; } = string.Empty;

    [JsonPropertyName("RightPath")]
    public string RightPath { get; set; } = string.Empty;
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

    [JsonPropertyName("PaneId")]
    public string? PaneId { get; set; }

    [JsonPropertyName("FocusItem")]
    public string? FocusItem { get; set; }
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