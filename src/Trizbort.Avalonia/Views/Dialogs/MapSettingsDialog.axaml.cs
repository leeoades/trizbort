using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class MapSettingsDialog : Window
{
    public MapSettingsDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MapSettingsViewModel vm && !vm.Validate())
        {
            if (vm.HasRegionErrors)
            {
                // Show the Regions tab (the last one) and focus the offending name, as the reference does.
                var regionTabs = this.FindControl<TabControl>("Tabs");
                if (regionTabs != null) regionTabs.SelectedIndex = regionTabs.ItemCount - 1;
                var nameBox = this.FindControl<TextBox>("RegionNameBox");
                nameBox?.Focus();
                nameBox?.SelectAll();
                return;
            }

            // Show the tab holding the default room text (Rooms & Editing) and focus the field, as the reference does.
            var tabs = this.FindControl<TabControl>("Tabs");
            if (tabs != null) tabs.SelectedIndex = 2;
            var box = this.FindControl<TextBox>("DefaultRoomNameBox");
            box?.Focus();
            box?.SelectAll();
            return;
        }

        SaveLastTab();
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SaveLastTab();
        Close(false);
    }

    private void SaveLastTab()
    {
        if (DataContext is MapSettingsViewModel vm) vm.SaveLastTab();
    }
}
