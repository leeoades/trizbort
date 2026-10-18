using System;
using System.Collections.Generic;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Controls;

/// <summary>
/// Pure helper for transforming coordinates between world/canvas space and minimap space.
/// </summary>
public static class MinimapTransform
{
    public static Rect ComputeCanvasBounds(IEnumerable<Element>? elements)
    {
        if (elements == null) return Rect.Empty;

        var bounds = Rect.Empty;
        var hasElements = false;
        foreach (var element in elements)
        {
            bounds = element.UnionBoundsWith(bounds, true);
            hasElements = true;
        }

        return hasElements ? bounds : Rect.Empty;
    }

    public static Rect WorldToMinimap(Rect worldRect, Rect canvasBounds, Rect clientArea)
    {
        if (canvasBounds == Rect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 ||
            clientArea.Width <= 0 || clientArea.Height <= 0)
        {
            return Rect.Empty;
        }

        var scaleX = clientArea.Width / canvasBounds.Width;
        var scaleY = clientArea.Height / canvasBounds.Height;

        var x = clientArea.X + (worldRect.X - canvasBounds.X) * scaleX;
        var y = clientArea.Y + (worldRect.Y - canvasBounds.Y) * scaleY;
        var w = worldRect.Width * scaleX;
        var h = worldRect.Height * scaleY;

        return new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
    }

    public static Vector WorldToMinimap(Vector worldPoint, Rect canvasBounds, Rect clientArea)
    {
        if (canvasBounds == Rect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 ||
            clientArea.Width <= 0 || clientArea.Height <= 0)
        {
            return new Vector(clientArea.X, clientArea.Y);
        }

        var scaleX = clientArea.Width / canvasBounds.Width;
        var scaleY = clientArea.Height / canvasBounds.Height;

        var x = clientArea.X + (worldPoint.X - canvasBounds.X) * scaleX;
        var y = clientArea.Y + (worldPoint.Y - canvasBounds.Y) * scaleY;

        return new Vector(x, y);
    }

    public static Vector MinimapToWorld(Vector minimapPoint, Rect canvasBounds, Rect clientArea)
    {
        if (canvasBounds == Rect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 ||
            clientArea.Width <= 0 || clientArea.Height <= 0)
        {
            return canvasBounds == Rect.Empty ? Vector.Zero : canvasBounds.Center;
        }

        var clampedX = Math.Clamp(minimapPoint.X, clientArea.Left, clientArea.Right);
        var clampedY = Math.Clamp(minimapPoint.Y, clientArea.Top, clientArea.Bottom);

        var normX = (clampedX - clientArea.Left) / clientArea.Width;
        var normY = (clampedY - clientArea.Top) / clientArea.Height;

        var worldX = canvasBounds.Left + canvasBounds.Width * normX;
        var worldY = canvasBounds.Top + canvasBounds.Height * normY;

        return new Vector(worldX, worldY);
    }

    public static Vector CalculateOriginFromMinimapPoint(
        Vector minimapPoint,
        Rect canvasBounds,
        Rect clientArea,
        Vector viewportWorldSize)
    {
        if (canvasBounds == Rect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 ||
            clientArea.Width <= 0 || clientArea.Height <= 0)
        {
            return canvasBounds == Rect.Empty ? Vector.Zero : canvasBounds.Center;
        }

        var targetWorld = MinimapToWorld(minimapPoint, canvasBounds, clientArea);

        var restrictedBounds = canvasBounds;
        if (restrictedBounds.Width > viewportWorldSize.X)
        {
            restrictedBounds.Inflate(-viewportWorldSize.X / 2f, 0);
        }
        else
        {
            restrictedBounds.X += restrictedBounds.Width / 2f;
            restrictedBounds.Width = 0;
        }

        if (restrictedBounds.Height > viewportWorldSize.Y)
        {
            restrictedBounds.Inflate(0, -viewportWorldSize.Y / 2f);
        }
        else
        {
            restrictedBounds.Y += restrictedBounds.Height / 2f;
            restrictedBounds.Height = 0;
        }

        return restrictedBounds.Clamp(targetWorld);
    }

    public static Vector CalculateOriginDelta(Vector minimapDelta, Rect canvasBounds, Rect clientArea)
    {
        if (canvasBounds == Rect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 ||
            clientArea.Width <= 0 || clientArea.Height <= 0)
        {
            return Vector.Zero;
        }

        var scaleX = canvasBounds.Width / clientArea.Width;
        var scaleY = canvasBounds.Height / clientArea.Height;

        return new Vector(minimapDelta.X * scaleX, minimapDelta.Y * scaleY);
    }
}
