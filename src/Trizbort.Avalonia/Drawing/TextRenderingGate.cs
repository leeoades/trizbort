using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.Drawing;

/// <summary>Honours the Toggle Text debug switch (DebugDisableTextRendering) in the map drawing adapters.</summary>
internal static class TextRenderingGate
{
    public static bool IsEnabled => !ApplicationSettingsController.AppSettings.DebugDisableTextRendering;
}
