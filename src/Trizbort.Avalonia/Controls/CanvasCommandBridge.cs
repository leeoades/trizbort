using System.Windows.Input;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Elements;
using Trizbort.Editor.Commands;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Connects <see cref="MapCanvasControl" /> to the command catalogue: finds the catalogue, runs canvas-scope key
///   gestures, and supplies the canvas-specific actions (click position, system clipboard) for the context menu.
/// </summary>
internal static class CanvasCommandBridge
{
    private static CommandCatalogue? _fallbackCatalogue;

    /// <summary>The catalogue set by the host, else the application's, else a definitions-only default.</summary>
    public static CommandCatalogue Resolve(MapCanvasControl canvas)
    {
        return Find(canvas) ?? (_fallbackCatalogue ??= AppCommands.CreateCatalogue(null));
    }

    private static CommandCatalogue? Find(MapCanvasControl canvas)
    {
        if (canvas.Catalogue != null) return canvas.Catalogue;
        var services = App.Services;
        return services?.GetService<CommandCatalogue>();
    }

    /// <summary>Runs the canvas-scope command bound to the pressed key, if any. Returns true when it handled the key.</summary>
    public static bool TryRunCanvasGesture(MapCanvasControl canvas, KeyEventArgs e)
    {
        var session = canvas.CommandService?.Session;
        if (session == null) return false;
        var catalogue = Resolve(canvas);

        var input = AvaloniaKeyMapper.ToKeyInput(e.Key, e.KeyModifiers, AvaloniaKeyMapper.IsMacOS);
        var command = catalogue.FindByGesture(input, CommandScope.Canvas);
        if (command == null || !command.CanExecute(session)) return false;

        _ = command.ExecuteAsync(session);
        return true;
    }

    /// <summary>Canvas-specific execution for a context-menu command, or null to run the catalogue's own action.</summary>
    public static ICommand? ContextCommandFor(MapCanvasControl canvas, EditorCommand command, CoreVector worldPos, Element? activeElement)
    {
        switch (command.Id)
        {
            case "edit.properties":
                if (activeElement == null) return null;
                return new RelayCommand(() => { _ = canvas.ShowPropertiesDialogAsync(activeElement); });
            case "edit.cut":
                return new RelayCommand(() => { _ = canvas.CutSelectedToClipboardAsync(); });
            case "edit.copy":
                return new RelayCommand(() => { _ = canvas.CopySelectedToClipboardAsync(); });
            case "edit.delete":
                return new RelayCommand(() => { canvas.CommandService?.DeleteSelected(); });
            case "edit.paste":
                return new RelayCommand(() => { _ = canvas.PasteFromClipboardAsync(); });
            case "project.addRoom":
                return new RelayCommand(() => { canvas.CommandService?.AddRoom(worldPos); });
            case "project.addLabel":
                return new RelayCommand(() => { canvas.CommandService?.AddMapLabel(worldPos); });
            case "edit.mapSettings":
                return new RelayCommand(() => { _ = canvas.ShowMapSettingsDialogAsync(); });
            case "edit.mapTheme":
                return new RelayCommand(() => { _ = canvas.ShowMapThemeDialogAsync(); });
            case "edit.appSettings":
                return new RelayCommand(() => { _ = canvas.ShowAppSettingsDialogAsync(); });
            default:
                return null;
        }
    }
}
