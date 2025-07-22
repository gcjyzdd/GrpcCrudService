using System;
using System.Text.Json.Serialization;

namespace SimpleGui.Models;

[JsonSerializable(typeof(AppConfiguration))]
public partial class AppConfigurationContext : JsonSerializerContext
{
}

public class AppConfiguration
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = "Default User";

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";

    [JsonPropertyName("autoSave")]
    public bool AutoSave { get; set; } = true;

    [JsonPropertyName("serverUrl")]
    public string ServerUrl { get; set; } = "localhost:5000";

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.Now;
}