using System.Collections.Generic;
using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public interface IDriveService
{
    IEnumerable<DriveItem> GetDrives();
}
