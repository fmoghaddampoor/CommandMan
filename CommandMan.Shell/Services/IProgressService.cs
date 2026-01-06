using System.Threading.Tasks;

namespace CommandMan.Shell.Services;

public interface IProgressService
{
    Task ReportProgress(string fileName, int percentage);
    Task ClearProgress();
}
