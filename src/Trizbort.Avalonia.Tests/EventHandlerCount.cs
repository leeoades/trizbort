using System.Reflection;

namespace Trizbort.Avalonia.Tests;

internal static class EventHandlerCount
{
    public static int For(object instance, string eventName)
    {
        var type = instance.GetType();
        var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public);
        if (field?.GetValue(instance) is not Delegate handler)
        {
            return 0;
        }

        return handler.GetInvocationList().Length;
    }
}
