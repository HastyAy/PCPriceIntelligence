using System.Text.Json.Serialization;

namespace web.Services;

public class CompatibilityResult
{
    [JsonPropertyName("compatible")]
    public bool Compatible { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();
}
