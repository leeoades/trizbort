using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using Trizbort.UI;

namespace Trizbort.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class MapThemeUiTests
{
  [Test]
  public void ThemesMenu_IsNextToMapSettings_WithPresetsAndImportExport()
  {
    var previousForm = TrizbortApplication.MainForm;
    try
    {
      using var form = new MainForm();
      var tools = form.MainMenuStrip.Items.OfType<ToolStripMenuItem>().Single(item => item.Text == "&Tools");
      var items = tools.DropDownItems.OfType<ToolStripMenuItem>().ToList();
      var settingsIndex = items.FindIndex(item => item.Text == "Map &Settings...");
      var menu = items[settingsIndex + 1];
      menu.Text.ShouldBe("&Themes");
      menu.DropDownItems.OfType<ToolStripMenuItem>().Select(item => item.Text).ShouldBe(
        new[] {
          "Classic", "Parchment", "Dark", "High contrast", "Sketch", "&Import theme...", "&Export current theme...",
          "Infer default room style from &rooms..."
        });
      var room = new Room(Project.Current) { HandDrawnStyle = HandDrawnStyle.MapDefault };
      Project.Current.Elements.Add(room);
      menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Dark").PerformClick();
      room.Shape.ShouldBe(MapTheme.BuiltInThemes()[2].DefaultRoomShape);
      MapTheme.HasIndividualStyles(Project.Current).ShouldBeFalse();
      form.Canvas.BackColor.ToArgb()
          .ShouldBe(ColorTranslator.FromHtml(MapTheme.BuiltInThemes()[2].Colors["canvas"]).ToArgb());
      menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Classic").PerformClick();
      room.Shape.ShouldBe(RoomShape.SquareCorners);
      MapTheme.HasIndividualStyles(Project.Current).ShouldBeFalse();
    }
    finally
    {
      TrizbortApplication.MainForm = previousForm;
    }
  }
}
