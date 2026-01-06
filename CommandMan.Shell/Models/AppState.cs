using System.Text.Json.Serialization;

namespace CommandMan.Shell.Models;

public class AppState
{
    [JsonPropertyName("LeftPath")]
    public string LeftPath { get; set; } = string.Empty;

    [JsonPropertyName("RightPath")]
    public string RightPath { get; set; } = string.Empty;
}
