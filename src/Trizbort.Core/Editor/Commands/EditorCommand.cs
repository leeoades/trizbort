using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trizbort.Editor.Commands;

/// <summary>
///   One user-invokable editor action. Menus, toolbar, context menus and key bindings are all generated from
///   these definitions.
/// </summary>
public sealed record EditorCommand
{
  public string Id { get; init; }

  public string Title { get; init; }

  /// <summary>Slash-separated menu location, e.g. <c>Edit/Rooms/Swap</c>. Null for hidden (key-only) commands.</summary>
  public string MenuPath { get; init; }

  /// <summary>Separator group inside the menu; commands are ordered by group, then registration order.</summary>
  public string Group { get; init; }

  public IReadOnlyList<KeyGesture> Gestures { get; init; } = Array.Empty<KeyGesture>();

  public CommandScope Scope { get; init; } = CommandScope.Window;

  public ContextTargets ContextTargets { get; init; } = ContextTargets.None;

  public bool ShowInToolbar { get; init; }

  public string IconKey { get; init; }

  public bool Hidden { get; init; }

  public Func<EditorSession, bool> CanExecuteFunc { get; init; }

  public Func<EditorSession, bool> IsCheckedFunc { get; init; }

  public Func<EditorSession, string> DynamicTitle { get; init; }

  public Func<EditorSession, Task> ExecuteAsyncFunc { get; init; }

  public bool CanExecute(EditorSession session)
  {
    return ExecuteAsyncFunc != null && (CanExecuteFunc == null || CanExecuteFunc(session));
  }

  /// <summary>Null when the command is not a toggle.</summary>
  public bool? IsChecked(EditorSession session)
  {
    return IsCheckedFunc == null ? null : IsCheckedFunc(session);
  }

  public string TitleFor(EditorSession session)
  {
    return DynamicTitle == null ? Title : DynamicTitle(session);
  }

  public Task ExecuteAsync(EditorSession session)
  {
    return ExecuteAsyncFunc == null ? Task.CompletedTask : ExecuteAsyncFunc(session);
  }
}
