using System.Text.Json.Serialization;

namespace CommandMan.Shell.Models;

public class BridgeRequest
{
    [JsonPropertyName("Action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("Path")]
    public string? Path { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Items")]
    public List<string>? Items { get; set; }

    [JsonPropertyName("TargetPath")]
    public string? TargetPath { get; set; }

    [JsonPropertyName("PaneId")]
    public string? PaneId { get; set; }

    [JsonPropertyName("State")]
    public AppState? State { get; set; }
}
