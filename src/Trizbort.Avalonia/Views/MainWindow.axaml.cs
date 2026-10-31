using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        AddHandler(InputElement.KeyDownEvent, OnModalKeyGuard, RoutingStrategies.Tunnel);
        AddHandler(InputElement.KeyDownEvent, OnCancelGestureKey, RoutingStrategies.Tunnel);
        PositionChanged += (_, _) => CaptureNormalBounds();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ClientSizeProperty) CaptureNormalBounds();
        };
        RestoreGeometry();
    }

    /// <summary>
    ///   While a modal dialog is open the main window must not act on keys (some window managers leave the
    ///   keyboard focus on the owner). Swallow them and bring the dialog forward so it receives the next ones.
    /// </summary>
    private void OnModalKeyGuard(object? sender, KeyEventArgs e)
    {
        var owned = OwnedWindows;
        if (owned.Count == 0 || e.Handled) return;
        e.Handled = true;
        owned[owned.Count - 1].Activate();
    }

    /// <summary>
    ///   Esc during a canvas drag cancels it even when keyboard focus is not on the canvas (a drag started with the
    ///   pointer must not need a second press to reach the canvas). Runs before any control sees the key.
    /// </summary>
    private void OnCancelGestureKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Escape) return;
        var canvas = this.FindControl<MapCanvasControl>("CanvasControl");
        if (canvas != null && canvas.TryCancelGesture()) e.Handled = true;
    }

    private WindowGeometry? _normalGeometry;

    /// <summary>Restores the saved size, position and maximised state, kept on a visible screen.</summary>
    private void RestoreGeometry()
    {
        try
        {
            var saved = ApplicationSettingsController.AppSettings.WindowState;
            if (saved == null) return;

            var screens = new List<ScreenRect>();
            var scaling = 1.0;
            foreach (var screen in Screens.All)
            {
                var area = screen.WorkingArea;
                screens.Add(new ScreenRect(area.X, area.Y, area.Width, area.Height));
            }

            var fitted = WindowGeometry.Fit(saved, screens);
            if (fitted == null) return;

            foreach (var screen in Screens.All)
            {
                var area = screen.WorkingArea;
                if (fitted.X >= area.X && fitted.X < area.X + area.Width &&
                    fitted.Y >= area.Y && fitted.Y < area.Y + area.Height)
                {
                    scaling = screen.Scaling;
                    break;
                }
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(fitted.X, fitted.Y);
            Width = fitted.Width / scaling;
            Height = fitted.Height / scaling;
            if (fitted.Maximised) WindowState = WindowState.Maximized;
            _normalGeometry = fitted;
        }
        catch (Exception)
        {
            // window placement is best effort
        }
    }

    /// <summary>Remembers the normal (not maximised, not minimised) bounds in pixels.</summary>
    private void CaptureNormalBounds()
    {
        if (WindowState != WindowState.Normal) return;
        var scaling = RenderScaling;
        _normalGeometry = new WindowGeometry
        {
            X = Position.X,
            Y = Position.Y,
            Width = (int)Math.Round(ClientSize.Width * scaling),
            Height = (int)Math.Round(ClientSize.Height * scaling)
        };
    }

    private void SaveGeometry()
    {
        try
        {
            if (WindowState == WindowState.Minimized) return;
            CaptureNormalBounds();
            if (_normalGeometry == null || _normalGeometry.Width <= 0 || _normalGeometry.Height <= 0) return;
            _normalGeometry.Maximised = WindowState == WindowState.Maximized;
            ApplicationSettingsController.AppSettings.WindowState = _normalGeometry;
        }
        catch (Exception)
        {
            // never block closing because the window state could not be read
        }
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

        // The mini map follows the persisted ShowMiniMap setting (View > Mini Map).
        var minimap = this.FindControl<Control>("Minimap");
        if (minimap != null)
        {
            minimap.IsVisible = session.ShowMiniMap;
            session.Changed += _ => minimap.IsVisible = session.ShowMiniMap;
        }

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

            AddRecentMapsMenu(menu, vm);
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

    private static void AddRecentMapsMenu(Menu menu, MainWindowViewModel vm)
    {
        foreach (var top in menu.Items)
        {
            if (top is MenuItem { Header: string header } fileMenu && header == "_File")
            {
                var recent = App.Services?.GetService<IRecentFilesService>();
                if (recent != null) RecentMapsMenu.InsertInto(fileMenu, recent, vm.OpenRecentMapAsync);
                return;
            }
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
            case "view.zoomIn": return vm.ZoomInCommand;
            case "view.zoomOut": return vm.ZoomOutCommand;
            case "view.resetZoom": return vm.ResetZoomCommand;
            case "view.fitToMap": return vm.FitToMapCommand;
            case "view.toggleGrid": return vm.ToggleGridCommand;
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
            if (App.FileActivation != null) await App.FileActivation.MarkReady();
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

    private void SaveSettingsOnExit()
    {
        SaveGeometry();
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
