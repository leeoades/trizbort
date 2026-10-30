using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Runs a catalogue command, or an optional host-supplied inner command (for example a view-model command that also
///   updates the status bar). Enabled state always comes from the catalogue.
/// </summary>
internal sealed class CatalogueCommandAdapter : ICommand
{
    private readonly EditorCommand _command;
    private readonly EditorSession _session;
    private readonly ICommand? _inner;

    public CatalogueCommandAdapter(EditorCommand command, EditorSession session, ICommand? inner)
    {
        _command = command;
        _session = session;
        _inner = inner;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (!_command.CanExecute(_session)) return false;
        return _inner == null || _inner.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        if (_inner != null)
        {
            _inner.Execute(parameter);
            return;
        }

        _command.ExecuteAsync(_session).ContinueWith(
            t => Debug.WriteLine($"Command '{_command.Id}' failed: {t.Exception}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
///   Generates the main menu, context menu, toolbar and window key bindings from a <see cref="CommandCatalogue" />.
///   <c>commandFor</c> may return a host command to run instead of the catalogue's own action (or null to use it).
/// </summary>
public static class CatalogueMenuBuilder
{
    private static readonly string[] _rootOrder = { "File", "Edit", "View", "Project", "Help" };

    private sealed class Entry
    {
        public Entry(EditorCommand command, string[] segments, int index)
        {
            Command = command;
            Segments = segments;
            Index = index;
        }

        public EditorCommand Command { get; }

        public string[] Segments { get; }

        public int Index { get; }
    }

    private sealed class Node
    {
        public string Group { get; set; } = string.Empty;

        public int Index { get; set; }

        public Control Control { get; set; } = null!;
    }

    public static IReadOnlyList<MenuItem> BuildMainMenu(
        CommandCatalogue catalogue,
        EditorSession session,
        Func<EditorCommand, ICommand?>? commandFor = null)
    {
        var refreshers = new List<Action>();
        var entries = new List<Entry>();
        var index = 0;
        foreach (var command in catalogue.All)
        {
            if (!command.Hidden && !string.IsNullOrEmpty(command.MenuPath))
            {
                entries.Add(new Entry(command, command.MenuPath.Split('/'), index));
            }
            index++;
        }

        var roots = entries.Select(e => e.Segments[0]).Distinct().ToList();
        var ordered = _rootOrder.Where(roots.Contains)
            .Concat(roots.Where(r => !_rootOrder.Contains(r)))
            .ToList();

        var result = new List<MenuItem>();
        foreach (var root in ordered)
        {
            var top = new MenuItem { Header = Mnemonic(root) };
            foreach (var child in BuildLevel(entries.Where(e => e.Segments[0] == root).ToList(), 1, session, commandFor, refreshers))
            {
                top.Items.Add(child);
            }
            result.Add(top);
        }

        Track(session, refreshers);
        return result;
    }

    public static ContextMenu BuildContextMenu(
        CommandCatalogue catalogue,
        EditorSession session,
        ContextTargets target,
        Func<EditorCommand, ICommand?>? commandFor = null)
    {
        var menu = new ContextMenu();
        var commands = catalogue.ForContext(target)
            .Select((c, i) => (Command: c, Index: i))
            .OrderBy(x => x.Command.ContextOrder)
            .ThenBy(x => x.Index)
            .Select(x => x.Command);

        string? previousGroup = null;
        foreach (var command in commands)
        {
            var group = command.ContextGroup ?? string.Empty;
            if (previousGroup != null && group != previousGroup)
            {
                menu.Items.Add(new Separator());
            }
            previousGroup = group;

            var adapter = new CatalogueCommandAdapter(command, session, commandFor?.Invoke(command));
            var item = new MenuItem { Header = command.TitleFor(session), Command = adapter };
            // Items in a closed menu are not attached yet, so do not rely on the command to set this.
            item.IsEnabled = adapter.CanExecute(null);
            menu.Items.Add(item);
        }

        return menu;
    }

    public static IReadOnlyList<Control> BuildToolbar(
        CommandCatalogue catalogue,
        EditorSession session,
        Func<EditorCommand, ICommand?>? commandFor = null)
    {
        var refreshers = new List<Action>();
        var result = new List<Control>();
        var macOS = AvaloniaKeyMapper.IsMacOS;
        string? previousGroup = null;

        var commands = catalogue.All
            .Where(c => c.ShowInToolbar && !c.Hidden)
            .Select((c, i) => (Command: c, Index: i))
            .OrderBy(x => x.Command.ToolbarOrder)
            .ThenBy(x => x.Index)
            .Select(x => x.Command);

        foreach (var command in commands)
        {
            var group = command.ToolbarGroup ?? string.Empty;
            if (previousGroup != null && group != previousGroup)
            {
                result.Add(new Border
                {
                    Width = 1,
                    Margin = new Thickness(4, 2),
                    Background = Brushes.Gray,
                    Opacity = 0.5,
                    VerticalAlignment = VerticalAlignment.Stretch
                });
            }
            previousGroup = group;

            var adapter = new CatalogueCommandAdapter(command, session, commandFor?.Invoke(command));
            var title = StripMnemonic(command.TitleFor(session));
            var tip = title;
            if (command.Gestures.Count > 0)
            {
                tip += " (" + command.Gestures[0].ToDisplayString(macOS) + ")";
            }

            var button = new Button
            {
                Command = adapter,
                Padding = new Thickness(6, 4),
                Content = CreateIcon(command.IconKey) ?? new TextBlock { Text = title }
            };
            ToolTip.SetTip(button, tip);
            AutomationProperties.SetAutomationId(button, command.Id);
            AutomationProperties.SetName(button, title);
            result.Add(button);

            refreshers.Add(() =>
            {
                adapter.RaiseCanExecuteChanged();
                button.IsEnabled = adapter.CanExecute(null);
            });
        }

        Track(session, refreshers);
        return result;
    }

    public static IReadOnlyList<KeyBinding> BuildKeyBindings(
        CommandCatalogue catalogue,
        EditorSession session,
        Func<EditorCommand, ICommand?>? commandFor = null)
    {
        var macOS = AvaloniaKeyMapper.IsMacOS;
        var result = new List<KeyBinding>();
        foreach (var command in catalogue.All)
        {
            if (command.Scope != CommandScope.Window || command.GestureDisplayOnly) continue;

            foreach (var gesture in command.Gestures)
            {
                if (!AvaloniaKeyMapper.TryToAvaloniaGesture(gesture, macOS, out var avaloniaGesture)) continue;

                result.Add(new KeyBinding
                {
                    Gesture = avaloniaGesture,
                    Command = new CatalogueCommandAdapter(command, session, commandFor?.Invoke(command))
                });
            }
        }

        return result;
    }

    private static List<Control> BuildLevel(
        List<Entry> entries,
        int level,
        EditorSession session,
        Func<EditorCommand, ICommand?>? commandFor,
        List<Action> refreshers)
    {
        var nodes = new List<Node>();

        foreach (var entry in entries.Where(e => e.Segments.Length == level))
        {
            nodes.Add(new Node
            {
                Group = GroupAt(entry.Command, level - 1),
                Index = entry.Index,
                Control = CreateMenuItem(entry.Command, session, commandFor, refreshers)
            });
        }

        var submenuNames = entries.Where(e => e.Segments.Length > level)
            .Select(e => e.Segments[level])
            .Distinct()
            .ToList();
        foreach (var name in submenuNames)
        {
            var children = entries.Where(e => e.Segments.Length > level && e.Segments[level] == name).ToList();
            var first = children
                .OrderBy(e => GroupAt(e.Command, level - 1), StringComparer.Ordinal)
                .ThenBy(e => e.Index)
                .First();

            var submenu = new MenuItem { Header = Mnemonic(name) };
            foreach (var child in BuildLevel(children, level + 1, session, commandFor, refreshers))
            {
                submenu.Items.Add(child);
            }

            nodes.Add(new Node { Group = GroupAt(first.Command, level - 1), Index = first.Index, Control = submenu });
        }

        var result = new List<Control>();
        string? previousGroup = null;
        foreach (var node in nodes.OrderBy(n => n.Group, StringComparer.Ordinal).ThenBy(n => n.Index))
        {
            if (previousGroup != null && node.Group != previousGroup)
            {
                result.Add(new Separator());
            }
            previousGroup = node.Group;
            result.Add(node.Control);
        }

        return result;
    }

    private static MenuItem CreateMenuItem(
        EditorCommand command,
        EditorSession session,
        Func<EditorCommand, ICommand?>? commandFor,
        List<Action> refreshers)
    {
        var macOS = AvaloniaKeyMapper.IsMacOS;
        var adapter = new CatalogueCommandAdapter(command, session, commandFor?.Invoke(command));
        var item = new MenuItem { Header = command.TitleFor(session), Command = adapter };

        if (command.Gestures.Count > 0 &&
            AvaloniaKeyMapper.TryToAvaloniaGesture(command.Gestures[0], macOS, out var gesture))
        {
            item.InputGesture = gesture;
        }

        if (command.IsCheckedFunc != null)
        {
            item.ToggleType = MenuItemToggleType.CheckBox;
        }

        refreshers.Add(() =>
        {
            adapter.RaiseCanExecuteChanged();
            // Items in a closed submenu are not attached yet, so do not rely on the command to set this.
            item.IsEnabled = adapter.CanExecute(null);
            if (command.IsCheckedFunc != null) item.IsChecked = command.IsChecked(session) == true;
            if (command.DynamicTitle != null) item.Header = command.TitleFor(session);
        });
        return item;
    }

    private static void Track(EditorSession session, List<Action> refreshers)
    {
        void RefreshAll()
        {
            foreach (var refresh in refreshers) refresh();
        }

        RefreshAll();
        session.Changed += _ => RefreshAll();
    }

    /// <summary>
    ///   The separator group of a command at a menu depth. A group like <c>2-export/1-image</c> has one part per
    ///   nesting level; a command with fewer parts uses its last one.
    /// </summary>
    private static string GroupAt(EditorCommand command, int depth)
    {
        var group = command.Group ?? string.Empty;
        var parts = group.Split('/');
        return depth < parts.Length ? parts[depth] : parts[parts.Length - 1];
    }

    private static string Mnemonic(string name)
    {
        return name.Contains('_') ? name : "_" + name;
    }

    private static string StripMnemonic(string title)
    {
        var text = title.Replace("_", string.Empty);
        return text.EndsWith("...", StringComparison.Ordinal) ? text.Substring(0, text.Length - 3) : text;
    }

    private static Control? CreateIcon(string? key)
    {
        if (key == null || Application.Current == null) return null;
        if (Application.Current.TryFindResource(key, out var resource) && resource is Geometry geometry)
        {
            return new PathIcon { Data = geometry, Width = 16, Height = 16 };
        }
        return null;
    }
}
