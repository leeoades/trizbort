using System;
using Trizbort.Domain.Application;

namespace Trizbort.Domain.Misc;

public class ProjectChangedEventArgs : EventArgs {
  public ProjectChangedEventArgs(Project oldProject, Project newProject)
  {
    OldProject = oldProject;
    NewProject = newProject;
  }

  public Project NewProject { get; }

  public Project OldProject { get; }
}

public class ItemEventArgs<T> : EventArgs {
  public ItemEventArgs(T item)
  {
    Item = item;
  }

  public T Item { get; }
}

public delegate void ProjectChangedEventHandler(object sender, ProjectChangedEventArgs e);

public delegate void ItemEventHandler<T>(object sender, ItemEventArgs<T> e);