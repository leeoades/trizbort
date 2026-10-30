using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 056: export to file gestures and the enable-when-rooms rule.</summary>
[TestFixture]
[Category("Unit")]
public class ExportGestureTests : IsolatedProjectTests
{
  private static CommandCatalogue Build(bool macOS, ExportLanguage[] ran = null)
  {
    var catalogue = CommandCatalogue.CreateDefault();
    ExportCommands.Register(catalogue, language =>
    {
      if (ran != null && ran.Length > 0) ran[0] = language;
      return Task.CompletedTask;
    }, macOS);
    return catalogue;
  }

  private static KeyGesture[] GesturesOf(CommandCatalogue catalogue, ExportLanguage language)
  {
    return catalogue.Find(ExportCommands.FileId(language)).Gestures.ToArray();
  }

  [TestCase(ExportLanguage.Alan, InputKey.L)]
  [TestCase(ExportLanguage.Hugo, InputKey.H)]
  [TestCase(ExportLanguage.Inform7, InputKey.D7)]
  [TestCase(ExportLanguage.Inform6, InputKey.D6)]
  [TestCase(ExportLanguage.Tads, InputKey.T)]
  [TestCase(ExportLanguage.Zil, InputKey.I)]
  [TestCase(ExportLanguage.Quest, InputKey.Q)]
  public void FileExport_HasPrimaryShiftGesture(ExportLanguage language, InputKey key)
  {
    var gestures = GesturesOf(Build(false), language);
    gestures.ShouldBe(new[] { new KeyGesture(key, ModifierKeys.Primary | ModifierKeys.Shift) });
  }

  [Test]
  public void FileExport_Adventuron_HasNoGesture()
  {
    GesturesOf(Build(false), ExportLanguage.Adventuron).ShouldBeEmpty();
  }

  [Test]
  public void FileExport_QuestOnMac_UsesAltVariant()
  {
    GesturesOf(Build(true), ExportLanguage.Quest)
      .ShouldBe(new[] { new KeyGesture(InputKey.Q, ModifierKeys.Primary | ModifierKeys.Alt | ModifierKeys.Shift) });
  }

  [Test]
  public void FileExport_ZilDoesNotUseRedoGesture()
  {
    var catalogue = Build(false);
    var zil = GesturesOf(catalogue, ExportLanguage.Zil).Single();
    zil.ShouldNotBe(new KeyGesture(InputKey.Z, ModifierKeys.Primary | ModifierKeys.Shift));
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Catalogue_HasNoGestureClashes(bool macOS)
  {
    CatalogueValidator.Validate(Build(macOS)).ShouldBeEmpty();
  }

  [Test]
  public void FileExport_DisabledWithoutRooms_EnabledWithRooms()
  {
    var catalogue = Build(false);
    using var session = new EditorSession();
    var ids = new[]
    {
      ExportLanguage.Alan, ExportLanguage.Adventuron, ExportLanguage.Hugo, ExportLanguage.Inform6,
      ExportLanguage.Inform7, ExportLanguage.Tads, ExportLanguage.Quest, ExportLanguage.Zil
    }.Select(ExportCommands.FileId).ToList();
    ids.Count.ShouldBe(8);

    foreach (var id in ids) catalogue.Find(id).CanExecute(session).ShouldBeFalse(id);

    ProjectRegressionTests.AddRoom("Observatory");

    foreach (var id in ids) catalogue.Find(id).CanExecute(session).ShouldBeTrue(id);
  }

  [Test]
  public async Task FileExport_RunsHostDelegateWithLanguage()
  {
    var ran = new ExportLanguage[1];
    var catalogue = Build(false, ran);
    using var session = new EditorSession();
    ProjectRegressionTests.AddRoom("Observatory");

    await catalogue.Find(ExportCommands.FileId(ExportLanguage.Tads)).ExecuteAsync(session);

    ran[0].ShouldBe(ExportLanguage.Tads);
  }
}
