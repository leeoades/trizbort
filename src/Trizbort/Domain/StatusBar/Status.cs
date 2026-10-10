using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Trizbort.Domain.StatusBar;

public enum StatusItems
{
  TsbInfo = 0,
  TsbCapsLock,
  TsbNumLock,
  TsbZoom
}

public class StatusItem
{
  public StatusItems Id { get; set; }
  public bool Show { get; set; }
  public ToolStripStatusLabel Control { get; set; }
  public IStatusWidget Widget { get; set; }
}

public class Status
{
  public Status(StatusStrip statusBar)
  {
    StatusBar = statusBar;
    StatusBar.MouseLeave += ShowDefaultInfoMessage;
  }

  private StatusStrip StatusBar { get; }
  public List<StatusItem> Items { get; set; }
  public string LastStatus { get; set; }

  private void ShowDefaultInfoMessage(object sender, EventArgs e)
  {
    UpdateInfoMessage(string.Empty);
  }

  public void UpdateStatusBar()
  {
    if (Items == null)
    {
      SetDefaultItems();
      AddItemsToStatusBar();
    }

    foreach (var statusItem in Items.Where(p => p.Id != StatusItems.TsbInfo))
    {
      statusItem.Control.Text = statusItem.Widget.DisplayText();
      statusItem.Control.ForeColor = statusItem.Widget.DisplayColor;
    }
  }

  private void AddItemsToStatusBar()
  {
    foreach (var statusItem in Items)
      if (statusItem.Id == StatusItems.TsbInfo)
      {
        var infoLabel = new ToolStripStatusLabel(statusItem.Id.ToString()) {
          Spring = true,
          Alignment = ToolStripItemAlignment.Left,
          TextAlign = ContentAlignment.MiddleLeft
        };
        StatusBar.Items.Add(infoLabel);
        statusItem.Control = infoLabel;
      }
      else
      {
        var itemLabel = new ToolStripStatusLabel(statusItem.Id.ToString()) { Tag = statusItem.Id };
        itemLabel.MouseEnter += ShowHelp;
        itemLabel.Click += HandleClick;
        StatusBar.Items.Add(itemLabel);
        statusItem.Control = itemLabel;
      }
  }

  private void HandleClick(object sender, EventArgs e)
  {
    var control = (ToolStripStatusLabel)sender;
    var widget = Items.Find(p => p.Id == (StatusItems)control.Tag);
    widget.Widget.ClickHandler();
  }

  private void ShowHelp(object sender, EventArgs eventArgs)
  {
    var control = (ToolStripStatusLabel)sender;

    var helpItem = Items.Find(p => p.Id == (StatusItems)control.Tag);

    UpdateInfoMessage(helpItem.Widget.HelpText);
  }

  private void UpdateInfoMessage(string text)
  {
    var item = Items.Find(p => p.Id == StatusItems.TsbInfo);
    item.Control.Text = text;
  }

  private void UpdateInfoMessage(IStatusWidget helpItem)
  {
    UpdateInfoMessage(helpItem.HelpText);
  }

  private void SetDefaultItems()
  {
    Items = new List<StatusItem> {
      new() { Id = StatusItems.TsbInfo, Show = true },
      new() { Id = StatusItems.TsbCapsLock, Show = true, Widget = new CapsLockStatusWidget() },
      new() { Id = StatusItems.TsbNumLock, Show = true, Widget = new NumLockStatusWidget() },
      new() { Id = StatusItems.TsbZoom, Show = true, Widget = new ZoomStatusWidget() }
    };
  }
}