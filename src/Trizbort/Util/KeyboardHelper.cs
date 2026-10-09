using System.Runtime.InteropServices;

namespace Trizbort.Util {
  public static class KeyboardHelper {
    private const byte VkScrollLock = 0x91;
    const int VkCapsLock = 0x14;
    const int VkNumLock = 0x90;
    private const uint KeyEventKeyUp = 0x2;

    
    [DllImport("user32.dll", EntryPoint="keybd_event", SetLastError=true)]
    static extern void KeybdEvent(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("user32.dll", EntryPoint = "GetKeyState", SetLastError = true)]
    static extern short GetKeyState(uint nVirtKey);

    public static void SetScrollLockKey(bool newState)
    {
      bool scrollLockSet = GetKeyState(VkScrollLock) != 0;
      if (scrollLockSet != newState)
      {
        KeybdEvent(VkScrollLock, 0, 0, 0);
        KeybdEvent(VkScrollLock, 0, KeyEventKeyUp, 0);
      }
    }
    
    public static void SetCapsLockKey(bool newState) {
      bool capsLockSet = GetKeyState(VkCapsLock) != 0;
      if (capsLockSet != newState)
      {
        KeybdEvent(VkCapsLock, 0, 0, 0);
        KeybdEvent(VkCapsLock, 0, KeyEventKeyUp, 0);
      }
    }    
    
    public static void SetNumLockKey(bool newState) {
      bool numLockSet = GetKeyState(VkNumLock) != 0;
      if (numLockSet != newState)
      {
        KeybdEvent(VkNumLock, 0, 0, 0);
        KeybdEvent(VkNumLock, 0, KeyEventKeyUp, 0);
      }
    }
  }
}