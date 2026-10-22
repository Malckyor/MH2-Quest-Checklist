using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MH2QuestChecklist.Models;
using MH2QuestChecklist.Services;
using MH2QuestChecklist.ViewModels;

namespace MH2QuestChecklist;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _vm = new();
    private readonly AppState _windowState;

    private static readonly string[] ColumnKeys =
    {
        "check", "no", "stars", "name", "season", "fee",
        "reward", "hrp", "time", "locale", "requirements", "goals",
    };

    private readonly System.Collections.Generic.Dictionary<CategoryTab, (string Column, bool Ascending)> _sortState = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        if (Application.Current is App app)
        {
            app.InitializeTheme(_vm.Theme);
        }

        _windowState = new DataService().LoadState();

        if (_windowState.WindowWidth is > 0)
        {
            Width = _windowState.WindowWidth.Value;
        }

        if (_windowState.WindowHeight is > 0)
        {
            Height = _windowState.WindowHeight.Value;
        }

        if (_windowState.WindowLeft is >= 0 or <= -0)
        {
            if (_windowState.WindowLeft.Value >= SystemParameters.VirtualScreenLeft - 100 &&
                _windowState.WindowLeft.Value <= SystemParameters.VirtualScreenWidth - 100)
            {
                Left = _windowState.WindowLeft.Value;
            }
        }

        if (_windowState.WindowTop is >= 0)
        {
            if (_windowState.WindowTop.Value >= SystemParameters.VirtualScreenTop - 100 &&
                _windowState.WindowTop.Value <= SystemParameters.VirtualScreenHeight - 100)
            {
                Top = _windowState.WindowTop.Value;
            }
        }

        WindowState = _windowState.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
        => ApplyDarkTitleBar();

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not GridViewColumnHeader header)
        {
            return;
        }

        var list = FindAncestor<ListView>(header);
        if (list?.DataContext is not CategoryTab tab || list.View is not GridView grid)
        {
            return;
        }

        var index = header.Column != null ? grid.Columns.IndexOf(header.Column) : -1;
        if (index < 0 || index >= ColumnKeys.Length)
        {
            return;
        }

        var column = ColumnKeys[index];
        var ascending = true;
        if (_sortState.TryGetValue(tab, out var prev) && prev.Column == column)
        {
            ascending = !prev.Ascending;
        }

        _sortState[tab] = (column, ascending);
        tab.SortBy(column, ascending);
    }

    private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
        => _vm.Japanese = !_vm.Japanese;

    private void OnLoadSaveClick(object sender, RoutedEventArgs e)
    {
        var files = _vm.FindSaveFiles();
        if (files.Count == 0)
        {
            return;
        }

        var chosen = files.Count == 1
            ? files[0]
            : SavePickerWindow.Pick(this, files);

        if (string.IsNullOrWhiteSpace(chosen))
        {
            return;
        }

        _vm.LoadSave(chosen);
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _vm.Theme = _vm.Theme switch
        {
            "Dark" => "Light",
            "Light" => "System",
            _ => "Dark"
        };

        if (Application.Current is App app)
        {
            app.InitializeTheme(_vm.Theme);
        }

        _vm.Flush();
    }

    private void Footer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var url = _vm.FooterUrl;
            if (!string.IsNullOrWhiteSpace(url))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        }
        catch
        {
        }

        e.Handled = true;
    }

    /// <summary>
    /// Clicking anywhere on a row toggles its completion, except when the click
    /// already landed on the checkbox (which handles itself).
    /// </summary>
    private void QuestList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject origin && FindAncestor<CheckBox>(origin) != null)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject src)
        {
            return;
        }

        var container = FindAncestor<ListViewItem>(src);
        if (container?.DataContext is QuestItem item)
        {
            item.Completed = !item.Completed;
            e.Handled = true;
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private void ApplyDarkTitleBar()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            var size = Marshal.SizeOf(enabled);

            // DWMWA_USE_IMMERSIVE_DARK_MODE: 20 on Win10 20H1+, 19 before.
            if (DwmSetWindowAttribute(handle, 20, ref enabled, size) != 0)
            {
                DwmSetWindowAttribute(handle, 19, ref enabled, size);
            }
        }
        catch
        {
            // Dark title bar is cosmetic; ignore failures on older systems.
        }
    }

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnClosing(CancelEventArgs e)
    {
        _windowState.WindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal)
        {
            _windowState.WindowLeft = Left;
            _windowState.WindowTop = Top;
            _windowState.WindowWidth = Width;
            _windowState.WindowHeight = Height;
        }
        else
        {
            _windowState.WindowLeft = RestoreBounds.Left;
            _windowState.WindowTop = RestoreBounds.Top;
            _windowState.WindowWidth = RestoreBounds.Width;
            _windowState.WindowHeight = RestoreBounds.Height;
        }

        _vm.Flush(_windowState);
        base.OnClosing(e);
    }
}
