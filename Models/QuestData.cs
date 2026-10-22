using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MH2QuestChecklist.Models;

public sealed class QuestsFile
{
    [JsonPropertyName("groups")]
    public List<QuestGroupModel> Groups { get; set; } = new();
}

public sealed class QuestGroupModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("nameJp")]
    public string NameJp { get; set; } = "";

    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; } = new();
}

public sealed class Category
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("nameJp")]
    public string NameJp { get; set; } = "";

    [JsonPropertyName("quests")]
    public List<Quest> Quests { get; set; } = new();

    public string DisplayName(bool japanese) => japanese ? NameJp : Name;
}

public sealed class Quest
{
    [JsonPropertyName("hex")]
    public string Hex { get; set; } = "";

    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("nameJp")]
    public string NameJp { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("titleJp")]
    public string TitleJp { get; set; } = "";

    [JsonPropertyName("season")]
    public string Season { get; set; } = "";

    [JsonPropertyName("seasonJp")]
    public string SeasonJp { get; set; } = "";

    [JsonPropertyName("seasonClass")]
    public string SeasonClass { get; set; } = "";

    [JsonPropertyName("fee")]
    public int Fee { get; set; }

    [JsonPropertyName("reward")]
    public List<int> Reward { get; set; } = new();

    [JsonPropertyName("hrp")]
    public List<int> Hrp { get; set; } = new();

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "";

    [JsonPropertyName("localeJp")]
    public string LocaleJp { get; set; } = "";

    [JsonPropertyName("goals")]
    public List<Goal> Goals { get; set; } = new();

    [JsonPropertyName("requirements")]
    public string Requirements { get; set; } = "";
}

public sealed class Goal
{
    [JsonPropertyName("en")]
    public string En { get; set; } = "";

    [JsonPropertyName("jp")]
    public string Jp { get; set; } = "";
}
