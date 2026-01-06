using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace CommandMan.Shell.Services;

public class ProgressService : IProgressService
{
    public async Task ReportProgress(string? fileName, int percentage)
    {
        if (SignalRServer.HubContext != null)
        {
            await SignalRServer.HubContext.Clients.All.SendAsync("ReceiveProgress", fileName, percentage);
        }
    }

    public async Task ClearProgress()
    {
        await ReportProgress(null, 0);
    }
}
