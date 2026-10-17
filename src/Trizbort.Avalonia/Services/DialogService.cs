using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Views;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Services;

public class DialogService(IServiceScopeFactory serviceScopeFactory) : IDialogService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public async Task ShowAboutDialogAsync(Window? owner)
    {
        var dialog = new AboutDialog();
        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    public async Task ShowMessageAsync(Window? owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 80
        };
        okButton.Click += (_, _) => dialog.Close();
        panel.Children.Add(okButton);

        dialog.Content = panel;

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    public async Task<bool> ShowConfirmationAsync(Window? owner, string title, string message)
    {
        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var grid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        grid.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        Grid.SetRow(buttonPanel, 1);

        var okButton = new Button
        {
            Content = "Yes",
            Width = 75
        };
        okButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        var cancelButton = new Button
        {
            Content = "No",
            Width = 75
        };
        cancelButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return result;
    }

    public async Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        // In Step 4, we will instantiate RoomPropertiesViewModel from the scoped container
        await ShowMessageAsync(owner, "Room Properties", $"Properties for room: {room.Name}");
        return false;
    }

    public async Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await ShowMessageAsync(owner, "Connection Properties", $"Properties for connection: {connection.Name}");
        return false;
    }

    public async Task<bool> ShowMapSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await ShowMessageAsync(owner, "Map Settings", "Map Settings dialog");
        return false;
    }

    public async Task<bool> ShowMapThemeAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await ShowMessageAsync(owner, "Map Theme", "Map Theme dialog");
        return false;
    }

    public async Task<bool> ShowAppSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await ShowMessageAsync(owner, "App Settings", "App Settings dialog");
        return false;
    }
}
