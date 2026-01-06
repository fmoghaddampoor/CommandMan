using System;
using System.IO;
using System.Text.Json;
using CommandMan.Shell.Models;

namespace CommandMan.Shell.Services;

public class ConfigService : IConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "CommandMan", 
        "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void SaveState(AppState state)
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

    public AppState GetState()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var state = JsonSerializer.Deserialize<AppState>(json, JsonOptions);
                if (state != null)
                {
                    // Validate paths
                    if (string.IsNullOrEmpty(state.LeftPath) || !Directory.Exists(state.LeftPath))
                        state.LeftPath = GetDefaultDrive();
                    if (string.IsNullOrEmpty(state.RightPath) || !Directory.Exists(state.RightPath))
                        state.RightPath = GetDefaultDrive();
                    
                    return state;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading config: {ex.Message}");
        }

        return CreateDefaultState();
    }

    private string GetDefaultDrive()
    {
        return DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed)?.Name ?? "C:\\";
    }

    private AppState CreateDefaultState()
    {
        var drive = GetDefaultDrive();
        return new AppState
        {
            LeftPath = drive,
            RightPath = drive
        };
    }
}
