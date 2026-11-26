using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Domain.Enums;

namespace Trizbort.Avalonia.Tests;

/// <summary>Text Align: the usual alignment buttons for labels and label styles. Lee, 26 Nov 2026.</summary>
[TestFixture]
public class AlignButtonsTests
{
    private static ToggleButton Button(Panel row, int index) => (ToggleButton)row.Children[index];

    [AvaloniaTest]
    public void Across_ClickToChoose_ClickAgainForUsual()
    {
        var row = new LabelAlignButtons();
        new Window { Content = row }.Show();
        row.Children.Count.ShouldBe(3);
        row.Children.OfType<ToggleButton>().ShouldAllBe(button => button.IsChecked == false);

        Button(row, 2).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        row.Value.ShouldBe(LabelAlign.Right);
        Button(row, 2).IsChecked.ShouldBe(true);
        Button(row, 0).IsChecked.ShouldBe(false);

        Button(row, 2).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        row.Value.ShouldBe(LabelAlign.Usual);
        row.Children.OfType<ToggleButton>().ShouldAllBe(button => button.IsChecked == false);
    }

    [AvaloniaTest]
    public void UpAndDown_ShowsTheValueSetOnIt()
    {
        var row = new LabelVerticalAlignButtons { Value = LabelVerticalAlign.Bottom };
        new Window { Content = row }.Show();
        Button(row, 2).IsChecked.ShouldBe(true);

        row.Value = LabelVerticalAlign.Top;
        Button(row, 0).IsChecked.ShouldBe(true);
        Button(row, 2).IsChecked.ShouldBe(false);

        Button(row, 1).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        row.Value.ShouldBe(LabelVerticalAlign.Middle);
    }
}
