using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 057: Export to Clipboard commands.</summary>
[TestFixture]
[Category("Unit")]
public class ExportClipboardTests : IsolatedProjectTests
{
  private static readonly ExportLanguage[] _languages = Enum.GetValues(typeof(ExportLanguage)).Cast<ExportLanguage>().ToArray();

  private static string Normalize(string text)
  {
    // Quest writes a fresh game id on every export.
    return Regex.Replace(text, "<gameid>[^<]*</gameid>", "<gameid>x</gameid>");
  }

  private static void BuildMap()
  {
    var first = ProjectRegressionTests.AddRoom("Observatory");
    first.IsStartRoom = true;
    var second = ProjectRegressionTests.AddRoom("Gallery");
    ProjectRegressionTests.Connect(first, second);
  }

  private static CommandCatalogue Build(bool macOS = false, Action<string> status = null)
  {
    var catalogue = CommandCatalogue.CreateDefault();
    ExportCommands.Register(catalogue, _ => Task.CompletedTask, macOS, status);
    return catalogue;
  }

  [TestCaseSource(nameof(_languages))]
  public async Task Clipboard_ReceivesExporterOutput(ExportLanguage language)
  {
    BuildMap();
    var catalogue = Build();
    var clipboard = new InMemoryClipboard();
    using var session = new EditorSession { Clipboard = clipboard };

    var command = catalogue.Find(ExportCommands.ClipboardId(language));
    command.ShouldNotBeNull();
    command.CanExecute(session).ShouldBeTrue();
    await command.ExecuteAsync(session);

    var expected = CodeExportService.ExportToText(language, Trizbort.Domain.Application.Project.Current);
    expected.ShouldNotBeNullOrEmpty();
    Normalize(clipboard.Text).ShouldBe(Normalize(expected));
  }

  [TestCaseSource(nameof(_languages))]
  public async Task Clipboard_DisabledAndUntouchedWithoutRooms(ExportLanguage language)
  {
    var catalogue = Build();
    var clipboard = new InMemoryClipboard { Text = "before" };
    using var session = new EditorSession { Clipboard = clipboard };

    var command = catalogue.Find(ExportCommands.ClipboardId(language));
    command.CanExecute(session).ShouldBeFalse();
    await command.ExecuteAsync(session);

    clipboard.Text.ShouldBe("before");
  }

  [Test]
  public async Task Clipboard_IsNotAnUndoStep()
  {
    BuildMap();
    var catalogue = Build();
    using var session = new EditorSession { Clipboard = new InMemoryClipboard() };
    var before = session.Undo.CanUndo;

    await catalogue.Find(ExportCommands.ClipboardId(ExportLanguage.Alan)).ExecuteAsync(session);

    session.Undo.CanUndo.ShouldBe(before);
  }

  [Test]
  public async Task Clipboard_ReportsStatus()
  {
    BuildMap();
    string message = null;
    var catalogue = Build(false, text => message = text);
    using var session = new EditorSession { Clipboard = new InMemoryClipboard() };

    await catalogue.Find(ExportCommands.ClipboardId(ExportLanguage.Hugo)).ExecuteAsync(session);

    message.ShouldNotBeNull();
    message.ShouldContain("Hugo");
  }

  [Test]
  public void Gestures_FollowShortcutMap()
  {
    var catalogue = Build();
    var pa = ModifierKeys.Primary | ModifierKeys.Alt;
    KeyGesture[] G(ExportLanguage l) => catalogue.Find(ExportCommands.ClipboardId(l)).Gestures.ToArray();

    G(ExportLanguage.Alan).ShouldBe(new[] { new KeyGesture(InputKey.L, pa) });
    G(ExportLanguage.Adventuron).ShouldBe(new[] { new KeyGesture(InputKey.A, pa) });
    G(ExportLanguage.Hugo).ShouldBe(new[] { new KeyGesture(InputKey.H, pa) });
    G(ExportLanguage.Inform7).ShouldBe(new[] { new KeyGesture(InputKey.D7, pa) });
    G(ExportLanguage.Inform6).ShouldBe(new[] { new KeyGesture(InputKey.D6, pa) });
    G(ExportLanguage.Tads).ShouldBe(new[] { new KeyGesture(InputKey.T, pa) });
    G(ExportLanguage.Zil).ShouldBe(new[] { new KeyGesture(InputKey.Z, pa) });
    G(ExportLanguage.QuestRooms).ShouldBe(new[] { new KeyGesture(InputKey.Q, pa) });
    G(ExportLanguage.Quest).ShouldBeEmpty();
  }

  [Test]
  public void Gestures_HugoOnMacAddsShift()
  {
    var catalogue = Build(true);
    catalogue.Find(ExportCommands.ClipboardId(ExportLanguage.Hugo)).Gestures
      .ShouldBe(new[] { new KeyGesture(InputKey.H, ModifierKeys.Primary | ModifierKeys.Alt | ModifierKeys.Shift) });
  }
}
