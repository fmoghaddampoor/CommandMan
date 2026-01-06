using System.Text.Json.Serialization;

namespace CommandMan.Shell.Models;

public class FileSystemItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("IsDirectory")]
    public bool IsDirectory { get; set; }
    
    [JsonPropertyName("Size")]
    public long Size { get; set; }
    
    [JsonPropertyName("Modified")]
    public DateTime Modified { get; set; }
    
    [JsonPropertyName("Extension")]
    public string? Extension { get; set; }
}
