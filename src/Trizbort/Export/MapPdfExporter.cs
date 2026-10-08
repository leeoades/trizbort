using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.UI.Controls;

namespace Trizbort.Export {
  internal static class MapPdfExporter {
    public static void Save(Canvas canvas, string fileName) {
      using (var document = new PdfDocument()) {
        document.Info.Title = Project.Current.Title;
        document.Info.Author = Project.Current.Author;
        document.Info.Creator = Application.ProductName;
        document.Info.CreationDate = DateTime.Now;
        document.Info.Subject = Project.Current.Description;
        var page = document.AddPage();
        var bounds = canvas.ComputeCanvasBounds(true);
        var width = Math.Max(1, bounds.Width);
        var height = Math.Max(1, bounds.Height);
        page.Width = new XUnit(width);
        page.Height = new XUnit(height);
        using (var graphics = XGraphics.FromPdfPage(page))
          canvas.Draw(graphics, true, width, height);
        foreach (var room in Project.Current.Elements.OfType<Room>().Where(room => room.HasDescription)) {
          var side = Math.Min(room.Width, room.Height) / 4;
          var rectangle = new XRect(
            room.X - bounds.Left + (room.Shape == RoomShape.SquareCorners ? 0 : room.Width / 2 - side / 2),
            bounds.Height - (room.Y - bounds.Top + side), side, side);
          page.Annotations.Add(new PdfTextAnnotation {
            Contents = room.PrimaryDescription,
            Color = Color.Orange,
            Icon = PdfTextAnnotationIcon.Note,
            Rectangle = new PdfRectangle(rectangle)
          });
        }
        document.Save(fileName);
      }
    }
  }
}
