using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Controllers;

public class ElementController
{
  public void ShowElementProperties(Element element)
  {
    if (element is Room room)
    {
      room.ShowRoomDialog();
    }
    else if (element is Connection conn)
    {
      conn.ShowDialog();
    }
    else if (element is MapLabel label)
    {
      label.ShowDialog();
    }
  }
}
