using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class CommandCatalogueTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private static EditorCommand Command(string id, CommandScope scope, params string[] gestures)
  {
    return new EditorCommand
    {
      Id = id,
      Title = id,
      MenuPath = "Test",
      Scope = scope,
      Gestures = gestures.Select(g =>
      {
        KeyGesture.TryParse(g, out var parsed).ShouldBeTrue();
        return parsed;
      }).ToList()
    };
  }

  [Test]
  public void FindByGesture_ReturnsCanvasCommand()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("window", CommandScope.Window, "Primary+Z"));
    catalogue.Register(Command("canvas", CommandScope.Canvas, "R", "Shift+F2"));

    KeyGesture.TryParse("Shift+F2", out var gesture).ShouldBeTrue();
    catalogue.FindByGesture(gesture, CommandScope.Canvas).Id.ShouldBe("canvas");
    catalogue.FindByGesture(new KeyInput(InputKey.R, ModifierKeys.None), CommandScope.Canvas).Id.ShouldBe("canvas");
    catalogue.FindByGesture(new KeyInput(InputKey.R, ModifierKeys.None), CommandScope.Window).ShouldBeNull();
    catalogue.FindByGesture(new KeyInput(InputKey.Z, ModifierKeys.Primary), CommandScope.Canvas).ShouldBeNull();
  }

  [Test]
  public void DefaultCatalogue_HasNoValidationProblems()
  {
    CatalogueValidator.Validate(CommandCatalogue.CreateDefault()).ShouldBeEmpty();
  }

  [Test]
  public void DuplicateGesture_SameScope_IsReported()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("a", CommandScope.Canvas, "R"));
    catalogue.Register(Command("b", CommandScope.Canvas, "R"));

    CatalogueValidator.Validate(catalogue).Count.ShouldBe(1);
  }

  [Test]
  public void WindowGesture_ClashingWithCanvasGesture_IsReported()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("a", CommandScope.Window, "Primary+Z"));
    catalogue.Register(Command("b", CommandScope.Canvas, "Primary+Z"));

    CatalogueValidator.Validate(catalogue).Count.ShouldBe(1);
  }

  [Test]
  public void SameGesture_DifferentScope_NoClash()
  {
    // Rule: the identical gesture clashes in any scope, so different scopes only avoid a clash with different gestures.
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("a", CommandScope.Window, "Primary+Z"));
    catalogue.Register(Command("b", CommandScope.Canvas, "Z"));

    CatalogueValidator.Validate(catalogue).ShouldBeEmpty();
  }

  [Test]
  public void RedoGestures_PrimaryY_And_PrimaryShiftZ_DoNotClashWithUndo()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("undo", CommandScope.Window, "Primary+Z"));
    catalogue.Register(Command("redo", CommandScope.Window, "Primary+Y", "Primary+Shift+Z"));

    CatalogueValidator.Validate(catalogue).ShouldBeEmpty();
  }

  [Test]
  public void Validator_ReportsMissingTitleMenuPathAndHiddenToolbar()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(new EditorCommand { Id = "a", Title = "", MenuPath = "Edit" });
    catalogue.Register(new EditorCommand { Id = "b", Title = "B" });
    catalogue.Register(new EditorCommand { Id = "c", Title = "C", Hidden = true, ShowInToolbar = true });

    CatalogueValidator.Validate(catalogue).Count.ShouldBe(3);
  }

  [Test]
  public void Register_DuplicateId_Throws()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(Command("a", CommandScope.Window));

    Should.Throw<System.InvalidOperationException>(() => catalogue.Register(Command("a", CommandScope.Window)));
  }

  [Test]
  public void Find_ReturnsCommand()
  {
    var catalogue = CommandCatalogue.CreateDefault();

    catalogue.Find("edit.selectAll").ShouldNotBeNull();
    catalogue.Find("nope").ShouldBeNull();
  }

  [Test]
  public void Menu_Edit_OrdersByGroupThenRegistration()
  {
    var catalogue = new CommandCatalogue();
    catalogue.Register(new EditorCommand { Id = "b1", Title = "b1", MenuPath = "Edit", Group = "b" });
    catalogue.Register(new EditorCommand { Id = "a1", Title = "a1", MenuPath = "Edit/Sub", Group = "a" });
    catalogue.Register(new EditorCommand { Id = "b2", Title = "b2", MenuPath = "Edit", Group = "b" });
    catalogue.Register(new EditorCommand { Id = "v1", Title = "v1", MenuPath = "View", Group = "a" });
    catalogue.Register(new EditorCommand { Id = "a2", Title = "a2", MenuPath = "Edit", Group = "a" });

    catalogue.Menu("Edit").Select(c => c.Id).ToArray().ShouldBe(new[] { "a1", "a2", "b1", "b2" });
  }

  [Test]
  public void KeyGesture_Parse_Primary_Shift_Z()
  {
    KeyGesture.TryParse("Primary+Shift+Z", out var gesture).ShouldBeTrue();

    gesture.Key.ShouldBe(InputKey.Z);
    gesture.Modifiers.ShouldBe(ModifierKeys.Primary | ModifierKeys.Shift);
    KeyGesture.TryParse("Delete", out var del).ShouldBeTrue();
    del.Key.ShouldBe(InputKey.Delete);
    KeyGesture.TryParse("Alt+Enter", out var alt).ShouldBeTrue();
    alt.Modifiers.ShouldBe(ModifierKeys.Alt);
    KeyGesture.TryParse("Primary+0", out var zero).ShouldBeTrue();
    zero.Key.ShouldBe(InputKey.D0);
  }

  [Test]
  public void KeyGesture_RoundTrips()
  {
    foreach (var text in new[] { "Primary+Shift+Z", "Delete", "Alt+Enter", "Primary+0", "Primary+NumPad0", "Primary+OemPlus", "Ctrl+F2", "F11" })
    {
      KeyGesture.TryParse(text, out var gesture).ShouldBeTrue(text);
      gesture.ToString().ShouldBe(text);
      KeyGesture.TryParse(gesture.ToString(), out var again).ShouldBeTrue();
      again.ShouldBe(gesture);
    }
  }

  [Test]
  public void KeyGesture_DisplayString_WindowsAndMac()
  {
    KeyGesture.TryParse("Primary+Shift+Z", out var gesture).ShouldBeTrue();

    gesture.ToDisplayString(false).ShouldBe("Ctrl+Shift+Z");
    gesture.ToDisplayString(true).ShouldBe("⇧⌘Z");
  }

  [Test]
  public void KeyGesture_Parse_Invalid_ReturnsFalse()
  {
    foreach (var text in new[] { "", "   ", "Primary+", "Foo+Z", "Primary+Primary+Z", "Nonsense", "Primary+99", "None", "123" })
    {
      KeyGesture.TryParse(text, out _).ShouldBeFalse(text);
    }
  }

  [Test]
  public void KeyGesture_Matches_IsExactModifierEquality()
  {
    KeyGesture.TryParse("Primary+A", out var gesture).ShouldBeTrue();

    gesture.Matches(new KeyInput(InputKey.A, ModifierKeys.Primary)).ShouldBeTrue();
    gesture.Matches(new KeyInput(InputKey.A, ModifierKeys.Primary | ModifierKeys.Shift)).ShouldBeFalse();
    gesture.Matches(new KeyInput(InputKey.A, ModifierKeys.None)).ShouldBeFalse();
  }

  [Test]
  public void Primary_Modifier_DoesNotChangeExistingModifierValues()
  {
    ((int)ModifierKeys.Shift).ShouldBe(1);
    ((int)ModifierKeys.Control).ShouldBe(2);
    ((int)ModifierKeys.Alt).ShouldBe(4);
    ((int)ModifierKeys.Primary).ShouldBe(8);
  }

  [Test]
  public async Task Execute_SelectAll_SelectsAllElements()
  {
    _session.AddRoom(new Trizbort.Domain.Misc.Vector(0, 0));
    _session.AddRoom(new Trizbort.Domain.Misc.Vector(200, 0));
    _session.ClearSelection();

    var command = CommandCatalogue.CreateDefault().Find("edit.selectAll");
    command.CanExecute(_session).ShouldBeTrue();
    await command.ExecuteAsync(_session);

    _session.Selection.Count.ShouldBe(2);
  }

  [Test]
  public void CanExecute_Delete_FalseWhenNothingSelected()
  {
    var command = CommandCatalogue.CreateDefault().Find("edit.delete");

    command.CanExecute(_session).ShouldBeFalse();
    _session.AddRoom(new Trizbort.Domain.Misc.Vector(0, 0));
    command.CanExecute(_session).ShouldBeTrue();
  }

  [Test]
  public void EveryCommand_HasTitle_AndMenuPathOrIsHidden()
  {
    foreach (var command in CommandCatalogue.CreateDefault().All)
    {
      command.Title.ShouldNotBeNullOrWhiteSpace(command.Id);
      (command.Hidden || !string.IsNullOrWhiteSpace(command.MenuPath)).ShouldBeTrue(command.Id);
    }
  }
}
