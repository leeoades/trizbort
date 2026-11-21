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
