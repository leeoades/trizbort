using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.Services;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   The dynamic File &gt; Recent Maps submenu. Its items are rebuilt from <see cref="IRecentFilesService" /> each
///   time the File menu or the submenu opens; it is disabled while the list is empty.
/// </summary>
public static class RecentMapsMenu
{
    public static MenuItem Create(IRecentFilesService service, Func<string, Task> open)
    {
        var item = new MenuItem { Header = "Recent _Maps" };
        item.SubmenuOpened += (_, _) => Refresh(item, service, open);
        Refresh(item, service, open);
        return item;
    }

    /// <summary>Adds the submenu after "Open Map..." in <paramref name="fileMenu" /> and keeps it fresh.</summary>
    public static void InsertInto(MenuItem fileMenu, IRecentFilesService service, Func<string, Task> open)
    {
        var recent = Create(service, open);
        var index = 0;
        var position = -1;
        foreach (var child in fileMenu.Items)
        {
            index++;
            if (child is MenuItem { Header: string header } && header == "_Open Map...")
            {
                position = index;
                break;
            }
        }

        if (position < 0) position = Math.Min(2, fileMenu.Items.Count);
        fileMenu.Items.Insert(position, recent);
        fileMenu.SubmenuOpened += (_, _) => Refresh(recent, service, open);
    }

    public static void Refresh(MenuItem item, IRecentFilesService service, Func<string, Task> open)
    {
        var entries = service.GetEntries();
        item.Items.Clear();
        foreach (var entry in entries)
        {
            var path = entry.Path;
            // The core label uses '&' for the number key; Avalonia uses '_' and treats other '_' as mnemonics.
            var child = new MenuItem { Header = "_" + entry.Label.Substring(1).Replace("_", "__") };
            child.Click += (_, _) => _ = open(path);
            item.Items.Add(child);
        }

        item.IsEnabled = entries.Count > 0;
    }
}
