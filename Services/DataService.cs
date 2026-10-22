using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.Services;

    /// <summary>
    /// Loads static quest data and persists user state (checked quests + language + theme)
    /// to a JSON config file. Saves are debounced so rapid checkbox changes never
    /// block the UI thread and still hit disk in near real time.
    /// </summary>
public sealed class DataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _dataDir;
    private readonly string _configPath;

    public string QuestsPath { get; }
    public string ConfigPath => _configPath;
    public string SavesDir { get; }

    public DataService()
    {
        _dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(_dataDir);
        QuestsPath = Path.Combine(_dataDir, "quests.json");
        _configPath = Path.Combine(_dataDir, "config.json");
        SavesDir = Path.Combine(AppContext.BaseDirectory, "savedata");
        Directory.CreateDirectory(SavesDir);
    }

    public QuestsFile LoadQuests()
    {
        if (!File.Exists(QuestsPath))
        {
            return new QuestsFile();
        }

        var json = File.ReadAllText(QuestsPath);
        return JsonSerializer.Deserialize<QuestsFile>(json, JsonOptions) ?? new QuestsFile();
    }

    public AppState LoadState()
    {
        if (!File.Exists(_configPath))
        {
            return new AppState();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void SaveState(AppState state)
    {
        var tmp = _configPath + ".tmp";
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(tmp, json);
        File.Move(tmp, _configPath, overwrite: true);
    }
}
