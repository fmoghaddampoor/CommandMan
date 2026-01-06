using System.Collections.Generic;
using System.Threading.Tasks;
using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public interface IFileSystemService
{
    IEnumerable<FileSystemItem> GetDirectoryContents(string path);
    void CreateDirectory(string path, string name);
    void DeleteItems(IEnumerable<string> items);
    void RenameItem(string oldPath, string newName);
    void CopyItems(IEnumerable<string> items, string targetPath);
    void MoveItems(IEnumerable<string> items, string targetPath);
    void OpenPath(string path);
    void EditFile(string path);
    void SetWatcher(string path, string paneId);
    event EventHandler<FileSystemChangedEventArgs> FileSystemChanged;
}
