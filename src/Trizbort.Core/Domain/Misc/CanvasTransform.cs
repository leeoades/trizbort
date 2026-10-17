using System;
using System.Collections.Generic;
using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc;

public class CanvasTransform
{
    public Vector Origin { get; set; } = Vector.Zero;
    public float ZoomFactor { get; set; } = 1.0f;
    public double ViewportWidth { get; set; }
    public double ViewportHeight { get; set; }

    public Vector CanvasToClient(Vector world) =>
        new((world.X - Origin.X) * ZoomFactor + (float)(ViewportWidth / 2.0),
            (world.Y - Origin.Y) * ZoomFactor + (float)(ViewportHeight / 2.0));

    public Vector ClientToCanvas(Vector screen) =>
        new((float)((screen.X - ViewportWidth / 2.0) / ZoomFactor + Origin.X),
            (float)((screen.Y - ViewportHeight / 2.0) / ZoomFactor + Origin.Y));

    public Rect GetVisibleWorldBounds()
    {
        var topLeft = ClientToCanvas(new Vector(0, 0));
        var bottomRight = ClientToCanvas(new Vector((float)ViewportWidth, (float)ViewportHeight));
        return new Rect(topLeft.X, topLeft.Y, Math.Max(0, bottomRight.X - topLeft.X), Math.Max(0, bottomRight.Y - topLeft.Y));
    }

    public void ZoomAt(Vector cursorScreen, float newZoom)
    {
        var worldBefore = ClientToCanvas(cursorScreen);
        ZoomFactor = Math.Clamp(newZoom, 0.1f, 10.0f);
        var worldAfter = ClientToCanvas(cursorScreen);
        Origin += (worldBefore - worldAfter);
    }

    public void FitToMap(IEnumerable<Element> elements, double margin = 40.0)
    {
        var bounds = Rect.Empty;
        var hasElements = false;
        foreach (var el in elements)
        {
            bounds = el.UnionBoundsWith(bounds, true);
            hasElements = true;
        }

        if (!hasElements || bounds.Width <= 0 || bounds.Height <= 0)
        {
            Origin = Vector.Zero;
            ZoomFactor = 1.0f;
            return;
        }

        Origin = new Vector(bounds.X + bounds.Width / 2.0f, bounds.Y + bounds.Height / 2.0f);

        var availableWidth = Math.Max(100.0, ViewportWidth - margin * 2.0);
        var availableHeight = Math.Max(100.0, ViewportHeight - margin * 2.0);

        var zoomX = (float)(availableWidth / bounds.Width);
        var zoomY = (float)(availableHeight / bounds.Height);
        var bestZoom = Math.Min(1.0f, Math.Min(zoomX, zoomY));
        ZoomFactor = Math.Clamp(bestZoom, 0.1f, 10.0f);
    }
}
