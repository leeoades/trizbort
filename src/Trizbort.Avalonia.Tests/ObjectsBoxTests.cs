using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 073: Enter editing (Tab moves focus) in the Room dialog's Objects box.</summary>
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
    public void Tab_IsNotInterceptedSoFocusMoves()
    {
        var dialog = ShowDialog("Chest\nPouch");
        try
        {
            var box = dialog.ObjectsBox;
            box.CaretIndex = 8;
            var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab };
            box.RaiseEvent(args);
            box.Text.ShouldBe("Chest\nPouch");
            box.AcceptsTab.ShouldBeFalse();
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
    public void ObjectSyntaxHelp_IsClosedByDefault_AndEscClosesOnlyTheHint()
    {
        var dialog = ShowDialog(string.Empty);
        try
        {
            dialog.SyntaxPopup.IsOpen.ShouldBeFalse();
            dialog.SyntaxToggle.IsChecked = true;
            dialog.SyntaxPopup.IsOpen.ShouldBeTrue();

            var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape };
            dialog.RaiseEvent(args); // the Objects box sits on a tab that is not selected, so raise on the dialog
            args.Handled.ShouldBeTrue();
            dialog.SyntaxPopup.IsOpen.ShouldBeFalse();
            dialog.IsVisible.ShouldBeTrue();
        }
        finally
        {
            dialog.Close();
        }
    }
}
