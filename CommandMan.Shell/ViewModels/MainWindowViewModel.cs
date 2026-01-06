using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandMan.Shell.Models;
using CommandMan.Shell.Services;

namespace CommandMan.Shell.ViewModels;

public class MainWindowViewModel
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDriveService _driveService;
    private readonly IConfigService _configService;
    private readonly IProgressService _progressService;
    
    public Action<BridgeResponse>? SendMessage { get; set; }

    public MainWindowViewModel(
        IFileSystemService fileSystemService,
        IDriveService driveService,
        IConfigService configService,
        IProgressService progressService)
    {
        _fileSystemService = fileSystemService;
        _driveService = driveService;
        _configService = configService;
        _progressService = progressService;

        _fileSystemService.FileSystemChanged += OnFileSystemChanged;
    }

    public async Task HandleRequest(BridgeRequest request)
    {
        try
        {
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
                    if (request.State != null) _configService.SaveState(request.State);
                    break;
                case "getState":
                    HandleGetState();
                    break;
                case "createDirectory":
                    HandleCreateDirectory(request.Path, request.Name, request.PaneId);
                    break;
                case "openPath":
                    _fileSystemService.OpenPath(request.Path!);
                    break;
                case "deleteItems":
                    if (request.Items != null)
                        await HandleDeleteItems(request.Items, request.PaneId);
                    break;
                case "renameItem":
                    HandleRenameItem(request.Path, request.Name, request.PaneId);
                    break;
                case "copyItems":
                    if (request.Items != null && request.TargetPath != null)
                        await HandleCopyItems(request.Items, request.TargetPath, request.PaneId);
                    break;
                case "moveItems":
                    if (request.Items != null && request.TargetPath != null)
                        await HandleMoveItems(request.Items, request.TargetPath, request.PaneId);
                    break;
                case "editFile":
                    _fileSystemService.EditFile(request.Path!);
                    break;
            }
        }
        catch (Exception ex)
        {
            SendError(ex.Message);
        }
    }

    private void HandleGetDirectoryContents(string? path, string? paneId, string? focusItem = null)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        _fileSystemService.SetWatcher(path, paneId!);
        var items = _fileSystemService.GetDirectoryContents(path);
        
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "directoryContents",
            Data = items,
            CurrentPath = path,
            PaneId = paneId,
            FocusItem = focusItem
        });
    }

    private void HandleGetDrives()
    {
        var drives = _driveService.GetDrives();
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "drives",
            Drives = drives.ToList()
        });
    }

    private void HandleGetAppInfo()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "appInfo",
            Data = new { Version = version, AppName = "CommandMan" }
        });
    }

    private void HandleGetState()
    {
        var state = _configService.GetState();
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "state",
            Data = state
        });
    }

    private void HandleCreateDirectory(string? path, string? name, string? paneId)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) return;
        _fileSystemService.CreateDirectory(path, name);
        HandleGetDirectoryContents(path, paneId, name);
    }

    private async Task HandleDeleteItems(List<string> items, string? paneId)
    {
        try
        {
            var totalCount = items.Count;
            await Task.Run(async () =>
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var path = items[i];
                    var fileName = Path.GetFileName(path);
                    int progress = (int)((double)i / totalCount * 100);
                    await _progressService.ReportProgress($"Deleting {fileName}...", progress);
                    
                    _fileSystemService.DeleteItems(new[] { path });
                }
            });

            await _progressService.ReportProgress("Complete", 100);
            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            SendError($"Delete failed: {ex.Message}");
        }
        finally
        {
            await _progressService.ClearProgress();

            if (items.Count > 0)
            {
                var parentPath = Path.GetDirectoryName(items[0]);
                HandleGetDirectoryContents(parentPath, paneId);
            }
        }
    }

    private void HandleRenameItem(string? oldPath, string? newName, string? paneId)
    {
        if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newName)) return;
        _fileSystemService.RenameItem(oldPath, newName);
        var parentDir = Path.GetDirectoryName(oldPath);
        HandleGetDirectoryContents(parentDir, paneId, newName);
    }

    private async Task HandleCopyItems(List<string> items, string targetPath, string? paneId)
    {
        try
        {
            var totalCount = items.Count;
            await Task.Run(async () => 
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var item = items[i];
                    var fileName = Path.GetFileName(item);
                    int progress = (int)((double)i / totalCount * 100);
                    await _progressService.ReportProgress($"Copying {fileName}...", progress);
                    
                    _fileSystemService.CopyItems(new[] { item }, targetPath);
                }
            });

            await _progressService.ReportProgress("Complete", 100);
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            SendError($"Copy failed: {ex.Message}");
        }
        finally
        {
            await _progressService.ClearProgress();
            HandleGetDirectoryContents(targetPath, paneId == "left" ? "right" : "left");
        }
    }

    private async Task HandleMoveItems(List<string> items, string targetPath, string? paneId)
    {
        try
        {
            var totalCount = items.Count;
            await Task.Run(async () =>
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var item = items[i];
                    var fileName = Path.GetFileName(item);
                    int progress = (int)((double)i / totalCount * 100);
                    await _progressService.ReportProgress($"Moving {fileName}...", progress);
                    
                    _fileSystemService.MoveItems(new[] { item }, targetPath);
                }
            });

            await _progressService.ReportProgress("Complete", 100);
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            SendError($"Move failed: {ex.Message}");
        }
        finally
        {
            await _progressService.ClearProgress();

            HandleGetDirectoryContents(targetPath, paneId == "left" ? "right" : "left");
            if (items.Count > 0)
            {
                var sourcePath = Path.GetDirectoryName(items[0]);
                HandleGetDirectoryContents(sourcePath, paneId);
            }
        }
    }

    private void OnFileSystemChanged(object? sender, FileSystemChangedEventArgs e)
    {
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "refreshPane",
            PaneId = e.PaneId,
            CurrentPath = e.Path
        });
    }

    private void SendError(string message)
    {
        SendMessage?.Invoke(new BridgeResponse
        {
            Action = "error",
            Error = message
        });
    }
}
