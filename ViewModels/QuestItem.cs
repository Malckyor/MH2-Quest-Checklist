using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class QuestItem : ObservableObject
{
    private bool _completed;

    public Quest Source { get; }

    public QuestItem(Quest source)
    {
        Source = source;
    }

    public bool Completed
    {
        get => _completed;
        set
        {
            if (_completed == value)
            {
                return;
            }

            _completed = value;
            OnPropertyChanged();
        }
    }

    public bool Japanese { get; set; }

    public string DisplayName => Japanese ? Source.NameJp : Source.Name;

    public string DisplayStars => new string('★', Source.Stars);

    public string DisplaySeason => Japanese ? Source.SeasonJp : Source.Season;

    public string DisplayLocale => Japanese ? Source.LocaleJp : Source.Locale;

    public string DisplayGoals
    {
        get
        {
            IEnumerable<string> items = Japanese
                ? Source.Goals.Select(g => g.Jp)
                : Source.Goals.Select(g => g.En);
            return string.Join("\n", items.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }

    public string DisplayReward => FormatMoney(Source.Reward);

    public string DisplayHrp => FormatPlain(Source.Hrp);

    public string DisplayFee => Source.Fee > 0 ? Source.Fee.ToString("N0") + "z" : "0z";

    public string DisplayTime => Source.Time > 0 ? Source.Time + " " + (Japanese ? "分" : "min") : "";

    public string DisplayRequirements => Source.Requirements;

    private static string FormatMoney(List<int> values)
        => string.Join("\n", values.Select(v => v.ToString("N0") + "z"));

    private static string FormatPlain(List<int> values)
        => string.Join("\n", values.Select(v => v.ToString()));

    public string Key { get; set; } = "";

    public int SortStars => Source.Stars;

    public int SortFee => Source.Fee;

    public int SortReward => Source.Reward.Count > 0 ? Source.Reward.Max() : 0;

    public int SortHrp => Source.Hrp.Count > 0 ? Source.Hrp.Max() : 0;

    public int SortTime => Source.Time;

    public string SortNumber => Source.Hex;

    public string SortName => Japanese ? Source.NameJp : Source.Name;

    public string SortSeason => Japanese ? Source.SeasonJp : Source.Season;

    public string SortLocale => Japanese ? Source.LocaleJp : Source.Locale;

    public string SortGoals => GoalTarget();

    /// <summary>
    /// Sort key for the Goals column: the target monster/item of the first goal,
    /// with the action verb stripped (e.g. "Hunt the Rathalos" -> "Rathalos").
    /// </summary>
    private string GoalTarget()
    {
        if (Source.Goals.Count == 0)
        {
            return "";
        }

        var goal = Source.Goals[0];
        var text = Japanese ? goal.Jp : goal.En;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Trim();

        if (Japanese)
        {
            // e.g. "リオレウスを狩猟" -> "リオレウス"
            var marker = text.IndexOf('を');
            return marker > 0 ? text[..marker] : text;
        }

        foreach (var verb in new[] { "Hunt the ", "Slay the ", "Defeat the ", "Repel the ", "Capture the ", "Kill the ", "Hunt ", "Slay ", "Defeat ", "Deliver " })
        {
            if (text.StartsWith(verb, System.StringComparison.OrdinalIgnoreCase))
            {
                return text[verb.Length..].Trim();
            }
        }

        return text;
    }

    public string SortRequirements => Source.Requirements;

    /// <summary>Re-raises change notifications so the grid refreshes on language toggle.</summary>
    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplaySeason));
        OnPropertyChanged(nameof(DisplayLocale));
        OnPropertyChanged(nameof(DisplayGoals));
        OnPropertyChanged(nameof(DisplayTime));
        OnPropertyChanged(nameof(DisplayRequirements));
    }
}
