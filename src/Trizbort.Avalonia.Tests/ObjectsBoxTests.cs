using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 073: Tab / Shift+Tab / Enter editing in the Room dialog's Objects box.</summary>
[TestFixture]
[Category("Unit")]
public class ObjectsBoxTests
{
    private static RoomPropertiesDialog ShowDialog(string objects)
    {
        var dialog = new RoomPropertiesDialog { DataContext = new RoomPropertiesViewModel { Objects = objects } };
        dialog.Show();
        return dialog;
    }

    private static void Press(TextBox box, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers });
    }

    [AvaloniaTest]
    public void Tab_IndentsCurrentLine()
    {
        var dialog = ShowDialog("Chest\nPouch");
        try
        {
            var box = dialog.ObjectsBox;
            box.CaretIndex = 8;
            Press(box, Key.Tab);
            box.Text.ShouldBe("Chest\n- Pouch");
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void ShiftTab_OutdentsSelectedLines()
    {
        var dialog = ShowDialog("Chest\n- Pouch\n- Gem");
        try
        {
            var box = dialog.ObjectsBox;
            box.SelectAll();
            Press(box, Key.Tab, KeyModifiers.Shift);
            box.Text.ShouldBe("Chest\nPouch\nGem");
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void Enter_ContinuesBullet()
    {
        var dialog = ShowDialog("Chest\n- Pouch");
        try
        {
            var box = dialog.ObjectsBox;
            box.CaretIndex = box.Text!.Length;
            Press(box, Key.Enter);
            box.Text.ShouldBe("Chest\n- Pouch\n- ");
            box.CaretIndex.ShouldBe(box.Text.Length);
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void ObjectSyntaxHelp_IsCollapsedByDefault()
    {
        var dialog = ShowDialog(string.Empty);
        try
        {
            dialog.ObjectSyntaxExpander.IsExpanded.ShouldBeFalse();
        }
        finally
        {
            dialog.Close();
        }
    }
}
