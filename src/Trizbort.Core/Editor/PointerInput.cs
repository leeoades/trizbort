using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>Pointer buttons as seen by Core.</summary>
public enum PointerButton
{
  None,
  Left,
  Middle,
  Right
}

/// <summary>
///   A pointer event in canvas client pixels. <see cref="Button" /> is the button that was pressed or released
///   (<see cref="PointerButton.None" /> for moves).
/// </summary>
public readonly record struct PointerInput(PointerButton Button, ModifierKeys Modifiers, Vector Client, int ClickCount);

/// <summary>
///   A mouse wheel or trackpad scroll event; positive <see cref="DeltaY" /> is "away from the user" (zoom in for a
///   wheel). <see cref="DeltaX" /> is the horizontal scroll (trackpads only) and defaults to 0.
/// </summary>
public readonly record struct WheelInput(float DeltaY, ModifierKeys Modifiers, Vector Client, float DeltaX = 0);
