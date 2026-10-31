using System.Globalization;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class AnchoredTextLayoutTests
{
    [AvaloniaTest]
    public void ConstrainToNaturalWidth_DoesNotWrapTrailingWhitespaceFromFlaggedObjects()
    {
        var displayText = ObjectList.FormatForDisplay("lamp [s]\nlong sword [cs]");
        displayText.ShouldContain("lamp ");
        displayText.ShouldContain("long sword ");

        var unconstrained = Create(displayText);
        var naturalHeight = unconstrained.Height;
        unconstrained.WidthIncludingTrailingWhitespace.ShouldBeGreaterThan(unconstrained.Width);

        var wrappedByWidth = Create(displayText);
        wrappedByWidth.MaxTextWidth = wrappedByWidth.Width;
        wrappedByWidth.Height.ShouldBeGreaterThan(naturalHeight);

        var constrained = Create(displayText);
        constrained.ConstrainToNaturalWidth();
        constrained.Height.ShouldBe(naturalHeight, 0.1);
        constrained.MaxTextWidth.ShouldBe(constrained.WidthIncludingTrailingWhitespace, 0.1);
    }

    private static FormattedText Create(string text) =>
        new(text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            14,
            Brushes.Black);
}
