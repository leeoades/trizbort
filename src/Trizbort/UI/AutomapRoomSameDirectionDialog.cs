using System;
using System.Windows.Forms;
using Trizbort.Domain.Elements;

namespace Trizbort.UI;

public enum AutomapSameDirectionResult
{
  KeepRoom1,
  KeepRoom2,
  KeepBoth
}

public partial class AutomapRoomSameDirectionDialog : Form
{
  public AutomapRoomSameDirectionDialog()
  {
    InitializeComponent();
  }


  public string Room2 { get; set; }
  public Room Room1 { get; set; }
  public AutomapSameDirectionResult Result { get; private set; }

  private void AutomapRoomSameDirectionDialog_Shown(object sender, EventArgs e)
  {
    _lblMessage.Text =
      $"Room '{Room1.Name} is already defined in the same direction as room '{Room2}'.  What would you like to do?";
    _btnRoom1.Text = $"Keep '{Room1.Name}'";
    _btnRoom2.Text = $"Keep '{Room2}'";
  }

  private void BtnRoom1Click(object sender, EventArgs e)
  {
    Result = AutomapSameDirectionResult.KeepRoom1;
    Hide();
  }

  private void BtnRoom2Click(object sender, EventArgs e)
  {
    Result = AutomapSameDirectionResult.KeepRoom2;
    Hide();
  }

  private void BtnKeepBothClick(object sender, EventArgs e)
  {
    Result = AutomapSameDirectionResult.KeepBoth;
    Hide();
  }
}