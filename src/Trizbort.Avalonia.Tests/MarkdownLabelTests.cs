using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Tests;

/// <summary>Lee, 21 Nov 2026: formatted text on the map, with tables and room links you can click.</summary>
[TestFixture]
public class MarkdownLabelTests
{
    [AvaloniaTest]
    public void ATable_IsLaidOut_WithItsRoomLinksWhereTheyAreDrawn()
    {
        var bounds = new Rect(100, 100, 300, 200);
        var renderer = MarkdownLabelRenderer.Lay(
            "# Treasures\n| Treasure | Room |\n|---|---|\n| Golden eggs | [[Giant Room]] |",
            bounds, new Typeface(FontFamily.Default), 13, Brushes.Black, Brushes.Blue, new Pen(Brushes.Gray));

        renderer.Height.ShouldBeGreaterThan(0);
        renderer.Links.Count.ShouldBe(1);
        var (where, link) = renderer.Links[0];
        link.ShouldBe("room:Giant Room");
        // in the right-hand column, below the heading
        where.Left.ShouldBeGreaterThan(bounds.Left + 40);
        where.Top.ShouldBeGreaterThan(bounds.Top + 13);
        renderer.LinkAt(where.Center).ShouldBe("room:Giant Room");
        renderer.LinkAt(new Point(bounds.Left + 1, bounds.Top + 1)).ShouldBeNull();
    }

    /// <summary>Lee, 25 Nov 2026: a new style (a bigger font) doesn't widen a label to unwrap its sentence.</summary>
    [AvaloniaTest]
    public void ANewLook_KeepsTheLabelsWidth_AndItsLines_GrowingTallerIfNeedBe()
    {
        var size = new Trizbort.Domain.Misc.Vector(100, 40);
        var label = new MapLabel(Project.Current) { Text = "This is a little sticky note" };
        label.Own = new Trizbort.Setup.LabelStyle { FontSize = 18 };

        MapLabelDrawingAdapter.FitText(label, size).X.ShouldBeGreaterThan(size.X); // typed: widened to fit
        var kept = MapLabelDrawingAdapter.FitText(label, size, keepWidth: true);
        kept.X.ShouldBe(size.X);
        kept.Y.ShouldBeGreaterThan(size.Y);

        var roomy = new Trizbort.Domain.Misc.Vector(100, 400);
        MapLabelDrawingAdapter.FitText(label, roomy, keepWidth: true).ShouldBe(roomy);
    }

    /// <summary>Lee, 22 Nov 2026: the label grows as its text does.</summary>
    [AvaloniaTest]
    public void ALabel_GrowsToFitItsText_AndItsTables_ButNoSmallerThanItWas()
    {
        var small = new Trizbort.Domain.Misc.Vector(120, 40);
        var label = new MapLabel(Project.Current) { Markdown = true, Text = "## Treasures" };
        // fits already: as it was
        MapLabelDrawingAdapter.FitText(label, small).ShouldBe(small);

        label.Text = "## Treasures\n" + Trizbort.Domain.Misc.MarkdownEditing.SampleTable;
        var fitted = MapLabelDrawingAdapter.FitText(label, small);
        // wider for the table, so its cells don't wrap, and taller for its rows
        fitted.X.ShouldBeGreaterThan(small.X);
        fitted.X.ShouldBeLessThanOrEqualTo(MapLabelDrawingAdapter.WidestFitted);
        fitted.Y.ShouldBeGreaterThan(small.Y + 60);

        // a long paragraph wraps at the label's width, which stays
        label.Text = string.Join(" ", Enumerable.Repeat("The lamp is getting dim.", 12));
        var wrapped = MapLabelDrawingAdapter.FitText(label, small);
        wrapped.X.ShouldBe(small.X);
        wrapped.Y.ShouldBeGreaterThan(small.Y);

        // plain text, centred: short, as it was; a long line, wider; more lines, taller
        label.Markdown = false;
        label.Text = "Lamp";
        MapLabelDrawingAdapter.FitText(label, small).ShouldBe(small);
        label.Text = "Where the brass lantern was found";
        var line = MapLabelDrawingAdapter.FitText(label, small);
        line.X.ShouldBeGreaterThan(small.X);
        line.Y.ShouldBe(small.Y);
        label.Text = "Lamp\nRod\nBird\nCage";
        MapLabelDrawingAdapter.FitText(label, small).Y.ShouldBeGreaterThan(small.Y);
    }

    [AvaloniaTest]
    public void TheLabelDialog_TurnsMarkdownOnAndOff()
    {
        var label = new MapLabel(Project.Current) { Text = "Plain" };
        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.Markdown.ShouldBeFalse();

        vm.Markdown = true;
        vm.ApplyTo(label);

        label.Markdown.ShouldBeTrue();
    }
}
