using System.Collections.ObjectModel;
using System.Linq;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.ViewModels;

/// <summary>One of the three top-level tabs: Village (Offline), Town (Online), Events.</summary>
public sealed class QuestGroup : ObservableObject
{
    private bool _japanese;

    public string Key { get; }

    public ObservableCollection<CategoryTab> Categories { get; } = new();

    public QuestGroup(QuestGroupModel source)
    {
        Key = source.Key;
        Name = source.Name;
        NameJp = source.NameJp;
        foreach (var category in source.Categories)
        {
            var tab = new CategoryTab(category);
            tab.Changed += OnCategoryChanged;
            Categories.Add(tab);
        }
    }

    private void OnCategoryChanged()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(IsComplete));
        OnPropertyChanged(nameof(CompletionMark));
    }

    public bool IsComplete => TotalCount > 0 && CompletedCount == TotalCount;

    public string CompletionMark => IsComplete ? "✔" : "";

    public string Name { get; }

    public string NameJp { get; }

    public string Header => _japanese ? NameJp : Name;

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
            foreach (var category in Categories)
            {
                category.Japanese = value;
            }
        }
    }

    public int CompletedCount => Categories.Sum(c => c.CompletedCount);

    public int TotalCount => Categories.Sum(c => c.TotalCount);

    public string Progress => $"{CompletedCount}/{TotalCount}";
}
