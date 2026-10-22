using System;
using System.Windows;
using Microsoft.Win32;
using MH2QuestChecklist.Models;
using MH2QuestChecklist.Services;

namespace MH2QuestChecklist;

public partial class App : Application
{
    private void ApplyTheme(string theme)
    {
        var dictionaries = Resources.MergedDictionaries;
        dictionaries.Clear();

        var effective = theme;
        if (string.IsNullOrWhiteSpace(effective) || effective == "System")
        {
            effective = IsSystemDark() ? "Dark" : "Light";
        }

        var source = effective == "Light" ? "Themes/Light.xaml" : "Themes/Dark.xaml";
        dictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i)
            {
                return i == 0;
            }
        }
        catch
        {
        }

        return true;
    }

    public void SetTheme(string theme)
    {
        var state = new AppState { Theme = theme };
        try
        {
            var service = new DataService();
            var existing = service.LoadState();
            existing.Theme = theme;
            state = existing;
        }
        catch
        {
        }

        ApplyTheme(theme);
        try
        {
            new DataService().SaveState(state);
        }
        catch
        {
        }
    }

    public void InitializeTheme(string theme)
    {
        ApplyTheme(theme);
    }
}
