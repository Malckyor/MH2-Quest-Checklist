using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MH2QuestChecklist.Services;

namespace MH2QuestChecklist.ViewModels;

/// <summary>
/// Modal picker shown when more than one .psu file exists in the saves folder.
/// Pure code-built window so it follows the app theme resources.
/// </summary>
public static class SavePickerWindow
{
    public static string? Pick(Window owner, IReadOnlyList<string> files)
    {
        if (files.Count == 0)
        {
            return null;
        }

        string? chosen = null;
        bool japanese = false;
        try
        {
            var ds = new DataService();
            var state = ds.LoadState();
            japanese = state.Japanese;
        }
        catch { }

        var list = new ListBox
        {
            Margin = new Thickness(12),
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
        };
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            var item = new ListBoxItem
            {
                Content = name,
                Tag = file,
                Padding = new Thickness(10, 6, 10, 6),
                Background = Brushes.Transparent,
                Foreground = (Brush?)Application.Current?.Resources["TextPrimary"] ?? Brushes.White,
            };
            item.SetResourceReference(Control.ForegroundProperty, "TextPrimary");
            list.Items.Add(item);
        }

        StyleListItem(list);
        var ok = new Button { Content = "OK", Padding = new Thickness(18, 6, 18, 6), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = japanese ? "キャンセル" : "Cancel", Padding = new Thickness(18, 6, 18, 6), IsCancel = true };
        StyleButton(ok);
        StyleButton(cancel);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12),
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var panel = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        panel.Children.Add(buttons);
        panel.Children.Add(list);

        var window = new Window
        {
            Owner = owner,
            Title = japanese ? "セーブ読込" : "Load Save",
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 320,
            MaxWidth = 480,
            MaxHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Content = panel,
            Background = (Brush?)Application.Current?.Resources["WindowBackground"] ?? Brushes.Black,
        };
        try { window.SetResourceReference(Panel.BackgroundProperty, "WindowBackground"); } catch { }

        window.Loaded += (_, _) => ApplyDarkTitleBar(window);

        if (list.Items.Count > 0)
        {
            list.SelectedIndex = 0;
            ((ListBoxItem)list.Items[0]!).Focus();
        }

        ok.Click += (_, _) =>
        {
            chosen = list.SelectedItem is ListBoxItem item ? item.Tag as string : null;
            window.Close();
        };
        cancel.Click += (_, _) => window.Close();
        list.MouseDoubleClick += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject source && Ancestor<ListBoxItem>(source) is { } item)
            {
                chosen = item.Tag as string;
                window.Close();
            }
        };
        list.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                chosen = list.SelectedItem is ListBoxItem item ? item.Tag as string : null;
                window.Close();
                e.Handled = true;
            }
        };

        window.ShowDialog();
        return chosen;
    }

    private static void StyleButton(Button button)
    {
        button.Background = (Brush?)Application.Current?.Resources["ButtonBackground"] ?? Brushes.Gray;
        button.Foreground = (Brush?)Application.Current?.Resources["TextPrimary"] ?? Brushes.White;
        button.BorderBrush = (Brush?)Application.Current?.Resources["BorderSecondary"] ?? Brushes.DarkGray;
        button.Cursor = Cursors.Hand;
        button.SetResourceReference(Control.BackgroundProperty, "ButtonBackground");
        button.SetResourceReference(Control.ForegroundProperty, "TextPrimary");
        button.SetResourceReference(Control.BorderBrushProperty, "BorderSecondary");
    }

    private static void StyleListItem(ListBox list)
    {
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Application.Current?.Resources["TextPrimary"] ?? Brushes.White));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 6, 10, 6)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        var template = new ControlTemplate(typeof(ListBoxItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Bd";
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, Application.Current?.Resources["HoverBackground"] ?? Brushes.Gray, "Bd"));
        template.Triggers.Add(hover);

        var selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, Application.Current?.Resources["SelectedBackground"] ?? Brushes.DarkGray, "Bd"));
        template.Triggers.Add(selected);

        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        list.ItemContainerStyle = style;
    }

    private static T? Ancestor<T>(DependencyObject current) where T : DependencyObject
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

    private static void ApplyDarkTitleBar(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            var size = Marshal.SizeOf(enabled);

            if (DwmSetWindowAttribute(handle, 20, ref enabled, size) != 0)
            {
                DwmSetWindowAttribute(handle, 19, ref enabled, size);
            }
        }
        catch
        {
        }
    }

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
