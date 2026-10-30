using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor.Commands;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class UndoUiTests
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

    private static string Plain(object? header) => (header?.ToString() ?? string.Empty).Replace("_", string.Empty);

    private MenuItem EditItem(string name)
    {
        var edit = CatalogueMenuBuilder.BuildMainMenu(_catalogue!, _service!.Session).First(m => Plain(m.Header) == "Edit");
        return edit.Items.OfType<MenuItem>().First(m => Plain(m.Header) == name || Plain(m.Header).StartsWith(name + " "));
    }

    private Window ShowWindowWithBindings()
    {
        var window = new Window();
        foreach (var binding in CatalogueMenuBuilder.BuildKeyBindings(_catalogue!, _service!.Session))
        {
            window.KeyBindings.Add(binding);
        }

        window.Show();
        return window;
    }

    private int RoomCount() => Project.Current.Elements.OfType<Room>().Count();

    [AvaloniaTest]
    public void EditMenu_ContainsUndoRedo_DisabledInitially()
    {
        EditItem("Undo").IsEnabled.ShouldBeFalse();
        EditItem("Redo").IsEnabled.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void AddRoom_ThenUndoMenu_RemovesRoom()
    {
        var undo = EditItem("Undo");
        _service!.AddRoom(new Vector(0, 0));
        RoomCount().ShouldBe(1);

        undo.Command!.Execute(null);

        RoomCount().ShouldBe(0);
    }

    [AvaloniaTest]
    public void Toolbar_HasUndoRedoButtons()
    {
        var toolbar = CatalogueMenuBuilder.BuildToolbar(_catalogue!, _service!.Session);
        var ids = toolbar.OfType<Button>()
            .Select(b => global::Avalonia.Automation.AutomationProperties.GetAutomationId(b))
            .ToList();

        ids.ShouldContain("edit.undo");
        ids.ShouldContain("edit.redo");
    }

    [AvaloniaTest]
    public void CtrlZ_KeyBinding_Undoes()
    {
        _service!.AddRoom(new Vector(0, 0));
        var window = ShowWindowWithBindings();
        try
        {
            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);

            RoomCount().ShouldBe(0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void CtrlY_And_CtrlShiftZ_Redo()
    {
        _service!.AddRoom(new Vector(0, 0));
        var window = ShowWindowWithBindings();
        try
        {
            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            RoomCount().ShouldBe(0);

            window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            RoomCount().ShouldBe(1);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            RoomCount().ShouldBe(0);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
            RoomCount().ShouldBe(1);
        }
        finally
        {
            window.Close();
        }
    }
}
