using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public class FileSystemChangedEventArgs : EventArgs
{
    public string Path { get; }
    public string? PaneId { get; }

    public FileSystemChangedEventArgs(string path, string? paneId)
    {
        Path = path;
        PaneId = paneId;
    }
}

public class FileSystemService : IFileSystemService, IDisposable
{
    private readonly IProgressService _progressService;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

    public event EventHandler<FileSystemChangedEventArgs>? FileSystemChanged;

    public FileSystemService(IProgressService progressService)
    {
        _progressService = progressService;
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    public IEnumerable<FileSystemItem> GetDirectoryContents(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

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

        return items;
    }

    public void CreateDirectory(string path, string name)
    {
        var fullPath = Path.Combine(path, name);
        if (Directory.Exists(fullPath))
            throw new IOException("Directory already exists.");

        Directory.CreateDirectory(fullPath);
    }

    public void DeleteItems(IEnumerable<string> items)
    {
        foreach (var path in items)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else if (File.Exists(path))
                File.Delete(path);
        }
    }

    public void RenameItem(string oldPath, string newName)
    {
        var parentDir = Path.GetDirectoryName(oldPath);
        if (parentDir == null) return;
        
        var newPath = Path.Combine(parentDir, newName);
        
        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);
        else if (File.Exists(oldPath))
            File.Move(oldPath, newPath);
    }

    public void CopyItems(IEnumerable<string> items, string targetPath)
    {
        foreach (var item in items)
        {
            var fileName = Path.GetFileName(item);
            var destPath = Path.Combine(targetPath, fileName);

            if (Directory.Exists(item))
                CopyDirectory(item, destPath);
            else
                File.Copy(item, destPath, true);
        }
    }

    public void MoveItems(IEnumerable<string> items, string targetPath)
    {
        foreach (var item in items)
        {
            var fileName = Path.GetFileName(item);
            var destPath = Path.Combine(targetPath, fileName);

            if (Directory.Exists(item))
            {
                if (Directory.Exists(destPath))
                    throw new IOException($"Target directory already exists: {destPath}");
                Directory.Move(item, destPath);
            }
            else
            {
                File.Move(item, destPath, true);
            }
        }
    }

    public void OpenPath(string path)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    public void EditFile(string path)
    {
        string editorPath = "notepad.exe";
        var nppPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Notepad++", "notepad++.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Notepad++", "notepad++.exe")
        };

        foreach (var nppPath in nppPaths)
        {
            if (File.Exists(nppPath))
            {
                editorPath = nppPath;
                break;
            }
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(editorPath, $"\"{path}\"") { UseShellExecute = true });
    }

    public void SetWatcher(string path, string paneId)
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

            watcher.Created += (s, e) => FileSystemChanged?.Invoke(this, new FileSystemChangedEventArgs(path, paneId));
            watcher.Deleted += (s, e) => FileSystemChanged?.Invoke(this, new FileSystemChangedEventArgs(path, paneId));
            watcher.Renamed += (s, e) => FileSystemChanged?.Invoke(this, new FileSystemChangedEventArgs(path, paneId));
            watcher.Changed += (s, e) => FileSystemChanged?.Invoke(this, new FileSystemChangedEventArgs(path, paneId));

            _watchers[paneId] = watcher;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up watcher: {ex.Message}");
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
}
