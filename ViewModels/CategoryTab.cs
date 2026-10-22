using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.ViewModels;

public sealed class CategoryTab : ObservableObject
{
    private bool _japanese;
    private string? _sortColumn;
    private bool _sortAscending = true;

    public Category Source { get; }

    public ObservableCollection<QuestItem> Items { get; } = new();
    public List<QuestItem> AllItems { get; } = new();
    public List<QuestItem>? FilteredItems { get; set; }

    public string Key => Source.Key;

    public CategoryTab(Category source)
    {
        Source = source;
        var index = 0;
        foreach (var quest in source.Quests)
        {
            var item = new QuestItem(quest)
            {
                Key = $"{source.Key}/{quest.Hex}/{index}"
            };
            item.PropertyChanged += OnItemChanged;
            Items.Add(item);
            AllItems.Add(item);
            index++;
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
            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(ColCheck));
            OnPropertyChanged(nameof(ColNo));
            OnPropertyChanged(nameof(ColStars));
            OnPropertyChanged(nameof(ColQuest));
            OnPropertyChanged(nameof(ColSeason));
            OnPropertyChanged(nameof(ColFee));
            OnPropertyChanged(nameof(ColReward));
            OnPropertyChanged(nameof(ColHrp));
            OnPropertyChanged(nameof(ColTime));
            OnPropertyChanged(nameof(ColLocale));
            OnPropertyChanged(nameof(ColRequirements));
            OnPropertyChanged(nameof(ColGoals));
        foreach (var item in AllItems)
        {
            item.Japanese = value;
            item.RefreshDisplay();
        }
        foreach (var item in Items)
        {
            item.Japanese = value;
            item.RefreshDisplay();
        }
    }
    }

    public string Header => Japanese ? Source.NameJp : Source.Name;

    public string ColCheck => "✓";

    public string ColNo => "No.";

    public string ColStars => "★";

    public string ColQuest => Japanese ? "クエスト名" : "Quest";

    public string ColSeason => Japanese ? "季節" : "Season";

    public string ColFee => Japanese ? "受注金" : "Fee";

    public string ColReward => Japanese ? "報酬" : "Reward";

    public string ColHrp => "HRP";

    public string ColTime => Japanese ? "制限時間" : "Time";

    public string ColLocale => Japanese ? "場所" : "Locale";

    public string ColRequirements => Japanese ? "受注条件" : "Requirements";

    public string ColGoals => Japanese ? "目的" : "Goals";

    public string ArrowCheck => Arrow("check");

    public string ArrowNo => Arrow("no");

    public string ArrowStars => Arrow("stars");

    public string ArrowQuest => Arrow("name");

    public string ArrowSeason => Arrow("season");

    public string ArrowFee => Arrow("fee");

    public string ArrowReward => Arrow("reward");

    public string ArrowHrp => Arrow("hrp");

    public string ArrowTime => Arrow("time");

    public string ArrowLocale => Arrow("locale");

    public string ArrowRequirements => Arrow("requirements");

    public string ArrowGoals => Arrow("goals");

    /// <summary>Sort arrow for the given column, shown only when it is the active sort.</summary>
    private string Arrow(string column)
        => _sortColumn == column ? (_sortAscending ? "▲" : "▼") : "";

    private void NotifyColumns()
    {
        OnPropertyChanged(nameof(ColCheck));
        OnPropertyChanged(nameof(ColNo));
        OnPropertyChanged(nameof(ColStars));
        OnPropertyChanged(nameof(ColQuest));
        OnPropertyChanged(nameof(ColSeason));
        OnPropertyChanged(nameof(ColFee));
        OnPropertyChanged(nameof(ColReward));
        OnPropertyChanged(nameof(ColHrp));
        OnPropertyChanged(nameof(ColTime));
        OnPropertyChanged(nameof(ColLocale));
        OnPropertyChanged(nameof(ColRequirements));
        OnPropertyChanged(nameof(ColGoals));
        OnPropertyChanged(nameof(ArrowCheck));
        OnPropertyChanged(nameof(ArrowNo));
        OnPropertyChanged(nameof(ArrowStars));
        OnPropertyChanged(nameof(ArrowQuest));
        OnPropertyChanged(nameof(ArrowSeason));
        OnPropertyChanged(nameof(ArrowFee));
        OnPropertyChanged(nameof(ArrowReward));
        OnPropertyChanged(nameof(ArrowHrp));
        OnPropertyChanged(nameof(ArrowTime));
        OnPropertyChanged(nameof(ArrowLocale));
        OnPropertyChanged(nameof(ArrowRequirements));
        OnPropertyChanged(nameof(ArrowGoals));
    }

    /// <summary>Sorts the items in place by the given column key.</summary>
    public void SortBy(string key, bool ascending)
    {
        _sortColumn = key;
        _sortAscending = ascending;
        NotifyColumns();

        var source = FilteredItems ?? (IEnumerable<QuestItem>)Items;
        System.Collections.Generic.IEnumerable<QuestItem> ordered = key switch
        {
            "check" => ascending
                ? source.OrderBy(i => i.Completed).ThenBy(i => i.SortStars)
                : source.OrderByDescending(i => i.Completed).ThenBy(i => i.SortStars),
            "no" => Order(source, i => i.SortNumber, ascending),
            "stars" => Order(source, i => i.SortStars, ascending),
            "name" => Order(source, i => i.SortName, ascending),
            "season" => Order(source, i => i.SortSeason, ascending),
            "fee" => Order(source, i => i.SortFee, ascending),
            "reward" => Order(source, i => i.SortReward, ascending),
            "hrp" => Order(source, i => i.SortHrp, ascending),
            "time" => Order(source, i => i.SortTime, ascending),
            "locale" => Order(source, i => i.SortLocale, ascending),
            "requirements" => Order(source, i => i.SortRequirements, ascending),
            "goals" => Order(source, i => i.SortGoals, ascending),
            _ => source,
        };

        var list = ordered.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var current = Items.IndexOf(list[i]);
            if (current != i)
            {
                Items.Move(current, i);
            }
        }

        if (FilteredItems != null)
        {
            FilteredItems = list.ToList();
        }
    }

    private static System.Collections.Generic.IEnumerable<QuestItem> Order<TKey>(
        System.Collections.Generic.IEnumerable<QuestItem> items,
        System.Func<QuestItem, TKey> selector,
        bool ascending)
        => ascending ? items.OrderBy(selector) : items.OrderByDescending(selector);

    public int CompletedCount => (FilteredItems ?? (IEnumerable<QuestItem>)Items).Count(i => i.Completed);

    public int TotalCount => (FilteredItems ?? (IEnumerable<QuestItem>)Items).Count();

    public bool IsComplete => TotalCount > 0 && CompletedCount == TotalCount;

    public string CompletionMark => IsComplete ? "✔" : "";

    public string Progress => $"{CompletedCount}/{TotalCount}";

    public void RefreshStats()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(IsComplete));
        OnPropertyChanged(nameof(CompletionMark));
        OnPropertyChanged(nameof(Progress));
    }

    // Raised when a checkbox flips, so the shell can persist state.
    public event Action? Changed;

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestItem.Completed))
        {
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsComplete));
            OnPropertyChanged(nameof(CompletionMark));
            Changed?.Invoke();
        }
    }
}
