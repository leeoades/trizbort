using System;
using System.Windows.Input;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Editor.Commands;

namespace Trizbort.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _isClosingConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
        Opened += OnWindowOpened;
    }

    private bool _startupActionsRun;
    private bool _commandSurfacesBuilt;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BuildCommandSurfaces();
    }

    /// <summary>Fills the menu, toolbar and key bindings from the command catalogue (once, when the view model arrives).</summary>
    private void BuildCommandSurfaces()
    {
        if (_commandSurfacesBuilt || DataContext is not MainWindowViewModel vm) return;

        var session = vm.CommandService.Session;
        if (session == null) return;
        _commandSurfacesBuilt = true;

        var services = App.Services;
        var catalogue = services?.GetService<CommandCatalogue>();
        if (catalogue == null || !ReferenceEquals(services!.GetService<MainWindowViewModel>(), vm))
        {
            catalogue = AppCommands.CreateCatalogue(vm);
        }

        ICommand? CommandFor(EditorCommand command) => MapToViewModelCommand(vm, command);

        var menu = this.FindControl<Menu>("MainMenu");
        if (menu != null)
        {
            foreach (var item in CatalogueMenuBuilder.BuildMainMenu(catalogue, session, CommandFor))
            {
                menu.Items.Add(item);
            }
        }

        var toolbar = this.FindControl<StackPanel>("Toolbar");
        if (toolbar != null)
        {
            foreach (var control in CatalogueMenuBuilder.BuildToolbar(catalogue, session, CommandFor))
            {
                toolbar.Children.Add(control);
            }
        }

        foreach (var binding in CatalogueMenuBuilder.BuildKeyBindings(catalogue, session, CommandFor))
        {
            KeyBindings.Add(binding);
        }

        var canvas = this.FindControl<MapCanvasControl>("CanvasControl");
        if (canvas != null)
        {
            canvas.Catalogue = catalogue;
        }
    }

    /// <summary>
    ///   Core commands that have a view-model counterpart run that instead, so status-bar text and the system
    ///   clipboard behave as before. Everything else runs the catalogue's own action.
    /// </summary>
    private static ICommand? MapToViewModelCommand(MainWindowViewModel vm, EditorCommand command)
    {
        switch (command.Id)
        {
            case "edit.selectAll": return vm.SelectAllCommand;
            case "edit.delete": return vm.DeleteSelectedCommand;
            case "edit.copy": return vm.CopyCommand;
            case "edit.cut": return vm.CutCommand;
            case "edit.paste": return vm.PasteCommand;
            case "view.zoomIn": return vm.ZoomInCommand;
            case "view.zoomOut": return vm.ZoomOutCommand;
            case "view.resetZoom": return vm.ResetZoomCommand;
            case "view.fitToMap": return vm.FitToMapCommand;
            case "view.toggleGrid": return vm.ToggleGridCommand;
            case "project.addRoom": return vm.AddRoomCommand;
            case "project.addLabel": return vm.AddMapLabelCommand;
            default: return null;
        }
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_startupActionsRun) return;
        _startupActionsRun = true;

        var services = App.Services;
        if (services == null || DataContext is not MainWindowViewModel vm) return;

        try
        {
            await StartupActions.RunAsync(
                vm,
                AppOptions.Current,
                services.GetRequiredService<IStorageService>(),
                services.GetRequiredService<IDialogService>(),
                this);
        }
        catch (Exception)
        {
            // startup actions must never crash the app
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosingConfirmed) return;

        if (DataContext is MainWindowViewModel vm)
        {
            if (Project.Current.IsDirty)
            {
                e.Cancel = true;
                var canClose = await vm.CanLoseProjectAsync();
                if (canClose)
                {
                    if (vm.IsAutomapActive)
                    {
                        vm.StopAutomap();
                    }
                    SaveSettingsOnExit();
                    _isClosingConfirmed = true;
                    Close();
                }
            }
            else
            {
                if (vm.IsAutomapActive)
                {
                    vm.StopAutomap();
                }
                SaveSettingsOnExit();
            }
        }
    }

    private static void SaveSettingsOnExit()
    {
        try
        {
            ApplicationSettingsController.SaveSettings();
        }
        catch (Exception)
        {
            // never block closing because settings could not be written
        }
    }
}
