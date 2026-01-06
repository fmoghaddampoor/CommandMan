using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public class DriveService : IDriveService
{
    public IEnumerable<DriveItem> GetDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new DriveItem
            {
                Name = d.Name,
                Label = string.IsNullOrEmpty(d.VolumeLabel) ? d.DriveType.ToString() : d.VolumeLabel,
                TotalSize = d.TotalSize,
                FreeSpace = d.AvailableFreeSpace,
                DriveType = d.DriveType.ToString()
            });
    }
}
