using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using MH2QuestChecklist.Models;
using MH2QuestChecklist.Services;

namespace MH2QuestChecklist.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly DataService _data;
    private readonly AppState _state;
    private readonly DispatcherTimer _saveTimer;
    private List<VillageQuestMap.QuestBit> _questMap = new();
    private List<VillageQuestMap.QuestBit> _townMap = new();
    private bool _japanese;
    private string _theme = "System";
    private QuestGroup? _selectedGroup;
    private CategoryTab? _selectedCategory;
    private string _searchText = "";
    private SaveSlot? _selectedSlot;

    public ObservableCollection<QuestGroup> Groups { get; } = new();

    public ObservableCollection<SaveSlot> Slots { get; } = new();

    public ShellViewModel()
    {
        _data = new DataService();
        _state = _data.LoadState();

        var file = _data.LoadQuests();
        foreach (var model in file.Groups)
        {
            var group = new QuestGroup(model);
            foreach (var category in group.Categories)
            {
                category.Changed += ScheduleSave;
            }

            Groups.Add(group);
        }

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _saveTimer.Tick += (_, _) => Flush();

        _japanese = _state.Japanese;
        _theme = string.IsNullOrWhiteSpace(_state.Theme) ? "System" : _state.Theme;
        foreach (var group in Groups)
        {
            group.Japanese = _japanese;
            foreach (var category in group.Categories)
            {
                category.Japanese = _japanese;
                foreach (var item in category.Items)
                {
                    item.Completed = _state.Completed.Contains(item.Key);
                }
            }
        }

        // Restore the previously viewed group/category.
        SelectedGroup = Groups.FirstOrDefault(g => g.Key == _state.Group) ?? Groups.FirstOrDefault();
        SelectedCategory = SelectedGroup?.Categories.FirstOrDefault(c => c.Key == _state.Category)
            ?? SelectedGroup?.Categories.FirstOrDefault();

        _questMap = VillageQuestMap.Build(file);
        _townMap = TownQuestMap.Build(file);

        // Reopen the last used save so the slot dropdown (and the quest marks
        // it drives) is populated without the user having to click again.
        var lastPath = _state.LastSavePath;
        if (!string.IsNullOrWhiteSpace(lastPath) && File.Exists(lastPath) && LoadSave(lastPath) &&
            !string.IsNullOrWhiteSpace(_state.LastSlotName))
        {
            var preferred = Slots.FirstOrDefault(s =>
                string.Equals(s.Name, _state.LastSlotName, StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
            {
                SelectedSlot = preferred;
            }
        }
    }

    public bool Japanese
    {
        get => _japanese;
        set
        {
            if (_japanese == value)
            {
                return;
            }

            _japanese = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(AppTitle));
            OnPropertyChanged(nameof(OverallCaption));
            OnPropertyChanged(nameof(LanguageCaption));
            OnPropertyChanged(nameof(ThemeCaption));
            OnPropertyChanged(nameof(ThemeLabel));
            OnPropertyChanged(nameof(FooterText));
            OnPropertyChanged(nameof(FooterUrl));
            OnPropertyChanged(nameof(FooterTooltip));
            OnPropertyChanged(nameof(SearchHint));
            OnPropertyChanged(nameof(LoadSaveLabel));
            OnPropertyChanged(nameof(SaveSlotLabel));
            foreach (var slot in Slots)
            {
                slot.Japanese = value;
                OnPropertyChanged(nameof(Slots));
            }
            foreach (var group in Groups)
            {
                group.Japanese = value;
            }

            ScheduleSave();
        }
    }

    public string Theme
    {
        get => _theme;
        set
        {
            if (_theme == value)
            {
                return;
            }

            _theme = string.IsNullOrWhiteSpace(value) ? "System" : value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeLabel));
            ScheduleSave();
        }
    }

    public QuestGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (_selectedGroup == value)
            {
                return;
            }

            _selectedGroup = value;
            OnPropertyChanged();
            SelectedCategory = value?.Categories.FirstOrDefault();
            ScheduleSave();
        }
    }

    public CategoryTab? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
            {
                return;
            }

            _selectedCategory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            ApplySearch();
            ScheduleSave();
        }
    }

    public bool HasSelection => _selectedCategory != null;

    public bool HasSlots => Slots.Count > 0;

    public SaveSlot? SelectedSlot
    {
        get => _selectedSlot;
        set
        {
            if (ReferenceEquals(_selectedSlot, value))
            {
                return;
            }

            _selectedSlot = value;
            OnPropertyChanged();
            ApplySlot(value);
        }
    }

    /// <summary>.psu files available in the saves folder, sorted by name.</summary>
    public List<string> FindSaveFiles()
    {
        try
        {
            Directory.CreateDirectory(_data.SavesDir);
            return Directory.EnumerateFiles(_data.SavesDir, "*.psu")
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Decrypts the save in memory and fills the slot dropdown.</summary>
    public bool LoadSave(string path)
    {
        List<SaveSlot> slots;
        try
        {
            slots = Mh2SaveCodec.ReadSlots(File.ReadAllBytes(path));
        }
        catch
        {
            slots = new List<SaveSlot>();
        }

        Slots.Clear();
        foreach (var slot in slots)
        {
            slot.Japanese = _japanese;
            Slots.Add(slot);
        }

        if (slots.Count > 0)
        {
            _state.LastSavePath = path;
            ScheduleSave();
        }

        OnPropertyChanged(nameof(HasSlots));
        SelectedSlot = slots.FirstOrDefault();
        return slots.Count > 0;
    }

    /// <summary>Marks every quest the selected character has completed (village + Town bitfields).</summary>
    private void ApplySlot(SaveSlot? slot)
    {
        if (slot == null)
        {
            return;
        }

        _state.LastSlotName = slot.Name;
        ApplyBits(slot, "offline", _questMap);
        ApplyBits(slot, "online", _townMap);
        ScheduleSave();
    }

    private void ApplyBits(SaveSlot slot, string groupKey, List<VillageQuestMap.QuestBit> map)
    {
        if (map.Count == 0)
        {
            return;
        }

        var group = Groups.FirstOrDefault(g => g.Key == groupKey);
        if (group == null)
        {
            return;
        }

        var categories = group.Categories.ToDictionary(c => c.Key);
        foreach (var quest in map)
        {
            if (!categories.TryGetValue(quest.CategoryKey, out var category))
            {
                continue;
            }

            var item = category.AllItems.FirstOrDefault(i =>
                string.Equals(i.Source.Hex, quest.Hex, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Completed = slot.IsQuestCompleted(quest.Bit);
            }
        }
    }

    public string LoadSaveLabel => _japanese ? "セーブ読込" : "Load Save";

    public string SaveSlotLabel => _japanese ? "スロット" : "Slot";

    public string LanguageLabel => _japanese ? "日本語" : "English";

    public string AppTitle => _japanese
        ? "モンスターハンター2 (DOS) — クエストチェックリスト"
        : "Monster Hunter 2 (DOS) — Quest Checklist";

    public string OverallCaption => _japanese ? "   全体: " : "   Overall: ";

    public string LanguageCaption => _japanese ? "言語: " : "Language: ";

    public string ThemeCaption => _japanese ? "テーマ: " : "Theme: ";

    public string ThemeLabel => _theme switch
    {
        "Light" => _japanese ? "ライト" : "Light",
        "Dark" => _japanese ? "ダーク" : "Dark",
        _ => _japanese ? "システム" : "System"
    };

    public string FooterText => _japanese
        ? "情報: https://mh2kaiseki.web.fc2.com/index.html"
        : "Source: https://wiki.mholdschool.com/mh2/quests";

    public string FooterUrl => _japanese
        ? "https://mh2kaiseki.web.fc2.com/index.html"
        : "https://wiki.mholdschool.com/mh2/quests";

    public string FooterTooltip => _japanese
        ? "クリックしてブラウザで開く"
        : "Click to open in browser";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchHint));
            ApplySearch();
        }
    }

    public string SearchHint => string.IsNullOrWhiteSpace(_searchText)
        ? (_japanese
            ? "検索: 名前/季節/場所/受注条件/目的 (カンマ区切り、部分一致)"
            : "Search: name/season/locale/requirements/goals (comma separated, partial match)")
        : "";

    public string OverallProgress
    {
        get
        {
            var done = Groups.Sum(g => g.CompletedCount);
            var total = Groups.Sum(g => g.TotalCount);
            return $"{done}/{total}";
        }
    }

    private void ApplySearch()
    {
        if (SelectedCategory == null)
        {
            return;
        }

        var terms = (_searchText ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToArray();

        var all = SelectedCategory.AllItems;
        if (all.Count == 0)
        {
            return;
        }

        if (terms.Length == 0)
        {
            SelectedCategory.FilteredItems = null;
            SelectedCategory.Items.Clear();
            foreach (var it in all)
            {
                SelectedCategory.Items.Add(it);
            }
            SelectedCategory.RefreshStats();
            return;
        }

        var filtered = all.Where(item =>
        {
            var hay = string.Join(" | ",
                item.DisplayName ?? "",
                item.DisplaySeason ?? "",
                item.DisplayLocale ?? "",
                item.DisplayRequirements ?? "",
                item.DisplayGoals ?? "",
                item.Source.Name ?? "",
                item.Source.NameJp ?? "",
                item.Source.Season ?? "",
                item.Source.SeasonJp ?? "",
                item.Source.Locale ?? "",
                item.Source.LocaleJp ?? "",
                item.Source.Requirements ?? "",
                string.Join(" ", item.Source.Goals.Select(g => (g.En ?? "") + " " + (g.Jp ?? "")))
            ).ToLowerInvariant();

            foreach (var term in terms)
            {
                if (hay.Contains(term.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }).ToList();

        SelectedCategory.FilteredItems = filtered;
        SelectedCategory.Items.Clear();
        foreach (var it in filtered)
        {
            SelectedCategory.Items.Add(it);
        }
        SelectedCategory.RefreshStats();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
        OnPropertyChanged(nameof(OverallProgress));
    }

    public void Flush()
    {
        Flush(_state);
    }

    public void Flush(AppState state)
    {
        _saveTimer.Stop();
        state.Japanese = _japanese;
        state.Theme = _theme;
        state.Group = _selectedGroup?.Key ?? "";
        state.Category = _selectedCategory?.Key ?? "";
        state.LastSavePath = _state.LastSavePath;
        state.LastSlotName = _state.LastSlotName;
        state.Completed.Clear();
        foreach (var group in Groups)
        {
            foreach (var category in group.Categories)
            {
                foreach (var item in category.Items)
                {
                    if (item.Completed)
                    {
                        state.Completed.Add(item.Key);
                    }
                }
            }
        }

        try
        {
            _data.SaveState(state);
        }
        catch
        {
            // Never crash on a save failure; the next change retries.
        }
    }
}
