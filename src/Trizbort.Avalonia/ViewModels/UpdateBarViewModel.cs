using Trizbort.Avalonia.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.AppSettings;
using Trizbort.Updates;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///   The bar under the toolbar that says a new version of Recast is out, downloading or ready, and offers what to do
///   next, as the user chose in App Settings (Lee, 26 Nov 2026).
/// </summary>
public sealed partial class UpdateBarViewModel : ObservableObject
{
    public const string ReleasesPage = "https://github.com/leeoades/trizbort/releases";

    private readonly UpdateCoordinator _updates;
    private readonly Func<string, Task> _openLink;
    private readonly Action _closeWindow;

    /// <param name="updates">What's happening.</param>
    /// <param name="openLink">Opens a web page.</param>
    /// <param name="closeWindow">Closes Recast's window, asking about unsaved changes as usual.</param>
    public UpdateBarViewModel(UpdateCoordinator updates, Func<string, Task> openLink, Action closeWindow)
    {
        _updates = updates;
        _openLink = openLink;
        _closeWindow = closeWindow;
        _updates.Changed += (_, _) =>
        {
            if (Dispatcher.UIThread.CheckAccess()) Refresh();
            else Dispatcher.UIThread.Post(Refresh);
        };
        Refresh();
    }

    public UpdateCoordinator Updates => _updates;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _showDownload;
    [ObservableProperty] private bool _showRestart;
    [ObservableProperty] private string _restartText = "Restart to Update";
    [ObservableProperty] private bool _showWhatsNew;
    [ObservableProperty] private bool _showReleases;
    [ObservableProperty] private bool _showTryAgain;
    [ObservableProperty] private bool _showProgress;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _dismissText = "Not Now";

    /// <summary>Looks for a new version now (Help > Check for Updates), and says what it found.</summary>
    public Task CheckNowAsync() => _updates.CheckAsync(true);

    [RelayCommand]
    private Task Download() => _updates.DownloadAsync();

    [RelayCommand]
    private void Restart()
    {
        _updates.RestartToUpdate();
        _closeWindow();
    }

    [RelayCommand]
    private Task WhatsNew() =>
        _openLink(_updates.Update is { } update ? $"{ReleasesPage}/tag/recast-v{update.Version}" : ReleasesPage);

    [RelayCommand]
    private Task Releases() => _openLink(ReleasesPage);

    [RelayCommand]
    private Task TryAgain() => _updates.CheckAsync(true);

    [RelayCommand]
    private void Dismiss() => _updates.Dismiss();

    private void Refresh()
    {
        var version = _updates.Update?.Version ?? string.Empty;
        var automatic = _updates.Mode == UpdateMode.Automatic;
        ShowDownload = ShowRestart = ShowWhatsNew = ShowReleases = ShowTryAgain = ShowProgress = false;
        DismissText = "OK";
        RestartText = "Restart to Update";
        Progress = _updates.Progress;

        switch (_updates.State)
        {
            case UpdateState.Checking:
                Message = "Looking for a new version of Recast...";
                break;
            case UpdateState.Available:
                Message = $"Recast {version} is out.";
                ShowDownload = ShowWhatsNew = true;
                DismissText = "Not Now";
                break;
            case UpdateState.Downloading:
                Message = $"Downloading Recast {version}...";
                ShowProgress = true;
                DismissText = "Hide";
                break;
            case UpdateState.Ready when automatic:
                Message = $"Recast {version} is ready. It will be installed when you close Recast.";
                ShowRestart = ShowWhatsNew = true;
                RestartText = "Restart Now";
                break;
            case UpdateState.Ready:
                Message = $"Recast {version} is ready to install.";
                ShowRestart = ShowWhatsNew = true;
                DismissText = "Later";
                break;
            case UpdateState.UpToDate:
                Message = string.IsNullOrEmpty(_updates.CurrentVersion)
                    ? "You have the newest version of Recast."
                    : $"You have the newest version of Recast ({_updates.CurrentVersion}).";
                break;
            case UpdateState.NotInstalled:
                Message = $"This copy of Recast wasn't installed, so it can't update itself. To get updates, {UpdateDownload.HowToInstall()}.";
                ShowReleases = true;
                break;
            case UpdateState.Failed:
                Message = _updates.Error ?? "Couldn't check for updates.";
                ShowTryAgain = true;
                break;
            case UpdateState.Updated:
                Message = $"Recast has been updated to {version}.";
                ShowWhatsNew = true;
                break;
            default:
                Message = string.Empty;
                break;
        }

        IsVisible = _updates.State != UpdateState.None && !_updates.Dismissed;
    }
}
