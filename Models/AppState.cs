using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MH2QuestChecklist.Models;

public sealed class AppState
{
    [JsonPropertyName("japanese")]
    public bool Japanese { get; set; }

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "System";

    [JsonPropertyName("group")]
    public string Group { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("completed")]
    public HashSet<string> Completed { get; set; } = new();

    [JsonPropertyName("lastSavePath")]
    public string LastSavePath { get; set; } = "";

    [JsonPropertyName("lastSlotName")]
    public string LastSlotName { get; set; } = "";

    [JsonPropertyName("windowLeft")]
    public double? WindowLeft { get; set; }

    [JsonPropertyName("windowTop")]
    public double? WindowTop { get; set; }

    [JsonPropertyName("windowWidth")]
    public double? WindowWidth { get; set; }

    [JsonPropertyName("windowHeight")]
    public double? WindowHeight { get; set; }

    [JsonPropertyName("windowMaximized")]
    public bool WindowMaximized { get; set; }
}
