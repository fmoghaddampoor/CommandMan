using System.Text.Json.Serialization;

namespace CommandMan.Shell.Models;

public class BridgeResponse
{
    [JsonPropertyName("Action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("Data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("CurrentPath")]
    public string? CurrentPath { get; set; }
    
    [JsonPropertyName("Error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("Drives")]
    public List<DriveItem>? Drives { get; set; }

    [JsonPropertyName("PaneId")]
    public string? PaneId { get; set; }

    [JsonPropertyName("FocusItem")]
    public string? FocusItem { get; set; }
}
