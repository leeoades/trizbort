using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Editor.Commands;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class ContextMenuSettingsOrderTests
{
    [TestCase(ContextTargets.Room)]
    [TestCase(ContextTargets.Empty)]
    public void SettingsCommands_AreContiguousAndLastInContextMenu(ContextTargets target)
    {
        // I-023: the settings commands shared ContextOrder values with the room edits and interleaved with them.
        var catalogue = AppCommands.CreateCatalogue(null);
        var ordered = catalogue.ForContext(target)
            .Select((c, i) => (Command: c, Index: i))
            .OrderBy(x => x.Command.ContextOrder)
            .ThenBy(x => x.Index)
            .Select(x => x.Command)
            .ToList();

        var settingsIds = new[] { "edit.mapSettings", "edit.mapTheme", "edit.appSettings" };
        ordered.Skip(ordered.Count - 3).Select(c => c.Id).ShouldBe(settingsIds);

        var settingsGroup = ordered[^1].ContextGroup;
        ordered.Skip(ordered.Count - 3).ShouldAllBe(c => c.ContextGroup == settingsGroup);
        ordered[^4].ContextGroup.ShouldNotBe(settingsGroup);
    }
}
