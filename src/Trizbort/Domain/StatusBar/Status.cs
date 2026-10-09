using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Trizbort.Domain.StatusBar {
  public enum StatusItems {
    TsbInfo = 0,
    TsbCapsLock,
    TsbNumLock,
    TsbZoom
  }

  public class StatusItem {
    public StatusItems Id { get;set; }
    public bool Show { get; set; }
    public ToolStripStatusLabel Control { get; set; }
    public IStatusWidget Widget { get; set; }
  }



  public class Status {
    public Status(StatusStrip statusBar) {
      this.StatusBar = statusBar;
      this.StatusBar.MouseLeave += ShowDefaultInfoMessage;
    }

    private void ShowDefaultInfoMessage(object sender, EventArgs e) {
      UpdateInfoMessage(string.Empty);
    }

    private StatusStrip StatusBar { get; set; }
    public List<StatusItem> Items { get; set; } = null;
    public string LastStatus { get; set; }

    public void UpdateStatusBar() {
      if (Items == null) {
        SetDefaultItems();
        AddItemsToStatusBar();
      }

      foreach (var statusItem in Items.Where(p=>p.Id != StatusItems.TsbInfo)) {
        statusItem.Control.Text =  statusItem.Widget.DisplayText();
        statusItem.Control.ForeColor = statusItem.Widget.DisplayColor;
      }
    }

    private void AddItemsToStatusBar() {
      foreach (var statusItem in Items) {
        if (statusItem.Id == StatusItems.TsbInfo) {
          var infoLabel = new ToolStripStatusLabel(statusItem.Id.ToString()) {
            Spring = true,
            Alignment = ToolStripItemAlignment.Left,
            TextAlign = ContentAlignment.MiddleLeft
          };
          StatusBar.Items.Add(infoLabel);
          statusItem.Control = infoLabel;
        } else {
          var itemLabel = new ToolStripStatusLabel(statusItem.Id.ToString()) {Tag = statusItem.Id};
          itemLabel.MouseEnter += ShowHelp;
          itemLabel.Click += HandleClick;
          StatusBar.Items.Add(itemLabel);
          statusItem.Control = itemLabel;
        }
      }
      
    }

    private void HandleClick(object sender, EventArgs e) {
      var control = (ToolStripStatusLabel) sender;
      var widget = Items.Find(p => p.Id == (StatusItems) control.Tag);
      widget.Widget.ClickHandler();
    }

    private void ShowHelp(object sender, EventArgs eventArgs) {
      var control = (ToolStripStatusLabel) sender;

      var helpItem = Items.Find(p => p.Id == (StatusItems) control.Tag);
      
      UpdateInfoMessage(helpItem.Widget.HelpText);
    }

    private void UpdateInfoMessage(string text) {
      var item = Items.Find(p => p.Id == StatusItems.TsbInfo);
      item.Control.Text = text;
    }

    private void UpdateInfoMessage(IStatusWidget helpItem) {
      UpdateInfoMessage(helpItem.HelpText);
    }

    private void SetDefaultItems() {
      Items = new List<StatusItem> {
        new StatusItem {Id = StatusItems.TsbInfo, Show = true},
        new StatusItem {Id = StatusItems.TsbCapsLock, Show = true, Widget = new CapsLockStatusWidget()},
        new StatusItem {Id = StatusItems.TsbNumLock, Show = true, Widget = new NumLockStatusWidget()}, 
        new StatusItem {Id = StatusItems.TsbZoom, Show = true, Widget = new ZoomStatusWidget()}
      };
    }
  }
}