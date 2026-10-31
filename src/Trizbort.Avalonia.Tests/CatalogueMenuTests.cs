using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using CoreModifierKeys = Trizbort.Domain.Enums.ModifierKeys;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class CatalogueMenuTests
{
    private Project? _previousProject;
    private CommandService? _service;
    private CommandCatalogue? _catalogue;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _service = new CommandService();
        _catalogue = AppCommands.CreateCatalogue(null);
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private IReadOnlyList<MenuItem> BuildMenu() => CatalogueMenuBuilder.BuildMainMenu(_catalogue!, _service!.Session);

    private static string Plain(object? header) => (header?.ToString() ?? string.Empty).Replace("_", string.Empty);

    private static MenuItem Find(IEnumerable<object?> items, string plainHeader) =>
        items.OfType<MenuItem>().First(m => Plain(m.Header) == plainHeader || Plain(m.Header).StartsWith(plainHeader + " "));

    [AvaloniaTest]
    public void MainMenu_HasFileEditViewProjectHelpTopLevel()
    {
        BuildMenu().Select(m => Plain(m.Header)).ShouldBe(new[] { "File", "Edit", "View", "Project", "Help" });
    }

    [AvaloniaTest]
    public void MainMenu_FileMenu_ContainsExpectedItemsInOrder()
    {
        var file = Find(BuildMenu(), "File");
        var entries = file.Items.OfType<Control>()
            .Select(c => c is Separator ? "-" : Plain((c as MenuItem)!.Header))
            .ToList();
        // Reference grouping (legacy MainForm File menu): New | Open, Open from Web | Save, Save As, Smart Save | Backup (new) | Export | Exit.
        entries.ShouldBe(new[] { "New Map", "-", "Open Map...", "Open Map from Web...", "-", "Save Map", "Save Map As...", "Smart Save", "-", "Backup", "-", "Export", "-", "Exit" });

        var export = Find(file.Items, "Export");
        export.Items.OfType<Control>()
            .Select(c => c is Separator ? "-" : Plain((c as MenuItem)!.Header))
            .ShouldBe(new[]
            {
                "Image...", "-", "Inform 7 Source...", "Inform 6 Source...", "TADS Source...", "Hugo Source...",
                "ZIL Source...", "Quest Source...", "Alan Source...", "Adventuron Source...", "-", "To Clipboard"
            });
    }

    [AvaloniaTest]
    public void MainMenu_EditMenu_HasGesturesShown()
    {
        var edit = Find(BuildMenu(), "Edit");
        var modifiers = AvaloniaKeyMapper.ToKeyModifiers(CoreModifierKeys.Primary, AvaloniaKeyMapper.IsMacOS);
        Find(edit.Items, "Select All").InputGesture.ShouldBe(new global::Avalonia.Input.KeyGesture(Key.A, modifiers));
        Find(edit.Items, "Copy").InputGesture.ShouldBe(new global::Avalonia.Input.KeyGesture(Key.C, modifiers));
        Find(edit.Items, "Delete").InputGesture.ShouldBe(new global::Avalonia.Input.KeyGesture(Key.Delete));
    }

    [AvaloniaTest]
    public void MenuItem_Delete_DisabledWithoutSelection_EnabledWithSelection()
    {
        var delete = Find(Find(BuildMenu(), "Edit").Items, "Delete");
        delete.IsEnabled.ShouldBeFalse();

        var room = _service!.AddRoom(new Vector(0, 0));
        _service.Select(room);
        delete.IsEnabled.ShouldBeTrue();

        _service.ClearSelection();
        delete.IsEnabled.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void MenuItem_Undo_FollowsSessionUndoStack_AndRunsPerformUndo()
    {
        var undo = Find(Find(BuildMenu(), "Edit").Items, "Undo");
        undo.IsEnabled.ShouldBeFalse();

        _service!.AddRoom(new Vector(0, 0));
        undo.IsEnabled.ShouldBeTrue();

        undo.Command!.Execute(null);
        undo.IsEnabled.ShouldBeFalse();
        _service.Session.Undo.CanRedo.ShouldBeTrue();
        Find(Find(BuildMenu(), "Edit").Items, "Redo").IsEnabled.ShouldBeTrue();
    }

    [AvaloniaTest]
    public void KeyBindings_ContainPrimaryN_O_S()
    {
        var bindings = CatalogueMenuBuilder.BuildKeyBindings(_catalogue!, _service!.Session);
        var modifiers = AvaloniaKeyMapper.ToKeyModifiers(CoreModifierKeys.Primary, AvaloniaKeyMapper.IsMacOS);
        foreach (var key in new[] { Key.N, Key.O, Key.S })
        {
            bindings.Any(b => b.Gesture == new global::Avalonia.Input.KeyGesture(key, modifiers)).ShouldBeTrue();
        }
    }

    [AvaloniaTest]
    public void ContextMenu_Room_ContainsPropertiesAndDelete()
    {
        var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service!.Session, ContextTargets.Room);
        var headers = menu.Items.OfType<MenuItem>().Select(m => Plain(m.Header)).ToList();
        headers.ShouldContain("Properties...");
        headers.ShouldContain("Delete");
        headers.ShouldContain("Cut");
        headers.ShouldContain("Copy");
    }

    [AvaloniaTest]
    public void ContextMenu_Empty_ContainsAddRoomAndPaste()
    {
        var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service!.Session, ContextTargets.Empty);
        var headers = menu.Items.OfType<MenuItem>().Select(m => Plain(m.Header)).ToList();
        headers.ShouldContain("Add Room");
        headers.ShouldContain("Paste");
        headers.ShouldContain("Map Settings...");
        headers.ShouldNotContain("Properties...");
        headers.ShouldNotContain("Delete");
    }

    [AvaloniaTest]
    public void Toolbar_ButtonsMatchOldToolbar()
    {
        var toolbar = CatalogueMenuBuilder.BuildToolbar(_catalogue!, _service!.Session);
        var buttons = toolbar.OfType<Button>().ToList();
        buttons.Select(b => global::Avalonia.Automation.AutomationProperties.GetAutomationId(b)).ShouldBe(new[]
        {
            "file.new", "file.open", "file.save",
            "project.addRoom", "project.addLabel", "edit.delete", "edit.undo", "edit.redo",
            "connection.toggleDotted", "connection.toggleOneWay",
            "view.zoomIn", "view.zoomOut", "view.resetZoom", "view.toggleGrid",
            "project.startAutomap", "edit.mapSettings"
        });
        toolbar.Count(c => c is not Button).ShouldBe(3);
    }

    [AvaloniaTest]
    public void CtrlA_SelectsAll_ViaGeneratedBinding()
    {
        _service!.AddRoom(new Vector(0, 0));
        _service.AddRoom(new Vector(200, 0));
        _service.ClearSelection();

        var window = new Window();
        foreach (var binding in CatalogueMenuBuilder.BuildKeyBindings(_catalogue!, _service.Session))
        {
            window.KeyBindings.Add(binding);
        }

        window.Show();
        try
        {
            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
            _service.SelectedElements.Count.ShouldBe(2);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PrimaryMapsToMetaOnMac()
    {
        AvaloniaKeyMapper.ToKeyModifiers(CoreModifierKeys.Primary, macOS: true).ShouldBe(KeyModifiers.Meta);
        AvaloniaKeyMapper.ToKeyModifiers(CoreModifierKeys.Primary, macOS: false).ShouldBe(KeyModifiers.Control);
        AvaloniaKeyMapper.ToModifierKeys(KeyModifiers.Meta, macOS: true).ShouldBe(CoreModifierKeys.Primary);
        AvaloniaKeyMapper.ToModifierKeys(KeyModifiers.Control, macOS: false).ShouldBe(CoreModifierKeys.Primary);
        AvaloniaKeyMapper.ToModifierKeys(KeyModifiers.Control, macOS: true).ShouldBe(CoreModifierKeys.Control);
    }

    [AvaloniaTest]
    public void InputKeyMapping_RoundTrips()
    {
        AvaloniaKeyMapper.ToInputKey(Key.Z).ShouldBe(InputKey.Z);
        AvaloniaKeyMapper.TryToKey(InputKey.F4, out var key).ShouldBeTrue();
        key.ShouldBe(Key.F4);
    }
}
