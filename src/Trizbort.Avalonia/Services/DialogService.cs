using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
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
            Width = 420,
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
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
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

    public async Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message)
    {
        var result = ConfirmationResult.Cancel;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 160,
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
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        Grid.SetRow(buttonPanel, 1);

        var saveButton = new Button { Content = "Save", Width = 80 };
        saveButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Save;
            dialog.Close();
        };

        var discardButton = new Button { Content = "Don't Save", Width = 95 };
        discardButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Discard;
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 80 };
        cancelButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Cancel;
            dialog.Close();
        };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(discardButton);
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
        var viewModel = scope.ServiceProvider.GetRequiredService<RoomPropertiesViewModel>();
        viewModel.Initialize(room);

        var dialog = new RoomPropertiesDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyTo(room);
            Project.Current.IsDirty = true;
        }
        return result;
    }

    public async Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<ConnectionPropertiesViewModel>();
        viewModel.Initialize(connection);

        var dialog = new ConnectionPropertiesDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyTo(connection);
            Project.Current.IsDirty = true;
        }
        return result;
    }

    public async Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapLabelPropertiesViewModel>();
        viewModel.Initialize(label);

        var dialog = new MapLabelPropertiesDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyTo(label);
            Project.Current.IsDirty = true;
        }
        return result;
    }

    public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element)
    {
        return element switch
        {
            Room room => ShowRoomPropertiesAsync(owner, room),
            Connection connection => ShowConnectionPropertiesAsync(owner, connection),
            MapLabel label => ShowMapLabelPropertiesAsync(owner, label),
            _ => Task.FromResult(false)
        };
    }

    public async Task<bool> ShowMapSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapSettingsViewModel>();
        viewModel.Initialize();

        var dialog = new MapSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyToSettings();
        }
        return result;
    }

    public async Task<bool> ShowMapThemeAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapThemeViewModel>();
        viewModel.Initialize();

        var dialog = new MapThemeDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplySelectedTheme();
        }
        return result;
    }

    public async Task<bool> ShowAppSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<AppSettingsViewModel>();
        viewModel.Initialize();

        var dialog = new AppSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyToSettings();
        }
        return result;
    }

    public async Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<AutomapSettingsViewModel>();
        var currentSettings = ApplicationSettingsController.AppSettings.Automap;
        viewModel.Initialize(currentSettings);

        var dialog = new AutomapSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            var settings = viewModel.GetSettings();
            if (!string.IsNullOrWhiteSpace(settings.FileName))
            {
                ApplicationSettingsController.AppSettings.Automap = settings;
                ApplicationSettingsController.SaveSettings();
            }
            return settings;
        }
        return null;
    }
}
