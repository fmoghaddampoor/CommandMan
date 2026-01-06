using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public interface IConfigService
{
    void SaveState(AppState state);
    AppState GetState();
}
