using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Characterization")]
public class ClassicManualFixtureModelSummaryTests : IsolatedProjectTests
{
  private const string SnapshotFileName = "classic-manual-model-summaries.txt";

  private static IEnumerable<string> ManualMaps()
  {
    return Directory.GetFiles(PersistenceRegressionTests.FixtureDirectory, "*.trizbort")
                    .Select(Path.GetFileName).OrderBy(name => name);
  }

  [Test]
  public void ClassicManualFixtures_LoadToExpectedModelSummaries()
  {
    var actual = BuildAllSummaries();
    var snapshotPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", SnapshotFileName);
    if (Environment.GetEnvironmentVariable("TRIZBORT_UPDATE_CHARACTERIZATION") == "1")
    {
      var output = Environment.GetEnvironmentVariable("TRIZBORT_CHARACTERIZATION_OUTPUT");
      output.ShouldNotBeNullOrWhiteSpace(
        "TRIZBORT_CHARACTERIZATION_OUTPUT must point to the checked-in snapshot when regenerating characterization data.");
      var outputDirectory = Path.GetDirectoryName(output);
      if (!string.IsNullOrEmpty(outputDirectory))
        Directory.CreateDirectory(outputDirectory);
      File.WriteAllText(output, actual, Encoding.UTF8);
      return;
    }

    File.Exists(snapshotPath).ShouldBeTrue(
      $"{SnapshotFileName} must be copied to the test output. Set TRIZBORT_UPDATE_CHARACTERIZATION=1 and TRIZBORT_CHARACTERIZATION_OUTPUT to regenerate it deliberately.");
    var expected = File.ReadAllText(snapshotPath, Encoding.UTF8).Replace("\r\n", "\n");
    actual.ShouldBe(expected);
  }

  private static string BuildAllSummaries()
  {
    var builder = new StringBuilder();
    builder.AppendLine("# Classic manual fixture model summaries");
    builder.AppendLine("# This is a characterization snapshot. Update deliberately when classic fixture semantics intentionally change.");
    foreach (var fixture in ManualMaps())
    {
      Project.Current = new Project();
      Settings.Reset(false);
      PersistenceRegressionTests.Load(
        Project.Current,
        Path.Combine(PersistenceRegressionTests.FixtureDirectory, fixture));
      AppendFixtureSummary(builder, fixture);
      Project.Current.Dispose();
    }

    return builder.ToString().Replace("\r\n", "\n");
  }

  private static void AppendFixtureSummary(StringBuilder builder, string fixture)
  {
    var rooms = Project.Current.Elements.OfType<Room>().OrderBy(room => room.Id).ToArray();
    var connections = Project.Current.Elements.OfType<Connection>().OrderBy(connection => connection.Id).ToArray();
    var labels = Project.Current.Elements.OfType<MapLabel>().OrderBy(label => label.Id).ToArray();
    var bounds = Project.Current.Elements.Aggregate(Rect.Empty, (current, element) => element.UnionBoundsWith(current, true));

    builder.AppendLine();
    builder.AppendLine($"## {fixture}");
    builder.AppendLine(
      $"counts|rooms={rooms.Length}|connections={connections.Length}|labels={labels.Length}|elements={Project.Current.Elements.Count}");
    builder.AppendLine(
      $"bounds|x={Number(bounds.X)}|y={Number(bounds.Y)}|w={Number(bounds.Width)}|h={Number(bounds.Height)}");
    builder.AppendLine(
      $"settings|grid={Number(Settings.GridSize)}|snap={Bool(Settings.IsGridVisible)}|origin={Bool(Settings.ShowOrigin)}|lineWidth={Number(Settings.LineWidth)}|handDrawn={Bool(Settings.HandDrawn)}|arrow={Number(Settings.ConnectionArrowSize)}|textOffset={Number(Settings.TextOffsetFromConnection)}|stalk={Number(Settings.ConnectionStalkLength)}|darkStripe={Number(Settings.DarknessStripeSize)}|objectOffset={Number(Settings.ObjectListOffsetFromRoom)}");
    builder.AppendLine(
      $"colors|canvas={ColorValue(Settings.Color[Colors.Canvas])}|border={ColorValue(Settings.Color[Colors.Border])}|line={ColorValue(Settings.Color[Colors.Line])}|selectedLine={ColorValue(Settings.Color[Colors.SelectedLine])}|hoverLine={ColorValue(Settings.Color[Colors.HoverLine])}|subtitle={ColorValue(Settings.Color[Colors.Subtitle])}|smallText={ColorValue(Settings.Color[Colors.SmallText])}|lineText={ColorValue(Settings.Color[Colors.LineText])}|grid={ColorValue(Settings.Color[Colors.Grid])}|startRoom={ColorValue(Settings.Color[Colors.StartRoom])}|endRoom={ColorValue(Settings.Color[Colors.EndRoom])}");
    builder.AppendLine(
      "regions|" + string.Join(
        ";",
        Settings.Regions.OrderBy(region => region.RegionName, StringComparer.OrdinalIgnoreCase)
                .Select(region => $"{Text(region.RegionName)}:{ColorValue(region.RColor)}:{ColorValue(region.TextColor)}")));

    foreach (var room in rooms)
      builder.AppendLine(RoomSummary(room));
    foreach (var connection in connections)
      builder.AppendLine(ConnectionSummary(connection));
    foreach (var label in labels)
      builder.AppendLine(LabelSummary(label));
  }

  private static string RoomSummary(Room room)
  {
    var flags = new[] {
                  room.IsStartRoom ? "start" : null,
                  room.IsEndRoom ? "end" : null,
                  room.IsDark ? "dark" : null,
                  room.IsReference ? "reference" : null,
                  room.ArbitraryAutomappedPosition ? "arbitraryAuto" : null
                }.Where(value => value != null);
    return
      $"room|id={room.Id}|z={room.ZOrder}|name={Text(room.Name)}|subtitle={Text(room.SubTitle)}|pos={VectorValue(room.Position)}|size={VectorValue(room.Size)}|shape={room.Shape}|corners={Number(room.Corners.TopLeft)},{Number(room.Corners.TopRight)},{Number(room.Corners.BottomRight)},{Number(room.Corners.BottomLeft)}|borderStyle={room.BorderStyle}|region={Text(room.Region)}|flags={string.Join(",", flags)}|fill={ColorValue(room.RoomFillColor)}|second={ColorValue(room.SecondFillColor)}@{Text(room.SecondFillLocation)}|border={ColorValue(room.RoomBorderColor)}|nameColor={ColorValue(room.RoomNameColor)}|subtitleColor={ColorValue(room.RoomSubtitleColor)}|objectColor={ColorValue(room.RoomObjectTextColor)}|objectsPos={room.ObjectsPosition}|objectsCustom={Bool(room.ObjectsCustomPosition)}:{room.ObjectsCustomPositionRight}:{room.ObjectsCustomPositionDown}|descriptions={Text(string.Join("\\n---\\n", room.Descriptions))}|objects={Text(room.Objects)}";
  }

  private static string ConnectionSummary(Connection connection)
  {
    var door = connection.Door == null
      ? "none"
      : $"{Bool(connection.Door.Openable)},{Bool(connection.Door.Open)},{Bool(connection.Door.Lockable)},{Bool(connection.Door.Locked)}";
    return
      $"connection|id={connection.Id}|z={connection.ZOrder}|name={Text(connection.Name)}|style={connection.Style}|flow={connection.Flow}|color={ColorValue(connection.ConnectionColor)}|texts={Text(connection.StartText)},{Text(connection.MidText)},{Text(connection.EndText)}|door={door}|vertices={string.Join(">", connection.VertexList.Select(VertexValue))}|curve={string.Join(";", connection.GetSegments().Select(segment => $"{VectorValue(segment.Start)}>{VectorValue(segment.End)}"))}";
  }

  private static string LabelSummary(MapLabel label)
  {
    return
      $"label|id={label.Id}|z={label.ZOrder}|text={Text(label.Text)}|pos={VectorValue(label.Position)}|size={VectorValue(label.Size)}|shape={label.Shape}|borderStyle={label.BorderStyle}|background={Bool(label.HasBackground)}|textColor={ColorValue(label.TextColor)}|border={ColorValue(label.BorderColor)}|fill={ColorValue(label.BackgroundColor)}";
  }

  private static string VertexValue(Vertex vertex)
  {
    if (vertex.Port == null)
      return $"point:{VectorValue(vertex.Position)}";

    return $"{vertex.Port.Owner.Id}:{vertex.Port.Id}:{VectorValue(vertex.Position)}";
  }

  private static string VectorValue(Vector vector)
  {
    return $"{Number(vector.X)},{Number(vector.Y)}";
  }

  private static string ColorValue(Color color)
  {
    return color.IsEmpty ? "empty" : $"#{color.ToArgb():X8}";
  }

  private static string Bool(bool value)
  {
    return value ? "yes" : "no";
  }

  private static string Number(double value)
  {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }

  private static string Text(string value)
  {
    return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("|", "\\|");
  }
}
