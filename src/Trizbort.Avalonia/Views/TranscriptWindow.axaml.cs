using Avalonia.Controls;
using Avalonia.Threading;

namespace Trizbort.Avalonia.Views;

/// <summary>
///   The whole transcript automap has read so far, styled like the game window. It follows the end of the
///   transcript as automap plays on, unless some text is selected.
/// </summary>
public partial class TranscriptWindow : Window
{
    public TranscriptWindow()
    {
        InitializeComponent();
        TranscriptBox.PropertyChanged += (_, e) =>
        {
            if (e.Property != TextBox.TextProperty) return;
            if (TranscriptBox.SelectionStart != TranscriptBox.SelectionEnd) return;
            Dispatcher.UIThread.Post(() => TranscriptBox.CaretIndex = TranscriptBox.Text?.Length ?? 0,
                DispatcherPriority.Background);
        };
        Opened += (_, _) => TranscriptBox.CaretIndex = TranscriptBox.Text?.Length ?? 0;
    }
}
