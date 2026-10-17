using System;

namespace Trizbort.Domain.Enums;

[Flags]
public enum ModifierKeys
{
  None = 0,
  Shift = 1,
  Control = 2,
  Alt = 4
}
