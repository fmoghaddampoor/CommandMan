using System.Text.Json.Serialization;

namespace CommandMan.Shell.Models;

public class DriveItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("TotalSize")]
    public long TotalSize { get; set; }
    
    [JsonPropertyName("FreeSpace")]
    public long FreeSpace { get; set; }
    
    [JsonPropertyName("DriveType")]
    public string DriveType { get; set; } = string.Empty;
}
