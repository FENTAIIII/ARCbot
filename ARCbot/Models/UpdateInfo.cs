using System.Text.Json.Serialization;

namespace ARCbot.Models;

public class UpdateInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; }

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; }
}
